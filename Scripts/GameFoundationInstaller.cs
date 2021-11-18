namespace GameFoundation.Scripts
{
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow;
    using GameFoundation.Scripts.GameManager;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Network;
    using GameFoundation.Scripts.Network.Authentication;
    using GameFoundation.Scripts.ScreenFlow.Managers;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class GameFoundationInstaller : Installer<GameFoundationInstaller>
    {
        public MasterAudio MasterAudio;
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(this.Container);

            //CreateMasterAudio
            this.Container.Bind<MasterAudio>().FromComponentInNewPrefabResource("MechMasterAudio").AsSingle().NonLazy();
            this.Container.Bind<IMechSoundManager>().To<MasterMechSoundManager>().AsSingle().NonLazy();            
            
            //Localization services
            this.Container.Bind<LocalizationService>().AsCached().NonLazy();

            //Service
            this.Container.Bind<ILogService>().To<LogService>().AsSingle().NonLazy();

            //Game Manager
            this.Container.Bind<HandleLocalDataServices>().AsSingle().NonLazy();
            this.Container.Bind<GameFoundationLocalData>().FromResolveGetter<HandleLocalDataServices>(services => services.LoadModel<GameFoundationLocalData>()).AsCached();

            //Player state
            this.Container.Bind<PlayerState>().AsCached();
            

            //Genarate fps
            this.Container.Bind<Fps>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();

            //Installer
            BlueprintServicesInstaller.Install(this.Container);
            NetworkServicesInstaller.Install(this.Container);
            ScreenFlowInstaller.Install(this.Container);
            ServicesLoginInstaller.Install(this.Container);
            ApplicationServiceInstaller.Install(this.Container);
        }
    }
}