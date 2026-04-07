namespace GameBusiness.Transactions.Exceptions
{
    using System;
    using GameBusiness.Transactions.Blueprint;
    public class PaymentServiceException : Exception
    {
        public CostRecord CostRecord { get; }
        
        public PaymentServiceException(CostRecord costRecord, string message) : base(message)
        {
            this.CostRecord = costRecord;
        }
    }
}
