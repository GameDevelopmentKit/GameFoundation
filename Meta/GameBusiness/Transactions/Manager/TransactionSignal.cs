namespace GameBusiness.Transactions.Manager
{
    using System.Collections.Generic;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Model;

    public class MakePaymentSuccessSignal
    {
        public IReadOnlyCollection<CostRecord> CostRecords;
        public string                          Location;
    }
    
    public class PayoutsReceivedSignal
    {
        public TransactionResult transactionResult;
    }
    
    public class PayoutReceivedSignal
    {
        public Asset  PayoutAsset;
        public string Location;
    }
}
