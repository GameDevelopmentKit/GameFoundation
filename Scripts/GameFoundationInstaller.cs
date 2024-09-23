namespace GameFoundation.Scripts
{
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using VContainer;
    using VContainer.Signals.GameFoundation.Signals;

    public static class GameFoundationInstaller
    {
        public static void InstallGameFoundation(this IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(VContainerExtensions.SetCurrentContainer);
            builder.InstallSignalBus();
            builder.Register<GameAssets>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ObjectPoolManager>(Lifetime.Singleton).AsSelf();

            builder.Register<AudioService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LogService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SceneDirector>(Lifetime.Singleton);
            builder.Register<ScreenManager>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.DeclareSignal<StartLoadingNewSceneSignal>();
            builder.DeclareSignal<FinishLoadingNewSceneSignal>();
            builder.DeclareSignal<ScreenCloseSignal>();
            builder.DeclareSignal<ScreenShowSignal>();
            builder.DeclareSignal<ScreenHideSignal>();
            builder.DeclareSignal<ScreenSelfDestroyedSignal>();
            builder.DeclareSignal<PopupShowedSignal>();
            builder.DeclareSignal<PopupHiddenSignal>();
            builder.DeclareSignal<PopupBlurBgShowedSignal>();
        }
    }
}