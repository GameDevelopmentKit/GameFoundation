#if GDK_ZENJECT
#nullable enable
namespace GameFoundation.DI
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UnityEngine;
    using Zenject;

    public sealed class ZenjectWrapper : IDependencyContainer
    {
        private readonly DiContainer container;

        public ZenjectWrapper(DiContainer container) => this.container = container;

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

        object IDependencyContainer.Instantiate(Type type, params object[] @params) => this.container.Instantiate(type, @params);

        T IDependencyContainer.Instantiate<T>(params object[] @params) => this.container.Instantiate<T>(@params);

        void IDependencyContainer.Inject(object instance) => this.container.Inject(instance);

        void IDependencyContainer.InjectGameObject(GameObject instance) => this.container.InjectGameObject(instance);

        GameObject IDependencyContainer.InstantiatePrefab(GameObject prefab) => this.container.InstantiatePrefab(prefab);
    }

    public sealed class InjectAttribute : Zenject.InjectAttribute
    {
    }
}
#endif