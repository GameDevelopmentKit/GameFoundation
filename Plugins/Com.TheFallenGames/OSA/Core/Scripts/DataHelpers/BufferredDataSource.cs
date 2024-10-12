using System;
using UnityEngine;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.DataHelpers
{
    /// <summary>
    /// Data source that reads items via a <see cref="LazyList{T}"/> in chunks in the following way:
    /// <para>When an item with index K is requested and it doesn't exist, the provided <see cref="ChunkReader"/> is used
    /// to retrieve <see cref="BufferSize"/> items around K, i.e. half before and half after K, respecting boundaries</para>
    /// <para>The ideal value of <see cref="BufferSize"/> depends on how frequent big jumps in positions occur. 
    /// Smaller if frequent and/or big jumps and vice-versa. This also depends on your data main source's availability. 
    /// If you can afford reading very frequently from it, then set a smaller chunk size, and vice-versa.
    /// The best way is to find it by manually testing different values</para>
    /// </summary>
    public class BufferredDataSource<T>
    {
        public int BUFFER_MAX_SIZE_DEFAULT = 100;

        /// <summary>
        /// </summary>
        /// <param name="into">Array to read data into, starting at 0, and ending at countToRead-1</param>
        /// <param name="firstItemIndex">This is not the index at which to insert the item, but rather the index of the item in your complete data set</param>
        public delegate void ChunkReader(T[] into, int firstItemIndex, int countToRead);

        public T this[int index] => this._LazyList[index];

        public int Count => this._LazyList.Count;

        /// <summary>See <see cref="LazyList{T}.ExistingCount"/></summary>
        public int ExistingCount => this._LazyList.ExistingCount;

        public int BufferSize => this._BufferSize;

        private          LazyList<T> _LazyList;
        private          ChunkReader _ChunkReader;
        private          int         _BufferSize;
        private readonly bool        _ExpectManualBufferUpdates;
        private          T[]         _ReadBuffer;

        public BufferredDataSource(int totalCount, ChunkReader chunkReader, int bufferMaxSize, bool expectManualBufferUpdates)
        {
            if (bufferMaxSize < 1)
                bufferMaxSize                                  = this.BUFFER_MAX_SIZE_DEFAULT;
            else if (bufferMaxSize > totalCount) bufferMaxSize = totalCount;

            this._ChunkReader               = chunkReader;
            this._BufferSize                = bufferMaxSize;
            this._ExpectManualBufferUpdates = expectManualBufferUpdates;
            this._ReadBuffer                = new T[this._BufferSize];
            this._LazyList                  = new(this.ValueCreator, totalCount);
        }

        /// <summary><see cref="LazyList{T}.TryGet(int, out T)"/></summary>
        public bool TryGetCachedValue(int index, out T tuple)
        {
            return this._LazyList.TryGet(index, out tuple);
        }

        private T ValueCreator(int index)
        {
            T val;
            if (!this.TryGetCachedValue(index, out val))
            {
                var halfBufferSize = this._BufferSize / 2;

                var minStartIndex                        = index - halfBufferSize;
                var startIndex                           = minStartIndex < 0 ? 0 : minStartIndex;
                var additionalCountToReadToSatisfyBuffer = 0;
                if (minStartIndex < 0)
                {
                    startIndex                           = 0;
                    additionalCountToReadToSatisfyBuffer = -minStartIndex;
                }

                // Using longs where int overflow may happen, to avoid additional bound-checks before aritmethic ops

                var maxEndIndexExcl = (long)index + halfBufferSize + this._BufferSize % 2 /*when odd number, add one after to respect _BufferSize exactly*/;
                maxEndIndexExcl += additionalCountToReadToSatisfyBuffer;
                if (maxEndIndexExcl > int.MaxValue) maxEndIndexExcl = int.MaxValue;

                var endIndexExcl = maxEndIndexExcl > this.Count ? this.Count : (int)maxEndIndexExcl;

                int prevClosestIndex;
                int nextClosestIndex;
                this._LazyList.GetIndicesOfClosestExistingItems(index, out prevClosestIndex, out nextClosestIndex);

                //Debug.Log(index + ", " + prevClosestIndex + ", " + nextClosestIndex);

                if (prevClosestIndex != -1)
                    // An item exists before the current one => see if it's closer than our chosen startIndex
                    if (startIndex < prevClosestIndex)
                        startIndex = prevClosestIndex + 1; // we want to add items AFTER the existing closest index

                int countToRead;
                if (nextClosestIndex != -1)
                    // An item exists after the current one => see if it's closer than our chosen endIndex
                    if (nextClosestIndex < endIndexExcl)
                        endIndexExcl = nextClosestIndex;

                countToRead = endIndexExcl - startIndex;

                //Debug.Log("Start/End resolved to " + startIndex + ", " + endIndexExcl + "; countToRead " + countToRead + ", buf " + _BufferSize + ", TotalSlotsCount=" + Count);

                this._ChunkReader(this._ReadBuffer, startIndex, countToRead);

                if (this._ExpectManualBufferUpdates)
                    // Using the optimized version, since we'll initially only fill with empty slots (i.e. null values)
                    this._LazyList.AllocateNullSlots(startIndex, countToRead);
                else
                    this._LazyList.SetOrUpdateManuallyCreatedValues(startIndex, this._ReadBuffer, 0, countToRead);

                val = this._LazyList.GetUnchecked(index);
            }

            return val;
        }

        public T GetValue(int index)
        {
            return this._LazyList[index];
        }

        /// <summary>Throws an exception if the value wasn't already cached</summary>
        public T GetValueUnchecked(int index)
        {
            return this._LazyList.GetUnchecked(index);
        }

        public void ManuallyUpdateCreatedValues(int firstItemIndex, T[] values, int readStartIndex, int readCount)
        {
            if (!this._ExpectManualBufferUpdates) throw new OSAException("Not supported because _ExpectManualBufferUpdates is false");

            this._LazyList.SetOrUpdateManuallyCreatedValues(firstItemIndex, values, readStartIndex, readCount);
        }
    }
}