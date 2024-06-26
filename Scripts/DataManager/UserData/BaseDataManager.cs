namespace DataManager.UserData
{
    using System;
    using DataManager.MasterData;

    public abstract class BaseDataManager<TData> : IInitializeDataOnStart where TData : class, IUserData, new()
    {
        protected readonly MasterDataManager MasterDataManager;
        protected          TData             Data { get; private set; }

        public BaseDataManager(MasterDataManager masterDataManager)
        {
            this.MasterDataManager = masterDataManager;
            this.MasterDataManager.RegisterPreloadUserData(this);
        }
        public virtual void InitializeData(IUserData userData)
        {
            if (userData is not TData userDataAsTData) return;
            this.Data = userDataAsTData;
            this.OnDataLoaded();
        }

        protected virtual void OnDataLoaded() { }

        Type IInitializeDataOnStart.GetDataType() { return typeof(TData); }
    }
}