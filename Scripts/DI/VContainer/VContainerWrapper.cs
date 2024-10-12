#if GDK_VCONTAINER
#nullable enable
#nullable enable
namespace GameFoundation.DI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UnityEngine;
    using VContainer;
    using VContainer.Internal;
    using VContainer.Unity;
    using Object = UnityEngine.Object;
    using PreserveAttribute = UnityEngine.Scripting.PreserveAttribute;

    public sealed class VContainerWrapper : IDependencyContainer
    {
        private readonly IObjectResolver container;

        [Preserve]
        public VContainerWrapper(IObjectResolver container)
        {
            this.container = container;
        }

        bool IDependencyContainer.TryResolve(Type type, [MaybeNullWhen(false)] out object instance)
        {
            return this.container.TryResolve(type, out instance);
        }

        bool IDependencyContainer.TryResolve<T>([MaybeNullWhen(false)] out T instance)
        {
            return this.container.TryResolve(out instance);
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
            return ((IEnumerable)this.container.Resolve(typeof(IEnumerable<>).MakeGenericType(type))).Cast<object>().ToArray();
        }

        T[] IDependencyContainer.ResolveAll<T>()
        {
            return this.container.Resolve<IEnumerable<T>>().ToArray();
        }

        object IDependencyContainer.Instantiate(Type type, params object[] @params)
        {
            return this.container.Instantiate(type, @params.Select(param => new Parameter(param)).ToArray());
        }

        T IDependencyContainer.Instantiate<T>(params object[] @params)
        {
            return this.container.Instantiate<T>(@params.Select(param => new Parameter(param)).ToArray());
        }

        void IDependencyContainer.Inject(object instance)
        {
            this.container.Inject(instance);
        }

        void IDependencyContainer.InjectGameObject(GameObject instance)
        {
            this.container.InjectGameObject(instance);
        }

        GameObject IDependencyContainer.InstantiatePrefab(GameObject prefab)
        {
            return this.container.Instantiate(prefab);
        }
    }

    public sealed class InjectAttribute : VContainer.InjectAttribute
    {
    }

    public static class VContainerExtensions
    {
        public static RegistrationBuilder RegisterResource<T>(this IContainerBuilder builder, string path, Lifetime lifetime) where T : Object
        {
            return builder.Register(_ => Object.Instantiate(Resources.Load<T>(path) ?? throw new($"{path} not found")), lifetime);
        }

        public static ComponentRegistrationBuilder RegisterComponentInNewPrefabResource<T>(this IContainerBuilder builder, string path, Lifetime lifetime) where T : Component
        {
            return builder.RegisterComponentInNewPrefab(_ => Resources.Load<T>(path) ?? throw new($"{path} not found"), lifetime);
        }

        public static RegistrationBuilder AsInterfacesAndSelf(this RegistrationBuilder registrationBuilder)
        {
            return registrationBuilder.AsImplementedInterfaces().AsSelf();
        }

        public static void AutoResolve(this IContainerBuilder builder, Type type)
        {
            builder.RegisterBuildCallback(container => container.Resolve(type));
        }

        public static void AutoResolve<T>(this IContainerBuilder builder)
        {
            builder.AutoResolve(typeof(T));
        }

        public static object Instantiate(this IObjectResolver container, Type type, IReadOnlyList<IInjectParameter>? parameters = null)
        {
            return InjectorCache.GetOrBuild(type).CreateInstance(container, parameters);
        }

        public static T Instantiate<T>(this IObjectResolver container, IReadOnlyList<IInjectParameter>? parameters = null)
        {
            return (T)container.Instantiate(typeof(T), parameters);
        }
    }

    public sealed class Parameter : IInjectParameter
    {
        private readonly object value;

        public Parameter(object value)
        {
            this.value = value;
        }

        bool IInjectParameter.Match(Type parameterType, string _)
        {
            return parameterType.IsInstanceOfType(this.value);
        }

        object IInjectParameter.GetValue(IObjectResolver _)
        {
            return this.value;
        }
    }
}
#endif