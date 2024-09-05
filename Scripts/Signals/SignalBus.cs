namespace GameFoundation.Signals
{
    using System;
    using System.Collections.Generic;
    using GameFoundation.DI;
    using MessagePipe;

    public class SignalBus : ILateDisposable
    {
        private readonly IDependencyContainer container;

        private readonly Dictionary<(Type SignalType, Delegate Callback), IDisposable> subscriptions = new();

        public SignalBus(IDependencyContainer container)
        {
            this.container = container;
        }

        public void Fire<TSignal>()
        {
            this.GetPublisher<TSignal>().Publish(default);
        }

        public void Fire<TSignal>(TSignal signal)
        {
            this.GetPublisher<TSignal>().Publish(signal);
        }

        public void Subscribe<TSignal>(Action callback)
        {
            if (!this.TrySubscribeInternal<TSignal>(callback)) throw new ArgumentException("Callback already subscribed");
        }

        public void Subscribe<TSignal>(Action<TSignal> callback)
        {
            if (!this.TrySubscribeInternal<TSignal>(callback)) throw new ArgumentException("Callback already subscribed");
        }

        public bool TrySubscribe<TSignal>(Action callback)
        {
            return this.TrySubscribeInternal<TSignal>(callback);
        }

        public bool TrySubscribe<TSignal>(Action<TSignal> callback)
        {
            return this.TrySubscribeInternal<TSignal>(callback);
        }

        public void Unsubscribe<TSignal>(Action callback)
        {
            if (!this.TryUnsubscribeInternal<TSignal>(callback)) throw new ArgumentException("Callback not subscribed");
        }

        public void Unsubscribe<TSignal>(Action<TSignal> callback)
        {
            if (!this.TryUnsubscribeInternal<TSignal>(callback)) throw new ArgumentException("Callback not subscribed");
        }

        public bool TryUnsubscribe<TSignal>(Action callback)
        {
            return this.TryUnsubscribeInternal<TSignal>(callback);
        }

        public bool TryUnsubscribe<TSignal>(Action<TSignal> callback)
        {
            return this.TryUnsubscribeInternal<TSignal>(callback);
        }

        private IPublisher<TSignal> GetPublisher<TSignal>()
        {
            if (!this.container.TryResolve<IPublisher<TSignal>>(out var publisher)) throw new("Signal not declared");
            return publisher;
        }

        private ISubscriber<TSignal> GetSubscriber<TSignal>()
        {
            if (!this.container.TryResolve<ISubscriber<TSignal>>(out var subscriber)) throw new("Signal not declared");
            return subscriber;
        }

        private bool TrySubscribeInternal<TSignal>(Delegate callback)
        {
            if (this.isDisposed) return true;
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);
            if (this.subscriptions.ContainsKey(key)) return false;
            var wrapper = callback switch
            {
                Action action          => _ => action(),
                Action<TSignal> action => action,
                _                      => throw new ArgumentException("Callback type not supported"),
            };
            var subscription = this.GetSubscriber<TSignal>().Subscribe(wrapper);
            this.subscriptions.Add(key, subscription);
            return true;
        }

        private bool TryUnsubscribeInternal<TSignal>(Delegate callback)
        {
            if (this.isDisposed) return true;
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);
            if (!this.subscriptions.Remove(key, out var subscription)) return false;
            subscription.Dispose();
            return true;
        }

        private bool isDisposed;

        void ILateDisposable.LateDispose()
        {
            foreach (var subscription in this.subscriptions.Values)
            {
                subscription.Dispose();
            }
            this.subscriptions.Clear();
            this.isDisposed = true;
        }
    }
}