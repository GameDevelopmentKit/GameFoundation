namespace GameFoundation.Scripts.UIModule.MVP
{
    /// <summary>
    /// Represent logic affect to views
    /// </summary>
    public interface IUIPresenter
    {
        public void SetView(IUIView viewInstance);
        void        OnViewReady() { }
    }

    public interface IUIPresenterWithModel<TModel> : IUIPresenter
    {
        void Init(IUIView viewInstance, TModel param);
    }
}