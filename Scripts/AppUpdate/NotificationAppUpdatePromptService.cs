namespace GameFoundation.Scripts.AppUpdate
{
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.CommonScreen;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;

    public class NotificationAppUpdatePromptService : IAppUpdatePromptService
    {
        private readonly IScreenManager screenManager;

        public NotificationAppUpdatePromptService(IScreenManager screenManager)
        {
            this.screenManager = screenManager;
        }

        public async UniTask<AppUpdatePromptResult> ShowPromptAsync(AppUpdateDecision decision, AppUpdateTexts texts)
        {
            var result = new UniTaskCompletionSource<AppUpdatePromptResult>();

            await this.screenManager.OpenScreen<NotificationPopupPresenter, NotificationPopupModel>(
                new NotificationPopupModel
                {
                    Title            = texts.Title,
                    Content          = texts.Message,
                    Type             = decision.IsForceUpdate ? NotificationType.Close : NotificationType.Option,
                    OkButtonText     = texts.UpdateButton,
                    CancelButtonText = texts.LaterButton,
                    OkAction         = () => result.TrySetResult(AppUpdatePromptResult.Update),
                    CancelAction     = () => result.TrySetResult(AppUpdatePromptResult.Later),
                });

            return await result.Task;
        }
    }
}
