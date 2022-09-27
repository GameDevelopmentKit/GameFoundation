namespace GameFoundation.Scripts.Utilities.Extension
{
    using System.Linq;
    using GameFoundation.Scripts.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.ScreenFlow.Managers;
    using GameFoundation.Scripts.ScreenFlow.Signals;
    using UnityEngine;
    using Zenject;

    public static class ZenjectUtils
    {
        
        /// <summary>Create factory with  </summary>
        public static void BindIFactoryForAllDriveTypeFromPool<T>(this DiContainer container)
        {
            var bindMemoryPoolMethod = container.GetType().GetMethods().First(methodInfo => methodInfo.Name.Equals("BindIFactory") && methodInfo.GetGenericArguments().Length == 1);
            var fromPoolableMemoryPoolMethod = typeof(FactoryFromBinder0Extensions).GetMethods()
                                                                                   .First(methodInfo => methodInfo.Name.Equals("FromPoolableMemoryPool") &&
                                                                                                        methodInfo.GetGenericArguments().Length == 1     && methodInfo.GetParameters().Length == 1);

            // Bind pool for all http request
            var allDriveType = ReflectionUtils.GetAllDerivedTypes<T>();
            foreach (var type in allDriveType)
            {
                var factoryToChoiceIdBinder = bindMemoryPoolMethod.MakeGenericMethod(type).Invoke(container, null);
                fromPoolableMemoryPoolMethod.MakeGenericMethod(type).Invoke(null, new[] { factoryToChoiceIdBinder });
            }
        }
        
        
        /// <summary>
        /// Utils use to initialize a screen presenter manually, and the view is already initialized on the scene
        /// </summary>
        /// <param name="container"></param>
        /// <typeparam name="T"> Type of screen presenter</typeparam>
        public static void InitScreenManually<T>(this DiContainer container) where T: IScreenPresenter
        {
            container.Bind<T>().AsSingle().OnInstantiated<T>((context, presenter) =>
            {
                context.Container.Resolve<SignalBus>().Fire(new ManualInitScreenSignal() { ScreenPresenter = presenter });
            }).NonLazy();
        }


        /// <summary>
        /// Binding all class type that inherited <paramref name="T"/>
        /// </summary>
        /// <param name="diContainer"></param>
        /// <typeparam name="T"></typeparam>
        public static void BindAllTypeDriveFrom<T>(this DiContainer diContainer)
        {
            foreach (var type in ReflectionUtils.GetAllDerivedTypes<T>())
            {
                diContainer.Bind(type).AsCached().NonLazy();
            }
        }

        private static SceneContext currentSceneContext;
        /// <summary>
        /// Get DiContainer from Scene context in the current active scene
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static  DiContainer GetCurrentContainer(this object obj)
        {
            if (currentSceneContext == null)
            {
                Debug.Log($"GetCurrentContainer on scene = {SceneDirector.CurrentSceneName}");
                currentSceneContext = Object.FindObjectOfType<SceneContext>();
            }

            return currentSceneContext.Container;
        }
    }
}