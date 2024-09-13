#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.DI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using VContainer;
    using VContainer.Internal;

    public sealed class VContainerWrapper : IDependencyContainer
    {
        private readonly IObjectResolver container;

        [UnityEngine.Scripting.Preserve]
        public VContainerWrapper(IObjectResolver container) => this.container = container;

        bool IDependencyContainer.TryResolve(Type type, [MaybeNullWhen(false)] out object instance) => this.container.TryResolve(type, out instance);

        bool IDependencyContainer.TryResolve<T>([MaybeNullWhen(false)] out T instance) => this.container.TryResolve(out instance);

        object IDependencyContainer.Resolve(Type type) => this.container.Resolve(type);

        T IDependencyContainer.Resolve<T>() => this.container.Resolve<T>();

        object[] IDependencyContainer.ResolveAll(Type type) => ((IEnumerable)this.container.Resolve(typeof(IEnumerable<>).MakeGenericType(type))).Cast<object>().ToArray();

        T[] IDependencyContainer.ResolveAll<T>() => this.container.Resolve<IEnumerable<T>>().ToArray();

        object IDependencyContainer.Instantiate(Type type) => InjectorCache.GetOrBuild(type).CreateInstance(this.container, null);

        T IDependencyContainer.Instantiate<T>() => (T)InjectorCache.GetOrBuild(typeof(T)).CreateInstance(this.container, null);
    }
}
#endif