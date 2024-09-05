#if GDK_ZENJECT
#nullable enable
namespace GameFoundation.DI
{
    using UnityEngine;

    public static class ZenjectExtensions
    {
        private static SceneContext CurrentSceneContext;

        /// <summary>
        ///     Get DiContainer from Scene context in the current active scene
        /// </summary>
        public static IDependencyContainer GetCurrentContainer()
        {
            if (!CurrentSceneContext)
            {
                CurrentSceneContext = Object.FindObjectOfType<SceneContext>();
            }
            return CurrentSceneContext.Container.Resolve<IDependencyContainer>();
        }

        /// <inheritdoc cref="GetCurrentContainer()"/>
        public static IDependencyContainer GetCurrentContainer(this object _) => GetCurrentContainer();
    }
}
#endif