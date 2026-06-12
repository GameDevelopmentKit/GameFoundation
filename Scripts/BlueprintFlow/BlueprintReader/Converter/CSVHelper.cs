namespace BlueprintFlow.BlueprintReader.Converter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using BlueprintFlow.BlueprintReader.Converter.TypeConversion;
    using Sylvan.Data.Csv;
    using UnityEngine;

    public static class CsvHelper
    {
        public static readonly  TypeConverterCache                 TypeConverterCache = new();
        private static readonly Dictionary<Type, List<MemberInfo>> MemberInfosCache   = new();

        public static readonly CsvDataReaderOptions CsvDataReaderOptions = new()
        {
            HasHeaders = true,
            Delimiter  = ','
        };

        public static void RegisterTypeConverter(Type type, ITypeConverter typeConverter) { TypeConverterCache.AddConverter(type, typeConverter); }

        public static ITypeConverter GetTypeConverter(Type type) { return TypeConverterCache.GetConverter(type); }

        public static string GetField(this CsvDataReader csvReader, string name)
        {
            try
            {
                return csvReader.GetString(csvReader.GetOrdinal(name));
            }
            catch (Exception e)
            {
                Debug.LogError($"GetField - {name}:" + e);

                return string.Empty;
            }
        }

        public static T GetField<T>(this CsvDataReader csvReader, string name)
        {
            var index = csvReader.GetOrdinal(name);

            return (T)GetField(csvReader, typeof(T), index);
        }

        public static T GetField<T>(this CsvDataReader csvReader, int index) { return (T)GetField(csvReader, typeof(T), index); }

        public static object GetField(this CsvDataReader csvReader, Type type, int ordinal)
        {
            var converter = TypeConverterCache.GetConverter(type);

            if (converter is ISpanTypeConverter spanConverter)
            {
                return spanConverter.ConvertFromSpan(
                    csvReader.GetFieldSpan(ordinal),
                    type);
            }

            return converter.ConvertFromString(
                csvReader.GetString(ordinal),
                type);
        }

        public static ReadOnlySpan<char> GetFieldSpan(this CsvDataReader csvReader, string name)
        {
            return csvReader.GetFieldSpan(
                csvReader.GetOrdinal(name));
        }

        /// <summary>
        ///     Utility to get all member infos from a class map
        /// </summary>
        // public static List<MemberInfo> GetAllFieldAndProperties(this Type typeInfo)
        // {
        //     if (MemberInfosCache.TryGetValue(typeInfo, out var results)) return results;
        //
        //     results = typeInfo.GetFields().Select(fieldInfo => new MemberInfo
        //     {
        //         MemberName = fieldInfo.Name, MemberType   = fieldInfo.FieldType,
        //         SetValue   = fieldInfo.SetValue, GetValue = fieldInfo.GetValue,
        //         IsDefined  = type => fieldInfo.IsDefined(type, false)
        //     }).ToList();
        //
        //     results.AddRange(typeInfo.GetProperties().Select(propertyInfo => new MemberInfo
        //     {
        //         MemberName = propertyInfo.Name, MemberType   = propertyInfo.PropertyType,
        //         SetValue   = propertyInfo.SetValue, GetValue = propertyInfo.GetValue,
        //         IsDefined  = type => propertyInfo.IsDefined(type, false)
        //     }));
        //     MemberInfosCache[typeInfo] = results;
        //     return results;
        // }
        public static List<MemberInfo> GetAllFieldAndProperties(this Type typeInfo)
        {
            if (MemberInfosCache.TryGetValue(typeInfo, out var results)) return results;

            results = typeInfo.GetFields().Select(fieldInfo =>
            {
                var objParam = Expression.Parameter(typeof(object), "obj");
                var valParam = Expression.Parameter(typeof(object), "val");

                Expression instanceForGet = typeInfo.IsValueType
                    ? Expression.Unbox(objParam, typeInfo)
                    : Expression.Convert(objParam, typeInfo);

                Expression instanceForSet = typeInfo.IsValueType
                    ? Expression.Unbox(objParam, typeInfo)
                    : Expression.Convert(objParam, typeInfo);

                var getter = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Field(instanceForGet, fieldInfo),
                        typeof(object)),
                    objParam
                ).Compile();

                var setter = Expression.Lambda<Action<object, object>>(
                    Expression.Assign(
                        Expression.Field(instanceForSet, fieldInfo),
                        Expression.Convert(valParam, fieldInfo.FieldType)),
                    objParam, valParam
                ).Compile();

                return new MemberInfo
                {
                    MemberName = fieldInfo.Name,
                    MemberType = fieldInfo.FieldType,
                    SetValue   = setter,
                    GetValue   = getter,
                    IsDefined  = type => fieldInfo.IsDefined(type, false)
                };
            }).ToList();

            results.AddRange(typeInfo.GetProperties()
                .Where(p => p.GetIndexParameters().Length == 0)
                .Select(propertyInfo =>
                {
                    var objParam = Expression.Parameter(typeof(object), "obj");
                    var valParam = Expression.Parameter(typeof(object), "val");

                    var instanceForGet = typeInfo.IsValueType
                        ? Expression.Unbox(objParam, typeInfo)
                        : Expression.Convert(objParam, typeInfo);

                    var getter = Expression.Lambda<Func<object, object>>(
                        Expression.Convert(
                            Expression.Property(instanceForGet, propertyInfo),
                            typeof(object)),
                        objParam
                    ).Compile();

                    Action<object, object> setter = null;

                    if (propertyInfo.CanWrite && propertyInfo.SetMethod != null)
                    {
                        var instanceForSet = typeInfo.IsValueType
                            ? Expression.Unbox(objParam, typeInfo)
                            : Expression.Convert(objParam, typeInfo);

                        setter = Expression.Lambda<Action<object, object>>(
                            Expression.Assign(
                                Expression.Property(instanceForSet, propertyInfo),
                                Expression.Convert(valParam, propertyInfo.PropertyType)),
                            objParam, valParam
                        ).Compile();
                    }

                    return new MemberInfo
                    {
                        MemberName = propertyInfo.Name,
                        MemberType = propertyInfo.PropertyType,
                        SetValue   = setter,
                        GetValue   = getter,
                        IsDefined  = type => propertyInfo.IsDefined(type, false)
                    };
                }));

            MemberInfosCache[typeInfo] = results;

            return results;
        }

        public static object ConvertToObject(string dataRotation, Type type)
        {
            var converter = TypeConverterCache.GetConverter(type);

            return converter.ConvertFromString(dataRotation, type);
        }
    }

    public class MemberInfo
    {
        public Func<object, object>   GetValue;
        public string                 MemberName;
        public Type                   MemberType;
        public Action<object, object> SetValue;
        public Func<Type, bool>       IsDefined;
    }

    public class CachedMember
    {
        public MemberInfo MemberInfo;

        public int Ordinal;

        public Type MemberType;

        public ITypeConverter Converter;

        public ISpanTypeConverter SpanConverter;
    }

    public class CachedBlueprintCollection
    {
        public MemberInfo MemberInfo;

        public Func<object> Factory;
        public int          FieldCount;
    }
}