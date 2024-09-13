using GDKConfig = Models.GDKConfig;

#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.Scripts
{
    using GameFoundation.BlueprintFlow;
    using GameFoundation.DI;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.Utilities.LoadImage;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameFoundation.Signals;
    using GameFoundation.UIModule.UIModule;
    using GameFoundation.Utilities.ApplicationServices;
    using GameFoundation.Utilities.GameQueueAction;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;

    public static class GameFoundationVContainer
    {
        public static void RegisterGameFoundation(this IContainerBuilder builder)
        {
            builder.Register<VContainerWrapper>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<VContainerAdapter>(Lifetime.Scoped).AsImplementedInterfaces();

            builder.RegisterSignalBus();
            builder.RegisterBlueprints();
            builder.RegisterScreenManager();
            builder.RegisterApplicationServices();
            builder.RegisterGameQueueActionService();

            builder.RegisterInstance(Resources.Load<GDKConfig>("GameConfigs/GDKConfig"));

            builder.Register<GameAssets>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ObjectPoolManager>(Lifetime.Singleton);
            builder.Register<AudioService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LogService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HandleLocalUserDataServices>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LoadImageHelper>(Lifetime.Singleton);
            builder.RegisterComponentOnNewGameObject<Fps>(Lifetime.Singleton);

            builder.DeclareSignal<UserDataLoadedSignal>();
        }
    }
}
#endif