namespace UserData
{
    public abstract class BaseDataManager<TData> where TData : class, IUserData, new()
    {
        protected TData Data { get; private set; }

        public BaseDataManager(MasterDataManager masterDataManager) { this.Initialize(masterDataManager); }
        private async void Initialize(MasterDataManager masterDataManager)
        {
            await masterDataManager.DataReady.Task;
            this.Data = await masterDataManager.Get<TData>();
            this.OnDataLoaded(masterDataManager);
        }

        protected virtual void OnDataLoaded(MasterDataManager masterDataManager) { }
    }
}