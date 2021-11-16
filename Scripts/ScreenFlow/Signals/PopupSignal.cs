namespace GameFoundation.Scripts.ScreenFlow.Signals
{
    using GameFoundation.Scripts.ScreenFlow.BaseScreen.Presenter;

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