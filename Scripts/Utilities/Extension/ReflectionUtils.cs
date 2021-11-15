namespace Mech.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionUtils
    {
        /// <summary>Get all types dives from T or Implement interface T that are not abstract.</summary>
        public static IEnumerable<Type> GetAllDriveType<T>()
        {
            return Assembly.GetAssembly(typeof(T)).GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(T).IsAssignableFrom(type));
        } 
    }
}