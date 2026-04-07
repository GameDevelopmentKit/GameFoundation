namespace GameBusiness.Transactions.Exceptions
{
    using System;
    using GameBusiness.Transactions.Blueprint;
    public class InsufficientAssetException : Exception
    {
        public CostRecord CostRecord { get; }
        public InsufficientAssetException(CostRecord costRecord, string message) : base(message)
        {
            this.CostRecord = costRecord;
        }
    }
}
