namespace GameBusiness.Wallet.Model
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public partial class Currency : IObservable<Currency>, IDisposable
    {
        [JsonIgnore] private readonly List<IObserver<Currency>> observers = new();
        [JsonIgnore] private          bool                      isDisposed;
        
        public IDisposable Subscribe(IObserver<Currency> observer)
        {
            if (this.isDisposed)
            {
                observer.OnCompleted();
            }
            else
            {
                // raise latest value on subscribe
                observer.OnNext(this);

                if (!this.observers.Contains(observer)) this.observers.Add(observer);
            }

            return new Unsubscriber(this.observers, observer);
        }

        private void RaiseOnNext(Currency newValue)
        {
            foreach (var observer in this.observers)
                observer.OnNext(newValue);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed) return;

            this.isDisposed = true;

            foreach (var observer in this.observers)
                observer.OnCompleted();

            this.observers.Clear();
        }

        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<Currency>> observers;
            private readonly IObserver<Currency>       observer;

            public Unsubscriber(List<IObserver<Currency>> observers, IObserver<Currency> observer)
            {
                this.observers = observers;
                this.observer  = observer;
            }

            public void Dispose()
            {
                if (this.observer != null) this.observers.Remove(this.observer);
            }
        }
    }
}