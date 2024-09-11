#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.DI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VContainer.Unity;

    public sealed class VContainerAdapter : IStartable, VContainer.Unity.ITickable, VContainer.Unity.ILateTickable, VContainer.Unity.IFixedTickable, IDisposable
    {
        private readonly IReadOnlyList<IInitializable>  initializables;
        private readonly IReadOnlyList<ITickable>       tickables;
        private readonly IReadOnlyList<ILateTickable>   lateTickables;
        private readonly IReadOnlyList<IFixedTickable>  fixedTickables;
        private readonly IReadOnlyList<ILateDisposable> lateDisposables;

        public VContainerAdapter(
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

        void IStartable.Start()
        {
            foreach (var initializable in this.initializables)
            {
                initializable.Initialize();
            }
        }

        void VContainer.Unity.ITickable.Tick()
        {
            foreach (var tickable in this.tickables)
            {
                tickable.Tick();
            }
        }

        void VContainer.Unity.ILateTickable.LateTick()
        {
            foreach (var lateTickable in this.lateTickables)
            {
                lateTickable.LateTick();
            }
        }

        void VContainer.Unity.IFixedTickable.FixedTick()
        {
            foreach (var fixedTickable in this.fixedTickables)
            {
                fixedTickable.FixedTick();
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var lateDisposable in this.lateDisposables)
            {
                lateDisposable.LateDispose();
            }
        }
    }
}
#endif