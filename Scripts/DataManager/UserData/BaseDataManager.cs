namespace DataManager.UserData
{
    using Cysharp.Threading.Tasks;
    using DataManager.MasterData;

    public abstract class BaseDataManager<TData> where TData : class, IUserData, new()
    {
        protected TData Data { get; private set; }

        public BaseDataManager(MasterDataManager masterDataManager) { this.Initialize(masterDataManager).Forget(); }
        private async UniTaskVoid Initialize(MasterDataManager masterDataManager)
        {
            this.Data = await masterDataManager.Get<TData>();
            await  UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            this.OnDataLoaded(masterDataManager);
        }

        protected virtual void OnDataLoaded(MasterDataManager masterDataManager) { }
    }
}