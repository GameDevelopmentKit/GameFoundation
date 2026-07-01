namespace DataManager.MasterData
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintController;
    using DataManager.LocalSave;
    using DataManager.LocalSave.Encryption;
    using DataManager.LocalSave.Handler;
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
        private readonly SignalBus                            signalBus;
        private readonly LazyInject<IHandleLocalDataServices> handleLocalDataService;
        private readonly LazyInject<BlueprintReaderManager>   blueprintReaderManager;
        private readonly LazyInject<LocalSaveConfig>          dataManagerConfig;
        private readonly LazyInject<IEncryptionService>       encryptionService;

        public UniTaskCompletionSource<bool> IsReady { get; } = new();

        private readonly Dictionary<string, IUserData> userDataCache = new();

        protected virtual HashSet<IDataManagerLifecycle> DataManagerLifecyclesRequest { get; } = new();
        protected virtual HashSet<Type>                  LoadedDataManagerTypes       { get; } = new();

        private bool isEndOfFrameBatchScheduled = false;

        public MasterDataManager(SignalBus signalBus, LazyInject<IHandleLocalDataServices> handleLocalDataService,
            LazyInject<BlueprintReaderManager> blueprintReaderManager, LazyInject<LocalSaveConfig> dataManagerConfig,
            LazyInject<IEncryptionService> encryptionService)
        {
            this.signalBus              = signalBus;
            this.handleLocalDataService = handleLocalDataService;
            this.blueprintReaderManager = blueprintReaderManager;
            this.dataManagerConfig      = dataManagerConfig;
            this.encryptionService      = encryptionService;
            this.signalBus.Subscribe<MasterDataRegisterSignal>(signal =>
                RegisterDataManagerLifecycle(signal.DataManager));
        }

        public async UniTask Initialize()
        {
            if (this.IsReady.Task.Status == UniTaskStatus.Succeeded) return;

            try
            {
                //Todo refactor when implement load user data from remote flow later
                this.userDataCache.Clear();

                // Migrate legacy PlayerPrefs data to new provider-based system.
                // Must run BEFORE InitializeAsync() so the service finds correct data.
                if (this.handleLocalDataService.Value is HandleLocalDataServices concreteService)
                {
                    await LegacyPlayerPrefsMigrator.MigrateIfNeeded(
                        this.dataManagerConfig.Value,
                        concreteService.PrimaryProvider,
                        this.encryptionService.Value);
                }

                await this.handleLocalDataService.Value.InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                this.IsReady.TrySetException(e);
            }
        }

        public async UniTask InitializeAllRegisteredDataManagers()
        {
            if (this.IsReady.Task.Status == UniTaskStatus.Succeeded) return;
            
            await this.blueprintReaderManager.Value.LoadBlueprint();

            await FlushBatchDataRequestsAsync(force: true);

            this.IsReady.TrySetResult(true);
            this.signalBus.Fire<MasterDataReadySignal>();
        }

        public void RegisterDataManagerLifecycle(IDataManagerLifecycle dataManager)
        {
            this.DataManagerLifecyclesRequest.Add(dataManager);

            if (this.IsReady.Task.Status == UniTaskStatus.Pending) return;

            // MasterDataManager is ready: batch requests until end of frame, then load them all at once
            _ = FlushBatchDataRequestsAsync(); // fire-and-forget - runs on main thread
        }

        public UniTask SaveAllData()
        {
            return this.handleLocalDataService.Value.SaveCurrentProfile();
        }

        public void DeleteAllData()
        {
            this.handleLocalDataService.Value.DeleteCurrentProfile();
        }

        public UniTask ClearAndReloadAllDataManager()
        {
            DeleteAllData();

            return ReloadAllDataManager();
        }

        public UniTask ReloadAllDataManager()
        {
            // Clear user data cache
            this.userDataCache.Clear();

            var currentDiContainer = this.GetCurrentContainer();

            //dispose all data manager lifecycle
            foreach (var type in this.LoadedDataManagerTypes)
            {
                if (TryResolveLoadedDataManagerLifecycle(currentDiContainer, type, out var dataManagerLifecycle))
                {
                    dataManagerLifecycle.Dispose();
                    DataManagerLifecyclesRequest.Add(dataManagerLifecycle);
                }
            }

            //reload all data manager lifecycle
            this.LoadedDataManagerTypes.Clear();
            return FlushBatchDataRequestsAsync();
        }

        private async UniTask FlushBatchDataRequestsAsync(bool force = false)
        {
            if (this.DataManagerLifecyclesRequest.Count == 0) return;
            try
            {
                if (!force)
                {
                    if (this.isEndOfFrameBatchScheduled) return;
                    this.isEndOfFrameBatchScheduled = true;
                    await UniTask.WaitForEndOfFrame();
                }

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
                    this.LoadedDataManagerTypes.Add(request.GetType());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                this.isEndOfFrameBatchScheduled = false;

                _ = FlushBatchDataRequestsAsync();
            }
        }

        private static bool TryResolveLoadedDataManagerLifecycle(DiContainer container, Type concreteType,
            out IDataManagerLifecycle dataManagerLifecycle)
        {
            if (TryResolveDataManagerLifecycle(container, concreteType, concreteType, out dataManagerLifecycle))
            {
                return true;
            }

            foreach (var contractType in concreteType.GetInterfaces())
            {
                if (ShouldSkipLifecycleContract(contractType))
                {
                    continue;
                }

                if (TryResolveDataManagerLifecycle(container, contractType, concreteType, out dataManagerLifecycle))
                {
                    return true;
                }
            }

            dataManagerLifecycle = null;
            return false;
        }

        private static bool TryResolveDataManagerLifecycle(DiContainer container, Type contractType,
            Type concreteType, out IDataManagerLifecycle dataManagerLifecycle)
        {
            dataManagerLifecycle = null;

            try
            {
                if (!container.HasBinding(contractType))
                {
                    return false;
                }

                if (container.TryResolve(contractType) is not IDataManagerLifecycle resolvedLifecycle)
                {
                    return false;
                }

                if (!concreteType.IsInstanceOfType(resolvedLifecycle))
                {
                    return false;
                }

                dataManagerLifecycle = resolvedLifecycle;
                return true;
            }
            catch (ZenjectException)
            {
                return false;
            }
        }

        private static bool ShouldSkipLifecycleContract(Type contractType)
        {
            return contractType == typeof(IDataManagerLifecycle) ||
                   contractType == typeof(IInitializeDataOnStart) ||
                   contractType == typeof(IDisposable) ||
                   contractType == typeof(IInitializable) ||
                   contractType == typeof(ITickable) ||
                   contractType == typeof(IFixedTickable) ||
                   contractType == typeof(ILateTickable);
        }


        public async UniTask<T> Get<T>() where T : class, IUserData, new()
        {
            await this.IsReady.Task;
            var type  = typeof(T);
            var value = await this.GetDataInternal(type);

            return value as T;
        }

        private async UniTask<IUserData> GetDataInternal(Type type)
        {
            if (this.userDataCache.TryGetValue(type.Name, out var value)) return value;

            var uniTask = this.handleLocalDataService.Value.LoadData(type);
            value = (IUserData)await uniTask;

            this.userDataCache.Add(type.Name, value);

            return value;
        }

        public void Tick()
        {
        }
    }
}
