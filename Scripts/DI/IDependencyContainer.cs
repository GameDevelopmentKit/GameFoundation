#nullable enable
namespace GameFoundation.DI
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using UnityEngine;

    public interface IDependencyContainer
    {
        public bool TryResolve(Type type, [MaybeNullWhen(false)] out object instance);

        public bool TryResolve<T>([MaybeNullWhen(false)] out T instance);

        public object Resolve(Type type);

        public T Resolve<T>();

        public object[] ResolveAll(Type type);

        public T[] ResolveAll<T>();

        public object Instantiate(Type type, params object[] @params);

        public T Instantiate<T>(params object[] @params);

        public void Inject(object instance);

        public void InjectGameObject(GameObject instance);

        public GameObject InstantiatePrefab(GameObject prefab);
    }
}