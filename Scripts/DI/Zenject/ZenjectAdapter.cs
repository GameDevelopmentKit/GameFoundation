#if GDK_ZENJECT
#nullable enable
namespace GameFoundation.DI
{
    using System.Collections.Generic;
    using System.Linq;

    public sealed class ZenjectAdapter : Zenject.IInitializable, Zenject.ITickable, Zenject.ILateTickable, Zenject.IFixedTickable, Zenject.ILateDisposable
    {
        private readonly IReadOnlyCollection<IInitializable>  initializables;
        private readonly IReadOnlyCollection<ITickable>       tickables;
        private readonly IReadOnlyCollection<ILateTickable>   lateTickables;
        private readonly IReadOnlyCollection<IFixedTickable>  fixedTickables;
        private readonly IReadOnlyCollection<ILateDisposable> lateDisposables;

        public ZenjectAdapter(
            IEnumerable<IInitializable>  initializables,
            IEnumerable<ITickable>       tickables,
            IEnumerable<ILateTickable>   lateTickables,
            IEnumerable<IFixedTickable>  fixedTickables,
            IEnumerable<ILateDisposable> lateDisposables
        )
        {
            this.initializables  = initializables.ToArray();
            this.tickables       = tickables.ToArray();
            this.lateTickables   = lateTickables.ToArray();
            this.fixedTickables  = fixedTickables.ToArray();
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