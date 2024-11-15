namespace Zenject
{
    using System;

    public interface ISignalBus
    {
        public void Fire<TSignal>();

        public void Fire<TSignal>(TSignal signal);

        public void Subscribe<TSignal>(Action callback);

        public void Subscribe<TSignal>(Action<TSignal> callback);

        public bool TrySubscribe<TSignal>(Action callback);

        public bool TrySubscribe<TSignal>(Action<TSignal> callback);

        public void Unsubscribe<TSignal>(Action callback);

        public void Unsubscribe<TSignal>(Action<TSignal> callback);

        public bool TryUnsubscribe<TSignal>(Action callback);

        public bool TryUnsubscribe<TSignal>(Action<TSignal> callback);
    }
}