namespace GameBusiness.Shop.Model
{
    using System.Collections.Generic;
    using DataManager.UserData;

    /// <summary>
    /// Contract for shop user data consumed by ShopManager&lt;TData&gt;.
    /// Your game's data class must implement this interface.
    /// Add game-specific fields (gacha data, seasonal data, etc.) to your concrete class.
    /// <example>
    /// <code>
    /// public class MyShopData : IShopData
    /// {
    ///     public Dictionary&lt;string, ExchangePackageData&gt; PurchasedPackages { get; set; } = new();
    ///     public Dictionary&lt;string, GachaItemData&gt; GachaItems = new(); // game-specific
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public interface IShopData : IUserData
    {
        Dictionary<string, ExchangePackageData> PurchasedPackages { get; }
    }
}
