#nullable enable
namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ReflectionExtensions
    {
        public static ConstructorInfo GetSingleConstructor(this Type type)
        {
            return type.GetConstructors() switch
            {
                { Length: 0 }    => throw new InvalidOperationException($"No constructor found for {type.Name}"),
                { Length: > 1 }  => throw new InvalidOperationException($"Multiple constructors found for {type.Name}"),
                { } constructors => constructors[0],
            };
        }

        public static Type GetSingleDerivedType(this Type type)
        {
            return type.GetDerivedTypes().ToArray() switch
            {
                { Length: 0 }   => throw new InvalidOperationException($"No derived type found for {type.Name}"),
                { Length: > 1 } => throw new InvalidOperationException($"Multiple derived types found for {type.Name}"),
                { } types       => types[0],
            };
        }

        public static Func<object> GetEmptyConstructor(this Type type)
        {
            var constructor = type.GetConstructors().SingleOrDefault(constructor => constructor.GetParameters().All(parameter => parameter.HasDefaultValue))
                ?? type.GetSingleConstructor();
            var parameters = constructor.GetParameters().Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
            return () => constructor.Invoke(parameters);
        }

        public static IEnumerable<FieldInfo> GetAllFields(this Type type, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        {
            return (type.BaseType?.GetAllFields(bindingFlags) ?? Enumerable.Empty<FieldInfo>()).Concat(type.GetFields(bindingFlags));
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        {
            return (type.BaseType?.GetAllProperties(bindingFlags) ?? Enumerable.Empty<PropertyInfo>()).Concat(type.GetProperties(bindingFlags));
        }

        public static IEnumerable<Type> GetDerivedTypes(this Type baseType)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic)
                .SelectMany(baseType.GetDerivedTypes);
        }

        public static IEnumerable<Type> GetDerivedTypes(this Type baseType, Assembly assembly)
        {
            return assembly.GetTypes().Where(type => !type.IsAbstract && baseType.IsAssignableFrom(type));
        }

        public static bool IsGenericTypeOf(this Type type, Type baseType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == baseType;
        }

        public static void CopyTo(this object from, object to)
        {
            from.GetType().GetAllFields()
                .Intersect(to.GetType().GetAllFields())
                .ForEach(field => field.SetValue(to, field.GetValue(from)));
        }

        public static bool IsBackingField(this FieldInfo field)
        {
            return field.Name.IsBackingFieldName();
        }

        public static bool IsBackingFieldName(this string str)
        {
            return str.StartsWith("<") && str.EndsWith(">k__BackingField");
        }

        public static string ToBackingFieldName(this string str)
        {
            return str.IsBackingFieldName() ? str : $"<{str}>k__BackingField";
        }

        public static string ToPropertyName(this string str)
        {
            return str.IsBackingFieldName() ? str.Substring(1, str.Length - 17) : str;
        }

        public static FieldInfo? ToBackingFieldInfo(this PropertyInfo property)
        {
            return property.DeclaringType?.GetField(property.Name.ToBackingFieldName());
        }

        public static PropertyInfo? ToPropertyInfo(this FieldInfo backingField)
        {
            return backingField.DeclaringType?.GetProperty(backingField.Name.ToPropertyName());
        }
    }
}