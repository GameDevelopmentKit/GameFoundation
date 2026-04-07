namespace GameBusiness.Transactions.PaymentService
{
    using Cysharp.Threading.Tasks;
    using GameBusiness.Transactions.Blueprint;

    public class FreePaymentService : IPaymentService
    {
        public string PaymentType => PaymentTypes.Free;

        public bool Available(string assetId) => true;

        public bool VerifyCost(string assetId, float value) => true;

        public UniTask<float> MakePayment(string assetId, float value) => UniTask.FromResult(0f);
    }
}
