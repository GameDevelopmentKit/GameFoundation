using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Com.ForbiddenByte.OSA.DataHelpers
{
    /// <summary>
    /// <para>Very handy List implementation that delays object creation until it's accessed,</para>
    /// <para>although the underlying List is still allocated with all slots from the beginning, only that they have default values - default(T)</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LazyList<T> : IList
    {
        object IList.this[int key] { get => this[key]; set => this[key] = (T)value; }

        public T this[int key]
        {
            get
            {
                if (key >= this.Count) throw new ArgumentOutOfRangeException("key", key, "must be less than Count=" + this.Count);

                T res;
                if (!this.TryGet(key, out res))
                {
                    res = this._NewValueCreator(key);

                    // Update 19-Jul-2019: Now checking if it already exists, as the code executed in _NewValueCreator
                    // could already set the value via SetOrUpdateManuallyCreatedValues
                    //SetManuallyCreatedValueForFirstTime(key, res);
                    if (!this._BackingMap.ContainsKey(key)) this.SetManuallyCreatedValueForFirstTime(key, res);
                }

                return res;
            }
            set => this.SetOrUpdateManuallyCreatedValue(key, value);
        }

        //public int Count { get { return _BackingList.Count; } }
        public int Count { get; private set; }

        /// <summary>The number of items already created. In other words, the number of non-empty slots</summary>
        public int ExistingCount => this._Keys.Count;

        /// <summary>
        /// Returns an enumerable version of this list, which only contains the already-cached values
        /// </summary>
        public EnumerableLazyList AsEnumerableForExistingItems => new(this);

        #region IList & ICollection properties

        bool IList.        IsFixedSize    => false;
        bool IList.        IsReadOnly     => false;
        bool ICollection.  IsSynchronized => false;
        object ICollection.SyncRoot       => this;

        #endregion

        //public int InitializedCount { get; private set; }

        //public int VirtualCount
        //{
        //	get { return _VirtualCount; }
        //	private set
        //	{
        //		_VirtualCount = value;

        //		IndexOfFirstExistingValue = IndexOfLastExistingValue = -1;
        //	}
        //}
        //public int IndexOfFirstExistingValue { get; private set; }
        //public int IndexOfLastExistingValue { get; private set; }

        //public bool IsReadOnly { get { return false; } }

        //IDictionary<int, TValue> BackingDictionaryAsInterface { get { return (_BackingDictionary as IDictionary<int, TValue>); } }

        //List<T> _BackingList = new List<T>();
        private Dictionary<int, T> _BackingMap;
        private List<int>          _Keys = new(); // sorted

        private Func<int, T> _NewValueCreator;

        //int _VirtualCount;
        private IniniteNullList _InfiniteNullList = new();

        public LazyList(Func<int, T> newValueCreator, int initialCount)
        {
            initialCount          = initialCount > 0 ? initialCount : 0;
            this._NewValueCreator = newValueCreator;

            // Bugfix 19-Jul-2019: The dictionary will already be allocated in InitWithNewCount
            //_BackingMap = new Dictionary<int, T>(initialCount);

            this.InitWithNewCount(initialCount);
        }

        public void InitWithNewCount(int newCount)
        {
            // Update 19-Jul-2019: Clear not needed here, and it even slows down loading. We're allocating new lists below, anyway
            //Clear();
            var cap = Math.Min(newCount, 1024);
            this._BackingMap = new(cap);
            this._Keys       = new(cap);
            this.Count       = newCount;
        }

        public void Add(int count)
        {
            this.Count += count;
        }

        public void InsertAtStart(int count)
        {
            this.InsertWhenKeyStartIdxKnown(0, 0, count);
        }

        public void Insert(int index, int count)
        {
            var keyStartIdx                  = this._Keys.BinarySearch(index);
            if (keyStartIdx < 0) keyStartIdx = ~keyStartIdx; // the index of the first potentially-existing key to be shifted to the right
            this.InsertWhenKeyStartIdxKnown(keyStartIdx, index, count);
        }

        private void InsertWhenKeyStartIdxKnown(int keyStartIdx, int index, int count)
        {
            var keyStartIdxExcl = keyStartIdx - 1;

            int key, newKey;
            // Going from end to start, to not overwrite dictionary existing items when shifting values
            for (var i = this._Keys.Count - 1; i > keyStartIdxExcl; --i)
            {
                key    = this._Keys[i];
                newKey = key + count;

                this._Keys[i]            = newKey;
                this._BackingMap[newKey] = this._BackingMap[key];
                this._BackingMap.Remove(key);
            }

            this.Count += count;
        }

        public void Clear()
        {
            this._BackingMap.Clear();
            this._Keys.Clear();
            this.Count = 0;
        }

        [Obsolete("Use Remove(int index, int count) below")]
        public void RemoveAt(int index)
        {
            this.Remove(index, 1);
        }

        /// <summary>Returns the index, if found</summary>
        public int Remove(T value)
        {
            foreach (var kv in this._BackingMap)
            {
                if (kv.Value.Equals(value))
                {
                    this.Remove(kv.Key, 1);
                    return kv.Key;
                }
            }

            return -1;
        }

        public void Remove(int index, int count)
        {
            var lastKeyToRemoveExcl              = index + count;
            var keyStartIndex                    = this._Keys.BinarySearch(index);
            if (keyStartIndex < 0) keyStartIndex = ~keyStartIndex; // the index of the first potentially-existing key to be removed

            int key, newKey;
            // Remove the matching keys, if existing
            var i = keyStartIndex;
            while (i < this._Keys.Count)
            {
                key = this._Keys[i];

                if (key < lastKeyToRemoveExcl)
                {
                    this._Keys.RemoveAt(i);
                    this._BackingMap.Remove(key);
                }
                else
                {
                    break;
                }
            }

            // Decrement the following keys, if existing
            for (; i < this._Keys.Count; ++i)
            {
                key    = this._Keys[i];
                newKey = key - count;

                this._Keys[i]            = newKey;
                this._BackingMap[newKey] = this._BackingMap[key];
                this._BackingMap.Remove(key);
            }

            this.Count -= count;
        }

        private IEnumerator<T> GetEnumeratorForExistingItems()
        {
            for (var i = 0; i < this._Keys.Count; ++i)
            {
                var v = this._BackingMap[this._Keys[i]];

                // Update 22-Jul-2019: Null items are still valid, since they have an associated key
                //if (v != null)
                //	yield return v;
                yield return v;
            }
        }

        /// <summary>Returns true if the value is already cached</summary>
        public bool TryGet(int index, out T val)
        {
            return this._BackingMap.TryGetValue(index, out val);
        }

        /// <summary>Throws an exception if the index is not already cached</summary>
        public T GetUnchecked(int index)
        {
            return this._BackingMap[index];
        }

        public void GetIndicesOfClosestExistingItems(int index, out int prevIndex, out int nextIndex)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index", "can't be negative");

            if (index == int.MaxValue) throw new ArgumentOutOfRangeException("index", "can't be int.MaxValue");

            var existingKeyIndexOrPotentialIndex = this._Keys.BinarySearch(index);
            var middleKeyExists                  = existingKeyIndexOrPotentialIndex >= 0;
            if (!middleKeyExists)
            {
                existingKeyIndexOrPotentialIndex = ~existingKeyIndexOrPotentialIndex;
                if (existingKeyIndexOrPotentialIndex > 0) prevIndex = this._Keys[existingKeyIndexOrPotentialIndex - 1];
            }

            var prevKeyIndex = existingKeyIndexOrPotentialIndex - 1;
            int nextKeyIndex;
            if (existingKeyIndexOrPotentialIndex == 0) // no prev key exists
            {
                nextKeyIndex = existingKeyIndexOrPotentialIndex + 1;
            }
            else
            {
                // Next index, if exists, is after the current one
                if (middleKeyExists)
                    nextKeyIndex = existingKeyIndexOrPotentialIndex + 1;
                else
                    nextKeyIndex = existingKeyIndexOrPotentialIndex;
            }

            if (prevKeyIndex >= 0 && prevKeyIndex < this._Keys.Count)
                prevIndex = this._Keys[prevKeyIndex];
            else
                prevIndex = -1;

            if (nextKeyIndex >= 0 && nextKeyIndex < this._Keys.Count)
                nextIndex = this._Keys[nextKeyIndex];
            else
                nextIndex = -1;
        }

        public void SetOrUpdateManuallyCreatedValue(int index, T val)
        {
            if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException("index", index, "Must be >=0 and less than Count(" + this.Count + ")");

            if (this._BackingMap.ContainsKey(index))
                this._BackingMap[index] = val;
            else
                this.SetManuallyCreatedValueForFirstTime(index, val);
        }

        /// <summary>
        /// Removes the value cached at <paramref name="index"/>, meaning <see cref="_NewValueCreator"/> will be 
        /// invoked next time the value at this index will be requested.
        /// Example use case: If the values are resource-heavy, you may want to occasionally call this.
        /// Returns whether the value existed or not.
        /// </summary>
        public bool UncacheValue(int index)
        {
            if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException("index", index, "Must be >=0 and less than Count(" + this.Count + ")");

            if (!this._BackingMap.Remove(index)) return false;

            var keyIndex = this._Keys.BinarySearch(index);
            if (keyIndex < 0) throw new InvalidOperationException("Internal bug: Key index " + keyIndex + " for key " + index + ", but the value was found in the backing map. Count=" + this.Count);

            this._Keys.RemoveAt(keyIndex);

            return true;
        }

        /// <summary>
        /// Simply does the same thing as <see cref="SetOrUpdateManuallyCreatedValues(int, IList{T}, int, int)"/>, but sets null values instead
        /// <para>This is more efficient as it doesn't create an unnecessary intermediary list of null values</para>
        /// </summary>
        public void AllocateNullSlots(int startingIndex, int valuesReadCount)
        {
            this.SetOrUpdateManuallyCreatedValues(startingIndex, this._InfiniteNullList, 0, valuesReadCount);
        }

        /// <summary>
        /// <para><paramref name="startingIndex"/> is the index of the first item in THIS list</para>
        /// <para><paramref name="values"/> is the list from which to extract the new values</para>
        /// <para><paramref name="valsReadIndex"/> is the index from which to start extracting values from <paramref name="values"/></para>
        /// <para><paramref name="valsReadCount"/> is the number of values to extract from <paramref name="values"/></para>
        /// <para>Much more efficient than <see cref="SetOrUpdateManuallyCreatedValue(int, T)"/> when you want to insert multiple consecutive values</para>
        /// </summary>
        public void SetOrUpdateManuallyCreatedValues(int startingIndex, IList<T> values, int valsReadIndex, int valsReadCount)
        {
            var startingKey                                                = startingIndex;
            var existingIndexOfStartingKey                                 = this._Keys.BinarySearch(startingKey);
            if (existingIndexOfStartingKey < 0) existingIndexOfStartingKey = ~existingIndexOfStartingKey;

            for (var i = 0; i < valsReadCount; ++i)
            {
                var currentKey = startingKey + i;
                this._BackingMap[currentKey] = values[valsReadIndex + i];
                var indexOfCurrentKey = existingIndexOfStartingKey + i;
                if (indexOfCurrentKey == this._Keys.Count || this._Keys[indexOfCurrentKey] != currentKey) this._Keys.Insert(indexOfCurrentKey, currentKey);
            }
        }

        private void SetManuallyCreatedValueForFirstTime(int index, T val)
        {
            this._BackingMap.Add(index, val);
            this._Keys.Insert(~this._Keys.BinarySearch(index), index);
        }

        #region IList & ICollection methods

        int IList.Add(object value)
        {
            var indexToInsert = this.Count;
            (this as IList).Insert(indexToInsert, value);

            return indexToInsert;
        }

        void IList.Clear()
        {
            this.Clear();
        }

        bool IList.Contains(object value)
        {
            return this.IndexOf(value) != -1;
        }

        int IList.IndexOf(object value)
        {
            return this.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            this.AllocateNullSlots(index, 1);
            this.SetOrUpdateManuallyCreatedValue(index, (T)value);
        }

        void IList.Remove(object value)
        {
            var index = this.IndexOf(value);
            if (index == -1) return;
            this.Remove(index, 1);
        }

        void IList.RemoveAt(int index)
        {
            this.Remove(index, 1);
        }

        /// <summary>
        /// Note that this creates all of the items in the list! If your list is huge (10k items or more), this would take a while.
        /// Use <see cref="AsEnumerableForExistingItems"/> if you only care about the existing items
        /// </summary>
        void ICollection.CopyTo(Array array, int index)
        {
            // See GetEnumerator() for info
            for (var i = 0; i < this.Count; i++) array.SetValue(this[i], index + i);
        }

        /// <summary>
        /// Note that this creates all of the items in the list! If your list is huge (10k items or more), this would take a while.
        /// Use <see cref="AsEnumerableForExistingItems"/> if you only care about the existing items
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            // This is only provided to satisfy the IList implementation, but because in most cases of enumeration
            // you'd want all of the items to be created, we do so (the alternatives being to either return 
            // nulls for not-yet-created positions or to only return already-created items). 
            // this[int] creates items lazily
            for (var i = 0; i < this.Count; i++) yield return this[i];
        }

        #endregion

        private int IndexOf(object value)
        {
            foreach (var kv in this._BackingMap)
                if ((object)kv.Value == value)
                    return kv.Key;

            return -1;
        }

        public class EnumerableLazyList : IEnumerable<T>
        {
            private LazyList<T> _LazyList;

            public EnumerableLazyList(LazyList<T> lazyList)
            {
                this._LazyList = lazyList;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this._LazyList.GetEnumeratorForExistingItems();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        //class RefInt// : IEquatable<RefInt>
        //{
        //	public int value;

        //	public override int GetHashCode() { return value.GetHashCode(); }
        //	public override bool Equals(object obj)
        //	{
        //		var refInt = obj as RefInt;
        //		if (refInt == null)
        //			return false;

        //		return value == refInt.value;
        //	}

        //	//public static bool operator ==(RefInt val, int val2) { return val.value == val2; }
        //	//public static bool operator !=(RefInt val, int val2) { return val.value != val2; }
        //	//public static bool operator ==(int val2, RefInt val) { return val.value == val2; }
        //	//public static bool operator !=(int val2, RefInt val) { return val.value != val2; }

        //	//public static implicit operator int(RefInt val) { return val.value; }
        //	//public static implicit operator RefInt(int val) { return new RefInt() { value = val }; }

        //	public override string ToString() { return value + ""; }

        //	//public bool Equals(RefInt other) { return value == other.value; }
        //}

        //public void Add(int key, TValue value) { _BackingDictionary.Add(key, value); }
        //public void Add(KeyValuePair<int, TValue> item) { BackingDictionaryAsInterface.Add(item); }
        //public bool Contains(KeyValuePair<int, TValue> item) { return _BackingDictionary.Contains(item); }
        //public bool ContainsKey(int key) { return key >= FirstIndex && key <= LastIndex; }
        //public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex) { BackingDictionaryAsInterface.CopyTo(array, arrayIndex); }
        //public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator() { return _BackingDictionary.GetEnumerator(); }
        //public bool Remove(int key) { return if (_BackingList.Remove(key); }
        //public bool Remove(KeyValuePair<int, TValue> item) { return BackingDictionaryAsInterface.Remove(item); }
        //public bool TryGetValue(int key, out TValue value) { return _BackingDictionary.TryGetValue(key, out value); }
        //IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        private class IniniteNullList : IList<T>
        {
            public T this[int index] { get => default; set { } }

            public int Count => -1;

            public bool IsReadOnly => true;

            public void Add(T item)
            {
            }

            public void Clear()
            {
            }

            public bool Contains(T item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public int IndexOf(T item)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, T item)
            {
            }

            public bool Remove(T item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        //class MyLinkedList
        //{
        //	public MyLinkedListNode First
        //	{
        //		get
        //		{
        //			if (_First == null)
        //			{
        //				if (Length == 0)
        //					return null;

        //			}
        //		}
        //	}
        //	public int Length { get; private set; }

        //	MyLinkedListNode _First;

        //	public void InitWithNewCount(int newCount) { Length = newCount; }
        //	public void Add(int count) { _BackingList.AddRange(Array.CreateInstance(typeof(T), count) as T[]); }
        //	public void InsertAtStart(int count) { _BackingList.InsertRange(0, Array.CreateInstance(typeof(T), count) as T[]); }
        //	public void Insert(int index, int count) { _BackingList.InsertRange(index, Array.CreateInstance(typeof(T), count) as T[]); }
        //	public void Clear() { _BackingList.Clear(); }
        //	public void Remove(T value) { _BackingList.Remove(value); }
        //	public void RemoveAt(int index) { _BackingList.RemoveAt(index); }
        //}

        //class MyLinkedListNode
        //{
        //	public int value;
        //	public MyLinkedListNode next;
        //}
    }
}