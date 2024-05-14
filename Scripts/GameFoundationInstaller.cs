namespace GameFoundation.Scripts
{
    using BlueprintFlow.BlueprintControlFlow;
    using Cysharp.Threading.Tasks;
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
    using UnityEngine;
    using Zenject;

    public class GameFoundationInstaller : Installer<GameFoundationInstaller>
    {
        private GameObject objectPoolContainer;

        public override void InstallBindings()
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

            //Game Manager
            this.Container.Bind<IHandleUserDataServices>().To<HandleLocalUserDataServices>().AsCached();
            this.Container.DeclareSignal<UserDataLoadedSignal>();

            //Genarate fps
            this.Container.Bind<Fps>().FromNewComponentOnNewGameObject().AsCached().NonLazy();

            //Helper
            this.Container.Bind<LoadImageHelper>().AsCached();
            //Installer
            BlueprintServicesInstaller.Install(this.Container);
            ScreenFlowInstaller.Install(this.Container);
            ApplicationServiceInstaller.Install(this.Container);
            GameQueueActionInstaller.Install(this.Container);
            this.BindSoundSetting();
            this.CreateObjectPool(AudioManager.AudioSourceKey, 3).Forget();
        }

        private async UniTask CreateObjectPool(string prefabName, int initialPoolSize = 1)
        {
            this.objectPoolContainer = new GameObject(nameof(this.objectPoolContainer));
            Object.DontDestroyOnLoad(this.objectPoolContainer);
            await this.Container.Resolve<ObjectPoolManager>().CreatePool(prefabName, initialPoolSize, this.objectPoolContainer);
        }

        private async void BindSoundSetting()
        {
            var localDataServices = this.Container.Resolve<IHandleUserDataServices>();
            var soundData         = await localDataServices.Load<SoundSetting>();
            this.Container.Bind<SoundSetting>().FromInstance(soundData).AsCached().NonLazy();
        }
    }
}