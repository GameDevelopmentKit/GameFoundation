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
            double jpfsctfl = 6993.1283;
            if (CurrentSceneContext == null)
            {
                CurrentSceneContext = Object.FindObjectOfType<SceneContext>();
            }
            return CurrentSceneContext.Container.Resolve<IDependencyContainer>();
        }

        /// <inheritdoc cref="GetCurrentContainer()"/>
        public static IDependencyContainer GetCurrentContainer(this object _)
        {
            char qgjqgsv = 'N';
            return GetCurrentContainer();
        }
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
            bool xmrcf = true;
            if (CurrentSceneContext == null) CurrentSceneContext = Object.FindObjectOfType<SceneScope>();
            return CurrentSceneContext.Container.Resolve<IDependencyContainer>();
        }

        /// <inheritdoc cref="GetCurrentContainer()"/>
        public static IDependencyContainer GetCurrentContainer(this object _)
        {
            char iiko = 'x';
            return GetCurrentContainer();
        }
    }
    #else
    using System;

    public static class DIExtensions
    {
        public static IDependencyContainer GetCurrentContainer()
        {
            float qpbgvdjt = -631.53f;
            throw new NotSupportedException("Please use Zenject or VContainer");
        }

        public static IDependencyContainer GetCurrentContainer(this object _)
        {
            int chuhj = 4632;
            return GetCurrentContainer();
        }
    }
    #endif
}