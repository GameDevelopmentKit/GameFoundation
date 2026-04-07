namespace GameBusiness.Shop.Manager
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.MasterData;
    using DataManager.UserData;
    using GameBusiness.Shop.Blueprint;
    using GameBusiness.Shop.Model;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Manager;
    using GameBusiness.Transactions.Model;
    using ServiceImplementation.IAPServices;
    using UnityEngine;
    using Zenject;

    /// <summary>
    /// Generic ShopManager for the GameBusiness module.
    /// Handles pure data queries and purchase logic. No toast, no localization, no asset display.
    /// Each game provides its own TData extending IShopData and wraps this with a game-specific handler.
    /// </summary>
    public class ShopManager<TData> : BaseDataManager<TData>
        where TData : class, IShopData, IUserData, new()
    {
        public const string MainShopLayout = "MainShop";

        private readonly ShopLayoutBlueprint      shopLayoutBlueprint;
        private readonly ExchangePackageBlueprint exchangePackageBlueprint;
        private readonly ITransactionManager      transactionManager;
        private readonly IIapServices             iapServices;

        public ShopManager(SignalBus signalBus, ShopLayoutBlueprint shopLayoutBlueprint,
            ExchangePackageBlueprint exchangePackageBlueprint, ITransactionManager transactionManager,
            IIapServices iapServices) : base(signalBus)
        {
            this.shopLayoutBlueprint      = shopLayoutBlueprint;
            this.exchangePackageBlueprint = exchangePackageBlueprint;
            this.transactionManager       = transactionManager;
            this.iapServices              = iapServices;
        }

        public override void OnDataInitialized()
        {
            base.OnDataInitialized();

            foreach (var packageData in Data.PurchasedPackages)
            {
                packageData.Value.UpdateRecord(this.exchangePackageBlueprint.GetDataById(packageData.Value.BlueprintId));
            }
        }

        /// <summary>
        /// Returns IAP product IDs from the exchange package blueprint.
        /// Does NOT initialize IAP — that's the game's responsibility.
        /// </summary>
        public Dictionary<string, ProductType> GetIapProductIds()
        {
            var iapPacks = new Dictionary<string, ProductType>();
            foreach (var packageRecord in exchangePackageBlueprint.Values)
            {
                foreach (var purchaseOption in packageRecord.PurchaseOptions)
                {
                    foreach (var cost in purchaseOption.Costs)
                    {
                        if (cost.PaymentType == PaymentTypes.IAP)
                        {
                            iapPacks.TryAdd(cost.CostAssetId, ProductType.Consumable);
                        }
                    }
                }
            }
            return iapPacks;
        }

        #region ShopLayout
        public bool TryGetShopLayoutRecord(string layoutId, out ShopLayoutRecord shopLayoutRecord)
        {
            return shopLayoutBlueprint.TryGetValue(layoutId, out shopLayoutRecord);
        }

        public bool TryGetDefaultShopLayoutRecord(out ShopLayoutRecord shopLayoutRecord)
        {
            return shopLayoutBlueprint.TryGetValue(MainShopLayout, out shopLayoutRecord);
        }
        #endregion

        #region ExchangePackage
        public List<ExchangePackageData> QueryExchangePackages(in List<string> packageIds)
        {
            var results = new List<ExchangePackageData>(packageIds.Count);
            foreach (var packageId in packageIds)
            {
                var package = this.QueryExchangePackage(packageId);
                if (package != null)
                {
                    results.Add(package);
                }
            }
            return results;
        }

        /// <summary>
        /// Query exchange package data by package id.
        /// </summary>
        /// <param name="packageId">Package id: can be instance id if have multiple instance, or blueprint id if single instance</param>
        /// <param name="packageBlueprintId">If packageId is instance id, provide the blueprint id here</param>
        public ExchangePackageData QueryExchangePackage(in string packageId, string packageBlueprintId = null)
        {
            bool isPurchasedPackage = this.Data.PurchasedPackages.TryGetValue(packageId, out var purchasedPackage);
            packageBlueprintId = string.IsNullOrEmpty(packageBlueprintId) ? packageId : packageBlueprintId;

            if (!isPurchasedPackage || (purchasedPackage.BlueprintId != packageBlueprintId))
            {
                purchasedPackage = new ExchangePackageData(exchangePackageBlueprint.GetDataById(packageBlueprintId), packageId);
            }

            return purchasedPackage.IsExistPurchaseOption() ? purchasedPackage : null;
        }

        public void UnlockExchangePackage(string packageId, string packageBlueprintId = null)
        {
            var package = QueryExchangePackage(packageId, packageBlueprintId);
            if (package == null) return;

            if (!package.IsUnlocked)
            {
                package.IsUnlocked = true;
                if (package.Record.AvailableTime > 0)
                {
                    package.EndTime = DateTime.Now.AddSeconds(package.Record.AvailableTime);
                }
                else
                {
                    package.EndTime = DateTime.MinValue;
                }

                this.Data.PurchasedPackages[package.InstancePackageId] = package;
            }
        }

        /// <summary>
        /// Try get the first valid purchase option that user can purchase.
        /// If none is valid, return false and output the last option as default.
        /// </summary>
        public bool TryGetPossiblePurchaseOptions(ExchangePackageData package, int quantity, out PurchaseOptionData option)
        {
            foreach (var purchaseOption in package.PurchaseOptions)
            {
                if (purchaseOption.IsAvailableToPurchase(out var isJustRefresh))
                {
                    if (purchaseOption.Record.AutoGeneratePayout && (isJustRefresh || package.CachedGeneratedPayoutAssets == null))
                    {
                        package.CachedGeneratedPayoutAssets                    = transactionManager.GetPayoutAssets(package.Record.Payouts);
                        this.Data.PurchasedPackages[package.InstancePackageId] = package;
                    }

                    if (this.transactionManager.VerifyCosts(purchaseOption.Record.Costs, quantity))
                    {
                        option = purchaseOption;
                        return true;
                    }
                }
            }
            option = package.PurchaseOptions[^1];
            return false;
        }

        /// <summary>
        /// Purchases an exchange package. Returns TransactionResult on success or throws.
        /// Throws ShopPurchaseException if package is expired/unavailable.
        /// Lets InsufficientAssetException, PaymentServiceException, and other exceptions propagate.
        /// </summary>
        public async UniTask<TransactionResult> PurchaseExchangePackageAsync(
            ExchangePackageData package, int quantity)
        {
            this.TryGetPossiblePurchaseOptions(package, quantity, out var purchaseOption);
            if (!purchaseOption.IsAvailableToPurchase(out _) || !package.IsUnlocked || package.IsExpired)
            {
                throw new ShopPurchaseException(package.IsExpired
                    ? ShopPurchaseError.PackageExpired
                    : ShopPurchaseError.PackageUnavailable);
            }

            TransactionResult transactionResult;
            if (package.CachedGeneratedPayoutAssets != null)
            {
                await this.transactionManager.MakePayments(purchaseOption.Record.Costs, package.BlueprintId, quantity);
                transactionResult = new TransactionResult(purchaseOption.Record.Costs, package.Record.Payouts, package.BlueprintId, package.CachedGeneratedPayoutAssets);
            }
            else
            {
                transactionResult = await this.transactionManager.BeginTransaction(purchaseOption.Record.Costs, package.Record.Payouts, package.BlueprintId, quantity);
            }

            this.Data.PurchasedPackages.TryAdd(package.InstancePackageId, package);
            purchaseOption.OnPurchase(quantity);
            return transactionResult;
        }
        #endregion
    }
}
