namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using TheOne.Logging;

    public abstract class BaseCloudUserDataService : BaseHandleUserDataServices, ICloudUserDataService
    {
        protected BaseCloudUserDataService(ILoggerManager loggerManager) : base(loggerManager)
        {
        }

        async UniTask ICloudUserDataService.SaveAllAsync()
        {
            await this.SaveAll();
            await this.SaveAllAsync();
        }

        async UniTask ICloudUserDataService.FetAllAsync()
        {
            await this.FetchAllAsync();
        }

        UniTask ICloudUserDataService.SaveAsync<T>(T data, bool force)
        {
            return this.Save(data, force);
        }

        UniTask<T> ICloudUserDataService.LoadAsync<T>()
        {
            return this.Load<T>();
        }

        async UniTask<ICloudData[]> ICloudUserDataService.LoadAsync(Type[] types)
        {
            return (await this.Load(types)).Cast<ICloudData>().ToArray();
        }

        protected abstract UniTask SaveAllAsync();

        protected abstract UniTask FetchAllAsync();
    }
}