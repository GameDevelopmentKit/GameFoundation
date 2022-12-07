namespace GameFoundation.Editor.Tools.ViewCreatorWizard
{
    public partial class ViewCreatorWizard
    {
        private const string ITEM_VIEW_TEMPLATE =
            @"namespace X_NAME_SPACE
{
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.MVP;
    
    public class X_MODEL_NAME
    {
    }
    
    public class X_VIEW_NAME : TViewMono
    {
    }
    
    public class X_PRESENTER_NAME : BaseUIItemPresenter<X_VIEW_NAME ,X_MODEL_NAME>
    {
        public X_PRESENTER_NAME(IGameAssets gameAssets) : base(gameAssets) { }
        public override void BindData(X_MODEL_NAME param) { }
    }
}";

        private const string POPUP_VIEW_TEMPLATE =
            @"namespace X_NAME_SPACE
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class X_MODEL_NAME
    {
    }

    public class X_VIEW_NAME : BaseView
    {
    }

    [PopupInfo(nameof(X_VIEW_NAME))]
    public class X_PRESENTER_NAME : BasePopupPresenter<X_VIEW_NAME, X_MODEL_NAME>
    {
        public X_PRESENTER_NAME(SignalBus signalBus, ILogService logService) : base(signalBus, logService) { }
        public override void BindData(X_MODEL_NAME popupModel) { }
    }
}";

        private const string POPUP_VIEW_NON_MODEL_TEMPLATE =
            @"namespace X_NAME_SPACE
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using Zenject;

    public class X_VIEW_NAME : BaseView
    {
    }

    [PopupInfo(nameof(X_VIEW_NAME))]
    public class X_PRESENTER_NAME : BasePopupPresenter<X_VIEW_NAME>
    {
        public X_PRESENTER_NAME(SignalBus signalBus) : base(signalBus) { }
        public override void BindData() { }
    }
}";

        private const string SCREEN_VIEW_TEMPLATE =
            @"namespace X_NAME_SPACE
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class X_MODEL_NAME
    {
    }

    public class X_VIEW_NAME : BaseView
    {
    }

    [ScreenInfo(nameof(X_VIEW_NAME))]
    public class X_PRESENTER_NAME : BaseScreenPresenter<X_VIEW_NAME, X_MODEL_NAME>
    {
        public X_PRESENTER_NAME(SignalBus signalBus, ILogService logger) : base(signalBus, logger) { }
        public override void BindData(X_MODEL_NAME screenModel) { }
    }
}";

        private const string SCREEN_VIEW_NON_MODEL_TEMPLATE =
            @"namespace X_NAME_SPACE
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using Zenject;

    public class X_VIEW_NAME : BaseView
    {
    }

    [ScreenInfo(nameof(X_VIEW_NAME))]
    public class X_PRESENTER_NAME : BaseScreenPresenter<X_VIEW_NAME>
    {
        public X_PRESENTER_NAME(SignalBus signalBus) : base(signalBus) { }
        public override void BindData() { }
    }
}";
    }
}