namespace caojweldjflwendl.Scripts.UIModule.ScreenFlow.Signals
{
    using caojweldjflwendl.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;

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