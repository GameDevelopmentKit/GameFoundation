namespace DataManager.UserData
{
    using System;
    using Cysharp.Threading.Tasks;
    using DataManager.MasterData;
    using Zenject;

    public abstract class BaseDataManager<TData> : IInitializeDataOnStart where TData : class, IUserData, new()
    {
        protected readonly SignalBus SignalBus;
        protected          TData     Data { get; private set; }

        public BaseDataManager(SignalBus signalBus)
        {
            this.SignalBus = signalBus;

            _ = RegisterMasterData();
        }
        private async UniTask RegisterMasterData()
        {
            await UniTask.WaitForEndOfFrame();
            this.SignalBus.Fire(new MasterDataRegisterSignal { DataManager = this });
        }

        public virtual void StartInitialize() { }

        public virtual void InitializeData(IUserData userData)
        {
            if (userData is not TData userDataAsTData) return;
            this.Data = userDataAsTData;
        }

        public virtual void OnDataInitialized() { }

        Type IInitializeDataOnStart.GetDataType() { return typeof(TData); }
    }
}
