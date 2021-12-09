namespace GameFoundation.Scripts.Network
{
    using GameFoundation.Editor.ServerConfig;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.Extension;
    using Zenject;

    /// <summary>Is used in zenject, install all stuffs relate to network into global context.</summary>
    public class NetworkServicesInstaller : Installer<NetworkServicesInstaller>
    {
        public override void InstallBindings()
        {
            var gameConfig = this.Container.Resolve<GameConfig>();

            // Network services
            this.Container.Bind<IHttpService>().To<BestHttpService>().AsCached().WithArguments(gameConfig.ServerConfig.GameServer);

            // //TODO move this into BestHttpService instead of separate them
            this.Container.Bind<NetworkConfig>().AsSingle().NonLazy();
            
            // Pooling for http request object, transfer data object
            this.Container.BindIFactoryForAllDriveTypeFromPool<BaseHttpRequest>();
            this.Container.BindIFactory<ClientWrappedHttpRequestData>().FromPoolableMemoryPool();
        }
    }
}