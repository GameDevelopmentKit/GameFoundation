#if GDK_ZENJECT
namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Linq;
    using Zenject;

    public static class ZenjectUtils
    {
        /// <summary>Create factory with  </summary>
        public static void BindIFactoryForAllDriveTypeFromPool<T>(this DiContainer container)
        {
            var bindMemoryPoolMethod = container.GetType().GetMethods().First(methodInfo => methodInfo.Name.Equals("BindIFactory") && methodInfo.GetGenericArguments().Length == 1);
            var fromPoolableMemoryPoolMethod = typeof(FactoryFromBinder0Extensions).GetMethods()
                .First(methodInfo => methodInfo.Name.Equals("FromPoolableMemoryPool") && methodInfo.GetGenericArguments().Length == 1 && methodInfo.GetParameters().Length == 1);

            // Bind pool for all http request
            var allDriveType = ReflectionUtils.GetAllDerivedTypes<T>();
            foreach (var type in allDriveType)
            {
                var factoryToChoiceIdBinder = bindMemoryPoolMethod.MakeGenericMethod(type).Invoke(container, null);
                fromPoolableMemoryPoolMethod.MakeGenericMethod(type).Invoke(null, new[] { factoryToChoiceIdBinder });
            }
        }

        /// <summary>
        /// Binding all class type that inherited <paramref name="T"/>
        /// </summary>
        /// <param name="diContainer"></param>
        /// <typeparam name="T"></typeparam>
        [Obsolete("Use BindAllDerivedTypes instead")]
        public static void BindAllTypeDriveFrom<T>(this DiContainer diContainer)
        {
            foreach (var type in ReflectionUtils.GetAllDerivedTypes<T>())
            {
                diContainer.Bind(type).AsCached().NonLazy();
            }
        }

        /// <summary>
        /// Bind all class type that derive from <typeparamref name="T"/>
        /// </summary>
        public static void BindAllDerivedTypes<T>(this DiContainer container, bool nonLazy = false, bool sameAssembly = false)
        {
            foreach (var type in ReflectionUtils.GetAllDerivedTypes<T>(sameAssembly))
            {
                if (nonLazy)
                {
                    container.Bind(type).AsCached().NonLazy();
                }
                else
                {
                    container.Bind(type).AsCached();
                }
            }
        }

        public static void BindInterfacesAndSelfToAllTypeDriveFrom<T>(this DiContainer diContainer)
        {
            foreach (var type in ReflectionUtils.GetAllDerivedTypes<T>())
            {
                diContainer.BindInterfacesAndSelfTo(type).AsCached().NonLazy();
            }
        }
    }
}
#endif