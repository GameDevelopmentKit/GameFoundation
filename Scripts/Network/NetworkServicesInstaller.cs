namespace GameFoundation.Scripts.Network
{
    using GameFoundation.Editor.ServerConfig;
    using GameFoundation.Scripts.Network.Authentication;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.Extension;
    using Zenject;

    /// <summary>Is used in zenject, install all stuffs relate to network into global context.</summary>
    public class NetworkServicesInstaller : Installer<NetworkServicesInstaller>
    {
        [Inject] private GameConfig gameConfig;
        public override void InstallBindings()
        {
            // Network services
            this.Container.Bind<IHttpService>().To<BestHttpService>().AsCached().WithArguments(this.gameConfig.ServerConfig.GameServer);
            this.Container.Bind<AuthenticationService>().To<AuthenticationService>().AsCached().WithArguments(this.gameConfig.ServerConfig.AuthServer);

            // //TODO move this into BestHttpService instead of separate them
            this.Container.Bind<NetworkConfig>().AsSingle().NonLazy();
            
            // Pooling for http request object, transfer data object
            this.Container.BindIFactoryForAllDriveTypeFromPool<BaseHttpRequest>();
            this.Container.BindIFactory<ClientWrappedHttpRequestData>().FromPoolableMemoryPool();
        }
    }
}