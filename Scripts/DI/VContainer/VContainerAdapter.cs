#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.DI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Scripting;
    using VContainer.Internal;

    public sealed class VContainerAdapter : VContainer.Unity.IStartable, VContainer.Unity.ITickable, VContainer.Unity.ILateTickable, VContainer.Unity.IFixedTickable, IDisposable
    {
        private readonly IReadOnlyList<IInitializable>  initializables;
        private readonly IReadOnlyList<ITickable>       tickables;
        private readonly IReadOnlyList<ILateTickable>   lateTickables;
        private readonly IReadOnlyList<IFixedTickable>  fixedTickables;
        private readonly IReadOnlyList<ILateDisposable> lateDisposables;

        [Preserve]
        public VContainerAdapter(
            ContainerLocal<IEnumerable<IInitializable>>  initializables,
            ContainerLocal<IEnumerable<ITickable>>       tickables,
            ContainerLocal<IEnumerable<ILateTickable>>   lateTickables,
            ContainerLocal<IEnumerable<IFixedTickable>>  fixedTickables,
            ContainerLocal<IEnumerable<ILateDisposable>> lateDisposables
        )
        {
            this.initializables  = initializables.Value.ToArray();
            this.tickables       = tickables.Value.ToArray();
            this.lateTickables   = lateTickables.Value.ToArray();
            this.fixedTickables  = fixedTickables.Value.ToArray();
            this.lateDisposables = lateDisposables.Value.ToArray();
        }

        void VContainer.Unity.IStartable.Start()
        {
            SafeForEach(this.initializables, initializable => initializable.Initialize());
        }

        void VContainer.Unity.ITickable.Tick()
        {
            SafeForEach(this.tickables, tickable => tickable.Tick());
        }

        void VContainer.Unity.ILateTickable.LateTick()
        {
            SafeForEach(this.lateTickables, lateTickable => lateTickable.LateTick());
        }

        void VContainer.Unity.IFixedTickable.FixedTick()
        {
            SafeForEach(this.fixedTickables, fixedTickable => fixedTickable.FixedTick());
        }

        void IDisposable.Dispose()
        {
            SafeForEach(this.lateDisposables, lateDisposable => lateDisposable.LateDispose());
        }

        private static void SafeForEach<T>(IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                try
                {
                    action(item);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
#endif