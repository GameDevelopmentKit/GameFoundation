namespace GameBusiness.Shop.UI.SectionView
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameBusiness.Shop.Manager;
    using GameBusiness.Shop.UI;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.Utilities.UILayoutElement;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using TMPro;
    using UnityEngine;
    using Zenject;
    public class ShopSectionGridView : ShopSectionView
    {
        [HideInInspector] public FollowLayoutElementPreferredSize layout;
        public                   TMP_Text                         txtRefreshIn;

        private void Awake()
        {
            this.layout = this.GetComponent<FollowLayoutElementPreferredSize>();
        }
    }

    [ShopSectionType("GridView")]
    public class ShopSectionGridPresenter : BaseShopSectionPresenter
    {
        private readonly ObjectPoolManager objectPoolManager;
        private          IDisposable       refreshDisposable;

        public ShopSectionGridPresenter(IGameAssets gameAssets, IShopService shopService, DiContainer diContainer, ObjectPoolManager objectPoolManager) : base(gameAssets, shopService, diContainer)
        {
            this.objectPoolManager = objectPoolManager;
        }
        protected override async UniTask<bool> BindContent(List<ShopItemModel> models, RectTransform contentRoot)
        {
            try
            {
                // clear old content
                for (int i = contentRoot.childCount - 1; i >= 0; i--)
                {
                    var child = contentRoot.GetChild(i);
                    child.Recycle();
                }

                foreach (var model in models)
                {
                    var itemView = await this.objectPoolManager.Spawn<BaseShopItemView>(model.PrefabName);
                    itemView.transform.SetParent(contentRoot, false);
                    itemView.transform.localScale = Vector3.one;
                    this.DiContainer.Inject(itemView);
                    itemView.BindData(model);
                    this.OnItemSpawned(itemView, model);
                }

                if (this.View is ShopSectionGridView gridView && gridView.txtRefreshIn != null)
                {
                    refreshDisposable?.Dispose();
                    var minTimeSpan = TimeSpan.MaxValue;
                    foreach (var itemModel in models)
                    {
                        foreach (var purchaseOption in itemModel.ExchangePackage.PurchaseOptions)
                        {
                            if (minTimeSpan > purchaseOption.RefreshIn)
                            {
                                minTimeSpan = purchaseOption.RefreshIn;
                            }
                        }
                    }
                    if (minTimeSpan != TimeSpan.MaxValue)
                    {
                        gridView.txtRefreshIn.gameObject.SetActive(true);
                        refreshDisposable = gridView.txtRefreshIn.CountDown(minTimeSpan, onComplete: RefreshContent);
                    }
                    else
                    {
                        gridView.txtRefreshIn.gameObject.SetActive(false);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        protected virtual void OnItemSpawned(BaseShopItemView itemView, ShopItemModel model) { }

        public override void Dispose()
        {
            base.Dispose();
            refreshDisposable?.Dispose();
        }
    }
}
