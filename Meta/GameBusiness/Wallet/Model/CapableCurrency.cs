namespace GameBusiness.Wallet.Model
{
    using System;

    public class CapableCurrency: Currency
    {
        public int CurrencyCap { get; private set; }

        public CapableCurrency(string id, int value, int currencyCap) : base(id, value)
        {
            this.CurrencyCap = currencyCap;
        }

        // Add currency from building or auto-gen (has cap)
        public override void Add(int value)
        {
            // add if current currency < cap
            if (this.Value < CurrencyCap)
            {
                base.Add(Math.Min(value, CurrencyCap - this.Value));
            }
        }

        public bool CheckOverCap(int value)
        {
            return this.Value + value > CurrencyCap;
        }

        public bool IsAtCap()
        {
            return this.Value >= this.CurrencyCap;
        }

        public void IncreaseCap(int amount)
        {
            this.CurrencyCap += amount;
        }
    }
}