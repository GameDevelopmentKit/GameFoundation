namespace GameBusiness.Transactions.PayoutService
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameBusiness.Transactions.Model;

    public interface IPayoutService
    {
        string  AssetType { get; }
        UniTask ReceivePayout(Asset asset, string location);
        string  GetPayoutDescription(Asset asset);
    }

    public interface IRandomGeneratePayoutService : IPayoutService
    {
        bool        IsAutoGenerate { get;  }
        List<Asset> GenerateRandomPayout(Asset asset);
    }
}
