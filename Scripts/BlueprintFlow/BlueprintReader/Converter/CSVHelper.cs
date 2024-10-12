namespace BlueprintFlow.BlueprintReader.Converter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            Delimiter  = ',',
        };

        public static void RegisterTypeConverter(Type type, ITypeConverter typeConverter)
        {
            TypeConverterCache.AddConverter(type, typeConverter);
        }

        public static ITypeConverter GetTypeConverter(Type type)
        {
            return TypeConverterCache.GetConverter(type);
        }

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

        public static T GetField<T>(this CsvDataReader csvReader, int index)
        {
            return (T)GetField(csvReader, typeof(T), index);
        }

        public static object GetField(this CsvDataReader csvReader, Type type, int index)
        {
            var field     = csvReader.GetString(index);
            var converter = TypeConverterCache.GetConverter(type);
            return converter.ConvertFromString(field, type);
        }

        /// <summary>
        ///     Utility to get all member infos from a class map
        /// </summary>
        public static List<MemberInfo> GetAllFieldAndProperties(this Type typeInfo)
        {
            if (MemberInfosCache.TryGetValue(typeInfo, out var results)) return results;

            results = typeInfo.GetFields().Select(fieldInfo => new MemberInfo
            {
                MemberName = fieldInfo.Name, MemberType   = fieldInfo.FieldType,
                SetValue   = fieldInfo.SetValue, GetValue = fieldInfo.GetValue,
                IsDefined  = type => fieldInfo.IsDefined(type, false),
            }).ToList();

            results.AddRange(typeInfo.GetProperties().Select(propertyInfo => new MemberInfo
            {
                MemberName = propertyInfo.Name, MemberType   = propertyInfo.PropertyType,
                SetValue   = propertyInfo.SetValue, GetValue = propertyInfo.GetValue,
                IsDefined  = type => propertyInfo.IsDefined(type, false),
            }));

            return results;
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
}