namespace Zenject
{
    using System;
    using System.Collections.Generic;
    using MessagePipe;

    public class SignalBus : ISignalBus, ILateDisposable
    {
        private readonly DiContainer container;

        private readonly Dictionary<(Type SignalType, Delegate Callback), IDisposable> subscriptions = new();

        public SignalBus(DiContainer container) { this.container = container; }

        public virtual void Fire<TSignal>() { this.GetPublisher<TSignal>().Publish(default); }

        public virtual void Fire<TSignal>(TSignal signal) { this.GetPublisher<TSignal>().Publish(signal); }

        public virtual void Subscribe<TSignal>(Action callback)
        {
            if (!this.TrySubscribeInternal<TSignal>(callback)) throw new ArgumentException($"Callback {typeof(TSignal)} already subscribed");
        }

        public virtual void Subscribe<TSignal>(Action<TSignal> callback)
        {
            if (!this.TrySubscribeInternal<TSignal>(callback)) throw new ArgumentException($"Callback {typeof(TSignal)} already subscribed");
        }

        public virtual bool TrySubscribe<TSignal>(Action callback) { return this.TrySubscribeInternal<TSignal>(callback); }

        public virtual bool TrySubscribe<TSignal>(Action<TSignal> callback) { return this.TrySubscribeInternal<TSignal>(callback); }

        public virtual void Unsubscribe<TSignal>(Action callback)
        {
            if (!this.TryUnsubscribeInternal<TSignal>(callback)) throw new ArgumentException($"Callback {typeof(TSignal)} not subscribed");
        }

        public virtual void Unsubscribe<TSignal>(Action<TSignal> callback)
        {
            if (!this.TryUnsubscribeInternal<TSignal>(callback)) throw new ArgumentException($"Callback {typeof(TSignal)} not subscribed");
        }

        public virtual bool TryUnsubscribe<TSignal>(Action callback) { return this.TryUnsubscribeInternal<TSignal>(callback); }

        public virtual bool TryUnsubscribe<TSignal>(Action<TSignal> callback) { return this.TryUnsubscribeInternal<TSignal>(callback); }

        private IPublisher<TSignal> GetPublisher<TSignal>()
        {
            if (this.container.TryResolve<IPublisher<TSignal>>() is not { } publisher) throw new($"Signal {typeof(TSignal)} not declared");

            return publisher;
        }

        private ISubscriber<TSignal> GetSubscriber<TSignal>()
        {
            if (this.container.TryResolve<ISubscriber<TSignal>>() is not { } subscriber) throw new($"Signal {typeof(TSignal)} not declared");

            return subscriber;
        }

        private bool TrySubscribeInternal<TSignal>(Delegate callback)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);

            if (this.subscriptions.ContainsKey(key)) return false;

            var wrapper = callback switch
            {
                Action action => _ => action(),
                Action<TSignal> action => action,
                _ => throw new ArgumentException("Callback type not supported"),
            };

            var subscription = this.GetSubscriber<TSignal>().Subscribe(wrapper);
            this.subscriptions.Add(key, subscription);

            return true;
        }

        private bool TryUnsubscribeInternal<TSignal>(Delegate callback)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);

            if (!this.subscriptions.Remove(key, out var subscription)) return false;
            subscription.Dispose();

            return true;
        }

        void ILateDisposable.LateDispose()
        {
            this.LateDispose();
        }

        public virtual void LateDispose()
        {
            foreach (var subscription in this.subscriptions.Values)
            {
                subscription.Dispose();
            }

            this.subscriptions.Clear();
        }
    }
}