namespace BuildScripts.Runtime.ConstantValues
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using UnityEngine;

    public static class ConstantValuesHelper
    {
        private static Dictionary<string, string> Data { get; } = new();

        private static void PopulateToStaticProperties()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var classAttribute = type.GetCustomAttribute<ConstantValueAttribute>();

                    if (classAttribute is null) continue;

                    var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var property in properties)
                    {
                        var propertyAttribute = property.GetCustomAttribute<ConstantValueAttribute>();

                        if (propertyAttribute is null) continue;

                        var key = $"{classAttribute.Key ?? type.Name}.{propertyAttribute.Key ?? property.Name}";

                        if (!ConstantValuesHelper.TryGetValue(key, property.PropertyType, out var value)) continue;

                        property.SetValue(null, value);
                    }
                }
            }
        }

        private static void FromResources(string path)
        {
            var resource = Resources.Load<TextAsset>(path);
            var env      = resource.text;

            var lines = env.Split('\n');

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var i = line.IndexOf('=');

                if (i == -1)
                {
                    continue;
                }

                var key   = line[..i].Trim();
                var value = line[(i + 1)..].Trim();

                ConstantValuesHelper.Data[key] = value;
            }

            Resources.UnloadAsset(resource);
        }

        public static string GetOrNull(string key)
        {
            return ConstantValuesHelper.Data.TryGetValue(key, out var value) ? value : null;
        }

        public static bool TryGetValue(string key, Type type, out object value)
        {
            try
            {
                value = TypeDescriptor
                        .GetConverter(type)
                        .ConvertFromInvariantString(ConstantValuesHelper.Data[key]);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public static void Init()
        {
            ConstantValuesHelper.FromResources("ConstantValues");
            ConstantValuesHelper.PopulateToStaticProperties();
        }
    }
}