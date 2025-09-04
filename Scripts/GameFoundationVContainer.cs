using GDKConfig = Models.GDKConfig;

#if GDK_VCONTAINER
#nullable enable
namespace caojweldjflwendl.Scripts
{
    using caojweldjflwendl.BlueprintFlow;
    using caojweldjflwendl.DI;
    using caojweldjflwendl.Scripts.AssetLibrary;
    using caojweldjflwendl.Scripts.UIModule.Utilities.LoadImage;
    using caojweldjflwendl.Scripts.Utilities;
    using caojweldjflwendl.Scripts.Utilities.Extension;
    using caojweldjflwendl.Scripts.Utilities.LogService;
    using caojweldjflwendl.Scripts.Utilities.ObjectPool;
    using caojweldjflwendl.Scripts.Utilities.UserData;
    using caojweldjflwendl.Signals;
    using caojweldjflwendl.UIModule.UIModule;
    using caojweldjflwendl.Utilities.ApplicationServices;
    using caojweldjflwendl.Utilities.GameQueueAction;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;

    public static class GameFoundationVContainer
    {
        public static void RegisterGameFoundation(this IContainerBuilder builder, Transform rootTransform)
        {
            builder.Register<VContainerWrapper>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<VContainerAdapter>(Lifetime.Scoped).AsImplementedInterfaces();

            builder.RegisterSignalBus();
            builder.RegisterBlueprints();
            builder.RegisterScreenManager();
            builder.RegisterApplicationServices(rootTransform);
            builder.RegisterGameQueueActionService();

            builder.RegisterInstance(Resources.Load<GDKConfig>("GameConfigs/GDKConfig"));

            builder.Register<GameAssets>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ObjectPoolManager>(Lifetime.Singleton);
            builder.Register<AudioService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LogService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HandleLocalUserDataServices>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LoadImageHelper>(Lifetime.Singleton);
            builder.RegisterComponentOnNewGameObject<Fps>(Lifetime.Singleton).UnderTransform(rootTransform);
            builder.AutoResolve<Fps>();

            builder.DeclareSignal<UserDataLoadedSignal>();
        }
    }
}
#endif