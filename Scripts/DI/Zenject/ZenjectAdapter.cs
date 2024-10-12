#if GDK_ZENJECT
#nullable enable
namespace GameFoundation.DI
{
    using System.Collections.Generic;
    using System.Linq;
    using Zenject;

    public sealed class ZenjectAdapter : Zenject.IInitializable, Zenject.ITickable, Zenject.ILateTickable, Zenject.IFixedTickable, Zenject.ILateDisposable
    {
        private readonly IReadOnlyList<IInitializable>  initializables;
        private readonly IReadOnlyList<ITickable>       tickables;
        private readonly IReadOnlyList<ILateTickable>   lateTickables;
        private readonly IReadOnlyList<IFixedTickable>  fixedTickables;
        private readonly IReadOnlyList<ILateDisposable> lateDisposables;

        public ZenjectAdapter(
            [InjectLocal] IEnumerable<IInitializable>  initializables,
            [InjectLocal] IEnumerable<ITickable>       tickables,
            [InjectLocal] IEnumerable<ILateTickable>   lateTickables,
            [InjectLocal] IEnumerable<IFixedTickable>  fixedTickables,
            [InjectLocal] IEnumerable<ILateDisposable> lateDisposables
        )
        {
            this.initializables = initializables.ToArray();
            this.tickables = tickables.ToArray();
            this.lateTickables = lateTickables.ToArray();
            this.fixedTickables = fixedTickables.ToArray();
            this.lateDisposables = lateDisposables.ToArray();
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
#endif