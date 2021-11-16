namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using Zenject;

    public class ApplicationServiceInstaller : Installer<ApplicationServiceInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<ApplicationEventHelper>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.DeclareSignal<ApplicationModelSignal>();
        }
    }
}