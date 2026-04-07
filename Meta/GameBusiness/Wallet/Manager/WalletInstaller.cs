namespace GameBusiness.Wallet.Manager
{
    using GameBusiness.Wallet.Services;
    using GameBusiness.Wallet.Signal;
    using Zenject;

    public class WalletInstaller : Installer<WalletInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.DeclareSignal<EarnCurrencySignal>();
            this.Container.DeclareSignal<SpendCurrencySignal>();
            this.Container.Bind<NotifyCurrencyService>().AsSingle().NonLazy();
            this.Container.Bind<IWalletManager>().To<WalletManager>().AsSingle().NonLazy();
        }
    }
}