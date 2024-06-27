namespace DataManager.MasterData
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintController;
    using DataManager.LocalData;
    using DataManager.UserData;
    using UnityEngine;
    using Zenject;

    public class MasterDataManager
    {
        private readonly SignalBus                signalBus;
        private readonly IHandleLocalDataServices handleLocalDataService;
        private readonly BlueprintReaderManager   blueprintReaderManager;

        public UniTaskCompletionSource<bool> IsReady { get; } = new();

        private readonly Dictionary<string, IUserData> userDataCache = new();

        protected virtual List<IInitializeDataOnStart> PreloadUserDataTypes { get; } = new();
        public            List<UniTask>                LoadingUserDataTasks { get; } = new();

        public MasterDataManager(SignalBus signalBus, IHandleLocalDataServices handleLocalDataService, BlueprintReaderManager blueprintReaderManager)
        {
            this.signalBus              = signalBus;
            this.handleLocalDataService = handleLocalDataService;
            this.blueprintReaderManager = blueprintReaderManager;
        }

        public async UniTask InitializeData()
        {
            if (this.IsReady.Task.Status == UniTaskStatus.Succeeded) return;

            try
            {
                //Todo refactor when implement load user data from remote flow later
                this.userDataCache.Clear();
                
                // load all pref load data and blueprint
                var preloadDataTask = UniTask.WhenAll(this.PreloadUserDataTypes.Select(x => this.GetDataInternal(x.GetDataType())));
                await UniTask.WhenAll(preloadDataTask, this.blueprintReaderManager.LoadBlueprint());
                
                foreach (var preloadUserData in this.PreloadUserDataTypes)
                {
                    if (this.userDataCache.TryGetValue(preloadUserData.GetDataType().Name, out var data))
                    {
                        preloadUserData.InitializeData(data);
                    }
                }
                
                this.PreloadUserDataTypes.Clear();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                this.IsReady.TrySetException(e);
            }

            this.signalBus.Fire<MasterDataReadySignal>();
            this.IsReady.TrySetResult(true);
        }

        public void RegisterPreloadUserData(IInitializeDataOnStart initializeDataOnStart)
        {
            if (this.IsReady.Task.Status != UniTaskStatus.Succeeded)
            {
                this.PreloadUserDataTypes.Add(initializeDataOnStart);
            }
            else
            {
                this.GetDataInternal(initializeDataOnStart.GetDataType()).ContinueWith(async data =>
                {
                    // return data after constructor is called
                    await UniTask.Yield();
                    initializeDataOnStart.InitializeData(data);
                });
            }
        }

        private static bool IsLocalData(Type type) { return typeof(ILocalData).IsAssignableFrom(type); }

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
            if (IsLocalData(type))
            {
                var uniTask = this.handleLocalDataService.Load(type);
                this.LoadingUserDataTasks.Add(uniTask);
                value = (IUserData)await uniTask;
                this.LoadingUserDataTasks.Remove(uniTask);
            }
            else
                value = Activator.CreateInstance(type) as IUserData;

            this.userDataCache.Add(type.Name, value);

            return value;
        }
    }
}