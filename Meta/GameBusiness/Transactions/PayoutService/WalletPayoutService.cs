namespace GameBusiness.Transactions.PayoutService
{
    using Cysharp.Threading.Tasks;
    using GameBusiness.Transactions.Model;
    using GameBusiness.Wallet.Manager;

    public class WalletPayoutService : IPayoutService
    {
        readonly IWalletManager walletManager;
        public   string         AssetType => Blueprint.AssetDefaultType.Currency;

        public WalletPayoutService(IWalletManager walletManager) { this.walletManager = walletManager; }

        public UniTask ReceivePayout(Asset asset, string location)
        {
            this.walletManager.Add(asset.AssetId, (int) asset.Amount);
            return UniTask.CompletedTask;
        }

        public string GetPayoutDescription(Asset asset)
        {
            return "";
        }
    }
}