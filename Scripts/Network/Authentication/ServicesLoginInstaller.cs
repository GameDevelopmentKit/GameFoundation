namespace GameFoundation.Scripts.Network.Authentication
{
    using Zenject;

    /// <summary>Bind some element login services etc... </summary>
    public class ServicesLoginInstaller : Installer<ServicesLoginInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<DataLoginServices>().AsCached().NonLazy();
            this.Container.Bind<FacebookAuthenticationService>().AsSingle().NonLazy();
            this.Container.Bind<GoogleAuthenticationService>().AsSingle().NonLazy();
            this.Container.Bind<MetaMaskAuthenticationService>().AsSingle().NonLazy();
        }
    }
}