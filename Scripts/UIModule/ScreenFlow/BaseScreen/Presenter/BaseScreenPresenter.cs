namespace GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter
{
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;
    using Zenject;

    public abstract class BaseScreenPresenter<TView> : IScreenPresenter where TView : IScreenView
    {
        public         string       ScreenId        { get; private set; }
        public virtual bool         IsClosePrevious { get; protected set; } = false;
        public         ScreenStatus ScreenStatus    { get; protected set; } = ScreenStatus.Closed;

        public             TView     View;
        protected readonly SignalBus SignalBus;

        public BaseScreenPresenter(SignalBus signalBus) { this.SignalBus = signalBus; }

        #region Implement IUIPresenter

        [Inject] private ILogService logger;

        [Inject]
        public virtual void Initialize() { }

        public async void SetView(IUIView viewInstance)
        {
            this.View     = (TView)viewInstance;
            this.ScreenId = $"{SceneDirector.CurrentSceneName}/{typeof(TView).Name}";
            if (this.View.IsReadyToUse)
            {
                this.OnViewReady();
            }
            else
            {
                await UniTask.WaitUntil(() => this.View.IsReadyToUse);
                this.OnViewReady();
            }
        }

        public void SetViewParent(Transform parent)
        {
            if (parent == null)
            {
                this.logger.LogWithColor(parent.name + "is null", Color.green);
                return;
            }

            if (this.View.Equals(null)) return;
            this.View.RectTransform.SetParent(parent, false);
        }
        public Transform GetViewParent()  { return this.View.RectTransform.parent; }
        public Transform CurrentTransform => this.View.RectTransform;

        public abstract UniTask BindData();

        public virtual async UniTask OpenViewAsync()
        {
            // Always fill data for screen
            await this.BindData();

            if (this.ScreenStatus == ScreenStatus.Opened) return;
            this.ScreenStatus = ScreenStatus.Opened;
            this.SignalBus.Fire(new ScreenShowSignal() { ScreenPresenter = this });
            await this.View.Open();
        }

        public virtual async UniTask CloseViewAsync()
        {
            if (this.ScreenStatus == ScreenStatus.Closed) return;
            this.ScreenStatus = ScreenStatus.Closed;
            await this.View.Close();
            this.SignalBus.Fire(new ScreenCloseSignal() { ScreenPresenter = this });
            this.Dispose();
        }

        public virtual async void CloseView() { await this.CloseViewAsync(); }

        public virtual void HideView()
        {
            if (this.ScreenStatus == ScreenStatus.Hide) return;
            this.ScreenStatus = ScreenStatus.Hide;
            this.View.Hide();
            // this.SignalBus.Fire(new ScreenHideSignal() { ScreenPresenter = this }); // Active this signal later, when need
            this.Dispose();
        }
        public virtual void DestroyView()
        {
            if (this.ScreenStatus == ScreenStatus.Destroyed) return;
            this.ScreenStatus = ScreenStatus.Destroyed;
            if (this.View.Equals(null)) return;
            this.Dispose();
            this.View.DestroySelf();
        }

        public virtual void OnOverlap()      { }
        public         int  ViewSiblingIndex { get => this.View.RectTransform.GetSiblingIndex(); set => this.View.RectTransform.SetSiblingIndex(value); }

        #endregion


        protected virtual void OnViewReady()     { this.View.ViewDidDestroy += this.OnViewDestroyed; }
        protected virtual void OnViewDestroyed() { this.SignalBus.Fire(new ScreenSelfDestroyedSignal() { ScreenPresenter = this }); }

        public virtual void Dispose() { }
    }

    public abstract class BaseScreenPresenter<TView, TModel> : BaseScreenPresenter<TView>, IScreenPresenter<TModel> where TView : IScreenView
    {
        protected readonly ILogService Logger;
        protected          TModel      Model;
        protected BaseScreenPresenter(SignalBus signalBus, ILogService logger) : base(signalBus) { this.Logger = logger; }

        public override async UniTask OpenViewAsync()
        {
            if (this.Model != null)
            {
                await this.BindData(this.Model);
            }
            else
            {
                this.Logger.Warning($"{this.GetType().Name} don't have Model!!!");
            }
            await base.OpenViewAsync();
        }
        public virtual async UniTask OpenViewAsync(TModel model)
        {
            if (model != null)
            {
                this.Model = model;
            }

            await this.OpenViewAsync();
        }

        public sealed override UniTask BindData() { return UniTask.CompletedTask; }

        public abstract UniTask BindData(TModel screenModel);
    }
}