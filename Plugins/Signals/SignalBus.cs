namespace Zenject
{
    using System;
    using System.Collections.Generic;
    using MessagePipe;

    public class SignalBus
    {
        private readonly DiContainer container;

        private readonly Dictionary<(Type, Delegate), IDisposable> subscribers = new();

        public SignalBus(DiContainer container)
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
            if (!this.TrySubscribe_Internal<TSignal>(callback)) throw new ArgumentException("Callback already subscribed");
        }

        public void Subscribe<TSignal>(Action<TSignal> callback)
        {
            if (!this.TrySubscribe_Internal(callback)) throw new ArgumentException("Callback already subscribed");
        }

        public bool TrySubscribe<TSignal>(Action callback)
        {
            return this.TrySubscribe_Internal<TSignal>(callback);
        }

        public bool TrySubscribe<TSignal>(Action<TSignal> callback)
        {
            return this.TrySubscribe_Internal(callback);
        }

        public void Unsubscribe<TSignal>(Action callback)
        {
            if (!this.TryUnsubscribe_Internal<TSignal>(callback)) throw new ArgumentException("Callback not subscribed");
        }

        public void Unsubscribe<TSignal>(Action<TSignal> callback)
        {
            if (!this.TryUnsubscribe_Internal(callback)) throw new ArgumentException("Callback not subscribed");
        }

        public bool TryUnsubscribe<TSignal>(Action callback)
        {
            return this.TryUnsubscribe_Internal<TSignal>(callback);
        }

        public bool TryUnsubscribe<TSignal>(Action<TSignal> callback)
        {
            return this.TryUnsubscribe_Internal(callback);
        }

        private IPublisher<TSignal> GetPublisher<TSignal>()
        {
            if (this.container.TryResolve<IPublisher<TSignal>>() is not { } publisher) throw new("Signal not declared");
            return publisher;
        }

        private ISubscriber<TSignal> GetSubscriber<TSignal>()
        {
            if (this.container.TryResolve<ISubscriber<TSignal>>() is not { } subscriber) throw new("Signal not declared");
            return subscriber;
        }

        private bool TrySubscribe_Internal<TSignal>(Action callback)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);
            if (this.subscribers.ContainsKey(key)) return false;
            this.subscribers.Add(key, this.GetSubscriber<TSignal>().Subscribe(_ => callback()));
            return true;
        }

        private bool TrySubscribe_Internal<TSignal>(Action<TSignal> callback)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);
            if (this.subscribers.ContainsKey(key)) return false;
            this.subscribers.Add(key, this.GetSubscriber<TSignal>().Subscribe(callback));
            return true;
        }

        private bool TryUnsubscribe_Internal<TSignal>(Action callback)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);
            if (!this.subscribers.Remove(key, out var subscriber)) return false;
            subscriber.Dispose();
            return true;
        }

        private bool TryUnsubscribe_Internal<TSignal>(Action<TSignal> callback)
        {
            if (callback is null) throw new ArgumentNullException(nameof(callback));
            var key = (typeof(TSignal), callback);
            if (!this.subscribers.Remove(key, out var subscriber)) return false;
            subscriber.Dispose();
            return true;
        }
    }
}