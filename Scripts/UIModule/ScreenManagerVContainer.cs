#if GDK_VCONTAINER
#nullable enable
namespace caojweldjflwendl.UIModule.UIModule
{
    using caojweldjflwendl.Scripts.UIModule.ScreenFlow.Managers;
    using caojweldjflwendl.Scripts.UIModule.ScreenFlow.Signals;
    using caojweldjflwendl.Scripts.UIModule.Utilities.UIStuff;
    using caojweldjflwendl.Signals;
    using VContainer;

    public static class ScreenManagerVContainer
    {
        public static void RegisterScreenManager(this IContainerBuilder builder)
        {
            builder.Register<SceneDirector>(Lifetime.Singleton);
            builder.Register<ScreenManager>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.DeclareSignal<StartLoadingNewSceneSignal>();
            builder.DeclareSignal<FinishLoadingNewSceneSignal>();
            builder.DeclareSignal<ScreenCloseSignal>();
            builder.DeclareSignal<ScreenShowSignal>();
            builder.DeclareSignal<ScreenHideSignal>();
            builder.DeclareSignal<ManualInitScreenSignal>();
            builder.DeclareSignal<ScreenSelfDestroyedSignal>();
            builder.DeclareSignal<PopupShowedSignal>();
            builder.DeclareSignal<PopupHiddenSignal>();
            builder.DeclareSignal<PopupBlurBgShowedSignal>();

            builder.Register<AutoCooldownTimer>(Lifetime.Transient);
        }
    }
}
#endif