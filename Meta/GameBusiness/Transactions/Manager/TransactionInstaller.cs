namespace GameBusiness.Transactions.Manager
{
    using GameBusiness.Transactions.PaymentService;
    using GameBusiness.Transactions.PayoutService;
    using GameFoundation.Scripts.Utilities.Extension;
    using Zenject;

    public class TransactionInstaller : Installer<TransactionInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesTo<TransactionManager>().AsSingle();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IPaymentService>();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IPayoutService>();
            
            this.Container.DeclareSignal<MakePaymentSuccessSignal>();
            this.Container.DeclareSignal<PayoutsReceivedSignal>();
            this.Container.DeclareSignal<PayoutReceivedSignal>();
        }
    }
}