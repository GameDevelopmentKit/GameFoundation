namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionUtils
    {
        // Cache derived-type lookups so repeated calls (e.g. blueprint types + user-data types) don't
        // re-scan all AppDomain assemblies every time.
        private static readonly Dictionary<Type, List<Type>> DerivedTypeCache = new();

        /// <summary>Get all types dives from T or Implement interface T that are not abstract. Note: only same assembly</summary>
        [Obsolete("Use GetAllDerivedTypes instead")]
        public static IEnumerable<Type> GetAllDriveType<T>()
        {
            return Assembly.GetAssembly(typeof(T)).GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(T).IsAssignableFrom(type));
        }

        /// <summary>
        /// Get all type that derive from <typeparamref name="T"/>.
        /// Results are cached after the first call – safe to call from any thread.
        /// </summary>
        public static IEnumerable<Type> GetAllDerivedTypes<T>(bool sameAssembly = false)
        {
            var baseType = typeof(T);

            lock (DerivedTypeCache)
            {
                if (DerivedTypeCache.TryGetValue(baseType, out var cached))
                    return cached;
            }

            var baseAsm = Assembly.GetAssembly(baseType);
            var result = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(asm => !asm.IsDynamic && (!sameAssembly || asm == baseAsm))
                                  .SelectMany(GetTypesSafely)
                                  .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                                  .ToList(); // materialise so we store a concrete list

            lock (DerivedTypeCache)
            {
                DerivedTypeCache[baseType] = result;
            }

            return result;
        }

        public static void CopyTo(this object from, object to)
        {
            var fromFieldInfos = from.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var toFieldInfos   = to.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var fromField in fromFieldInfos)
            {
                var toField = toFieldInfos.FirstOrDefault(toField => toField.Name == fromField.Name && toField.FieldType.IsAssignableFrom(fromField.FieldType));
                if (toField != null)
                {
                    toField.SetValue(to, fromField.GetValue(from));
                }
            }
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