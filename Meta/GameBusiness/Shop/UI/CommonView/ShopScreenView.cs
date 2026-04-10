using System;
using System.Collections.Generic;
using GameFoundation.Scripts.Utilities.Extension;

namespace GameBusiness.Shop.UI.CommonView
{
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameBusiness.Shop.Manager;
    using GameBusiness.Shop.UI.SectionView;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    public class ShopScreenView : BaseView
    {
        public ShopListSectionAdapter shopListSectionAdapter;
        public Button btnClose;
    }

    [ScreenInfo(nameof(ShopScreenView))]
    public class ShopScreenPresenter : BaseScreenPresenter<ShopScreenView>
    {
        private readonly DiContainer diContainer;
        private readonly IShopService shopService;

        private bool isInitialized = false;

        private readonly Dictionary<string, Type> sectionPresenterTypes = new();

        public ShopScreenPresenter(SignalBus signalBus, DiContainer diContainer, IShopService shopService) :
            base(signalBus)
        {
            this.diContainer = diContainer;
            this.shopService = shopService;
            this.InitSectionPresenterTypes();
        }

        protected override void OnViewReady()
        {
            base.OnViewReady();
            this.View.btnClose.onClick.AddListener(CloseView);
        }

        public override async UniTask BindData()
        {
            if (!isInitialized && shopService.TryGetDefaultShopLayoutRecord(out var layoutRecord))
            {
                var listModel = layoutRecord.Sections.Where(record =>
                        this.sectionPresenterTypes.ContainsKey(record.SectionTypeToPrefabView.Item1))
                    .OrderBy(record => record.SectionDisplayOrder)
                    .Select(record =>
                        new ShopSectionModel(record, this.sectionPresenterTypes[record.SectionTypeToPrefabView.Item1]))
                    .ToList();
                await this.View.shopListSectionAdapter.InitItemAdapter(listModel, this.diContainer)
                    .ContinueWith(() => isInitialized = false);
            }

            this.SignalBus.Subscribe<ShopPurchasePackageSuccessSignal>(OnPackagePurchaseSuccess);
        }

        private void InitSectionPresenterTypes()
        {
            var sectionPresenters = ReflectionUtils.GetAllDerivedTypes<IShopSectionPresenter>();
            foreach (var sectionPresenter in sectionPresenters)
            {
                var attribute =
                    (ShopSectionTypeAttribute)Attribute.GetCustomAttribute(sectionPresenter,
                        typeof(ShopSectionTypeAttribute));
                if (attribute != null)
                {
                    this.sectionPresenterTypes.Add(attribute.Type, sectionPresenter);
                }
                else
                {
                    Debug.LogError($"ShopSectionTypeAttribute not found for {sectionPresenter.Name}");
                }
            }
        }

        private void OnPackagePurchaseSuccess(ShopPurchasePackageSuccessSignal obj)
        {
            this.View.shopListSectionAdapter.ForceUpdateVisibleItems();
        }

        public override void Dispose()
        {
            this.SignalBus.Unsubscribe<ShopPurchasePackageSuccessSignal>(OnPackagePurchaseSuccess);
        }
    }
}
