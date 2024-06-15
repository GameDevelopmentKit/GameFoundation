namespace GameFoundation.Scripts
{
    using DataManager.Blueprint.BlueprintController;
    using DataManager.LocalData;
    using DataManager.MasterData;
    using GameConfigs;
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
    using I2.Loc;
    using Zenject;

    public class GameFoundationInstaller : Installer<GameFoundationInstaller>
    {
        public override  void InstallBindings()
        {
            SignalBusInstaller.Install(this.Container);

            this.Container.Bind<GDKConfig>().FromResource("GameConfigs/GDKConfig").AsSingle().NonLazy();

            this.Container.Bind<IGameAssets>().To<GameAssets>().AsCached();
            this.Container.Bind<ObjectPoolManager>().AsCached().NonLazy();

            this.Container.BindInterfacesTo<AudioManager>().AsCached().NonLazy();

            //Localization services
            this.Container.Bind<SetLanguage>().FromNewComponentOnNewGameObject().AsCached().NonLazy();
            this.Container.Bind<LocalizationService>().AsCached().NonLazy();

            //Service
            this.Container.Bind<ILogService>().To<LogService>().AsSingle().NonLazy();

            //Data Manager
            BlueprintServicesInstaller.Install(this.Container);
            this.Container.Bind<MinimizeAppService>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.Bind<IHandleLocalDataServices>().To<PlayerPrefsLocalDataServices>().AsCached();
            this.Container.DeclareSignal<MasterDataReadySignal>();

            //Genarate fps
            this.Container.Bind<Fps>().FromNewComponentOnNewGameObject().AsCached().NonLazy();

            //Helper
            this.Container.Bind<LoadImageHelper>().AsCached();
            //Installer
            ScreenFlowInstaller.Install(this.Container);
            ApplicationServiceInstaller.Install(this.Container);
            GameQueueActionInstaller.Install(this.Container);
            this.BindSoundSetting();
        }
        
        private async void BindSoundSetting()
        {
            var localDataServices = this.Container.Resolve<IHandleLocalDataServices>();
            var soundData         = await localDataServices.Load<SoundSetting>();
            this.Container.Bind<SoundSetting>().FromInstance(soundData).AsCached().NonLazy();
        }
    }
}