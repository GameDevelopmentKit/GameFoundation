namespace GameBusiness.Transactions.Manager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Exceptions;
    using GameBusiness.Transactions.Model;
    using GameBusiness.Transactions.PaymentService;
    using GameBusiness.Transactions.PayoutService;
    using Zenject;
    using Random = System.Random;

    public class TransactionManager : ITransactionManager, IInitializable
    {
        public UniTask<TransactionResult> BeginTransaction(TransactionRecord transactionRecord, int quantity = 1)
        {
            return this.BeginTransaction(transactionRecord.Costs, transactionRecord.Payouts, transactionRecord.Location, quantity);
        }

        public async UniTask<TransactionResult> BeginTransaction(IReadOnlyCollection<CostRecord> costRecords,
            IReadOnlyCollection<PayoutRecordAbstract> payoutRecords, string location, int quantity = 1)
        {
            await this.MakePayments(costRecords, location, quantity);

            // Process payout
            return new TransactionResult(costRecords, payoutRecords, location, this.GetPayoutAssets(payoutRecords, quantity));
        }

        public async UniTask<TransactionResult> BeginTransaction(IReadOnlyCollection<PaymentProgress> paymentProgresses,
            IReadOnlyCollection<PayoutRecordAbstract> payoutRecords, string location, int quantity = 1)
        {
            await this.MakePayments(paymentProgresses, location, quantity);

            // Process payout
            return new TransactionResult(
                paymentProgresses.Select(x => x.CostRecord).ToList(),
                payoutRecords,
                location,
                this.GetPayoutAssets(payoutRecords, quantity));
        }

        #region Payment
        /// <summary>
        /// // check payment service is exist and available
        /// </summary>
        /// <param name="paymentType"></param>
        /// <param name="paymentService"></param>
        /// <returns></returns>
        public bool TryGetPaymentService(string paymentType, out IPaymentService paymentService)
        {
            return this.paymentTypeToService.TryGetValue(paymentType, out paymentService);
        }


        public bool VerifyCosts(IReadOnlyCollection<CostRecord> costRecords, int quantity = 1)
        {
            foreach (var costRecord in this.FlattenCostRecords(costRecords, quantity))
            {
                if (this.TryGetPaymentService(costRecord.PaymentType, out var paymentService) &&
                    paymentService.VerifyCost(costRecord.CostAssetId, costRecord.CostAmount)) continue;
                return false;
            }

            return true;
        }

        public bool VerifyCost(CostRecord costRecord, int quantity = 1)
        {
            return this.TryGetPaymentService(costRecord.PaymentType, out var paymentService) &&
                   paymentService.VerifyCost(costRecord.CostAssetId, costRecord.CostAmount);
        }

        public async UniTask MakePayments(IReadOnlyCollection<CostRecord> costRecords, string location, int quantity = 1)
        {
            // Verify all costs
            var flattenCostRecords = this.FlattenCostRecords(costRecords, quantity).ToList();
            foreach (var costRecord in flattenCostRecords)
            {
                if (!this.TryGetPaymentService(costRecord.PaymentType, out var paymentService))
                {
                    throw new PaymentServiceException(costRecord, $"Payment service {costRecord.PaymentType} is not available");
                }

                if (!paymentService.VerifyCost(costRecord.CostAssetId, costRecord.CostAmount))
                {
                    throw new InsufficientAssetException(costRecord, $"Payment service {costRecord.PaymentType}: not enough {costRecord.CostAssetId}");
                }
            }

            // Make payment and wait for all payment to complete
            var paymentTasks = flattenCostRecords.Select(costRecord =>
                this.TryGetPaymentService(costRecord.PaymentType, out var paymentService)
                    ? paymentService.MakePayment(costRecord.CostAssetId, costRecord.CostAmount)
                    : UniTask.CompletedTask);
            await UniTask.WhenAll(paymentTasks);

            makePaymentSuccessSignal.CostRecords = flattenCostRecords;
            makePaymentSuccessSignal.Location    = location;
            signalBus.Fire(makePaymentSuccessSignal);
        }

        public async UniTask<bool> MakePayments(IReadOnlyCollection<PaymentProgress> paymentProgresses, string location, int quantity = 1)
        {
            // pay anything service has
            foreach (var paymentProgress in paymentProgresses)
            {
                if (paymentProgress.IsCompleted) continue;

                var paymentType = paymentProgress.CostRecord.PaymentType;
                if (this.TryGetPaymentService(paymentType, out var paymentService) &&
                    paymentService.Available(paymentProgress.CostRecord.CostAssetId))
                {
                    var remainingAmount = paymentProgress.RemainingAmount;
                    var remainingAmountAfterPayment =
                        await paymentService.MakePayment(paymentProgress.CostRecord.CostAssetId, remainingAmount * quantity);
                    paymentProgress.RemainingAmount = remainingAmountAfterPayment;
                }
            }

            return paymentProgresses.All(paymentProgress => paymentProgress.IsCompleted);
        }

        private IEnumerable<CostRecord> FlattenCostRecords(IReadOnlyCollection<CostRecord> costRecords, int repeat = 1)
        {
            return costRecords
                .GroupBy(x => new { x.CostAssetId, x.PaymentType })
                .Select(g => new CostRecord()
                {
                    CostAssetId = g.Key.CostAssetId,
                    CostAmount  = g.Sum(x => x.CostAmount) * repeat,
                    PaymentType = g.Key.PaymentType
                });
        }
        #endregion

        #region Payout
        public List<Asset> GetPayoutAssets(IReadOnlyCollection<PayoutRecordAbstract> payouts, int repeat = 1)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            var result = new List<Asset>();
            for (int i = 0; i < repeat; i++)
            {
                result.AddRange(payouts.Where(payoutRecord => !(payoutRecord.Chance < random.NextDouble()))
                    .Select(payoutRecord => new Asset()
                    {
                        AssetId   = payoutRecord.PayoutAssetId,
                        Amount    = payoutRecord.GetAmount(),
                        AssetType = payoutRecord.AssetType
                    }));
            }

            return this.FlattenPayoutAssets(result);
        }

        public List<Asset> FlattenPayoutAssets(List<Asset> assets)
        {
            assets = GroupAsset(assets);

            // try to generate all random payout for each asset first
            for (int index = 0; index < assets.Count;)
            {
                var asset = assets[index];
                if (this.TryGetPayoutService(asset.AssetType, out var payoutService) &&
                    payoutService is IRandomGeneratePayoutService randomPayoutService && randomPayoutService.IsAutoGenerate)
                {
                    assets.AddRange(randomPayoutService.GenerateRandomPayout(asset));
                    assets.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            return GroupAsset(assets);

            List<Asset> GroupAsset(List<Asset> inputAssets)
            {
                return inputAssets
                    .GroupBy<Asset, object>(x => x.GetGroupableKey())
                    .Select(g =>
                    {
                        var asset = g.First();
                        asset.Amount = g.Sum(x => x.Amount);
                        return asset;
                    })
                    .ToList();
            }
        }

        bool TryGetPayoutService(string type, out IPayoutService payoutService) => this.payoutTypeToService.TryGetValue(type, out payoutService);

        public async UniTask ReceivePayouts(TransactionResult transactionResult, bool isFlatten = false)
        {
            var assets = transactionResult.Assets;
            if (isFlatten)
            {
                assets = this.FlattenPayoutAssets(assets);
            }

            var payoutTasks = assets.Select(asset =>
                this.TryGetPayoutService(asset.AssetType, out var payoutService)
                    ? payoutService.ReceivePayout(asset, transactionResult.Location)
                    : UniTask.CompletedTask);
            await UniTask.WhenAll(payoutTasks);

            payoutsReceivedSignal.transactionResult = transactionResult;
            signalBus.Fire(payoutsReceivedSignal);
        }

        public UniTask ReceivePayout(Asset asset, string location)
        {
            if (this.TryGetPayoutService(asset.AssetType, out var payoutService))
            {
                var receivePayout = payoutService.ReceivePayout(asset, location);
                receivePayout.ContinueWith(() =>
                {
                    payoutReceivedSignal.PayoutAsset = asset;
                    payoutReceivedSignal.Location    = location;
                    signalBus.Fire(payoutReceivedSignal);
                });
                return receivePayout;
            }
            else
                return UniTask.CompletedTask;
        }

        public List<string> GeneratePayoutDescription(List<Asset> assetsRecord)
        {
            return assetsRecord.Select(asset =>
            {
                if (this.TryGetPayoutService(asset.AssetType, out var payoutService))
                {
                    return payoutService.GetPayoutDescription(asset);
                }
                return string.Empty;
            }).ToList();
        }
        #endregion


        #region Inject
        private          Dictionary<string, IPaymentService> paymentTypeToService;
        private          Dictionary<string, IPayoutService>  payoutTypeToService;
        private readonly DiContainer                         diContainer;
        private readonly SignalBus                           signalBus;

        private readonly MakePaymentSuccessSignal makePaymentSuccessSignal = new MakePaymentSuccessSignal();
        private readonly PayoutsReceivedSignal    payoutsReceivedSignal    = new PayoutsReceivedSignal();
        private readonly PayoutReceivedSignal     payoutReceivedSignal     = new PayoutReceivedSignal();

        public TransactionManager(DiContainer diContainer, SignalBus signalBus)
        {
            this.diContainer = diContainer;
            this.signalBus   = signalBus;
        }
        #endregion

        public void Initialize()
        {
            this.paymentTypeToService = diContainer.ResolveAll<IPaymentService>().ToDictionary(x => x.PaymentType);
            this.payoutTypeToService  = diContainer.ResolveAll<IPayoutService>().ToDictionary(x => x.AssetType);
        }
    }
}
