namespace GameFoundation.Scripts.AppUpdate
{
    using Cysharp.Threading.Tasks;

    public class DisabledAppUpdateConfigProvider : IAppUpdateConfigProvider
    {
        public UniTask<AppUpdateConfig> GetConfigAsync()
        {
            return UniTask.FromResult<AppUpdateConfig>(null);
        }
    }
}
