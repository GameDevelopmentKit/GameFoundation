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