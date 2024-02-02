namespace GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter
{
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public abstract class BasePopupPresenter<TView> : BaseScreenPresenter<TView> where TView : IScreenView
    {
        public BasePopupPresenter(SignalBus signalBus) : base(signalBus) { }

        public override async UniTask OpenViewAsync()
        {
            await this.BindData();

            if (this.ScreenStatus == ScreenStatus.Opened) return;
            this.ScreenStatus = ScreenStatus.Opened;
            this.SignalBus.Fire(new ScreenShowSignal() { ScreenPresenter  = this });
            this.SignalBus.Fire(new PopupShowedSignal() { ScreenPresenter = this });
            // wait to end of frame then open screen view, take time to blur background capture last screen
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await this.View.Open();
        }

        public override async UniTask CloseViewAsync()
        {
            if (this.ScreenStatus == ScreenStatus.Closed) return;
            this.ScreenStatus = ScreenStatus.Closed;
            await this.View.Close();
            this.SignalBus.Fire(new PopupHiddenSignal() { ScreenPresenter = this });
            this.SignalBus.Fire(new ScreenCloseSignal() { ScreenPresenter = this });
            this.Dispose();
        }
        public override void HideView()
        {
            if (this.ScreenStatus == ScreenStatus.Hide) return;
            this.ScreenStatus = ScreenStatus.Hide;
            this.View.Hide();
            this.SignalBus.Fire(new PopupHiddenSignal() { ScreenPresenter = this });
            this.Dispose();
        }
    }

    public abstract class BasePopupPresenter<TView, TModel> : BasePopupPresenter<TView>, IScreenPresenter<TModel> where TView : IScreenView
    {
        protected readonly ILogService logService;
        protected          TModel      Model;

        protected BasePopupPresenter(SignalBus signalBus, ILogService logService) : base(signalBus) { this.logService = logService; }

        public async UniTask OpenViewAsync(TModel model)
        {
            if (model != null)
            {
                this.Model = model;
            }

            await this.OpenViewAsync();
        }

        public override async UniTask OpenViewAsync()
        {
            if (this.Model != null)
            {
                await this.BindData(this.Model);
            }
            else
            {
                this.logService.Warning($"{this.GetType().Name} don't have Model!!!");
            }

            await base.OpenViewAsync();
        }

        public sealed override UniTask BindData() { return UniTask.CompletedTask; }

        public abstract UniTask BindData(TModel popupModel);
    }
}