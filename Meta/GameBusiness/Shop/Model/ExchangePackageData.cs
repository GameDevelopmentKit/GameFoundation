namespace GameBusiness.Shop.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameBusiness.Shop.Blueprint;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Model;
    using GameFoundation.Scripts.Utilities.Extension;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a purchasable exchange package instance.
    /// Contains purchase options, unlock state, and expiry tracking.
    /// Serialized to user save data via JSON. Runtime-only fields are marked [JsonIgnore].
    /// </summary>
    public class ExchangePackageData
    {
        public string                   InstancePackageId           { get; set; }
        public string                   BlueprintId                 { get; set; }
        public List<PurchaseOptionData> PurchaseOptions             { get; set; }
        public List<Asset>              CachedGeneratedPayoutAssets { get; set; }
        public bool                     IsUnlocked                  { get; set; }
        public DateTime                 EndTime                     { get; set; }

        [JsonIgnore] public bool IsExpired => EndTime != DateTime.MinValue && EndTime < DateTime.Now;
        [JsonIgnore] public TimeSpan RemainingTime => EndTime == DateTime.MinValue ? TimeSpan.MaxValue : EndTime - DateTime.Now;

        [JsonIgnore] public ExchangePackageRecord Record { get; private set; }

        public ExchangePackageData() { }

        public ExchangePackageData(ExchangePackageRecord record, string instancePackageId)
        {
            this.InstancePackageId = string.IsNullOrEmpty(instancePackageId) ? record.PackageId : instancePackageId;
            this.BlueprintId       = record.PackageId;
            this.Record            = record;
            this.PurchaseOptions   = new List<PurchaseOptionData>();
            InitializePurchaseOptions(record);

            if (record.DefaultUnlock)
            {
                this.IsUnlocked = true;
                if (record.AvailableTime > 0)
                {
                    this.EndTime = DateTime.Now.AddSeconds(record.AvailableTime);
                }
                else
                {
                    this.EndTime = DateTime.MinValue;
                }
            }
        }

        public void UpdateRecord(ExchangePackageRecord record)
        {
            this.Record = record;

            if (PurchaseOptions.Count != record.PurchaseOptions.Count)
            {
                InitializePurchaseOptions(record);
            }
            else
            {
                for (int i = 0; i < PurchaseOptions.Count; i++)
                {
                    PurchaseOptions[i].Record = record.PurchaseOptions[i];
                }
            }
        }

        public List<PayoutRecord> GetPayouts()
        {
            if (CachedGeneratedPayoutAssets != null)
            {
                return CachedGeneratedPayoutAssets.Select(asset => new PayoutRecord()
                    {
                        AssetType     = asset.AssetType,
                        PayoutAssetId = asset.AssetId,
                        PayoutAmount  = (int)asset.Amount,
                        Chance        = 1f
                    })
                    .ToList();
            }
            else
                return Record.Payouts;
        }

        public bool IsExistPurchaseOption()
        {
            return PurchaseOptions.Any(option => option.IsExist);
        }

        private void InitializePurchaseOptions(ExchangePackageRecord record)
        {
            PurchaseOptions.Clear();
            foreach (var optionRecord in record.PurchaseOptions)
            {
                var purchaseOptionData = new PurchaseOptionData()
                {
                    OptionIndex = optionRecord.PurchaseOptionIndex,
                    Record      = optionRecord
                };

                if (optionRecord.LimitAmount > 0)
                {
                    purchaseOptionData.PurchasedAmount = 0;
                    if (optionRecord.RefreshLimitTime > 0)
                    {
                        purchaseOptionData.LastRefreshTime = purchaseOptionData.LastRefreshTime.GetNearestTimeFromPeriod(optionRecord.RefreshLimitTime);
                    }
                    else
                    {
                        purchaseOptionData.LastRefreshTime = DateTime.MaxValue;
                    }
                }

                this.PurchaseOptions.Add(purchaseOptionData);
            }
        }
    }

    /// <summary>
    /// Tracks a single purchase option's state: purchase count, refresh timer, and availability.
    /// A package has one or more options ordered by priority. The first affordable option is used.
    /// </summary>
    public class PurchaseOptionData
    {
        public int      OptionIndex;
        public int      PurchasedAmount;
        public DateTime LastRefreshTime;

        [JsonIgnore] public PurchaseOptionRecord Record;

        [JsonIgnore] public bool IsExist => Record != null &&
                                            (Record.LimitAmount <= 0 || PurchasedAmount < Record.LimitAmount || Record.RefreshLimitTime > 0);

        [JsonIgnore] public bool IsLimited         => Record != null && Record.LimitAmount > 0;
        [JsonIgnore] public bool IsOneTimePurchase => IsLimited && Record.RefreshLimitTime <= 0;
        [JsonIgnore] public int  RemainAmount      => Record.LimitAmount - PurchasedAmount;
        [JsonIgnore] public TimeSpan RefreshIn
        {
            get
            {
                if (Record == null || Record.RefreshLimitTime <= 0) return TimeSpan.MaxValue;
                var nextRefreshTime = LastRefreshTime.AddSeconds(Record.RefreshLimitTime);
                return nextRefreshTime - DateTime.Now;
            }
        }

        /// <summary>
        /// Checks if this option can be purchased. Handles refresh logic for
        /// time-limited options (resets PurchasedAmount when refresh period elapses).
        /// </summary>
        /// <param name="isJustRefresh">True if the option's purchase count was just reset by a refresh.</param>
        /// <returns>True if the option has remaining purchases available.</returns>
        public bool IsAvailableToPurchase(out bool isJustRefresh)
        {
            isJustRefresh = false;
            if (!IsLimited) return true;

            isJustRefresh = CheckRefresh();

            return PurchasedAmount < Record.LimitAmount;
        }

        private bool CheckRefresh()
        {
            if (Record.RefreshLimitTime > 0)
            {
                var nextRefreshTime = LastRefreshTime.AddSeconds(Record.RefreshLimitTime);
                if (DateTime.Now >= nextRefreshTime)
                {
                    PurchasedAmount = 0;
                    LastRefreshTime = nextRefreshTime;
                    return true;
                }
            }
            return false;
        }

        public void OnPurchase(int quantity)
        {
            PurchasedAmount += quantity;
        }
    }
}
