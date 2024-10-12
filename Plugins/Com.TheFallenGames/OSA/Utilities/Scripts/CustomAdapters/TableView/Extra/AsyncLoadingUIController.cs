using System;
using UnityEngine;
using Com.ForbiddenByte.OSA.DataHelpers;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Extra
{
    /// <summary>
    /// Class for loading data via a <see cref="AsyncBufferredTableData{TTuple}"/> and notifying a <see cref="TableAdapter{TParams, TTupleViewsHolder, THeaderTupleViewsHolder}"/>
    /// about changes, while staying aware of its lifecycle events like <see cref="TableAdapter{TParams, TTupleViewsHolder, THeaderTupleViewsHolder}.ChangeItemsCount(Core.ItemCountChangeMode, int, int, bool, bool)"/>
    /// and invalidating any existing loading tasks when needed.
    /// It'll dispose everything on first count change.
    /// </summary>
    public class AsyncLoadingUIController<TTuple>
        where TTuple : ITuple, new()
    {
        private        ITableAdapter                   _Adapter;
        private        AsyncBufferredTableData<TTuple> _AsyncData;
        private static int                             _NumInstances; // just for debug purposes

        private int _ID = _NumInstances++;

        public AsyncLoadingUIController(ITableAdapter tableAdapter, AsyncBufferredTableData<TTuple> data)
        {
            this._Adapter                                =  tableAdapter;
            this._AsyncData                              =  data;
            this._AsyncData.Source.LoadingSessionStarted += this.OnAsyncDataLoadingStarted;
            this._AsyncData.Source.SingleTaskFinished    += this.OnAsyncDataSingleTaskFinished;
            this._AsyncData.Source.LoadingSessionEnded   += this.OnAsyncDataLoadingSessionEnded;
        }

        public void BeginListeningForSelfDisposal()
        {
            this._Adapter.ItemsRefreshed += this.OnAdapterItemsRefreshed;
        }

        private void OnAsyncDataLoadingStarted()
        {
            if (this._Adapter.Options != null) this._Adapter.Options.IsLoading = true;
        }

        private void OnAsyncDataSingleTaskFinished(AsyncBufferredDataSource<TTuple>.LoadingTask task)
        {
            // Make sure the adapter wasn't disposed meanwhile
            if (this._Adapter == null || !this._Adapter.IsInitialized)
            {
                this.Dispose();
                return;
            }

            this._Adapter.RefreshRange(task.FirstItemIndex, task.CountToRead);
        }

        private void OnAsyncDataLoadingSessionEnded()
        {
            // Make sure the adapter wasn't disposed meanwhile
            if (this._Adapter == null || !this._Adapter.IsInitialized)
            {
                this.Dispose();
                return;
            }

            if (this._Adapter.Options != null) this._Adapter.Options.IsLoading = false;
        }

        // If the adapter refreshes its items while one or more tasks are in progress, 
        // make sure to invalidate them so they'll be ignored when they'll fire OnFinishedOneTask
        private void OnAdapterItemsRefreshed(int prevCount, int newCount)
        {
            // When the adapter's items count changes or the views are fully refreshed (for example, as a result 
            // of resizing the ScrollView), it fires the ItemsRefreshed.
            // Check whether that was a result of a simple Refresh (which is done by 
            // OSA and thus preserves the data) or an external ResetTable call, which changes the data and thus 
            // requires this uiController to self-dispose. 
            var sameDataReferences = this._Adapter.Tuples == this._AsyncData && this._Adapter.Columns == this._AsyncData.Columns;

            if (sameDataReferences) return;

            var numRunning = this._AsyncData.Source.CurrentlyLoadingTasksCount;
            if (numRunning > 0)
                //if (_AsyncData.ShowLogs)
                //	Debug.Log("OnAdapterItemsRefreshed(count " + prevCount + " -> " + newCount + ") : Clearing all " + numRunning + " active tasks");
                // Update: this overrides an existing loading task, so it's left to the adapter's will to disable the loading state
                //if (_Adapter.Options != null)
                //	_Adapter.Options.IsLoading = false;
                this._AsyncData.Source.ClearAllRunningTasks();

            if (this._AsyncData.Source.ShowLogs)
                Debug.Log(
                    "AsyncLoadingUIController #" + this._ID + ": OnAdapterItemsRefreshed(count " + prevCount + " -> " + newCount + ") : Clearing all " + numRunning + " active tasks and disposing self"
                );

            this.Dispose();
        }

        private void Dispose()
        {
            // Unsubscribing from events makes this object available for GC

            if (this._AsyncData != null && this._AsyncData.Source != null)
            {
                this._AsyncData.Source.LoadingSessionStarted -= this.OnAsyncDataLoadingStarted;
                this._AsyncData.Source.SingleTaskFinished    -= this.OnAsyncDataSingleTaskFinished;
                this._AsyncData.Source.LoadingSessionEnded   -= this.OnAsyncDataLoadingSessionEnded;
            }
            if (this._Adapter != null) this._Adapter.ItemsRefreshed -= this.OnAdapterItemsRefreshed;

            this._Adapter   = null;
            this._AsyncData = null;
        }
    }
}