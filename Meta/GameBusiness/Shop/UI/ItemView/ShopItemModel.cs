namespace GameBusiness.Shop.UI
{
    using System;
    using System.Collections.Generic;
    using GameBusiness.Shop.Model;
    using UIModule.Adapter;

    /// <summary>
    /// View-model for a single shop item. Carries display data resolved by
    /// <see cref="BaseShopSectionPresenter"/> and consumed by <see cref="BaseShopItemView"/>.
    /// Extends <see cref="MultiplePrefabsModel"/> for multi-prefab OSA adapters.
    /// </summary>
    public class ShopItemModel : MultiplePrefabsModel
    {
        public override string PrefabName    { get; }
        public override Type   PresenterType { get; }

        /// <summary>Addressable icon key or sprite path.</summary>
        public string PackageIcon { get; }

        /// <summary>Palette colors for <see cref="ThemeConfig.ApplyTheme"/>.</summary>
        public List<string> ColorPalette { get; }

        /// <summary>Optional tag label (e.g. "HOT", "NEW", "-20%").</summary>
        public string TagDescription { get; }

        /// <summary>Optional tag icon addressable path.</summary>
        public string TagIconAssetPath { get; }

        /// <summary>The resolved exchange package data.</summary>
        public ExchangePackageData ExchangePackage { get; }

        public ShopItemModel(ExchangePackageData exchangePackage, string prefabName, string packageIcon,
            List<string> colorPalette, string tagDescription, string tagIconAssetPath)
        {
            this.ExchangePackage  = exchangePackage;
            this.PrefabName       = prefabName;
            this.PackageIcon      = packageIcon;
            this.ColorPalette     = colorPalette;
            this.TagDescription   = tagDescription;
            this.TagIconAssetPath = tagIconAssetPath;
        }

        public ShopItemModel(ExchangePackageData exchangePackage, string prefabName, Type presenterType,
            string packageIcon, List<string> colorPalette, string tagDescription, string tagIconAssetPath)
            : this(exchangePackage, prefabName, packageIcon, colorPalette, tagDescription, tagIconAssetPath)
        {
            this.PresenterType = presenterType;
        }
    }
}
