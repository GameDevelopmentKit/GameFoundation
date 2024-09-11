#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.UIModule.UIModule
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using GameFoundation.Signals;
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