namespace GameFoundation.Scripts.ScreenFlow.BaseScreen.Presenter
{
    using GameFoundation.Scripts.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.ScreenFlow.Signals;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public abstract class BasePopupPresenter<TView> : BaseScreenPresenter<TView> where TView : IScreenView
    {
        public BasePopupPresenter(SignalBus signalBus) : base(signalBus) { }

        protected override void OnViewReady()
        {
            base.OnViewReady();
            this.View.ViewDidOpen  += this.OnViewDidOpen;
            this.View.ViewDidClose += this.OnViewDidClose;
        }
        
        private            void OnViewDidOpen()   { this.SignalBus.Fire(new PopupShowedSignal() { ScreenPresenter = this }); }
        private            void OnViewDidClose()  { this.SignalBus.Fire(new PopupHiddenSignal() { ScreenPresenter = this }); }
        protected override void OnViewDestroyed()
        {
            base.OnViewDestroyed();
            this.SignalBus.Fire(new PopupHiddenSignal()
            {
                ScreenPresenter = this
            }); 
        }
    }

    public abstract class BasePopupPresenter<TView, TModel> : BasePopupPresenter<TView>, IScreenPresenter<TModel> where TView : IScreenView 
    {
        protected readonly ILogService logService;
        protected        TModel      Model;
        
        protected BasePopupPresenter(SignalBus signalBus, ILogService logService) : base(signalBus) { this.logService = logService; }

        public void OpenView(TModel model)
        {
            if (model != null)
            {
                this.Model = model;
            }
            this.OpenView();
        }
        
        public override void OpenView()
        {
            if (this.Model != null)
            {
                this.BindData(this.Model);
            }
            else
            {
                this.logService.Warning($"{this.GetType().Name} don't have Model!!!");
            }
            base.OpenView();
        }
        
        public sealed override void BindData() { }

        public abstract void BindData(TModel popupModel);
    }
}