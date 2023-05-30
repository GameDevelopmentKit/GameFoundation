namespace GameFoundation.Scripts
{
    using BlueprintFlow.BlueprintControlFlow;
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameFoundation.Scripts.UIModule.Utilities.LoadImage;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameFoundation.Scripts.Utilities.UserData;
    using global::Models;
    using I2.Loc;
    using Zenject;

    public class GameFoundationInstaller : Installer<GameFoundationInstaller>
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(this.Container);

            this.Container.Bind<GDKConfig>().FromResource("GameConfigs/GDKConfig").AsSingle().NonLazy();

            this.Container.Bind<IGameAssets>().To<GameAssets>().AsCached();
            this.Container.Bind<ObjectPoolManager>().AsCached().NonLazy();

            //CreateMasterAudio
            this.Container.Bind<PlaylistController>().FromComponentInNewPrefabResource("GameFoundationPlaylistController").AsCached().NonLazy();
            this.Container.Bind<MasterAudio>().FromComponentInNewPrefabResource("GameFoundationAudio").AsCached().NonLazy();
            this.Container.BindInterfacesTo<AudioManager>().AsCached().NonLazy();

            //Localization services
            this.Container.Bind<SetLanguage>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
            this.Container.Bind<LocalizationService>().AsCached().NonLazy();

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