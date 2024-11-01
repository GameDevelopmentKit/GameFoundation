namespace GameFoundation.Signals
{
    using System;
    using System.Collections.Generic;
    using GameFoundation.DI;
    using MessagePipe;
    using UnityEngine.Scripting;

    public class SignalBus : ILateDisposable
    {
        private readonly IDependencyContainer container;

        private readonly Dictionary<(Type SignalType, Delegate Callback), IDisposable> subscriptions = new();

        [Preserve]
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
            this.SubscribeInternal<TSignal>(callback);
        }

        public void Subscribe<TSignal>(Action<TSignal> callback)
        {
            this.SubscribeInternal<TSignal>(callback);
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
            this.UnsubscribeInternal<TSignal>(callback);
        }

        public void Unsubscribe<TSignal>(Action<TSignal> callback)
        {
            this.UnsubscribeInternal<TSignal>(callback);
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
            if (!this.container.TryResolve<IPublisher<TSignal>>(out var publisher)) throw new InvalidOperationException($"{typeof(TSignal).Name} - Signal not declared");
            return publisher;
        }

        private ISubscriber<TSignal> GetSubscriber<TSignal>()
        {
            if (!this.container.TryResolve<ISubscriber<TSignal>>(out var subscriber)) throw new InvalidOperationException($"{typeof(TSignal).Name} - Signal not declared");
            return subscriber;
        }

        private void SubscribeInternal<TSignal>(Delegate callback)
        {
            if (!this.TrySubscribeInternal<TSignal>(callback)) throw new ArgumentException($"{typeof(TSignal).Name} - {callback.Method} - Already subscribed");
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

        private void UnsubscribeInternal<TSignal>(Delegate callback)
        {
            if (!this.TryUnsubscribeInternal<TSignal>(callback)) throw new ArgumentException($"{typeof(TSignal).Name} - {callback.Method} - Not subscribed");
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
            foreach (var subscription in this.subscriptions.Values) subscription.Dispose();
            this.subscriptions.Clear();
            this.isDisposed = true;
        }
    }
}