namespace GameBusiness.Wallet.Signal
{
    public class EarnCurrencySignal : SpendCurrencySignal
    {
        public EarnCurrencySignal(string id, int value) : base(id, value) { }
    }

    public class SpendCurrencySignal
    {
        public string Id    { get; set; }
        public int    Value { get; set; }

        public SpendCurrencySignal(string id, int value)
        {
            this.Id    = id;
            this.Value = value;
        }
    }
}