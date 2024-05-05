namespace Zenject
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using MessagePipe;

    public class SignalBus
    {
        private readonly DiContainer container;

        private Dictionary<Type, Dictionary<Delegate, IDisposable>> typeAndActionToDisposeAble  = new();

        public SignalBus(
            DiContainer container)
        {
            this.container = container;
        }

        public void Fire<TSignal>(TSignal signal) { GlobalMessagePipe.GetPublisher<TSignal>().Publish(signal); }
        

        public void Fire<TSignal>()                     { GlobalMessagePipe.GetPublisher<TSignal>().Publish(default); }
        
        public void Subscribe<TSignal>(Action callback)
        {
            this.InternalSubscribe<TSignal>(callback, () => GlobalMessagePipe.GetSubscriber<TSignal>().Subscribe(_ => callback.Invoke()));
        }

        public void Subscribe<TSignal>(Action<TSignal> callback)
        {
            this.InternalSubscribe<TSignal>(callback, () => GlobalMessagePipe.GetSubscriber<TSignal>().Subscribe(callback));
        }

        private void InternalSubscribe<TSignal>(Delegate dlg, Func<IDisposable> iDisposableFunc)
        {
            if (this.typeAndActionToDisposeAble.TryGetValue(typeof(TSignal), out var actions))
            {
                if (actions.TryGetValue(dlg, out var disposable))
                {
                    throw new Exception("Callback already subscribed!!!");
                }
            }
            else
            {
                actions = new Dictionary<Delegate, IDisposable>(new DelegateEqualityComparer());
                this.typeAndActionToDisposeAble.Add(typeof(TSignal), actions);
            }

            actions.Add(dlg, iDisposableFunc());
        }

        public void Unsubscribe<TSignal>(Action callback)
        {
            this.InternalUnsubscribe<TSignal>(callback);
        }

        public void Unsubscribe<TSignal>(Action<TSignal> callback)
        {
            this.InternalUnsubscribe<TSignal>(callback);
        }
        
        public void TryUnsubscribe<TSignal>(Action callback)
        {
            try
            {
                this.InternalUnsubscribe<TSignal>(callback);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void TryUnsubscribe<TSignal>(Action<TSignal> callback)
        {
            try
            {
                this.InternalUnsubscribe<TSignal>(callback);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        private void InternalUnsubscribe<TSignal>(Delegate dlg)
        {
            if (this.typeAndActionToDisposeAble.TryGetValue(typeof(TSignal), out var actions))
            {
                if (actions.TryGetValue(dlg, out var disposable))
                {
                    disposable.Dispose();
                    actions.Remove(dlg);
                }
                else
                {
                    throw new Exception($"{typeof(TSignal)} - {dlg.Target}: Callback not subscribed!!!");
                }
            }
            else
            {
                throw new Exception($"{typeof(TSignal)}: Signal not subscribed!!!");
            }
        }
    }
}