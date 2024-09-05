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
        ///     Get DiContainer from Scene context in the current active scene
        /// </summary>
        public static IDependencyContainer GetCurrentContainer()
        {
            if (!CurrentSceneContext)
            {
                CurrentSceneContext = Object.FindObjectOfType<SceneContext>();
            }
            return (CurrentSceneContext?.Container ?? ProjectContext.Instance.Container).Resolve<IDependencyContainer>();
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