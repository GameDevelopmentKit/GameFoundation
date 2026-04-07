namespace GameBusiness.Wallet.Manager
{
    using System.Collections.Generic;
    using DataManager.UserData;
    using GameBusiness.Wallet.Blueprint;
    using GameBusiness.Wallet.Model;
    using GameBusiness.Wallet.Signal;
    using Zenject;

    public class WalletManager : BaseDataManager<WalletData>, IWalletManager
    {
        public Currency Get(string currencyId)         { return this.Data.Balances.GetValueOrDefault(currencyId); }
        public bool     HasCurrency(string currencyId) { return this.Data.Balances.ContainsKey(currencyId); }

        public ResourceRecord                GetStaticData(string currencyId) { return this.resourceBlueprint.GetDataById(currencyId); }
        public IReadOnlyCollection<Currency> GetAllBalances()                 { return this.Data.Balances.Values; }

        public bool CanPay(string currencyId, int value) { return this.Data.Balances.ContainsKey(currencyId) && this.Data.Balances[currencyId].HasValue(value); }

        public void Add(string currencyId, int value)
        {
            if (!this.Data.Balances.ContainsKey(currencyId))
            {
                var staticData = this.resourceBlueprint.GetDataById(currencyId);

                // check if has cap
                if (staticData.HasCap)
                {
                    this.Data.Balances.Add(currencyId, new CapableCurrency(currencyId, value, staticData.DefaultCapValue));
                }
                else
                {
                    this.Data.Balances.Add(currencyId, new Currency(currencyId, value) { StaticData = staticData });
                }
            }
            this.SignalBus.Fire(new EarnCurrencySignal(currencyId, value));
            this.Data.Balances[currencyId].Add(value);
        }

        public bool Pay(string currencyId, int value)
        {
            var canPay= this.CanPay(currencyId, value) && this.Data.Balances[currencyId].Remove(value);

            if (canPay)
            {
                this.SignalBus.Fire(new SpendCurrencySignal(currencyId, value));
            }

            return canPay;
        }

        public int TryPay(string currencyId, int value)
        {
            if (!this.Data.Balances.ContainsKey(currencyId))
                return value;

            var currency = this.Data.Balances[currencyId];

            if (currency.Value >= value)
            {
                currency.Remove(value);

                return 0;
            }

            var remain = value - currency.Value;
            currency.Remove(currency.Value);
            this.SignalBus.Fire(new SpendCurrencySignal(currencyId, value));
            return remain;
        }

        #region Inject

        private readonly ResourceBlueprint resourceBlueprint;

        public WalletManager(SignalBus signalBus, ResourceBlueprint resourceBlueprint) : base(signalBus)
        {
            this.resourceBlueprint = resourceBlueprint;
        }

        public override void OnDataInitialized()
        {
            //Init wallet
            foreach (var resource in this.resourceBlueprint)
            {
                if (this.Data.Balances.ContainsKey(resource.Key)) continue;

                if (resource.Value.HasCap)
                {
                    this.Data.Balances.Add(resource.Key, new CapableCurrency(resource.Value.Id, resource.Value.DefaultValue, resource.Value.DefaultCapValue));
                }
                else
                {
                    this.Data.Balances.Add(resource.Key, new Currency(resource.Value.Id, resource.Value.DefaultValue));
                }
            }

            //Init static data
            var balances = this.Data.Balances;

            foreach (var currency in balances)
            {
                currency.Value.StaticData = this.resourceBlueprint.GetDataById(currency.Key);
            }
        }

        #endregion
    }
}