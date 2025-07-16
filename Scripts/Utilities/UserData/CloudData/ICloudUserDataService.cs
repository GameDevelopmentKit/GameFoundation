namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using Cysharp.Threading.Tasks;

    public interface ICloudUserDataService : IHandleUserDataServices
    {
        UniTask               SaveAllAsync();
        UniTask               FetAllAsync();
        UniTask               SaveAsync<T>(T data, bool force = false) where T : class, ICloudData;
        UniTask<T>            LoadAsync<T>() where T : class, ICloudData;
        UniTask<ICloudData[]> LoadAsync(Type[] types);
    }
}