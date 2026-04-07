namespace GameBusiness.Transactions.PaymentService
{
    using Cysharp.Threading.Tasks;
    using GameBusiness.Transactions.Blueprint;
    using ServiceImplementation.IAPServices;

    public class IAPPaymentService : IPaymentService
    {
        private readonly IIapServices iapServices;
        public           string       PaymentType => PaymentTypes.IAP;


        public IAPPaymentService(IIapServices iapServices)
        {
            this.iapServices = iapServices;
        }

        public bool Available(string assetId) { return this.iapServices.IsProductAvailable(assetId); }

        public bool VerifyCost(string assetId, float value) { return this.iapServices.IsProductAvailable(assetId); }

        public UniTask<float> MakePayment(string assetId, float value)
        {
            return this.iapServices.PurchaseProduct(assetId).ContinueWith(() => 0f);
        }

    }

}
