namespace GameFoundation.Scripts.UIModule.ScreenFlow.Signals
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;

    public class PopupShowedSignal
    {
        public IScreenPresenter ScreenPresenter;
    }

    public class PopupHiddenSignal
    {
        public IScreenPresenter ScreenPresenter;
    }

    public class PopupBlurBgShowedSignal
    {
    }
}