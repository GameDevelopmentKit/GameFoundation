namespace GameFoundation.Scripts
{
    using DataManager.LocalSave;
    using DataManager.MasterData;
    using GameConfigs;
    using GameFoundation.Scripts.AppUpdate;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using GameFoundation.Scripts.UIModule.Utilities.LoadImage;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using Localization;
    using Zenject;

    public class GameFoundationInstaller : Installer<GameFoundationInstaller>
    {
        public override void InstallBindings()
        {
            //Data Manager
            DataManagerInstaller.Install(this.Container);

            SignalBusInstaller.Install(this.Container);
            LocalizationInstaller.Install(this.Container);
            ApplicationServiceInstaller.Install(this.Container);
            AppUpdateInstaller.Install(this.Container);

            this.Container.Bind<GDKConfig>().FromResource("GameConfigs/GDKConfig").AsSingle().NonLazy();

            this.Container.Bind<IGameAssets>().To<GameAssets>().AsCached();
            this.Container.Bind<ObjectPoolManager>().AsCached().NonLazy();

            //Service
            this.Container.Bind<ILogService>().To<LogService>().AsSingle().NonLazy();
            this.Container.Bind<ApplicationService>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();

            //Helper
            this.Container.Bind<LoadImageHelper>().AsCached();
            //Installer
            ScreenFlowInstaller.Install(this.Container);
            GameQueueActionInstaller.Install(this.Container);
            
        }
    }
}
