using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.ForbiddenByte.OSA.DataHelpers
{
    /// <summary>
    /// Class for loading chunks of data asynchronously into a <see cref="BufferredDataSource{T}"/> and firing events on 
    /// begin/end load multiple chunks and when finished loading each individual chunk
    /// </summary>
    public class AsyncBufferredDataSource<T>
        where T : new()
    {
        /// <summary>Make sure to call onDone on the main thread</summary>
        public delegate void Loader(T[] intoExistingValues, int firstItemIndex, int countToRead, Action onDone);

        /// <summary>
        /// Fired when a loading task begins, but only if no other tasks are currently running (which woud've had already fired this event).
        /// The term 'session' is used because multiple tasks can be started which could overlap in time.
        /// </summary>
        public event Action LoadingSessionStarted;

        /// <summary>Fired when any single loading task ends</summary>
        public event Action<LoadingTask> SingleTaskFinished;

        /// <summary>Fired when the last loading task ends, marking the end of the loading session</summary>
        public event Action LoadingSessionEnded;

        public int  Count                      => this._DataSource.Count;
        public bool ShowLogs                   { get; set; }
        public int  CurrentlyLoadingTasksCount => this._CurrentlyLoadingTasks.Count;

        private readonly BufferredDataSource<T> _DataSource;
        private readonly Loader                 _Loader;
        private readonly HashSet<LoadingTask>   _CurrentlyLoadingTasks = new();
        private          Queue<T[]>             _BuffersPool           = new(50);
        private          int                    _ChunkBufferSize;

        /// <summary>
        /// <paramref name="loader"/> can be called multiple times, even if previous loads weren't finished yet
        /// </summary>
        public AsyncBufferredDataSource(int count, int chunkBufferSize, Loader loader)
        {
            this._Loader          = loader;
            this._ChunkBufferSize = chunkBufferSize;
            this._DataSource      = new(count, this.ChunkReader, this._ChunkBufferSize, true /*items will be created indirectly, and then committed to the source*/);
        }

        public T GetValue(int index)
        {
            return this._DataSource[index];
        }

        public void ClearAllRunningTasks()
        {
            this._CurrentlyLoadingTasks.Clear();
        }

        private void ChunkReader(T[] into, int firstItemIndex, int countToRead)
        {
            // Important: passing a clone of the buffer to the task, as multiple tasks can overlap in time and so
            // we don't want them to modify the same buffer after the initial empty values were assigned
            var into_Copy = this.GetPooledBuffer();

            //// Filling slots with empty objects (i.e. valid objects, but with null/empty fields)
            //for (int i = 0; i < countToRead; i++)
            //{
            //	var val = _EmptyValueCreator();
            //	into[i] = val; // passing the empty values to the reqder, as requested
            //	into_Copy[i] = val; // but storing them in a separate local buffer (pooled)
            //}

            var task = new LoadingTask(into_Copy, firstItemIndex, countToRead, this._Loader, this.OnFinishedOneTask);

            // Some debugging to check if ranges overlap. They do not, as of 21-Jul-2019
            //foreach (var existingTask in _CurrentlyLoadingTasks)
            //{
            //	if (existingTask.FirstItemIndex == firstItemIndex)
            //		throw new InvalidOperationException(firstItemIndex + "");
            //	if (existingTask.LastItemIndexExcl == task.LastItemIndexExcl)
            //		throw new InvalidOperationException(task.LastItemIndexExcl + "");

            //	if (task.FirstItemIndex < existingTask.FirstItemIndex)
            //	{
            //		if (task.LastItemIndexExcl > existingTask.FirstItemIndex)
            //			throw new InvalidOperationException(existingTask.FirstItemIndex + ", " + existingTask.LastItemIndexExcl + ", " + task.FirstItemIndex + ", " + task.LastItemIndexExcl);
            //	}
            //	else
            //	{
            //		if (task.FirstItemIndex < existingTask.LastItemIndexExcl)
            //			throw new InvalidOperationException(existingTask.FirstItemIndex + ", " + existingTask.LastItemIndexExcl + ", " + task.FirstItemIndex + ", " + task.LastItemIndexExcl);
            //	}
            //}

            this.StartTask(task);
        }

        private void StartTask(LoadingTask task)
        {
            if (this._CurrentlyLoadingTasks.Count == 0)
                // On first task, notify listeners that a loading session begins
                if (this.LoadingSessionStarted != null)
                    this.LoadingSessionStarted();
            this._CurrentlyLoadingTasks.Add(task);

            if (this.ShowLogs) Debug.Log("Starting loading task '" + task + "'. numLoadingTasks now is " + this._CurrentlyLoadingTasks.Count);
            task.Start();
        }

        private void OnFinishedOneTask(LoadingTask task)
        {
            // Ignore the callback if meanwhile something invalidated the list of tasks
            if (!this._CurrentlyLoadingTasks.Remove(task))
            {
                if (this.ShowLogs) Debug.Log("Finished loading task '" + task + "', but the task list was invalidated meanwhile. numLoadingTasks is " + this._CurrentlyLoadingTasks.Count);

                return;
            }

            // Commit the populated local buffer to the data source
            this._DataSource.ManuallyUpdateCreatedValues(task.FirstItemIndex, task.Buffer, 0, task.CountToRead);

            // Recycle the buffer
            this._BuffersPool.Enqueue(task.Buffer);

            var noMoreTasks = this._CurrentlyLoadingTasks.Count == 0;
            if (this.ShowLogs) Debug.Log("Finished loading task '" + task + "'. numLoadingTasks now is " + this._CurrentlyLoadingTasks.Count);

            if (this.SingleTaskFinished != null) this.SingleTaskFinished(task);

            if (noMoreTasks)
                if (this.LoadingSessionEnded != null)
                    this.LoadingSessionEnded();
        }

        // Expecting values to be the same length as any pooled buffer, since _ChunkBufferSize is expected to be constant
        private T[] GetPooledBuffer()
        {
            T[] pooled;
            if (this._BuffersPool.Count == 0)
                pooled = new T[this._ChunkBufferSize];
            else
                pooled = this._BuffersPool.Dequeue();
            return pooled;
        }

        public class LoadingTask
        {
            public readonly int FirstItemIndex;
            public readonly int CountToRead;
            public          int LastItemIndexExcl => this.FirstItemIndex + this.CountToRead;

            public readonly  T[]                 Buffer;
            private readonly Loader              _Loader;
            private readonly Action<LoadingTask> _OnFinished;

            public LoadingTask(T[] intoExistingModels, int firstItemIndex, int countToRead, Loader loaderFunction, Action<LoadingTask> onFinished)
            {
                this.Buffer         = intoExistingModels;
                this.FirstItemIndex = firstItemIndex;
                this.CountToRead    = countToRead;
                this._Loader        = loaderFunction;
                this._OnFinished    = onFinished;
            }

            public void Start()
            {
                this._Loader(this.Buffer,
                    this.FirstItemIndex,
                    this.CountToRead,
                    this.OnFinishedLoadingOperation
                );
            }

            private void OnFinishedLoadingOperation()
            {
                if (this._OnFinished != null) this._OnFinished(this);
            }

            public override string ToString()
            {
                return "[" + this.FirstItemIndex + ", " + this.LastItemIndexExcl + ")" + ": " + this.CountToRead;
            }
        }
    }
}