namespace GameBusiness.Transactions.Model
{
    using System.Collections.Generic;
    using GameBusiness.Transactions.Blueprint;

    public class TransactionResult
    {
        /// <summary>
        /// The cost records associated with the transaction.
        /// </summary>
        public IReadOnlyCollection<CostRecord> CostRecords { get; }

        /// <summary>
        /// The payout records associated with the transaction.
        /// </summary>
        public IReadOnlyCollection<PayoutRecordAbstract> PayoutRecords { get; }
        
        /// <summary>
        /// The location where the transaction took place.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// The assets received from the transaction.
        /// </summary>
        public List<Asset> Assets { get; private set; }

        public TransactionResult(IReadOnlyCollection<CostRecord> costRecords,  IReadOnlyCollection<PayoutRecordAbstract> payoutRecords, string location, List<Asset> assets)
        {
            PayoutRecords = payoutRecords;
            Location           = location;
            Assets             = assets;
            CostRecords        = costRecords;
        }

        public TransactionResult(string location, List<Asset> assets)
        {
            Location    = location;
            Assets      = assets;
            CostRecords = null;
        }

        public void SetAssets(List<Asset> assets)
        {
            this.Assets = assets;
        }
    }

    public class Asset : IGroupableSelector
    {
        public string AssetId;
        public float  Amount;
        public string AssetType;

        public virtual object GetGroupableKey()
        {
            return new { AssetId, AssetType };
        }
    }

    public interface IGroupableSelector
    {
        object GetGroupableKey();
    }
}
