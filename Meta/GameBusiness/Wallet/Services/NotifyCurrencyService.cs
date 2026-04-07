namespace GameBusiness.Wallet.Services
{
    using System;
    using System.Collections.Generic;
    using GameBusiness.Wallet.Model;
    using UniRx;

    public class NotifyCurrencyService : IDisposable
    {
        private readonly CompositeDisposable                     compositeDisposable = new();
        private readonly Dictionary<string, ValueRecord>         valueRecords        = new();
        private readonly Dictionary<string, Action<ValueRecord>> listSubscriber      = new();

        public class ValueRecord
        {
            public int PreviousValue;
            public int NewestValue;
            public ValueRecord(int previousValue, int newestValue)
            {
                this.PreviousValue = previousValue;
                this.NewestValue   = newestValue;
            }
        }

        public void RegisterTracking(Currency source)
        {
            this.valueRecords[source.Id] = new ValueRecord(source.Value, source.Value);
            this.compositeDisposable.Add(source.Subscribe<Currency>(this.OnCurrencyChangeValue));
        }

        private void OnCurrencyChangeValue(Currency currencyChange)
        {
            var valueRecord = this.valueRecords[currencyChange.Id];
            valueRecord.NewestValue = currencyChange.Value;
            if (valueRecord.NewestValue != valueRecord.PreviousValue)
                this.Dispatch(currencyChange);
        }

        public void Subscribe(Currency source, Action<ValueRecord> callback, out NotifyListener notifyListener)
        {
            if (!this.valueRecords.ContainsKey(source.Id))
            {
                this.RegisterTracking(source);
            }

            if (!this.listSubscriber.ContainsKey(source.Id))
            {
                this.listSubscriber.Add(source.Id, null);
            }

            notifyListener = new NotifyListener(this, source, callback);

            callback?.Invoke(this.valueRecords[source.Id]);
            this.listSubscriber[source.Id] += callback;
        }

        public void Dispatch(Currency source)
        {
            if (this.listSubscriber.TryGetValue(source.Id, out var subscriber))
            {
                subscriber?.Invoke(this.valueRecords[source.Id]);
            }
        }


        public void Dispose()
        {
            this.compositeDisposable?.Dispose();
            this.listSubscriber.Clear();
            this.valueRecords.Clear();
        }

        public class NotifyListener : IDisposable
        {
            private NotifyCurrencyService notifyCurrencyService;
            private Action<ValueRecord>   action;
            private Currency              source;

            public NotifyListener(NotifyCurrencyService notifyCurrencyService, Currency source, Action<ValueRecord> action)
            {
                this.notifyCurrencyService = notifyCurrencyService;
                this.action                = action;
                this.source                = source;
            }


            public void MarkRead()
            {
                if (this.notifyCurrencyService.valueRecords.TryGetValue(this.source.Id, out var valueRecord))
                {
                    valueRecord.PreviousValue = valueRecord.NewestValue;
                }
            }

            public void Dispose() { this.notifyCurrencyService.listSubscriber[this.source.Id] -= this.action; }
        }
    }
}