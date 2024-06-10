namespace DataManager.MasterData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintController;
    using DataManager.LocalData;
    using DataManager.UserData;
    using UnityEngine;
    using Zenject;

    public class MasterDataManager
    {
        private readonly SignalBus               signalBus;
        private readonly IHandleLocalDataServices handleLocalDataService;
        private readonly BlueprintReaderManager  blueprintReaderManager;

        public UniTaskCompletionSource<bool> IsReady { get; } = new();

        private readonly Dictionary<string, IUserData> userDataCache = new();

        protected virtual List<Type> PreLoadUserDataTypes { get; } = new();
        public List<UniTask> LoadingUserDataTasks { get; } = new();


        public MasterDataManager(SignalBus signalBus, IHandleLocalDataServices handleLocalDataService, BlueprintReaderManager blueprintReaderManager)
        {
            this.signalBus              = signalBus;
            this.handleLocalDataService  = handleLocalDataService;
            this.blueprintReaderManager = blueprintReaderManager;
        }

        public async UniTask InitializeData()
        {
            if (this.IsReady.Task.Status == UniTaskStatus.Succeeded) return;

            try
            {
                await UniTask.WhenAll(this.LoadUserData(), this.blueprintReaderManager.LoadBlueprint());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                this.IsReady.TrySetException(e);
            }

            this.signalBus.Fire<MasterDataReadySignal>();
            this.IsReady.TrySetResult(true);
        }

        private async UniTask LoadUserData()
        {
            //Todo implement load user data from remote flow later
            this.userDataCache.Clear();

            // preload all user data that is LocalData type
            var localDataTypes = this.PreLoadUserDataTypes.Where(IsLocalData).ToArray();
            var localDatas     = await this.handleLocalDataService.Load(localDataTypes);
            foreach (var data in localDatas)
            {
                this.userDataCache.Add(data.GetType().Name, data as IUserData);
            }
        }

        private static bool IsLocalData(Type type) { return typeof(ILocalData).IsAssignableFrom(type); }

        public async UniTask<T> Get<T>() where T : class, IUserData, new()
        {
            await this.IsReady.Task;
            var type = typeof(T);
            if (!this.userDataCache.TryGetValue(type.Name, out var value))
            {
                if (IsLocalData(type))
                {
                    var uniTask = this.handleLocalDataService.Load(type);
                    this.LoadingUserDataTasks.Add(uniTask);
                    value = (T)await uniTask;
                    this.LoadingUserDataTasks.Remove(uniTask);
                }
                else
                    value = new T();

                this.userDataCache.Add(type.Name, value);
            }

            return value as T;
        }
    }
}