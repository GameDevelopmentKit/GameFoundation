namespace GameBusiness.Shop.UI
{
    using System;
    using GameBusiness.Shop.Manager;
    using GameBusiness.Shop.Model;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Model;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    /// <summary>
    /// Abstract base for a button that initiates a shop purchase.
    /// Handles cost text display, purchase-left counter, badge, and the click-to-purchase loop.
    /// Subclass to provide error handling (toasts), analytics, and reward presentation.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public abstract class BasePurchaseButton : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] protected int        quantity = 1;
        [SerializeField] private   TMP_Text   txtPrice;
        [SerializeField] private   GameObject notifyBadge;
        [SerializeField] private   TMP_Text   txtPurchaseLeft;

        #endregion

        #region Injected Dependencies

        [Inject] private IShopService      shopService;

        #endregion

        #region Runtime State

        protected ExchangePackageData             currentPackage;
        protected Action<bool, TransactionResult> onPurchaseComplete;

        private int internalQuantity;

        #endregion

        #region Properties

        protected IShopService      ShopService      => this.shopService;
        protected int               InternalQuantity  => this.internalQuantity;
        protected TMP_Text          TxtPrice          => this.txtPrice;

        #endregion

        #region Public API

        public virtual void BindData(string packageId, int overrideQuantity = -1,
            Action<bool, TransactionResult> purchaseComplete = null)
        {
            var exchangePackageData = this.shopService.QueryExchangePackage(packageId);
            this.BindData(exchangePackageData, overrideQuantity, purchaseComplete);
        }

        public virtual void BindData(ExchangePackageData exchangePackageData, int overrideQuantity = -1,
            Action<bool, TransactionResult> purchaseComplete = null)
        {
            this.GetComponent<Button>().onClick.RemoveAllListeners();
            this.GetComponent<Button>().onClick.AddListener(this.OnClickBtnPurchase);

            this.currentPackage     = exchangePackageData;
            this.internalQuantity   = overrideQuantity > 0 ? overrideQuantity : this.quantity;
            this.onPurchaseComplete = purchaseComplete;

            var canPurchase = this.shopService.TryGetPossiblePurchaseOptions(
                exchangePackageData, this.internalQuantity, out var purchaseOptions);

            if (this.txtPrice != null)
            {
                var cost = this.shopService.GenerateCostText(
                    purchaseOptions.Record.Costs, canPurchase, this.internalQuantity);
                this.SetCostText(cost);
            }

            if (this.notifyBadge != null)
            {
                this.notifyBadge.SetActive(purchaseOptions.Record.Costs.Exists(c =>
                    c.PaymentType == PaymentTypes.Ads || c.PaymentType == PaymentTypes.Free));
            }

            this.BindPurchaseLeftDisplay(purchaseOptions);
        }

        #endregion

        #region Protected Hooks

        protected virtual void SetCostText(string costText)
        {
            if (this.txtPrice != null)
            {
                this.txtPrice.text = costText;
            }
        }

        protected virtual void BindPurchaseLeftDisplay(PurchaseOptionData purchaseOptions)
        {
            if (this.txtPurchaseLeft == null) return;
            this.txtPurchaseLeft.gameObject.SetActive(false);

            if (purchaseOptions.IsLimited)
            {
                this.txtPurchaseLeft.gameObject.SetActive(true);
                this.txtPurchaseLeft.text = purchaseOptions.IsOneTimePurchase
                    ? "1x"
                    : purchaseOptions.RemainAmount.ToString();
            }
        }

        protected abstract void OnClickBtnPurchase();

        protected abstract void OnPurchaseComplete(TransactionResult transactionResult);

        #endregion
    }
}
