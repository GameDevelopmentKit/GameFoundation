using System;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.DataHelpers
{
    /// <summary>
    /// <para>Contains shortcuts for common operations on a list. Most notably, it adds/removes items for you and notifies the adapter after.</para>
    /// <para>If you need full control, consider using your own list and notifying the adapter after each modification. Inspect this class for how it's done</para>
    /// <para>This uses a <see cref="LazyList{T}"/> as storage. Models are created only when first accessed</para>
    /// </summary>
    public class LazyDataHelper<T>
    {
        public int Count => this._DataList.Count;

        /// <summary>See notes on <see cref="SimpleDataHelper{T}.List"/></summary>
        public LazyList<T> List => this._DataList;

        /// <summary>Will be set back to false after next event</summary>
        public bool SkipNotifyingAdapterForNextEvent { get; set; }

        protected IOSA        _Adapter;
        protected LazyList<T> _DataList;
        private   bool        _KeepVelocityOnCountChange;

        public LazyDataHelper(IOSA iAdapter, Func<int, T> newModelCreator, bool keepVelocityOnCountChange = true)
        {
            this._Adapter                   = iAdapter;
            this._DataList                  = new(newModelCreator, 0);
            this._KeepVelocityOnCountChange = keepVelocityOnCountChange;
        }

        public T GetOrCreate(int index)
        {
            return this._DataList[index];
        }

        public LazyList<T>.EnumerableLazyList GetEnumerableForExistingItems()
        {
            return this._DataList.AsEnumerableForExistingItems;
        }

        public void InsertItems(int index, int count, bool freezeEndEdge = false)
        {
            this._DataList.Insert(index, count);
            if (this.SkipNotifyingAdapterForNextEvent)
                this.SkipNotifyingAdapterForNextEvent = false;
            else
                this.InsertItemsInternal(index, count, freezeEndEdge);
        }

        public void InsertItemsAtStart(int count, bool freezeEndEdge = false)
        {
            this.InsertItems(0, count, freezeEndEdge);
        }

        public void InsertItemsAtEnd(int count, bool freezeEndEdge = false)
        {
            this.InsertItems(this._DataList.Count, count, freezeEndEdge);
        }

        /// <summary>NOTE: Use <see cref="InsertItems(int, int, bool)"/> for bulk inserts, as it's way faster</summary>
        public void InsertOneManuallyCreated(int index, T model, bool freezeEndEdge = false)
        {
            this._DataList.Insert(index, 1);
            this._DataList.SetOrUpdateManuallyCreatedValue(index, model);
            if (this.SkipNotifyingAdapterForNextEvent)
                this.SkipNotifyingAdapterForNextEvent = false;
            else
                this.InsertItemsInternal(index, 1, freezeEndEdge);
        }

        public void RemoveItems(int index, int count, bool freezeEndEdge = false)
        {
            this._DataList.Remove(index, count);
            if (this.SkipNotifyingAdapterForNextEvent)
                this.SkipNotifyingAdapterForNextEvent = false;
            else
                this.RemoveItemsInternal(index, count, freezeEndEdge);
        }

        public void RemoveItemsFromStart(int count, bool freezeEndEdge = false)
        {
            this.RemoveItems(0, count, freezeEndEdge);
        }

        public void RemoveItemsFromEnd(int count, bool freezeEndEdge = false)
        {
            this.RemoveItems(this._DataList.Count - count, count, freezeEndEdge);
        }

        public void RemoveOne(T model, bool freezeEndEdge = false)
        {
            var index = this._DataList.Remove(model);
            if (index == -1) throw new OSAException("Not found: " + model);
            if (this.SkipNotifyingAdapterForNextEvent)
                this.SkipNotifyingAdapterForNextEvent = false;
            else
                this.RemoveItemsInternal(index, 1, freezeEndEdge);
        }

        public void ResetItems(int count, bool freezeEndEdge = false)
        {
            this._DataList.InitWithNewCount(count);
            if (this.SkipNotifyingAdapterForNextEvent)
                this.SkipNotifyingAdapterForNextEvent = false;
            else
                this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        public void NotifyListChangedExternally(bool freezeEndEdge = false)
        {
            if (this.SkipNotifyingAdapterForNextEvent) throw new OSAException("Don't set SkipNotifyingAdapterForNextEvent=true before calling NotifyListChangedExternally");

            this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        private void InsertItemsInternal(int index, int count, bool freezeEndEdge)
        {
            if (this._Adapter.InsertAtIndexSupported)
                this._Adapter.InsertItems(index, count, freezeEndEdge, this._KeepVelocityOnCountChange);
            else
                this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        private void RemoveItemsInternal(int index, int count, bool freezeEndEdge)
        {
            if (this._Adapter.RemoveFromIndexSupported)
                this._Adapter.RemoveItems(index, count, freezeEndEdge, this._KeepVelocityOnCountChange);
            else
                this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }
    }
}