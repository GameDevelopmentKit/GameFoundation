using System;
using UnityEngine;
using Com.ForbiddenByte.OSA.Core;
using frame8.Logic.Misc.Other.Extensions;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Tuple
{
    public abstract class TupleAdapter<TParams, TTupleValueViewsHolder> : OSA<TParams, TTupleValueViewsHolder>, ITupleAdapter
        where TParams : TupleParams, new()
        where TTupleValueViewsHolder : TupleValueViewsHolder, new()
    {
        public event Action<TupleValueViewsHolder>         ValueClicked;
        public event Action<TupleValueViewsHolder, object> ValueChangedFromInput;

        public TupleParams              TupleParameters => this._Params;
        public ITupleAdapterSizeHandler SizeHandler     { get; set; }

        public RectTransform RTransform
        {
            get
            {
                if (this._RectTransform == null) this._RectTransform = this.transform as RectTransform;

                return this._RectTransform;
            }
        }

        // Can be null (for example, when data is not available and will be provided later)
        protected ITuple        _CurrentTuple;
        protected ITableColumns _ColumnsProvider;
        private   RectTransform _RectTransform;
        private   float         _MyPrevKnownTransvSize;

        public void ResetWithTuple(ITuple tuple, ITableColumns columnsProvider)
        {
            if (!this.IsInitialized) this.Init();

            this._CurrentTuple    = tuple;
            this._ColumnsProvider = columnsProvider;
            var columnsCount = this._ColumnsProvider.ColumnsCount;

            if (this.GetItemsCount() == columnsCount)
            {
                // Save massive amounts of performance by just updating the existing views holders rather than resetting the view.
                // Same count means existing items don't need to be enabled/disabled/destroyed, because their position won't change
                var thisAsITupleAdapter = this as ITupleAdapter;
                for (var i = 0; i < this.VisibleItemsCount; i++)
                {
                    var vh = this.GetItemViewsHolder(i);
                    thisAsITupleAdapter.ForceUpdateValueViewsHolder(vh);
                }

                // Force a ComputeVisibility pass, if needed
                //SetNormalizedPosition(GetNormalizedPosition());
            }
            else
                this.ResetItems(columnsCount);
        }

        /// <summary>
        /// Start was overridden so that Init is not called automatically (see base.Start()), because this is done manually in the first call of ResetWithTuple().
        /// <para>See <see cref="OSA{TParams, TItemViewsHolder}.Start"/></para>
        /// </summary>
        protected sealed override void Start()
        {
        }

        protected override void Update()
        {
            base.Update();

            if (!this.IsInitialized) return;

            this.CheckResizing();
        }

        protected override void OnInitialized()
        {
            this._MyPrevKnownTransvSize = this.GetMyCurrentTransversalSize();
            base.OnInitialized();
        }

        protected override void CollectItemsSizes(ItemCountChangeMode changeMode, int count, int indexIfInsertingOrRemoving, ItemsDescriptor itemsDesc)
        {
            base.CollectItemsSizes(changeMode, count, indexIfInsertingOrRemoving, itemsDesc);

            if (changeMode == ItemCountChangeMode.REMOVE || count == 0) return;

            int indexOfFirstItemThatWillChangeSize;
            if (changeMode == ItemCountChangeMode.RESET)
                indexOfFirstItemThatWillChangeSize = 0;
            else
                indexOfFirstItemThatWillChangeSize = indexIfInsertingOrRemoving;

            var end = indexOfFirstItemThatWillChangeSize + count;
            itemsDesc.BeginChangingItemsSizes(indexOfFirstItemThatWillChangeSize);
            for (var i = indexOfFirstItemThatWillChangeSize; i < end; i++)
            {
                var size             = this._ColumnsProvider.GetColumnState(i).CurrentSize;
                var useDefault       = size == -1;
                if (useDefault) size = this.Parameters.DefaultItemSize;

                itemsDesc[i] = size;
            }
            itemsDesc.EndChangingItemsSizes();
        }

        protected override TTupleValueViewsHolder CreateViewsHolder(int itemIndex)
        {
            var vh = new TTupleValueViewsHolder();
            vh.Init(this._Params.ItemPrefab, this._Params.Content, itemIndex);
            vh.SetClickListener(() => this.OnValueClicked(vh));
            vh.SetValueChangedFromInputListener(value => this.OnValueChangedFromInput(vh, value));

            // Fixing text that disappears because all layout elements and groups are disabled on the prefab when resize mode is none, for optimization purposes
            if (this._Params.ResizingMode == TableResizingMode.NONE) vh.TextComponent.RT.MatchParentSize(true);

            return vh;
        }

        protected override void UpdateViewsHolder(TTupleValueViewsHolder newOrRecycled)
        {
            object value;
            if (this._CurrentTuple == null) // data pending
                value = null;
            else
                value = this._CurrentTuple.GetValue(newOrRecycled.ItemIndex);
            newOrRecycled.UpdateViews(value, this._ColumnsProvider);
        }

        protected override void OnBeforeDestroyViewsHolder(TTupleValueViewsHolder vh, bool isActive)
        {
            vh.SetValueChangedFromInputListener(null);

            base.OnBeforeDestroyViewsHolder(vh, isActive);
        }

        protected override void OnBeforeRecycleOrDisableViewsHolder(TTupleValueViewsHolder inRecycleBinOrVisible, int newItemIndex)
        {
            base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);

            if (this._Params.ResizingMode == TableResizingMode.AUTO_FIT_TUPLE_CONTENT)
            {
                // Make sure items that will just become visible will be rebuilt shortly
                if (newItemIndex >= 0)
                    inRecycleBinOrVisible.HasPendingTransversalSizeChanges = true;
                else
                    inRecycleBinOrVisible.HasPendingTransversalSizeChanges = false;
            }
        }

        protected virtual void OnValueClicked(TTupleValueViewsHolder vh)
        {
            if (this.ValueClicked != null) this.ValueClicked(vh);
        }

        protected virtual void OnValueChangedFromInput(TTupleValueViewsHolder vh, object newValue)
        {
            if (this.ValueChangedFromInput != null) this.ValueChangedFromInput(vh, newValue);
        }

        void ITupleAdapter.ForceUpdateValueViewsHolderIfVisible(int withItemIndex)
        {
            var vh = this.GetItemViewsHolderIfVisible(withItemIndex);
            if (vh != null) this.UpdateViewsHolder(vh);
        }

        void ITupleAdapter.ForceUpdateValueViewsHolder(TupleValueViewsHolder vh)
        {
            this.UpdateViewsHolder(vh as TTupleValueViewsHolder);
        }

        void ITupleAdapter.OnWillBeRecycled(float newSize)
        {
            var transvPad      = (float)this._InternalState.layoutInfo.transversalPaddingStartPlusEnd;
            var autoFitEnabled = this._Params.ResizingMode == TableResizingMode.AUTO_FIT_TUPLE_CONTENT;
            var axis           = (RectTransform.Axis)(1 - this._InternalState.hor0_vert1);
            // When this entire tuple will be recycled, reset every vh, visible or in recycle cache
            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var vh = this.GetItemViewsHolder(i);
                this.OnBeforeRecycleOrDisableViewsHolder(vh, -1);
                if (autoFitEnabled)
                {
                    var valueItemSize = newSize - transvPad;
                    vh.root.SetSizeFromParentEdgeWithCurrentAnchors(this._Params.Content, this._InternalState.transvStartEdge, valueItemSize);
                }
            }
            if (autoFitEnabled)
            {
                for (var i = 0; i < this.RecyclableItemsCount; i++)
                {
                    var vh = this._RecyclableItems[i];
                    if (autoFitEnabled)
                    {
                        var valueItemSize = newSize - transvPad;
                        // SetSizeWithCurrentAnchors is more efficient when positioning is not important
                        vh.root.SetSizeWithCurrentAnchors(axis, valueItemSize);
                    }
                }
                for (var i = 0; i < this.BufferedRecyclableItemsCount; i++)
                {
                    var vh = this._BufferredRecyclableItems[i];
                    if (autoFitEnabled)
                    {
                        var valueItemSize = newSize - transvPad;
                        // SetSizeWithCurrentAnchors is more efficient when positioning is not important
                        vh.root.SetSizeWithCurrentAnchors(axis, valueItemSize);
                    }
                }
            }
        }

        private float GetMyCurrentTransversalSize()
        {
            return this._Params.ScrollViewRT.rect.size[1 - this._InternalState.hor0_vert1];
        }

        // When a children's size exceeds this adapter's size or all 
        // children become smaller than this adapter (meaning the adapter should be shrunk by the parent)
        private void CheckResizing()
        {
            var myTransvSize                           = this.GetMyCurrentTransversalSize();
            var biggestSize                            = myTransvSize;
            var biggestItemTransvSizePlusTransvPadding = 0f;
            var resizeNeeded                           = false;

            if (myTransvSize != this._MyPrevKnownTransvSize)
            {
                this._MyPrevKnownTransvSize = myTransvSize;
                resizeNeeded                = true;
            }

            // Check if any item has an even bigger size
            var indexOfBiggestItem        = -1;
            var transvPaddingStart        = (float)this._InternalState.layoutInfo.transversalPaddingContentStart;
            var transvPaddingStartPlusEnd = (float)this._InternalState.layoutInfo.transversalPaddingStartPlusEnd;
            var foundItemBiggerThanMe     = false;
            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var vh = this.GetItemViewsHolder(i);
                this.RebuildVHIfNeeded(vh);

                var transvSize                  = vh.root.rect.size[1 - this._InternalState.hor0_vert1];
                var transvSizePlusTransvPadding = transvSize + transvPaddingStartPlusEnd;
                if (transvSizePlusTransvPadding > biggestSize)
                {
                    biggestSize           = transvSizePlusTransvPadding;
                    foundItemBiggerThanMe = true;
                    indexOfBiggestItem    = i;
                }
                if (transvSizePlusTransvPadding > biggestItemTransvSizePlusTransvPadding) biggestItemTransvSizePlusTransvPadding = transvSizePlusTransvPadding;
            }

            var vhsSmallerThanBiggestSizeMinusPadding = new List<TTupleValueViewsHolder>();
            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var vh                          = this.GetItemViewsHolder(i);
                var transvSize                  = vh.root.rect.size[1 - this._InternalState.hor0_vert1];
                var transvSizePlusTransvPadding = transvSize + transvPaddingStartPlusEnd;

                if (transvSizePlusTransvPadding < biggestSize)
                    //Debug.Log(i + ", " + (biggestSize - transvSizePlusTransvPadding) + ", " + transvPaddingStartPlusEnd);
                    vhsSmallerThanBiggestSizeMinusPadding.Add(vh);
                //vhsSmallerThanMeSizes.Add(transvSize);
            }

            if (foundItemBiggerThanMe) resizeNeeded = true;

            var sizeToSet = biggestSize;
            if (!resizeNeeded)
                // All items are smaller and also this adapter's size didn't change => consider shrinking
                if (biggestItemTransvSizePlusTransvPadding > 0f)
                    // Only if it's a significant drop in size
                    if (myTransvSize - biggestItemTransvSizePlusTransvPadding > 1f)
                    {
                        resizeNeeded = true;
                        sizeToSet    = biggestItemTransvSizePlusTransvPadding;
                    }

            if (this._Params.ResizingMode == TableResizingMode.AUTO_FIT_TUPLE_CONTENT)
            {
                var itemsSizeToSet = sizeToSet - transvPaddingStartPlusEnd;
                // Resize smaller items to fill the empty space
                for (var i = 0; i < vhsSmallerThanBiggestSizeMinusPadding.Count; i++)
                {
                    // The biggest item is already sized correctly, is indexOfBiggestItem is not -1
                    if (i == indexOfBiggestItem) continue;

                    var vh = vhsSmallerThanBiggestSizeMinusPadding[i];
                    //Debug.Log(i + ": " + indexOfBiggestItem + ", " + itemsSizeToSet + ", vh " + vh.root.rect.height);
                    //_Params.SetPaddingTransvEndToAchieveTansvSizeFor(vh.root, vh.LayoutGroup, itemsSizeToSet);
                    vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(this._InternalState.layoutInfo.transvStartEdge, transvPaddingStart, itemsSizeToSet);
                }
            }

            if (resizeNeeded)
                //Debug.Log(resizeNeeded);
                //if (indexOfBiggestItem != -1)
                //	Debug.Log(", 1 " + biggestItemTransvSizePlusTransvPadding + ", b " + myTransvSize + ", c " + biggestSize + ", d " + indexOfBiggestItem, gameObject);
                if (this.SizeHandler != null)
                    this.SizeHandler.RequestChangeTransversalSize(this, sizeToSet);
        }

        private void RebuildVHIfNeeded(TTupleValueViewsHolder vh)
        {
            if (vh.HasPendingTransversalSizeChanges)
            {
                if (this._Params.ResizingMode == TableResizingMode.AUTO_FIT_TUPLE_CONTENT)
                    // Only rebuild strings
                    if (this._ColumnsProvider.GetColumnState(vh.ItemIndex).Info.ValueType == TableValueType.STRING)
                        if (vh.CSF)
                            this.ForceRebuildViewsHolder(vh);
                vh.HasPendingTransversalSizeChanges = false;
            }
        }
    }

    public interface ITupleAdapterSizeHandler
    {
        void RequestChangeTransversalSize(ITupleAdapter adapter, double size);
    }
}