namespace GameBusiness.Shop.UI
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameBusiness.Shop.Blueprint;
    using GameBusiness.Shop.Manager;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.MVP;
    using UnityEngine;
    using Zenject;

    /// <summary>
    /// Abstract base presenter for a shop section.
    /// Resolves the section's package list via <see cref="IShopService"/>,
    /// builds <see cref="ShopItemModel"/> list, and calls <see cref="BindContent"/>.
    /// Subclasses decide how to render (grid with ObjectPool, flex layout, paged adapter, etc.).
    /// </summary>
    public abstract class BaseShopSectionPresenter<TView, TModel> : BaseUIItemPresenter<TView, TModel> where TView : ShopSectionView where TModel : ShopSectionModel
    {
        protected readonly IShopService ShopService;
        protected readonly DiContainer  DiContainer;

        private ShopSectionModel model;

        public BaseShopSectionPresenter(IGameAssets gameAssets, IShopService shopService, DiContainer diContainer)
            : base(gameAssets)
        {
            this.ShopService = shopService;
            this.DiContainer = diContainer;
        }

        #region Public API

        public override void BindData(TModel param)
        {
            this.model = param;
            this.BindSectionHeader(param);
            this.PrepareContent(this.model, this.View.ContentRoot);
        }

        public void RefreshContent()
        {
            this.PrepareContent(this.model, this.View.ContentRoot);
        }

        #endregion

        #region Protected Hooks

        protected virtual void BindSectionHeader(ShopSectionModel sectionModel)
        {
            if (this.View.ThemeConfig != null)
            {
                this.View.ThemeConfig.ApplyTheme(sectionModel.SectionRecord.SectionColorPalette);
            }
        }

        protected virtual async void PrepareContent(ShopSectionModel sectionModel, RectTransform contentRoot)
        {
            try
            {
                var listContentModel = new List<ShopItemModel>();
                foreach (var packageRecord in sectionModel.SectionRecord.PackageRecords)
                {
                    var exchangePackage = this.ShopService.QueryExchangePackage(packageRecord.PackageId);
                    if (exchangePackage == null) continue;
                    if (!exchangePackage.IsUnlocked || exchangePackage.IsExpired) continue;

                    var itemModel = this.CreateItemModel(sectionModel, packageRecord, exchangePackage);
                    listContentModel.Add(itemModel);
                }

                sectionModel.HasPendingSizeChange.Value = await this.BindContent(listContentModel, contentRoot);
                this.OnBindContentComplete();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        protected virtual ShopItemModel CreateItemModel(ShopSectionModel sectionModel,
            ShopPackageRecord packageRecord, Model.ExchangePackageData exchangePackage)
        {
            var packageIcon = string.IsNullOrEmpty(packageRecord.OverrideIcon)
                ? exchangePackage.Record.PackageIcon
                : packageRecord.OverrideIcon;

            var packagePrefab = string.IsNullOrEmpty(packageRecord.OverridePrefabView)
                ? sectionModel.SectionRecord.SectionCommonPackagePrefabView
                : packageRecord.OverridePrefabView;

            var packageColorPalette = packageRecord.OverrideColorPalette?.Count == 0
                ? sectionModel.SectionRecord.SectionCommonPackageColorPalette
                : packageRecord.OverrideColorPalette;

            return new ShopItemModel(
                exchangePackage,
                packagePrefab,
                packageIcon,
                packageColorPalette,
                packageRecord.TagDescription,
                packageRecord.TagIconAssetPath);
        }

        protected abstract UniTask<bool> BindContent(List<ShopItemModel> models, RectTransform contentRoot);

        protected virtual void OnBindContentComplete() { }

        #endregion
    }

    /// <summary>
    /// Abstract base view for a shop section.
    /// </summary>
    public abstract class ShopSectionView : TViewMono
    {
        [field: SerializeField] public RectTransform  ContentRoot  { get; private set; }
        [field: SerializeField] public ThemeConfig    ThemeConfig  { get; private set; }
    }
}
