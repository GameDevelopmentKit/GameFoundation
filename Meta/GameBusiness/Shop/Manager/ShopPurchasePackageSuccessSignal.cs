namespace GameBusiness.Shop.Manager
{
    using GameBusiness.Shop.Model;
    /// <summary>
    /// Zenject signal fired after a successful exchange package purchase.
    /// Carries the purchased package data for UI updates (e.g., reward popup, inventory refresh).
    /// Fired by the game's handler after PurchaseExchangePackageAsync completes.
    /// </summary>
    public class ShopPurchasePackageSuccessSignal
    {
        public ExchangePackageData PurchasedPackage;

        public ShopPurchasePackageSuccessSignal(ExchangePackageData purchasedPackage)
        {
            this.PurchasedPackage = purchasedPackage;
        }
    }
}
