#if GDK_ZENJECT
using BlueprintServicesInstaller = BlueprintFlow.BlueprintControlFlow.BlueprintServicesInstaller;
using GDKConfig = Models.GDKConfig;

namespace caojweldjflwendl.Scripts
{
    using caojweldjflwendl.DI;
    using caojweldjflwendl.Scripts.AssetLibrary;
    using caojweldjflwendl.Scripts.Models;
    using caojweldjflwendl.Scripts.UIModule.ScreenFlow.Managers;
    using caojweldjflwendl.Scripts.UIModule.Utilities.GameQueueAction;
    using caojweldjflwendl.Scripts.UIModule.Utilities.LoadImage;
    using caojweldjflwendl.Scripts.Utilities;
    using caojweldjflwendl.Scripts.Utilities.ApplicationServices;
    using caojweldjflwendl.Scripts.Utilities.Extension;
    using caojweldjflwendl.Scripts.Utilities.LogService;
    using caojweldjflwendl.Scripts.Utilities.ObjectPool;
    using caojweldjflwendl.Scripts.Utilities.UserData;
    using caojweldjflwendl.Signals;
    using Zenject;

    public class GameFoundationInstaller : Installer<GameFoundationInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesTo<ZenjectWrapper>().AsSingle().CopyIntoAllSubContainers();
            this.Container.BindInterfacesTo<ZenjectAdapter>().AsSingle().CopyIntoAllSubContainers();

            SignalBusInstaller.Install(this.Container);

            this.Container.Bind<GDKConfig>().FromResource("GameConfigs/GDKConfig").AsSingle().NonLazy();

            this.Container.Bind<IGameAssets>().To<GameAssets>().AsCached();
            this.Container.Bind<ObjectPoolManager>().AsCached().NonLazy();

            //Audio service
            this.Container.BindInterfacesTo<AudioService>().AsCached().NonLazy();

            //Service
            this.Container.Bind<ILogService>().To<LogService>().AsSingle().NonLazy();

            //Game Manager
            this.Container.Bind<IHandleUserDataServices>().To<HandleLocalUserDataServices>().AsCached();
            this.Container.DeclareSignal<UserDataLoadedSignal>();

            //Player state
            this.Container.Bind<PlayerState>().AsCached();

            //Genarate fps
            this.Container.Bind<Fps>().FromNewComponentOnNewGameObject().AsCached().NonLazy();

            //Helper
            this.Container.Bind<LoadImageHelper>().AsCached();

            //Installer
            BlueprintServicesInstaller.Install(this.Container);
            ScreenFlowInstaller.Install(this.Container);
            ApplicationServiceInstaller.Install(this.Container);
            GameQueueActionInstaller.Install(this.Container);
        }
    }
}
#endif