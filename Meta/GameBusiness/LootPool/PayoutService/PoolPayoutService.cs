namespace GameBusiness.LootPool.PayoutService
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameBusiness.LootPool.Service;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Manager;
    using GameBusiness.Transactions.Model;
    using GameBusiness.Transactions.PayoutService;

    public class PoolPayoutService : IRandomGeneratePayoutService
    {
        public string AssetType => AssetDefaultType.Pool;
        public bool IsAutoGenerate => true;

        private readonly LootPoolService lootPoolService;
        private readonly ITransactionManager transactionManager;

        public PoolPayoutService(LootPoolService lootPoolService, ITransactionManager transactionManager)
        {
            this.lootPoolService = lootPoolService;
            this.transactionManager = transactionManager;
        }

        public UniTask ReceivePayout(Asset asset, string location)
        {
            return this.transactionManager.ReceivePayouts(new TransactionResult(location, this.GenerateRandomPayout(asset)));
        }

        public List<Asset> GenerateRandomPayout(Asset asset)
        {
            var results = new List<Asset>();

            for (var i = 0; i < asset.Amount; i++)
            {
                results.AddRange(this.lootPoolService.GetDropAssets(asset.AssetId));
            }

            return this.transactionManager.FlattenPayoutAssets(results);
        }

        public string GetPayoutDescription(Asset asset)
        {
            return string.Empty;
        }
    }
}
