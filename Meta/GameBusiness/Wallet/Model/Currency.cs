namespace GameBusiness.Wallet.Model
{
    using GameBusiness.Wallet.Blueprint;
    using Newtonsoft.Json;

    public partial class Currency
    {
        private int value;

        public string Id { get; }

        public int Value
        {
            get => this.value;
            protected set
            {
                if (this.value == value) return;
                this.value = value;
                if (this.isDisposed)
                    return;

                this.RaiseOnNext(this);
            }
        }

        [JsonIgnore] public ResourceRecord StaticData;

        public Currency(string id, int value)
        {
            this.Id  = id;
            this.Value = value;
        }

        public virtual bool HasValue(int value) { return this.Value >= value; }

        public virtual bool Remove(int value)
        {
            if (!this.HasValue(value)) return false;
            this.Value -= value;
            return true;
        }
        
        public virtual void Add(int value) { this.Value += value; }

        public override string ToString() { return this.Value.ToString(); }
    }
}