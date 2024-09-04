#nullable enable
namespace GameFoundation.DI.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    public sealed class ZenjectAdapter : IDependencyContainer, Zenject.IInitializable, Zenject.ITickable, Zenject.ILateTickable, Zenject.IFixedTickable, Zenject.ILateDisposable
    {
        private readonly Zenject.DiContainer                  container;
        private readonly IReadOnlyCollection<IInitializable>  initializables;
        private readonly IReadOnlyCollection<ITickable>       tickables;
        private readonly IReadOnlyCollection<ILateTickable>   lateTickables;
        private readonly IReadOnlyCollection<IFixedTickable>  fixedTickables;
        private readonly IReadOnlyCollection<ILateDisposable> lateDisposables;

        public ZenjectAdapter(
            Zenject.DiContainer          container,
            IEnumerable<IInitializable>  initializables,
            IEnumerable<ITickable>       tickables,
            IEnumerable<ILateTickable>   lateTickables,
            IEnumerable<IFixedTickable>  fixedTickables,
            IEnumerable<ILateDisposable> lateDisposables
        )
        {
            this.container       = container;
            this.initializables  = initializables.ToArray();
            this.tickables       = tickables.ToArray();
            this.lateTickables   = lateTickables.ToArray();
            this.fixedTickables  = fixedTickables.ToArray();
            this.lateDisposables = lateDisposables.ToArray();
        }

        bool IDependencyContainer.TryResolve(Type type, [MaybeNullWhen(false)] out object instance)
        {
            if (this.container.TryResolve(type) is { } obj)
            {
                instance = obj;
                return true;
            }
            instance = null;
            return false;
        }

        bool IDependencyContainer.TryResolve<T>([MaybeNullWhen(false)] out T instance)
        {
            if (this.container.TryResolve(typeof(T)) is { } obj)
            {
                instance = (T)obj;
                return true;
            }
            instance = default;
            return false;
        }

        object IDependencyContainer.Resolve(Type type)
        {
            return this.container.Resolve(type);
        }

        T IDependencyContainer.Resolve<T>()
        {
            return this.container.Resolve<T>();
        }

        object[] IDependencyContainer.ResolveAll(Type type)
        {
            return this.container.ResolveAll(type).Cast<object>().ToArray();
        }

        T[] IDependencyContainer.ResolveAll<T>()
        {
            return this.container.ResolveAll<T>().ToArray();
        }

        object IDependencyContainer.Instantiate(Type type)
        {
            return this.container.Instantiate(type);
        }

        T IDependencyContainer.Instantiate<T>()
        {
            return this.container.Instantiate<T>();
        }

        void Zenject.IInitializable.Initialize()
        {
            foreach (var initializable in this.initializables)
            {
                initializable.Initialize();
            }
        }

        void Zenject.ITickable.Tick()
        {
            foreach (var tickable in this.tickables)
            {
                tickable.Tick();
            }
        }

        void Zenject.ILateTickable.LateTick()
        {
            foreach (var lateTickable in this.lateTickables)
            {
                lateTickable.LateTick();
            }
        }

        void Zenject.IFixedTickable.FixedTick()
        {
            foreach (var fixedTickable in this.fixedTickables)
            {
                fixedTickable.FixedTick();
            }
        }

        void Zenject.ILateDisposable.LateDispose()
        {
            foreach (var lateDisposable in this.lateDisposables)
            {
                lateDisposable.LateDispose();
            }
        }
    }
}