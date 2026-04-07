namespace GameBusiness.Shop.Blueprint
{
    using DataManager.Blueprint.BlueprintReader;
    using GameBusiness.Transactions.Blueprint;

    [BlueprintReader("ExchangePackage")]
    public class ExchangePackageBlueprint : GenericBlueprintReaderByRow<string, ExchangePackageRecord>
    {

    }

    [CsvHeaderKey("PackageId")]
    public class ExchangePackageRecord
    {
        public string PackageId;
        public string PackageName;
        public string PackageDescription;
        public string PackageIcon;
        public float  AvailableTime;
        public bool   DefaultUnlock;

        public BlueprintByRow<PayoutRecord>         Payouts;
        public BlueprintByRow<PurchaseOptionRecord> PurchaseOptions;
    }

    /// <summary>
    /// Cost option for exchange package
    /// Priority is determined by the order in the blueprint (first has highest priority)
    /// </summary>
    [CsvHeaderKey("PurchaseOptionIndex")]
    public class PurchaseOptionRecord
    {
        public int PurchaseOptionIndex;
        public int LimitAmount; // -1 means no limit
        public int RefreshLimitTime; // in seconds, -1 means no refresh

        public BlueprintByRow<CostRecord> Costs;

        /// <summary>
        /// if true and if this asset type is a kind of pool table,
        /// the payout will be auto re-generated based on the reference limit time of the purchase option
        /// </summary>
        public bool AutoGeneratePayout;
    }
}
