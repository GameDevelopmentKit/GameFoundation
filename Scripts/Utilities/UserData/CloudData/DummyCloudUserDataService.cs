#if !THEONE_CLOUDSAVE
namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using Cysharp.Threading.Tasks;
    using TheOne.Logging;

    public sealed class DummyCloudUserDataService : BaseCloudUserDataService
    {
        public DummyCloudUserDataService(ILoggerManager loggerManager) : base(loggerManager)
        {
        }

        protected override UniTask SaveJsons(params (string key, string json)[] values)
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask<string[]> LoadJsons(params string[] keys)
        {
            return UniTask.FromResult(Array.Empty<string>());
        }

        protected override UniTask SaveAllAsync()
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask FetchAllAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
#endif