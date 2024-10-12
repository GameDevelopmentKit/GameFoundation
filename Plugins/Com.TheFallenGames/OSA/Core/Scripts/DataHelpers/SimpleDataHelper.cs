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
    /// <para>Contains shortcuts for common operations on a list. Most notably, it adds/removes items for you and notifies the adapter after.</para>
    /// <para>If you need full control, consider using your own list and notifying the adapter after each modification. Inspect this class to see how it's done</para>
    /// </summary>
    public class SimpleDataHelper<T> : IEnumerable<T>
    {
        public int Count => this._DataList.Count;

        public T this[int index] => this._DataList[index];

        /// <summary>
        /// <para>NOTE: If you modify the list directly, the changes won't be reflected in the adapter unless you call <see cref="NotifyListChangedExternally(bool)"/></para>
        /// <para>
        /// This is not encouraged for partial inserts/removes (i.e. when some of the items should be kept), because it updates all items' views. 
        /// Use only if necessary
        /// </para>
        /// </summary>
        public List<T> List => this._DataList;

        protected IOSA    _Adapter;
        protected List<T> _DataList;
        private   bool    _KeepVelocityOnCountChange;

        public SimpleDataHelper(IOSA iAdapter, bool keepVelocityOnCountChange = true)
        {
            this._Adapter                   = iAdapter;
            this._DataList                  = new();
            this._KeepVelocityOnCountChange = keepVelocityOnCountChange;
        }

        #region IEnumerator<T> implementation

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this._DataList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._DataList.GetEnumerator();
        }

        #endregion

        public void InsertItems(int index, IList<T> models, bool freezeEndEdge = false)
        {
            this._DataList.InsertRange(index, models);

            if (this._Adapter.InsertAtIndexSupported)
                this._Adapter.InsertItems(index, models.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
            else
                this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        public void InsertItemsAtStart(IList<T> models, bool freezeEndEdge = false)
        {
            this.InsertItems(0, models, freezeEndEdge);
        }

        public void InsertItemsAtEnd(IList<T> models, bool freezeEndEdge = false)
        {
            this.InsertItems(this._DataList.Count, models, freezeEndEdge);
        }

        /// <summary>NOTE: Use <see cref="InsertItems(int, IList{T}, bool)"/> for bulk inserts, as it's way faster</summary>
        public void InsertOne(int index, T model, bool freezeEndEdge = false)
        {
            this._DataList.Insert(index, model);
            if (this._Adapter.InsertAtIndexSupported)
                this._Adapter.InsertItems(index, 1, freezeEndEdge, this._KeepVelocityOnCountChange);
            else
                this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
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
            if (this._Adapter.RemoveFromIndexSupported)
                this._Adapter.RemoveItems(index, count, freezeEndEdge, this._KeepVelocityOnCountChange);
            else
                this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
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
        public void RemoveOne(int index, bool freezeEndEdge = false)
        {
            this._DataList.RemoveAt(index);
            if (this._Adapter.RemoveFromIndexSupported)
                this._Adapter.RemoveItems(index, 1, freezeEndEdge, this._KeepVelocityOnCountChange);
            else
                this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
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
            this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        /// <summary>
        /// In case of large lists of items, it is beneficial to replace the list instance, instead of using List's AddRange method, which <see cref="ResetItems(IList{T}, bool)"/> does.
        /// </summary>
        public void ResetItemsByReplacingListInstance(List<T> newListInstance, bool freezeEndEdge = false)
        {
            this._DataList.Clear();
            this._DataList = newListInstance;
            this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }

        public void NotifyListChangedExternally(bool freezeEndEdge = false)
        {
            this._Adapter.ResetItems(this._DataList.Count, freezeEndEdge, this._KeepVelocityOnCountChange);
        }
    }
}