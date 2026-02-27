namespace DataManager.MasterData
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintController;
    using DataManager.LocalData;
    using DataManager.UserData;
    using GameFoundation.Scripts.Utilities.Extension;
    using UnityEngine;
    using Zenject;

    /// <summary>
    ///implement ITickable to ensure this class is created on first 
    /// because Zenject creates ITickable instances first on startup before others <see cref="Zenject.ProjectContext.InstallBindings"/>
    /// </summary>
    public class MasterDataManager : ITickable
    {
        private readonly SignalBus signalBus;
        private readonly LazyInject<IHandleLocalDataServices> handleLocalDataService;
        private readonly LazyInject<BlueprintReaderManager> blueprintReaderManager;

        public UniTaskCompletionSource<bool> IsReady { get; } = new();

        private readonly Dictionary<string, IUserData> userDataCache = new();

        protected virtual HashSet<IDataManagerLifecycle> DataManagerLifecyclesRequest { get; } = new();
        protected virtual HashSet<IDataManagerLifecycle> LoadedDataManagerLifecycles { get; } = new();

        public MasterDataManager(SignalBus signalBus, LazyInject<IHandleLocalDataServices> handleLocalDataService,
            LazyInject<BlueprintReaderManager> blueprintReaderManager)
        {
            this.signalBus = signalBus;
            this.handleLocalDataService = handleLocalDataService;
            this.blueprintReaderManager = blueprintReaderManager;
            this.signalBus.Subscribe<MasterDataRegisterSignal>(signal =>
                RegisterDataManagerLifecycle(signal.DataManager));
        }

        public async UniTask InitializeData()
        {
            if (this.IsReady.Task.Status == UniTaskStatus.Succeeded) return;

            try
            {
                //Todo refactor when implement load user data from remote flow later
                this.userDataCache.Clear();

                await this.blueprintReaderManager.Value.LoadBlueprint();

                await HandleLoadDataRequests();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                this.IsReady.TrySetException(e);
            }

            this.IsReady.TrySetResult(true);
            this.signalBus.Fire<MasterDataReadySignal>();
        }


        public void RegisterDataManagerLifecycle(IDataManagerLifecycle dataManager)
        {
            this.DataManagerLifecyclesRequest.Add(dataManager);

            if (this.IsReady.Task.Status == UniTaskStatus.Pending) return;

            // MasterDataManager is ready: batch requests until end of frame, then load them all at once
            _ = FlushFrameBatchAsync(); // fire-and-forget - runs on main thread
        }

        public UniTask SaveAllData()
        {
            return this.handleLocalDataService.Value.SaveAll();
        }

        public void DeleteAllData()
        {
            this.userDataCache.Clear();
            this.handleLocalDataService.Value.DeleteAll();
        }

        public UniTask ReloadAllDataManager()
        {
            DeleteAllData();

            //dispose all data manager lifecycle
            foreach (var dataManagerLifecycle in this.LoadedDataManagerLifecycles)
            {
                dataManagerLifecycle.Dispose();
            }

            //reload all data manager lifecycle
            DataManagerLifecyclesRequest.AddRange(LoadedDataManagerLifecycles);
            LoadedDataManagerLifecycles.Clear();
            return FlushFrameBatchAsync();
        }

        private bool isEndOfFrameBatchScheduled = false;

        private async UniTask FlushFrameBatchAsync()
        {
            if (this.DataManagerLifecyclesRequest.Count == 0 || this.isEndOfFrameBatchScheduled) return;
            this.isEndOfFrameBatchScheduled = true;
            try
            {
                await UniTask.WaitForEndOfFrame();

                await HandleLoadDataRequests();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                this.isEndOfFrameBatchScheduled = false;

                _ = FlushFrameBatchAsync();
            }
        }

        private async UniTask HandleLoadDataRequests()
        {
            var dataManagerLifecycles = new List<IDataManagerLifecycle>(this.DataManagerLifecyclesRequest);
            this.DataManagerLifecyclesRequest.Clear();
            // Start initialize
            foreach (var request in dataManagerLifecycles)
            {
                request.StartInitialize();
            }

            var loadDataRequests = new List<IInitializeDataOnStart>();
            var loadingDataTasks = new List<UniTask>();
            foreach (var request in dataManagerLifecycles)
            {
                if (request is IInitializeDataOnStart initializeDataOnStart)
                {
                    loadDataRequests.Add(initializeDataOnStart);
                    loadingDataTasks.Add(this.GetDataInternal(initializeDataOnStart.GetDataType()));
                }
            }

            // Load all data in parallel
            await UniTask.WhenAll(loadingDataTasks);

            // Provide loaded data to requests
            foreach (var request in loadDataRequests)
            {
                if (this.userDataCache.TryGetValue(request.GetDataType().Name, out var data))
                {
                    request.InitializeData(data);
                }
            }

            // Finalize
            foreach (var request in dataManagerLifecycles)
            {
                request.OnDataInitialized();
                this.LoadedDataManagerLifecycles.Add(request);
            }
        }


        private static bool IsLocalData(Type type)
        {
            return typeof(ILocalData).IsAssignableFrom(type);
        }

        public async UniTask<T> Get<T>() where T : class, IUserData, new()
        {
            await this.IsReady.Task;
            var type = typeof(T);
            var value = await this.GetDataInternal(type);

            return value as T;
        }

        private async UniTask<IUserData> GetDataInternal(Type type)
        {
            if (this.userDataCache.TryGetValue(type.Name, out var value)) return value;
            if (IsLocalData(type))
            {
                var uniTask = this.handleLocalDataService.Value.Load(type);
                value = (IUserData)await uniTask;
            }
            else
                value = Activator.CreateInstance(type) as IUserData;

            this.userDataCache.Add(type.Name, value);

            return value;
        }

        public void Tick()
        {
        }
    }
}
