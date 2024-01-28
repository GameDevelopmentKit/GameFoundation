namespace UserData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BlueprintFlow.BlueprintControlFlow;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Interfaces;
    using GameFoundation.Scripts.Utilities.UserData;
    using UnityEngine;
    using Zenject;

    public class MasterDataManager
    {
        private readonly SignalBus               signalBus;
        private readonly IHandleUserDataServices handleUserDataService;
        private readonly BlueprintReaderManager  blueprintReaderManager;

        public UniTaskCompletionSource<bool> DataReady { get; } = new();

        private readonly Dictionary<string, IUserData> userDataCache = new();

        protected virtual List<Type> PreLoadUserDataTypes { get; } = new();

        public MasterDataManager(SignalBus signalBus, IHandleUserDataServices handleUserDataService, BlueprintReaderManager blueprintReaderManager)
        {
            this.signalBus              = signalBus;
            this.handleUserDataService  = handleUserDataService;
            this.blueprintReaderManager = blueprintReaderManager;
        }

        public async UniTask InitializeData()
        {
            if (this.DataReady.Task.Status == UniTaskStatus.Succeeded) return;

            try
            {
                await UniTask.WhenAll(this.LoadUserData(), this.blueprintReaderManager.LoadBlueprint());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                this.DataReady.TrySetException(e);
            }

            this.signalBus.Fire<UserDataLoadedSignal>();
            this.DataReady.TrySetResult(true);
        }

        private async UniTask LoadUserData()
        {
            //Todo implement load user data from remote flow later
            this.userDataCache.Clear();
            
            // preload all user data that is LocalData type
            var localDataTypes = this.PreLoadUserDataTypes.Where(IsLocalData).ToArray();
            var localDatas = await this.handleUserDataService.Load(localDataTypes);
            foreach (var data in localDatas)
            {
                this.userDataCache.Add(data.GetType().Name, data as IUserData);
            }
        }

        private static bool IsLocalData(Type type) { return typeof(ILocalData).IsAssignableFrom(type); }

        public async UniTask<T> Get<T>() where T : class, IUserData, new()
        {
            await this.DataReady.Task;
            var type = typeof(T);
            if (!this.userDataCache.TryGetValue(type.Name, out var value))
            {
                value = IsLocalData(type) ? (T)await this.handleUserDataService.Load(type) : new T();
                this.userDataCache.Add(type.Name, value);
            }

            return value as T;
        }
    }
}