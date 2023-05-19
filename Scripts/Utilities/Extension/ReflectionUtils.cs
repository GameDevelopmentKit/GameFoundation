namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionUtils
    {
        /// <summary>Get all types dives from T or Implement interface T that are not abstract. Note: only same assembly</summary>
        public static IEnumerable<Type> GetAllDriveType<T>() { return Assembly.GetAssembly(typeof(T)).GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(T).IsAssignableFrom(type)); }

        /// <summary>
        /// Get all derived types from a type<c>T</c>.
        /// </summary>
        /// <typeparam name="T">Type to get derived classes.</typeparam>
        public static IEnumerable<Type> GetAllDerivedTypes<T>()
        {
            var type = typeof(T);
            return AppDomain.CurrentDomain.GetAssemblies().Where(s => s.IsDynamic == false)
                .SelectMany(GetTypesSafely)
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
        }

        public static void CopyTo(this object from, object to)
        {
            var fromFieldInfos = from.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var toFieldInfos   = to.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var fromField in fromFieldInfos)
            {
                var toField = toFieldInfos.FirstOrDefault(field => field.Name == fromField.Name && field.FieldType == fromField.FieldType);
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