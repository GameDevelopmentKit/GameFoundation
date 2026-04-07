namespace DeepLink
{
    using DeepLink.Handle;
    using DeepLink.Signal;
    using GameFoundation.Scripts.Utilities.Extension;
    using Zenject;

    public class DeepLinkInstaller : Installer<DeepLinkInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<DeepLinkProcessing>().AsSingle().NonLazy();

            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IActionHandle>();

            this.Container.DeclareSignal<DeepLinkSignal>();
        }
    }
}