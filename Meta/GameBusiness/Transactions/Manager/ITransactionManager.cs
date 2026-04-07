namespace GameBusiness.Transactions.Manager
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameBusiness.Transactions.Blueprint;
    using GameBusiness.Transactions.Model;

    public interface ITransactionManager
    {
        /// <summary>
        ///   Begins a transaction with the specified <see cref="transactionRecord"/>.
        /// </summary>
        /// <param name="transactionRecord"></param>
        /// <param name="quantity"></param>
        UniTask<TransactionResult> BeginTransaction(TransactionRecord transactionRecord, int quantity = 1);

        /// <summary>
        ///  Begins a transaction with the specified <see cref="costRecords"/> and <see cref="payoutRecords"/>.
        /// </summary>
        /// <param name="costRecords"></param>
        /// <param name="payoutRecords"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        UniTask<TransactionResult> BeginTransaction(IReadOnlyCollection<CostRecord> costRecords, IReadOnlyCollection<PayoutRecordAbstract> payoutRecords, string location, int quantity = 1);

        /// <summary>
        /// Used for installment payments
        /// </summary>
        /// <param name="paymentProgresses"></param>
        /// <param name="payoutRecords"></param>
        /// <param name="repeat"></param>
        /// <returns></returns>
        UniTask<TransactionResult> BeginTransaction(IReadOnlyCollection<PaymentProgress> paymentProgresses, IReadOnlyCollection<PayoutRecordAbstract> payoutRecords, string location, int quantity = 1);

        /// <summary>
        ///  Verifies the costs with the specified <see cref="costRecords"/>.
        /// </summary>
        /// <param name="costRecords"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        bool VerifyCosts(IReadOnlyCollection<CostRecord> costRecords, int quantity = 1);
        
        /// <summary>
        ///  Verifies the cost with the specified <see cref="costRecord"/>.
        /// </summary>
        /// <param name="costRecord"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        bool VerifyCost(CostRecord costRecord, int quantity = 1);

        /// <summary>
        ///  Makes payments with the specified <see cref="costRecords"/>.
        /// </summary>
        /// <param name="costRecords"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        UniTask MakePayments(IReadOnlyCollection<CostRecord> costRecords, string location, int quantity = 1);

        /// <summary>
        /// Makes payments with the specified <see cref="paymentProgresses"/>.
        /// </summary>
        /// <param name="paymentProgresses"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        UniTask<bool> MakePayments(IReadOnlyCollection<PaymentProgress> paymentProgresses, string location, int quantity = 1);

        /// <summary>
        ///  Generates all payout assets in payouts list (automatically flattened)
        /// </summary>
        /// <param name="payouts"></param>
        /// <param name="repeat"></param>
        /// <returns></returns>
        List<Asset> GetPayoutAssets(IReadOnlyCollection<PayoutRecordAbstract> payouts, int repeat = 1);

        /// <summary>
        ///  Try to generate all random payout assets in assets list then flatten it.
        /// </summary>
        /// <param name="assets"></param>
        List<Asset> FlattenPayoutAssets(List<Asset> assets);
        
        /// <summary>
        /// Receives the payouts from the specified <see cref="transactionResult"/>.
        /// </summary>
        /// <param name="transactionResult"></param>
        /// <param name="isFlatten"> Is Flatten the assets before receive </param>
        /// <returns></returns>
        UniTask ReceivePayouts(TransactionResult transactionResult, bool isFlatten = false);
        
        UniTask ReceivePayout(Asset asset, string location);

        List<string> GeneratePayoutDescription(List<Asset> assetsRecord);
    }
}