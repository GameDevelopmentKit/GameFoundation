namespace GameFoundation.Scripts.AppUpdate
{
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using Zenject;

    public class AppUpdateInstaller : Installer<AppUpdateInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<AppUpdateTextSettings>().AsSingle();
            this.Container.Bind<AppUpdatePolicy>().AsSingle();
            this.Container.Bind<IAppUpdateConfigProvider>().To<DisabledAppUpdateConfigProvider>().AsSingle();
            this.Container.Bind<IAppUpdateTextProvider>().To<LocalizedAppUpdateTextProvider>().AsSingle();
            this.Container.Bind<IAppUpdatePromptService>().To<NotificationAppUpdatePromptService>().AsSingle();
            this.Container.Bind<AppUpdateGateService>().AsSingle();
        }
    }
}
