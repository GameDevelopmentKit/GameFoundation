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
        
        public static IEnumerable<Type> GetAllDerivedTypes(Type[] baseTypes, string[] assemblyNames)
        {
            var assemblyNamesHashset = new HashSet<string>(assemblyNames);
            var assemblies           = AppDomain.CurrentDomain.GetAssemblies();

            return assemblies.Where(asm => !asm.IsDynamic && assemblyNamesHashset.Contains(asm.GetName().Name))
                .SelectMany(GetTypesSafely)
                .Where(type => (type.IsClass || type.IsValueType) && !type.IsAbstract && baseTypes.Any(baseType => baseType.IsAssignableFrom(type)));
        }

        public static IEnumerable<Type> GetTypesInNamespace(Type[] baseTypes, string[] assemblyNames, string nameSpace)
        {
            return GetAllDerivedTypes(baseTypes, assemblyNames).Where(t => t.Namespace != null && t.Namespace.StartsWith(nameSpace));
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