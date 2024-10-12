using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Com.ForbiddenByte.OSA.Core
{
    /// Comments format: value if vertical scrolling/value if horizontal scrolling
    public class ItemsDescriptor
    {
        public int itemsCount;

        //public double cumulatedSizesOfAllItemsPlusSpacing;
        public int realIndexOfFirstItemInView;

        // Heuristic used to prevent destroying too much objects.
        // It's reset back to 0 when the NotifyScrollViewSizeChanged is called
        public int maxVisibleItemsSeenSinceLastScrollViewSizeChange = 0;

        // Heuristic similar to the above one. The bigger this is, the more items will be held in the recycle bin, leading to fewer GC calls in the long run.
        // It's reset back to 0 when the NotifyScrollViewSizeChanged is called
        public int destroyedItemsSinceLastScrollViewSizeChange = 0;

        //public double DefaultSizeAsDouble { get { return _DefaultSize; } }
        public double CumulatedSizeOfAllItems => this.itemsCount == 0 ? 0d : this.GetItemSizeCumulative(this.itemsCount - 1, false);

        public double this[int itemIndexInView]
        {
            get
            {
                // Make sure this does the same thing as GetItemSizeOrDefault(), whenever it's modified
                SizeInfo val;
                if (this._SizeInfos.TryGetValue(itemIndexInView, out val)) return val.size;

                return this._DefaultSize;
            }
            set
            {
                if (this._ChangingItemsSizesInProgress)
                {
                    if (itemIndexInView != this._IndexInViewOfLastItemThatChangedSizeDuringSizesChange + 1)
                        throw new OSAException(
                            "itemIndexInView=" + itemIndexInView + ", while the expected one is " + (this._IndexInViewOfLastItemThatChangedSizeDuringSizesChange + 1) + ". Sizes can only be changed for items one by one, one after another(e.g. 3,4,5,6,7..), starting with the one passed to BeginChangingItemsSizes(int)!");

                    //if (this[itemIndexInView] == value)
                    //	return;

                    //if (_IndexInViewOfLastItemThatChangedSize == -1 // the first size being set => add a new entry
                    //		|| itemIndexInView > _IndexInViewOfLastItemThatChangedSize + 1) // the current index skips some intermediary indices => analogous
                    //	_RangesOfIndicesInViewOfItemsWhichChangedSize.Add(itemIndexInView);
                    //else // the current idx is immediately after
                    //{
                    //	if (itemIndexInView < _IndexInViewOfLastItemThatChangedSize + 1)
                    //		throw new OSAException("Can only set sizes from smaller indices to bigger indices (3, 5, 8, 10.. not 5, 3, 6, 7, 1, 28)");

                    //	_RangesOfIndicesInViewOfItemsWhichChangedSize[_RangesOfIndicesInViewOfItemsWhichChangedSize.Count - 1] = itemIndexInView;
                    //}
                    //_IndexInViewOfLastItemThatChangedSize = itemIndexInView;
                    this.BinaryAddKeyToSortedListIfDoesntExist(itemIndexInView);
                    this._CumulatedSizesUntilNowDuringSizesChange += value;
                    //_Sizes[itemIndexInView] = value;
                    //_SizesCumulative[itemIndexInView] = _CumulatedSizesUntilNowDuringSizesChange;

                    SizeInfo sizeInfo;
                    if (!this._SizeInfos.TryGetValue(itemIndexInView, out sizeInfo)) this._SizeInfos.Add(itemIndexInView, sizeInfo = new());

                    sizeInfo.size                                               = value;
                    sizeInfo.cumulativeSize                                     = this._CumulatedSizesUntilNowDuringSizesChange;
                    this._IndexInViewOfLastItemThatChangedSizeDuringSizesChange = itemIndexInView;
                }
                else
                {
                    throw new OSAException("Call BeginChangingItemsSizes() before");
                }
            }
        }

        // Important: if an item's index it's not here, it's assumed that its size is the default one, NOT that it's unknown
        /// <summary>indices in view of items of non-default sizes</summary>
        private List<int> _Keys = new();

        //Dictionary<int, double> _Sizes = new Dictionary<int, double>(); // heights/widths
        //Dictionary<int, double> _SizesCumulative = new Dictionary<int, double>(); // heights/widths
        private Dictionary<int, SizeInfo> _SizeInfos = new(); // heights/widths
        private double                    _DefaultSize;
        private bool                      _ChangingItemsSizesInProgress;
        private int                       _IndexInViewOfFirstItemThatChangesSizeDuringSizesChange;
        private int                       _IndexInViewOfLastItemThatChangedSizeDuringSizesChange = -1;

        private double _CumulatedSizesUntilNowDuringSizesChange;
        //double _AverageSize;
        //bool _IgnoreItemsWithDefaultSizesWhenCalculatingAVGSize; 

        public ItemsDescriptor(double defaultSize) //, bool ignoreItemsWithDefaultSizesWhenCalculatingAVGSize)
        {
            //_IgnoreItemsWithDefaultSizesWhenCalculatingAVGSize = ignoreItemsWithDefaultSizesWhenCalculatingAVGSize;
            if (defaultSize <= 0) throw new OSAException("ItemsDescriptor can't be initialized with a 0 or negative itemDefaultSize: " + defaultSize);
            this.ReinitializeSizes(ItemCountChangeMode.RESET, 0, -1, defaultSize);
            this.ReinitializeRealIndexOfFirstItemInView(0, ItemCountChangeMode.RESET, 0, -1, -1, false);
        }

        public void ReinitializeRealIndexOfFirstItemInView(
            int                 oldCount,
            ItemCountChangeMode changeMode,
            int                 count,
            int                 indexIfInsertingOrRemoving,
            int                 itemIndexOfFirstVHIfInsertingOrRemoving,
            bool                canLoop
        )
        {
            var newCount = this.itemsCount;
            if (newCount == 0)
            {
                this.realIndexOfFirstItemInView = -1;
                return;
            }

            if (changeMode == ItemCountChangeMode.RESET)
            {
                this.realIndexOfFirstItemInView = 0;
                return;
            }

            if (canLoop) throw new OSAException("At the moment, only ItemCountChangeMode.RESET is supported when looping");

            this.realIndexOfFirstItemInView = 0;

            //int oldRealIndexOfFirstItemInView = realIndexOfFirstItemInView;
            //realIndexOfFirstItemInView = 0;
            //if (canLoop && oldRealIndexOfFirstItemInView >= 0)
            //{
            //	if (changeMode == ItemCountChangeMode.REMOVE)
            //	{
            //		int itemsAfterRealIndexPrev = oldCount - oldRealIndexOfFirstItemInView;
            //		if (itemsAfterRealIndexPrev < count)
            //			throw new OSAException("Looping and itemsAfterRealIndexPrev < removeCount, " + itemsAfterRealIndexPrev + "<" + count);
            //	}

            //	if (itemIndexOfFirstVHIfInsertingOrRemoving < 0)
            //		return;

            //	int change = (int)changeMode * count;
            //	realIndexOfFirstItemInView = oldRealIndexOfFirstItemInView;
            //	if (itemIndexOfFirstVHIfInsertingOrRemoving > indexIfInsertingOrRemoving 
            //		|| oldRealIndexOfFirstItemInView >= indexIfInsertingOrRemoving && itemIndexOfFirstVHIfInsertingOrRemoving != 0)
            //	{
            //		realIndexOfFirstItemInView += change;
            //	}

            //	//if (indexIfInsertingOrRemoving < oldRealIndexOfFirstItemInView)
            //	//{
            //	//	realIndexOfFirstItemInView += change;

            //	//}
            //	//else if (indexIfInsertingOrRemoving == oldRealIndexOfFirstItemInView)
            //	//{
            //	//	if (changeMode == ItemCountChangeMode.REMOVE)
            //	//	{
            //	//		int itemsAfterRealIndexPrev = oldCount - oldRealIndexOfFirstItemInView;
            //	//		if (itemsAfterRealIndexPrev < count)
            //	//			throw new OSAException("itemsAfterRealIndexPrev < count, " + itemsAfterRealIndexPrev + "<" + count);

            //	//		realIndexOfFirstItemInView += change;
            //	//	}
            //	//	else
            //	//		realIndexOfFirstItemInView += change;
            //	//}
            //	//else
            //	//{
            //	//	// Bugfix: in case of looping, items are not shifted towards end anymore, only their ItemIndex is increased,
            //	//	// but because no items are added before the viewport in this case, realIndexOfFirstItemInView should 
            //	//	// also increase to keep the ItemIndex distance from the first VH the same
            //	//	// Similar case if removing
            //	//	realIndexOfFirstItemInView += change;
            //	//}
            //}
        }

        public void ReinitializeSizes(
            ItemCountChangeMode changeMode,
            int                 count,
            int                 indexIfInsertingOrRemoving,
            double?             newDefaultSize = null
        )
        {
            if (newDefaultSize != null)
            {
                if (newDefaultSize != this._DefaultSize)
                {
                    if (changeMode != ItemCountChangeMode.RESET) throw new OSAException("Cannot preserve old sizes if the newDefaultItemSize is different!");

                    this._DefaultSize = newDefaultSize.Value;
                    //_AverageSize = _DefaultSize = newDefaultSize.Value;
                }
                this.AssureValidDefaultItemSizeOrThrow();
            }

            int newCount;
            if (changeMode == ItemCountChangeMode.RESET)
            {
                newCount = count;
                if (newCount < 0) throw new ArgumentOutOfRangeException("OSA.ItemsDescriptor.ReinitializeSizes: Can't have negative count: " + newCount);

                this.ClearAllKeysAndSizes();
            }
            else
            {
                if (indexIfInsertingOrRemoving < 0) throw new ArgumentOutOfRangeException("indexIfInsertingOrRemoving", indexIfInsertingOrRemoving, "Should be positive or 0");

                if (count < 0) throw new ArgumentOutOfRangeException("count", count, "Cannot be negative!");

                if (changeMode == ItemCountChangeMode.REMOVE)
                {
                    if (indexIfInsertingOrRemoving + count > this.itemsCount) throw new ArgumentOutOfRangeException("RemoveItems: index + count", indexIfInsertingOrRemoving + count, "Should be positive and less than- or or equal to itemsCount=" + this.itemsCount);
                }
                else
                {
                    if (indexIfInsertingOrRemoving > this.itemsCount) throw new ArgumentOutOfRangeException("InsertItems: indexIfInsertingOrRemoving", indexIfInsertingOrRemoving, "Should be positive and less than- or equal to itemsCount=" + this.itemsCount);
                }

                var change = (int)changeMode * count;
                newCount = this.itemsCount + change;
                if (newCount < 0) throw new ArgumentOutOfRangeException("newCount", "OSA.ItemsDescriptor.ReinitializeSizes: Can't have negative count " + newCount);

                if (newCount == 0)
                {
                    this.ClearAllKeysAndSizes();
                }
                else
                {
                    if (this.itemsCount > 0)
                        if (indexIfInsertingOrRemoving < this.itemsCount) // if it's the same, it means we're adding at the end, which doesn't require keys shifting
                            this.ShiftSizesKeysAfterInsertOrRemove(this.GetItemViewIndexFromRealIndexChecked(indexIfInsertingOrRemoving), change);
                }
            }
            this.itemsCount = newCount;
        }

        /// <summary>
        /// List of indexInView values for items which you've set a custom size.
        /// Use them to access an item's custom size via <see cref="this[int]"/> or as keys for the dictionary returned by <see cref="GetSizeInfosOfNonDefaultItems"/>
        /// </summary>
        public ReadOnlyCollection<int> GetIndicesInViewOfNonDefaultItems()
        {
            return this._Keys.AsReadOnly();
        }

        /// <summary>
        /// Dictionary that maps item indices to their sizes. Only contains indices of items for which you've set a custom size manually.
        /// Don't modify this collection!
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, SizeInfo> GetSizeInfosOfNonDefaultItems()
        {
            return this._SizeInfos;
        }

        private void AssureValidDefaultItemSizeOrThrow()
        {
            if (this._DefaultSize <= 0) throw new OSAException("ItemsDescriptor.AssureValidDefaultItemSizeOrThrow: Item prefab's size is zero or negative(" + this._DefaultSize + "). This is not allowed. Is your prefab badly resized at initialization?");
        }

        private void ClearAllKeysAndSizes()
        {
            //_Sizes.Clear();
            //_SizesCumulative.Clear();
            //_SizeInfos.Clear();
            //_Keys.Clear();

            // It's faster to just allocate new structures
            this._SizeInfos = new();
            this._Keys      = new();
        }

        private void BinaryAddKeyToSortedListIfDoesntExist(int key)
        {
            var indexOfKey = this._Keys.BinarySearch(key);
            if (indexOfKey < 0) // will be negative if it doesn't already exist
                this._Keys.Insert(~indexOfKey, key);
        }

        private void BinaryRemoveKeyFromSortedList(int key)
        {
            this._Keys.RemoveAt(this._Keys.BinarySearch(key));
        }

        //void Debug_ListSizesAndSizesCumulative()
        //{
        //	foreach (var k in _Keys)
        //		Debug.Log(k + ": " + _Sizes[k] + ", c=" + _SizesCumulative[k]);
        //}

        private void ShiftSizesKeysAfterInsertOrRemove(int startingKey, int amount)
        {
            //if (_Sizes.Count != _SizesCumulative.Count || _Sizes.Count != _Keys.Count)
            //	throw new OSAException("The sizes state was corrupted");

            if (this._SizeInfos.Count != this._Keys.Count) throw new OSAException("The sizes state was corrupted");

            //Debug.Log("Bef, startingKey= " + startingKey + ", amount="+ amount + ", oldCount=" + itemsCount);
            //Debug_ListSizesAndSizesCumulative();

            var indexOfStartingKeyOrFirstKeyAfter = this._Keys.BinarySearch(startingKey);
            if (indexOfStartingKeyOrFirstKeyAfter < 0) // doesn't exist => see if there's a key after
                indexOfStartingKeyOrFirstKeyAfter = ~indexOfStartingKeyOrFirstKeyAfter;

            long amountLong = amount;

            //var itemsKV = _Sizes.ToList();
            //int i = -1;
            //KeyValuePair<int, float> kv;
            var i = indexOfStartingKeyOrFirstKeyAfter;
            //while (++i < itemsKV.Count && itemsKV[i].Key < startingKey) ; // skip until at- or after the startingKey

            var contentSizeChange = 0d; // the shifting amount

            //Debug.Log("Adjust after:");

            // Negative amount means the items are being removed => remove existing items from startingKey to <startingKey+amount-1>
            int key;
            //double size;
            //double sizeCumu;
            SizeInfo sizeInfo, newSizeInfo;
            if (amount < 0)
            {
                long countBefore            = this._Keys.Count;
                var  amountAbs              = -amountLong;
                var  lastItemIndexExclusive = startingKey + amountAbs;
                //--i;
                //while (++i < _Keys.Count && (key=_Keys[i]) < lastItemIndexExclusive)
                while (i < this._Keys.Count && (key = this._Keys[i]) < lastItemIndexExclusive)
                {
                    sizeInfo = this._SizeInfos[key];
                    //contentSizeChange -= _Sizes[key];
                    contentSizeChange -= sizeInfo.size;

                    //_Sizes.Remove(key);
                    //_SizesCumulative.Remove(key);
                    this._SizeInfos.Remove(key);
                    this._Keys.RemoveAt(i);
                }
                var itemsRemoved = countBefore - this._Keys.Count;
                contentSizeChange -= (amountAbs - itemsRemoved) * this._DefaultSize;

                // Shift the indices following after to the left, starting with the left-most index (to prevent overwriting existing keys). 
                // i = index of the first stored key after the last removed one
                for (; i < this._Keys.Count; ++i)
                {
                    key = this._Keys[i];
                    //size = _Sizes[key];
                    //sizeCumu = _SizesCumulative[key];
                    sizeInfo = this._SizeInfos[key];

                    //_Sizes.Remove(key);
                    //_SizesCumulative.Remove(key);
                    this._SizeInfos.Remove(key);

                    var newKey = key + amount;
                    if (newKey < 0) // the item will be removed from the head of the list => don't add it 
                    {
                        //Debug.Log("here");
                        this._Keys.RemoveAt(i);
                        continue;
                    }
                    this._Keys[i] = newKey; // change the key to the new value

                    //_Sizes[newKey] = size;
                    //_SizesCumulative[newKey] = sizeCumu + contentSizeChange;

                    // Update existing or create new
                    if (!this._SizeInfos.TryGetValue(newKey, out newSizeInfo)) this._SizeInfos.Add(newKey, newSizeInfo = new());
                    newSizeInfo.size           = sizeInfo.size;
                    newSizeInfo.cumulativeSize = sizeInfo.cumulativeSize + contentSizeChange;
                }
            }
            else
            {
                contentSizeChange = amount * this._DefaultSize; // new items are supposed to have default size

                // Shift the indices following after to the right, but start from the right-most (to prevent overwriting existing keys). 
                // i = index of startingKey or (if starting key is not stored) the first stored key after it
                var indexOfLeftMostKeyToBeShifted = i;
                for (i = this._Keys.Count - 1; i >= indexOfLeftMostKeyToBeShifted; --i)
                {
                    key = this._Keys[i];
                    //size = _Sizes[key];
                    //sizeCumu = _SizesCumulative[key];
                    sizeInfo = this._SizeInfos[key];

                    //_Sizes.Remove(key);
                    //_SizesCumulative.Remove(key);
                    this._SizeInfos.Remove(key);

                    var newKey = key + amount;
                    this._Keys[i] = newKey; // change the key to the new value

                    //_Sizes[newKey] = size;
                    //_SizesCumulative[newKey] = sizeCumu + contentSizeChange;

                    // Update existing or create new
                    if (!this._SizeInfos.TryGetValue(newKey, out newSizeInfo)) this._SizeInfos.Add(newKey, newSizeInfo = new());
                    newSizeInfo.size           = sizeInfo.size;
                    newSizeInfo.cumulativeSize = sizeInfo.cumulativeSize + contentSizeChange;
                }
            }

            //Debug.Log("Aft");
            //Debug_ListSizesAndSizesCumulative();
        }

        /// <summary>
        /// UPDATE: Only consecutive indices are allowed now! 
        /// </summary>
        public void BeginChangingItemsSizes(int indexInViewOfFirstItemThatWillChangeSize)
        {
            if (this._ChangingItemsSizesInProgress) throw new OSAException("Call EndChangingItemsSizes() when done doing it");

            this._ChangingItemsSizesInProgress                           = true;
            this._IndexInViewOfFirstItemThatChangesSizeDuringSizesChange = indexInViewOfFirstItemThatWillChangeSize;
            this._IndexInViewOfLastItemThatChangedSizeDuringSizesChange  = this._IndexInViewOfFirstItemThatChangesSizeDuringSizesChange - 1;
            this._CumulatedSizesUntilNowDuringSizesChange                = this._IndexInViewOfFirstItemThatChangesSizeDuringSizesChange == 0 ? 0d : this.GetItemSizeCumulative(this._IndexInViewOfFirstItemThatChangesSizeDuringSizesChange - 1, false);
            //_IndexInViewOfLastItemThatChangedSize = -1;
            //_RangesOfIndicesInViewOfItemsWhichChangedSize.Clear();
        }

        public void EndChangingItemsSizes()
        {
            this._ChangingItemsSizesInProgress = false;

            if (this._IndexInViewOfLastItemThatChangedSizeDuringSizesChange == this._IndexInViewOfFirstItemThatChangesSizeDuringSizesChange - 1) return; // this[int] wasn't assigned between BeginChangingItemsSizes() and EndChangingItemsSizes(), i.e. nothing has changed

            var indexOfLastKeyThatChanged = this._Keys.BinarySearch(this._IndexInViewOfLastItemThatChangedSizeDuringSizesChange);
            if (indexOfLastKeyThatChanged < 0) // doesn't exist
                throw new OSAException("The sizes state was corrupted");

            var      cumulatedSizesUntilNow = this._CumulatedSizesUntilNowDuringSizesChange;
            var      prevKey                = this._IndexInViewOfLastItemThatChangedSizeDuringSizesChange;
            int      curKey;
            SizeInfo sizeInfo;
            for (var i = indexOfLastKeyThatChanged + 1; i < this._Keys.Count; ++i)
            {
                curKey = this._Keys[i];

                //cumulatedSizesUntilNow += (curKey - prevKey - 1) * _DefaultSize + _Sizes[curKey];
                //_SizesCumulative[curKey] = cumulatedSizesUntilNow;
                sizeInfo                =  this._SizeInfos[curKey];
                cumulatedSizesUntilNow  += (curKey - prevKey - 1) * this._DefaultSize + sizeInfo.size;
                sizeInfo.cumulativeSize =  cumulatedSizesUntilNow;
                prevKey                 =  curKey;
            }
        }

        public int GetItemRealIndexFromViewIndex(int indexInView)
        {
            return (int)(((long)this.realIndexOfFirstItemInView + indexInView) % this.itemsCount);
        }

        private int GetItemViewIndexFromRealIndexWithoutChecks(int realIndex)
        {
            return (int)(((long)realIndex - this.realIndexOfFirstItemInView + this.itemsCount) % this.itemsCount);
        }

        public int GetItemViewIndexFromRealIndexChecked(int realIndex)
        {
            if (realIndex < 0) throw new ArgumentOutOfRangeException("realIndex", realIndex, "OSA.GetItemViewIndexFromRealIndexNotAllowingTotalCount: argument should be >=0");
            if (realIndex >= this.itemsCount) throw new ArgumentOutOfRangeException("realIndex", realIndex, "OSA.GetItemViewIndexFromRealIndexNotAllowingTotalCount: argument should be < totalCount(=" + this.itemsCount + ")");

            return this.GetItemViewIndexFromRealIndexWithoutChecks(realIndex);
        }

        // Note to OSA devs: Make sure this does the same thing as "this[int itemIndexInView]" property, whenever it's modified
        public double GetItemSizeOrDefault(int itemIndexInView)
        {
            //double val;
            //if (_Sizes.TryGetValue(itemIndexInView, out val))
            //	return val;
            SizeInfo sizeInfo;
            if (this._SizeInfos.TryGetValue(itemIndexInView, out sizeInfo)) return sizeInfo.size;

            return this._DefaultSize;
        }

        public double GetItemSizeCumulative(int itemIndexInView, bool allowInferringFromNeighborAfter = true)
        {
            // TODO remove allowInferringFromNeighborAfter, as it's useless

            // No key in the dictionary. This also means that there's no size in 
            // _Sizes either (assuming the things are done correctly - when a size is set, the cumulative size is also set)
            if (this._Keys.Count > 0)
            {
                //double result;
                //if (_SizesCumulative.TryGetValue(itemIndexInView, out result))
                //	return result;
                SizeInfo sizeInfo;
                if (this._SizeInfos.TryGetValue(itemIndexInView, out sizeInfo)) return sizeInfo.cumulativeSize;

                var indexOfNextKey = this._Keys.BinarySearch(itemIndexInView);
                if (indexOfNextKey >= 0) throw new OSAException("The sizes state was corrupted. key not in _SizesCumulative, but present in _Keys");

                indexOfNextKey = ~indexOfNextKey;
                var indexOfPrevKey = indexOfNextKey - 1;
                //int itemsCountDeltaLeft;

                // Case where there's a key after (bigger) AND "can use next neighbor to infer size"
                if (indexOfNextKey < this._Keys.Count && allowInferringFromNeighborAfter)
                {
                    var indexInViewOfNextItemWithKnownSize = this._Keys[indexOfNextKey];
                    var itemsCountDeltaRight               = indexInViewOfNextItemWithKnownSize - itemIndexInView;

                    // .. and: (size for none of prev items was set OR the next one is closer)  => searched item's cumulative size is 
                    // the current item's cumulative size minus <currentItemSize + numItemsBetween * defaultSize>
                    if (indexOfPrevKey < 0 || itemsCountDeltaRight < /*itemsCountDeltaLeft =*/itemIndexInView - this._Keys[indexOfPrevKey])
                    {
                        //return _SizesCumulative[indexInViewOfNextItemWithKnownSize] - (this[indexInViewOfNextItemWithKnownSize] + (itemsCountDeltaRight - 1) * _DefaultSize);
                        sizeInfo = this._SizeInfos[indexInViewOfNextItemWithKnownSize];
                        return sizeInfo.cumulativeSize - (this[indexInViewOfNextItemWithKnownSize] + (itemsCountDeltaRight - 1) * this._DefaultSize);
                    }
                }
                // Case where there's no key after or can't use it, but there may be some before

                if (indexOfPrevKey >= 0)
                {
                    var indexInViewOfPrevItemWithKnownSize = this._Keys[indexOfPrevKey];
                    // Found an item before it that provides a starting point in calculating the searched cumulative size:
                    // It's biggestPrevCumulativeSize + itemsCountDelta * defaultSize; 3 possible reasons for this:
                    // a. inferring from next keys are not allowed (allowInferringFromNeighborAfter=false)
                    // b. there's no next key
                    // c. there's a prev key that's closer than the next key
                    // .. in all cases, => the prev item's data is generally more reliable
                    //return _SizesCumulative[indexInViewOfPrevItemWithKnownSize] + (itemIndexInView - indexInViewOfPrevItemWithKnownSize) * _DefaultSize;

                    sizeInfo = this._SizeInfos[indexInViewOfPrevItemWithKnownSize];
                    return sizeInfo.cumulativeSize + (itemIndexInView - indexInViewOfPrevItemWithKnownSize) * this._DefaultSize;
                }
            }

            // At this point, there are no keys stored OR the inferring can't or shouldn't be done using the next key => return based on the default size

            return (itemIndexInView + 1) * this._DefaultSize; // same as if there were no keys
        }

        public void RotateItemsSizesOnScrollViewLooped(int newValueOf_RealIndexOfFirstItemInView)
        {
            var oldValueOf_realIndexOfFirstItemInView = this.realIndexOfFirstItemInView;
            this.realIndexOfFirstItemInView = newValueOf_RealIndexOfFirstItemInView;

            var rotateAmount = oldValueOf_realIndexOfFirstItemInView - this.realIndexOfFirstItemInView;
            var keysCount    = this._Keys.Count;
            if (rotateAmount == 0 && keysCount == 0) return;
            if (rotateAmount < 0) rotateAmount += this.itemsCount;

            var keysOld = this._Keys;
            //var sizesOld = _Sizes;
            var sizesOld = this._SizeInfos;
            this._Keys = new(keysCount);
            //_Sizes = new Dictionary<int, double>(keysCount);
            ////_SizesCumulative.Clear();
            //_SizesCumulative = new Dictionary<int, double>(keysCount);
            this._SizeInfos = new(keysCount);

            int oldKeyWithCurSize, newKeyWithCurSize;
            //double size;
            SizeInfo sizeInfo;
            for (var i = 0; i < keysCount; ++i)
            {
                oldKeyWithCurSize = keysOld[i];
                newKeyWithCurSize = (oldKeyWithCurSize + rotateAmount) % this.itemsCount;
                this.BinaryAddKeyToSortedListIfDoesntExist(newKeyWithCurSize);
                //size = sizesOld[oldKeyWithCurSize];
                sizeInfo = sizesOld[oldKeyWithCurSize];

                //_Sizes[newKeyWithCurSize] = size;
                SizeInfo newSizeInfo;
                if (!this._SizeInfos.TryGetValue(newKeyWithCurSize, out newSizeInfo)) this._SizeInfos.Add(newKeyWithCurSize, newSizeInfo = new());
                newSizeInfo.size = sizeInfo.size;

                // Left this commented so there will be no urge to merge the loop below with this one. 
                // Explanation: BinaryAddKeyToSortedListIfDoesntExist() which is called above, changes the keys, so the first <keysCount> in the loop below aren't always the same
                //numGapsSinceLastKey = newKeyWithCurSize - prevKey - 1;
                //cumulatedSizesOfAllItemsUntilNow += numGapsSinceLastKey * _DefaultSize;
                //cumulatedSizesOfAllItemsUntilNow += size;
                //_SizesCumulative[newKeyWithCurSize] = cumulatedSizesOfAllItemsUntilNow;

                //prevKey = newKeyWithCurSize;
            }

            var cumulatedSizesOfAllItemsUntilNow = 0d;
            var prevKey                          = -1;
            int numGapsSinceLastKey;
            for (var i = 0; i < keysCount; ++i)
            {
                newKeyWithCurSize = this._Keys[i];

                //size = _Sizes[newKeyWithCurSize];
                sizeInfo = this._SizeInfos[newKeyWithCurSize];

                numGapsSinceLastKey              =  newKeyWithCurSize - prevKey - 1;
                cumulatedSizesOfAllItemsUntilNow += numGapsSinceLastKey * this._DefaultSize;
                cumulatedSizesOfAllItemsUntilNow += sizeInfo.size;

                //_SizesCumulative[newKeyWithCurSize] = cumulatedSizesOfAllItemsUntilNow;
                sizeInfo.cumulativeSize = cumulatedSizesOfAllItemsUntilNow;

                prevKey = newKeyWithCurSize;
            }
        }

        //public class ItemSizeInfo
        //{
        //	public int itemIndex;
        //	public double size;

        //	public ItemSizeInfo(int itemIndex, double size)
        //	{
        //		this.itemIndex = itemIndex;
        //		this.size = size;
        //	}
        //}

        public class SizeInfo
        {
            public double size;
            public double cumulativeSize;
        }
    }
}