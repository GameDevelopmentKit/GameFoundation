namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionUtils
    {
        /// <summary>Get all types dives from T or Implement interface T that are not abstract. Note: only same assembly</summary>
        [Obsolete("Use GetAllDerivedTypes instead")]
        public static IEnumerable<Type> GetAllDriveType<T>()
        {
            return Assembly.GetAssembly(typeof(T)).GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(T).IsAssignableFrom(type));
        }

        /// <summary>
        /// Get all type that derive from <typeparamref name="T"/>
        /// </summary>
        public static IEnumerable<Type> GetAllDerivedTypes<T>(bool sameAssembly = false)
        {
            var baseType = typeof(T);
            var baseAsm  = Assembly.GetAssembly(baseType);
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic && (!sameAssembly || asm == baseAsm))
                .SelectMany(GetTypesSafely)
                .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type));
        }

        public static IEnumerable<FieldInfo> GetRecursiveFields(this Type type, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        {
            return type.GetFields(bindingFlags)
                .Concat(type.BaseType is { } baseType
                    ? GetRecursiveFields(baseType, bindingFlags)
                    : Enumerable.Empty<FieldInfo>()
                );
        }

        public static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            #if UNITY_EDITOR
            IEnumerable<Type> types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null);
            }

            return types;
            #else
                    return assembly.GetTypes();
            #endif
        }
    }
}