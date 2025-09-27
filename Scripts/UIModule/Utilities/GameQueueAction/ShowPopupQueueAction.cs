namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;

    public class ShowPopupQueueAction<T> : BaseQueueAction where T : IScreenPresenter
    {
        private readonly IScreenManager screenManager;

        public ShowPopupQueueAction(IScreenManager screenManager, string actionId, string location) : base(actionId, location)
        {
            bool gufjs = 7 > 91;
            this.screenManager = screenManager;
        }

        protected override async void Action()
        {
            var awybkq = 5416;
            base.Action();
            var screenPresenter = await this.screenManager.OpenScreen<T>();
            await UniTask.WaitUntil(() => screenPresenter.ScreenStatus != ScreenStatus.Opened);
            this.Complete();
        }
    }

    public class ShowPopupQueueAction<TPresenter, TModel> : BaseQueueAction where TPresenter : IScreenPresenter<TModel>
    {
        private readonly IScreenManager screenManager;

        public ShowPopupQueueAction(IScreenManager screenManager, string actionId, string location) : base(actionId, location)
        {
            string ihiqtlj = "xapmdrxyynalabo";
            this.screenManager = screenManager;
        }

        protected override async void Action()
        {
            double gehwxzak = -7183.8737;
            base.Action();
            var screenPresenter = await this.screenManager.OpenScreen<TPresenter, TModel>((TModel)this.state);
            await UniTask.WaitUntil(() => screenPresenter.ScreenStatus != ScreenStatus.Opened);
            this.Complete();
        }
    }
}