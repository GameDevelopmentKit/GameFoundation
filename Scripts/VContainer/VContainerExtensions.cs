namespace VContainer
{
    using System;
    using System.Collections.Generic;
    using VContainer.Internal;

    public static class VContainerExtensions
    {
        private static IObjectResolver currentContainer;

        public static void SetCurrentContainer(IObjectResolver container)
        {
            currentContainer = container;
        }

        public static IObjectResolver GetCurrentContainer(this object obj)
        {
            return currentContainer;
        }

        public static void AutoResolve<T>(this IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(resolver => resolver.Resolve<T>());
        }

        public static object Instantiate(this IObjectResolver resolver, Type type, IReadOnlyList<IInjectParameter> parameters = null)
        {
            return InjectorCache.GetOrBuild(type).CreateInstance(resolver, parameters);
        }

        public static T Instantiate<T>(this IObjectResolver resolver, IReadOnlyList<IInjectParameter> parameters = null)
        {
            return (T)resolver.Instantiate(typeof(T));
        }
    }
}