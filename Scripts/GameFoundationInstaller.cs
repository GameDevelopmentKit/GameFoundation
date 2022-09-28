namespace GameFoundation.Scripts
{
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Network;
    using GameFoundation.Scripts.Network.Authentication;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameFoundation.Scripts.UIModule.Utilities.LoadImage;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using I2.Loc;
    using Zenject;

    public class GameFoundationInstaller : Installer<GameFoundationInstaller>
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(this.Container);

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
            this.Container.Bind<HandleLocalDataServices>().AsCached().NonLazy();
            this.Container.Bind<GameFoundationLocalData>().FromResolveGetter<HandleLocalDataServices>(services => services.Load<GameFoundationLocalData>()).AsCached();

            //Player state
            this.Container.Bind<PlayerState>().AsCached();

            //Genarate fps
            this.Container.Bind<Fps>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
            
            //Helper
            this.Container.Bind<LoadImageHelper>().AsCached();

            //Installer
            BlueprintServicesInstaller.Install(this.Container);
            NetworkServicesInstaller.Install(this.Container);
            ScreenFlowInstaller.Install(this.Container);
            ServicesLoginInstaller.Install(this.Container);
            ApplicationServiceInstaller.Install(this.Container);
            GameQueueActionInstaller.Install(this.Container);
        }
    }
}