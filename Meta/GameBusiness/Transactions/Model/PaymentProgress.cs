namespace GameBusiness.Transactions.Model
{
    using GameBusiness.Transactions.Blueprint;

    public class PaymentProgress
    {
        public CostRecord CostRecord      { get; }
        public float      RemainingAmount { get; set; }
        public bool       IsCompleted     => RemainingAmount <= 0;

        public PaymentProgress(CostRecord costRecord)
        {
            CostRecord      = costRecord;
            RemainingAmount = costRecord.CostAmount;
        }

        public string ToValueString() { return $"{CostRecord.CostAmount - RemainingAmount}/{CostRecord.CostAmount}"; }
    }
}