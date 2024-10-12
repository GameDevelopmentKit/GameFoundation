using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA;
using System;
using UnityEngine.Events;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI;
using Com.ForbiddenByte.OSA.Core;
using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.DataHelpers
{
    /// <summary>
    /// Data helper that sorts the given data and sorts it through a given predicate.
    /// 
    /// Set <see cref="UseFilteredData"/> to control whether you want to see the filtered or unfiltered sets.
    /// 
    /// Set <see cref="FilteringCriteria"/> to filter your dataset as needed, this does cause
    /// a rebuild of internal indexes at O(n) cost. 
    /// 
    /// Use <see cref="InsertItems(int, IList{T}, bool)"/> for bulk inserts.
    /// Make sure the index of what you insert is where you want it to be on the unfiltered set. 
    /// Make use of <see cref="GetUnfilteredIdex(int)"/> if you want to insert at a known filtered index.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilterableDataHelper<T> : IEnumerable<T>
    {
        private List<T>      _FilteredList;
        private List<T>      _DataList;
        private List<int>    _FilteredItemIndex;
        private bool         _UseFilteredData = true;
        private IOSA         _Adapter;
        private bool         _KeepVelocityOnCountChange;
        private Predicate<T> _FilteringCriteria;

        public FilterableDataHelper(IOSA iAdapter, bool keepVelocityOnCountChange = true)
        {
            this._DataList                  = new();
            this._Adapter                   = iAdapter;
            this._FilteredList              = new();
            this._FilteredItemIndex         = new();
            this._FilteringCriteria         = (T) => true;
            this._KeepVelocityOnCountChange = keepVelocityOnCountChange;
        }

        public bool UseFilteredData { get => this._UseFilteredData; set => this._UseFilteredData = value; }

        public T this[int index] => this.CurrentList[index];

        public int Count => this.CurrentList.Count;

        public Predicate<T> FilteringCriteria
        {
            set
            {
                if (value == null) throw new ArgumentNullException("Please make sure your predicate is valid");

                this._FilteringCriteria = value;
                this.RemakeFilteredItems();

                this._Adapter.ResetItems(this.CurrentList.Count);
            }
        }

        private List<T> CurrentList => this._UseFilteredData ? this._FilteredList : this._DataList;

        #region IEnumerator<T> implementation

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.CurrentList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.CurrentList.GetEnumerator();
        }

        #endregion

        #region ItemManipulation

        public void InsertItems(int index, IList<T> models, bool freezeEndEdge = false)
        {
            this._DataList.InsertRange(index, models);
            var filteredIndex          = this._FilteredItemIndex.BinarySearch(index);
            var inList                 = filteredIndex >= 0;
            if (!inList) filteredIndex = ~filteredIndex; //this becomes 0 in empty lists

            var modelsToAdd  = new List<T>();
            var modelIndexes = new List<int>();

            for (var i = 0; i < models.Count; i++)
            {
                var currentModel = models[i];
                if (this._FilteringCriteria(currentModel))
                {
                    modelsToAdd.Add(currentModel);
                    modelIndexes.Add(index + i);
                }
            }

            for (var i = filteredIndex; i < this._FilteredItemIndex.Count; i++) this._FilteredItemIndex[i] += models.Count;
            //The in list check is there mostly for cases where index is 0 the mathf max checks for bulk-add to empty lists

            this._FilteredList.InsertRange(Mathf.Max(filteredIndex + (inList ? 0 : -1), 0), modelsToAdd);
            this._FilteredItemIndex.InsertRange(Mathf.Max(filteredIndex + (inList ? 0 : -1), 0), modelIndexes);

            if (this._Adapter.InsertAtIndexSupported)
                if (this._UseFilteredData)
                    this._Adapter.InsertItems(filteredIndex, modelsToAdd.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
                else
                    this._Adapter.InsertItems(index, models.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
            else
                this._Adapter.ResetItems(this.CurrentList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        public void InsertItemsAtStart(IList<T> models, bool freezeEndEdge = false)
        {
            this.InsertItems(0, models, freezeEndEdge);
        }

        public void InsertItemsAtEnd(IList<T> models, bool freezeEndEdge = false)
        {
            this.InsertItems(this._DataList.Count, models, freezeEndEdge);
        }

        /// <summary>NOTE: Use <see cref="InsertItems(int, IList{T}, bool)"/> for bulk inserts, as it's way faster.
        /// Make sure the index is where you want it to be on the unfiltered set. 
        /// Make use of <see cref="GetUnfilteredIdex(int)"/> if you dont know the big dataset</summary>
        public void InsertOne(int unfilteredIndex, T model, bool freezeEndEdge = false)
        {
            this._DataList.Insert(unfilteredIndex, model);

            var filteredIndex          = this._FilteredItemIndex.BinarySearch(unfilteredIndex);
            var inList                 = filteredIndex >= 0;
            if (!inList) filteredIndex = Mathf.Max(~filteredIndex - 1, 0); //this becomes 0 in empty lists

            for (var i = filteredIndex; i < this._FilteredItemIndex.Count; i++) this._FilteredItemIndex[i]++;
            var isFilteredModel = this._FilteringCriteria(model);

            if (isFilteredModel)
            {
                this._FilteredList.Insert(filteredIndex, model);

                this._FilteredItemIndex.Insert(filteredIndex, unfilteredIndex);
            }

            if (this._Adapter.InsertAtIndexSupported)
            {
                var wetherToInsert                                           = 1;
                if (this.UseFilteredData && !isFilteredModel) wetherToInsert = 0;
                this._Adapter.InsertItems(this._UseFilteredData ? filteredIndex : unfilteredIndex, wetherToInsert, freezeEndEdge, this._KeepVelocityOnCountChange);
            }
            else
                this._Adapter.ResetItems(this.CurrentList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        public void InsertOneAtStart(T model, bool freezeEndEdge = false)
        {
            this.InsertOne(0, model, freezeEndEdge);
        }

        public void InsertOneAtEnd(T model, bool freezeEndEdge = false)
        {
            this.InsertOne(this._DataList.Count, model, freezeEndEdge);
        }

        public void RemoveItems(int index, int count, bool freezeEndEdge = false)
        {
            this._DataList.RemoveRange(index, count);

            var filteredMaxIndex = this._FilteredItemIndex.BinarySearch(index + count);
            var filteredMinIndex = this._FilteredItemIndex.BinarySearch(index);

            var inListMin                    = filteredMinIndex >= 0;
            var inListMax                    = filteredMaxIndex >= 0;
            if (!inListMin) filteredMinIndex = ~filteredMinIndex; //this becomes 0 in empty lists
            if (!inListMax) filteredMaxIndex = ~filteredMaxIndex;

            var filteredItemsToRemove = filteredMaxIndex - filteredMinIndex;

            for (var i = filteredMaxIndex; i < this._FilteredItemIndex.Count; i++) this._FilteredItemIndex[i] -= count;

            this._FilteredItemIndex.RemoveRange(filteredMinIndex, filteredItemsToRemove);
            this._FilteredList.RemoveRange(filteredMinIndex, filteredItemsToRemove);

            if (this._Adapter.RemoveFromIndexSupported)
            {
                if (this._UseFilteredData)
                {
                    if (filteredItemsToRemove > 0) this._Adapter.RemoveItems(filteredMinIndex, filteredItemsToRemove, freezeEndEdge, this._KeepVelocityOnCountChange);
                }
                else
                    this._Adapter.RemoveItems(index, count, this._KeepVelocityOnCountChange);
            }
            else
                this._Adapter.ResetItems(this.CurrentList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        public void RemoveItemsFromStart(int count, bool freezeEndEdge = false)
        {
            this.RemoveItems(0, count, freezeEndEdge);
        }

        public void RemoveItemsFromEnd(int count, bool freezeEndEdge = false)
        {
            this.RemoveItems(this._DataList.Count - count, count, freezeEndEdge);
        }

        /// <summary>NOTE: Use <see cref="RemoveItems(int, int, bool)"/> for bulk removes, as it's way faster</summary>
        public void RemoveOne(int unfilteredIndex, bool freezeEndEdge = false)
        {
            this.RemoveItems(unfilteredIndex, 1, freezeEndEdge);
        }

        public void RemoveOneFromStart(bool freezeEndEdge = false)
        {
            this.RemoveOne(0, freezeEndEdge);
        }

        public void RemoveOneFromEnd(bool freezeEndEdge = false)
        {
            this.RemoveOne(this._DataList.Count - 1, freezeEndEdge);
        }

        /// <summary>
        /// NOTE: In case of resets, the preferred way is to clear the <see cref="List"/> yourself, add the models through it, and then call <see cref="NotifyListChangedExternally(bool)"/>.
        /// This saves memory by avoiding creating an intermediary array/list
        /// </summary>
        public void ResetItems(IList<T> models, bool freezeEndEdge = false)
        {
            this._DataList.Clear();
            this._DataList.AddRange(models);
            this.RemakeFilteredItems();
            this._Adapter.ResetItems(this.CurrentList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        /// <summary>
        /// In case of large lists of items, it is beneficial to replace the list instance, instead of using List's AddRange method, which <see cref="ResetItems(IList{T}, bool)"/> does.
        /// </summary>
        public void ResetItemsByReplacingListInstance(List<T> newListInstance, bool freezeEndEdge = false)
        {
            this._DataList.Clear();
            this._DataList = newListInstance;
            this.RemakeFilteredItems();
            this._Adapter.ResetItems(this.CurrentList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        public void NotifyListChangedExternally(bool freezeEndEdge = false)
        {
            this.RemakeFilteredItems();
            this._Adapter.ResetItems(this.CurrentList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        #endregion

        /// <summary>
        /// Call this function whenever you want to get the index of the filtered item on the unfiltered list
        /// </summary>
        /// <param name="filteredIndex"></param>
        /// <returns></returns>
        public int GetUnfilteredIdex(int filteredIndex)
        {
            if (!this._UseFilteredData) return filteredIndex;

            return this._FilteredItemIndex[filteredIndex];
        }

        private void RemakeFilteredItems()
        {
            this._FilteredList.Clear();
            this._FilteredItemIndex.Clear();

            for (var i = 0; i < this._DataList.Count; i++)
            {
                var model = this._DataList[i];

                if (this._FilteringCriteria(model))
                {
                    this._FilteredList.Add(model);
                    this._FilteredItemIndex.Add(i);
                }
            }
        }
    }
}