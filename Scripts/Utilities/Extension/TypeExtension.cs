namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;

    public static class TypeExtension
    {
        public static T GetCustomAttribute<T>(this object instance) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(instance.GetType(), typeof(T));
            ;
        }

        /// <summary>
        /// Alternative version of <see cref="Type.IsSubclassOf"/> that supports raw generic types (generic types without
        /// any type parameters).
        /// </summary>
        /// <param name="baseType">The base type class for which the check is made.</param>
        /// <param name="toCheck">To type to determine for whether it derives from <paramref name="baseType"/>.</param>
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (baseType == cur) return true;

                toCheck = toCheck.BaseType;
            }

            return false;
        }
    }
}