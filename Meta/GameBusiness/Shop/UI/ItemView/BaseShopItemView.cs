namespace GameBusiness.Shop.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
    using GameFoundation.Scripts.UIModule.Utilities.LoadImage;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    /// <summary>
    /// Abstract base view for a single shop item. Handles theme application,
    /// title/tag display, reward amount, and purchase button binding.
    /// Subclass to add game-specific icon loading, asset display, and extra fields.
    /// </summary>
    public abstract class BaseShopItemView : TViewMono
    {
        #region Serialized Fields

        [SerializeField] private   TMP_Text                txtTitle;
        [SerializeField] private   Image                   imgIcon;
        [SerializeField] protected List<BasePurchaseButton> btnPurchase;
        [SerializeField] private   Image                   iconTag;
        [SerializeField] private   TMP_Text                txtTag;
        [SerializeField] private   ThemeConfig             themeConfig;
        [SerializeField] private   TMP_Text                txtRewardAmount;
        [SerializeField] private   string                  rewardFormat = "{0}";

        #endregion

        #region Properties

        protected Image      ImgIcon         => this.imgIcon;
        protected TMP_Text   TxtTitle        => this.txtTitle;
        protected TMP_Text   TxtRewardAmount => this.txtRewardAmount;
        protected string     RewardFormat    => this.rewardFormat;
        protected ThemeConfig ThemeConfig     => this.themeConfig;
        protected List<BasePurchaseButton> BtnPurchase => this.btnPurchase;

        #endregion

        [Inject] protected LoadImageHelper LoadImageHelper;
        
        
        #region Public API

        public virtual void BindData(ShopItemModel model)
        {
            if (this.themeConfig != null && model.ColorPalette != null)
            {
                this.themeConfig.ApplyTheme(model.ColorPalette);
            }

            if (this.iconTag != null)
            {
                this.iconTag.gameObject.SetActive(false);
            }

            if (!string.IsNullOrEmpty(model.TagDescription) && this.txtTag != null)
            {
                this.txtTag.text = model.TagDescription;
                this.BindTagIcon(model);
            }

            this.BindTitle(model);
            this.BindIcon(model);
            this.BindRewardAmount(model);
            this.BindPurchaseButton(model);
        }

        #endregion

        #region Protected Hooks

        protected virtual void BindTitle(ShopItemModel model)
        {
            if (this.txtTitle != null)
            {
                this.txtTitle.text = model.ExchangePackage.Record?.PackageName ?? string.Empty;
            }
        }

        protected virtual void BindIcon(ShopItemModel model)
        {
            if (!string.IsNullOrEmpty(model.PackageIcon))
            {
                this.LoadImageHelper.LoadLocalSprite(model.PackageIcon).ContinueWith(iconSprite =>
                {
                    this.ImgIcon.sprite = iconSprite;
                }).Forget();
            }
        }

        protected virtual void BindTagIcon(ShopItemModel model)
        {
            if (this.iconTag != null)
            {
                this.iconTag.gameObject.SetActive(!string.IsNullOrEmpty(model.TagDescription));
            }
        }

        protected virtual void BindRewardAmount(ShopItemModel model)
        {
            if (this.txtRewardAmount == null) return;

            this.txtRewardAmount.gameObject.SetActive(false);

            var payouts = model.ExchangePackage.GetPayouts();
            if (payouts.Count == 1 && payouts[0].PayoutAmount > 1)
            {
                this.txtRewardAmount.gameObject.SetActive(true);
                this.txtRewardAmount.text = string.Format(this.rewardFormat, payouts[0].PayoutAmount);
            }
        }

        protected virtual void BindPurchaseButton(ShopItemModel model)
        {
            if (this.btnPurchase == null) return;

            foreach (var btn in this.btnPurchase)
            {
                btn.BindData(model.ExchangePackage);
            }
        }

        #endregion
    }
}
