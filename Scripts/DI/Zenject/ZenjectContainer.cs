#if GDK_ZENJECT
#nullable enable
namespace GameFoundation.DI
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Zenject;

    public sealed class ZenjectContainer : IDependencyContainer
    {
        private readonly DiContainer container;

        public ZenjectContainer(DiContainer container) => this.container = container;

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

        object IDependencyContainer.Resolve(Type type) => this.container.Resolve(type);

        T IDependencyContainer.Resolve<T>() => this.container.Resolve<T>();

        object[] IDependencyContainer.ResolveAll(Type type) => this.container.ResolveAll(type).Cast<object>().ToArray();

        T[] IDependencyContainer.ResolveAll<T>() => this.container.ResolveAll<T>().ToArray();

        object IDependencyContainer.Instantiate(Type type) => this.container.Instantiate(type);

        T IDependencyContainer.Instantiate<T>() => this.container.Instantiate<T>();
    }
}
#endif