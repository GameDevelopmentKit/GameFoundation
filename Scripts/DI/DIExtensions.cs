#nullable enable
namespace GameFoundation.DI
{
    #if GDK_ZENJECT
    using UnityEngine;
    using Zenject;

    public static class DIExtensions
    {
        private static SceneContext? CurrentSceneContext;

        /// <summary>
        ///     Get current scene <see cref="IDependencyContainer"/>
        /// </summary>
        public static IDependencyContainer GetCurrentContainer()
        {
            if (CurrentSceneContext == null)
            {
                CurrentSceneContext = Object.FindObjectOfType<SceneContext>();
            }
            return CurrentSceneContext.Container.Resolve<IDependencyContainer>();
        }

        /// <inheritdoc cref="GetCurrentContainer()"/>
        public static IDependencyContainer GetCurrentContainer(this object _) => GetCurrentContainer();
    }
    #elif GDK_VCONTAINER
    using UnityEngine;
    using VContainer;

    public static class DIExtensions
    {
        private static SceneScope? CurrentSceneContext;

        /// <summary>
        ///     Get current scene <see cref="IDependencyContainer"/>
        /// </summary>
        public static IDependencyContainer GetCurrentContainer()
        {
            if (CurrentSceneContext == null) CurrentSceneContext = Object.FindObjectOfType<SceneScope>();
            return CurrentSceneContext.Container.Resolve<IDependencyContainer>();
        }

        public static IObjectResolver GetCurrentObjectResolver(this object _)
        {
            if (CurrentSceneContext == null) CurrentSceneContext = Object.FindObjectOfType<SceneScope>();
            return CurrentSceneContext.Container;
        }

        /// <inheritdoc cref="GetCurrentContainer()"/>
        public static IDependencyContainer GetCurrentContainer(this object _)
        {
            return GetCurrentContainer();
        }
        
    }
    #else
    using System;

    public static class DIExtensions
    {
        public static IDependencyContainer GetCurrentContainer()
        {
            throw new NotSupportedException("Please use Zenject or VContainer");
        }

        public static IDependencyContainer GetCurrentContainer(this object _) => GetCurrentContainer();
    }
    #endif
}