namespace GameFoundation.Scripts.AppUpdate
{
    using Cysharp.Threading.Tasks;

    public interface IAppUpdateConfigProvider
    {
        UniTask<AppUpdateConfig> GetConfigAsync();
    }

    public interface IAppUpdateTextProvider
    {
        AppUpdateTexts GetTexts(AppUpdateDecision decision);
    }

    public interface IAppUpdatePromptService
    {
        UniTask<AppUpdatePromptResult> ShowPromptAsync(AppUpdateDecision decision, AppUpdateTexts texts);
    }

    public enum AppUpdatePromptResult
    {
        Later,
        Update,
    }
}
