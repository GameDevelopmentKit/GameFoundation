namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using Zenject;

    public class ApplicationServiceInstaller : Installer<ApplicationServiceInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.DeclareSignal<ApplicationPauseSignal>();
            this.Container.DeclareSignal<UpdateTimeAfterFocusSignal>();
            this.Container.Bind<IApplicationBuildInfoProvider>().To<UnityApplicationBuildInfoProvider>().AsSingle();
            this.Container.Bind<ApplicationVersionService>().AsSingle();
        }
    }
}
