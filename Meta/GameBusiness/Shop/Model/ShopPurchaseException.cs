namespace GameBusiness.Shop.Model
{
    using System;

    public enum ShopPurchaseError
    {
        PackageExpired,
        PackageUnavailable
    }

    public class ShopPurchaseException : Exception
    {
        public ShopPurchaseError Error { get; }

        public ShopPurchaseException(ShopPurchaseError error)
            : base($"Shop purchase failed: {error}")
        {
            this.Error = error;
        }

        public ShopPurchaseException(ShopPurchaseError error, string message)
            : base(message)
        {
            this.Error = error;
        }
    }
}
