namespace UserData
{
    using Cysharp.Threading.Tasks;

    public abstract class BaseDataManager<TData> where TData : class, IUserData, new()
    {
        protected TData Data { get; private set; }

        public BaseDataManager(MasterDataManager masterDataManager) { this.Initialize(masterDataManager).Forget(); }
        private async UniTaskVoid Initialize(MasterDataManager masterDataManager)
        {
            await  UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await masterDataManager.DataReady.Task;
            this.Data = await masterDataManager.Get<TData>();
            this.OnDataLoaded(masterDataManager);
        }

        protected virtual void OnDataLoaded(MasterDataManager masterDataManager) { }
    }
}