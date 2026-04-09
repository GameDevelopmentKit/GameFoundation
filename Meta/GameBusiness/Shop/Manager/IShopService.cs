namespace GameBusiness.Shop.Manager
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameBusiness.Shop.Blueprint;
    using GameBusiness.Shop.Model;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Model;
    using ServiceImplementation.IAPServices;

    /// <summary>
    /// Non-generic shop service interface.
    /// Extracted from <see cref="Manager.ShopManager{TData}"/> so UI layers can depend on
    /// the interface rather than the generic concrete class.
    /// </summary>
    public interface IShopService
    {
        #region ShopLayout
        /// <summary>
        /// Tries to get a shop layout record by layout id.
        /// </summary>
        bool TryGetShopLayoutRecord(string layoutId, out ShopLayoutRecord shopLayoutRecord);

        /// <summary>
        /// Tries to get the default shop layout record (MainShop).
        /// </summary>
        bool TryGetDefaultShopLayoutRecord(out ShopLayoutRecord shopLayoutRecord);
        #endregion

        #region ExchangePackage
        /// <summary>
        /// Query exchange package data by package id.
        /// </summary>
        /// <param name="packageId">Package id: can be instance id if have multiple instance, or blueprint id if single instance.</param>
        /// <param name="packageBlueprintId">If packageId is instance id, provide the blueprint id here.</param>
        /// <returns>The package data, or null if no valid purchase options exist.</returns>
        ExchangePackageData QueryExchangePackage(in string packageId, string packageBlueprintId = null);

        /// <summary>
        /// Query multiple exchange packages by their ids.
        /// </summary>
        List<ExchangePackageData> QueryExchangePackages(in List<string> packageIds);

        /// <summary>
        /// Marks the given package as unlocked in the user data.
        /// </summary>
        void UnlockExchangePackage(string packageId, string packageBlueprintId = null);

        /// <summary>
        /// Try get the first valid purchase option that user can purchase.
        /// If none is valid, return false and output the last option as default.
        /// </summary>
        bool TryGetPossiblePurchaseOptions(ExchangePackageData package, int quantity, out PurchaseOptionData option);

        /// <summary>
        /// Purchases an exchange package. Returns TransactionResult on success.
        /// Throws <see cref="ShopPurchaseException"/> if package is expired/unavailable.
        /// Lets other exceptions (InsufficientAssetException, PaymentServiceException) propagate.
        /// </summary>
        UniTask<TransactionResult> PurchaseExchangePackageAsync(ExchangePackageData package, int quantity);
        #endregion

        #region IAP
        /// <summary>
        /// Returns IAP product IDs from the exchange package blueprint.
        /// Does NOT initialize IAP — that's the game's responsibility.
        /// </summary>
        Dictionary<string, ProductType> GetIapProductIds();
        #endregion

        #region Ultity
        string GenerateCostText(string packageId, int quantity = 1);

        string GenerateCostText(List<CostRecord> costs, bool canPurchase, int quantity = 1);
        #endregion
    }
}
