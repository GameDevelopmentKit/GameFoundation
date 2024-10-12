// Copyright 2009-2021 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

namespace BlueprintFlow.BlueprintReader.Converter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Reflection;
    using BlueprintFlow.BlueprintReader.Converter.TypeConversion;
    using UnityEngine;

    /// <summary>
    ///     Caches <see cref="ITypeConverter" />s for a given type.
    /// </summary>
    public class TypeConverterCache
    {
        private readonly Dictionary<Type, ITypeConverter> typeConverters = new();

        /// <summary>
        ///     Initializes the <see cref="TypeConverterCache" /> class.
        /// </summary>
        public TypeConverterCache()
        {
            // Set default culture is InvariantCulture to avoid issues relate to the region format. Example in Russian, decimal symbol is ',' but in current culture it's '.'
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            this.CreateDefaultConverters();
        }

        /// <summary>
        ///     Adds the <see cref="ITypeConverter" /> for the given <see cref="System.Type" />.
        /// </summary>
        /// <param name="type">The type the converter converts.</param>
        /// <param name="typeConverter">The type converter that converts the type.</param>
        public void AddConverter(Type type, ITypeConverter typeConverter)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (typeConverter == null) throw new ArgumentNullException(nameof(typeConverter));

            this.typeConverters[type] = typeConverter;
        }

        /// <summary>
        ///     Adds the <see cref="ITypeConverter" /> for the given <see cref="System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type the converter converts.</typeparam>
        /// <param name="typeConverter">The type converter that converts the type.</param>
        public void AddConverter<T>(ITypeConverter typeConverter)
        {
            if (typeConverter == null) throw new ArgumentNullException(nameof(typeConverter));

            this.typeConverters[typeof(T)] = typeConverter;
        }

        /// <summary>
        ///     Removes the <see cref="ITypeConverter" /> for the given <see cref="System.Type" />.
        /// </summary>
        /// <param name="type">The type to remove the converter for.</param>
        public void RemoveConverter(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            this.typeConverters.Remove(type);
        }

        /// <summary>
        ///     Removes the <see cref="ITypeConverter" /> for the given <see cref="System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type to remove the converter for.</typeparam>
        public void RemoveConverter<T>()
        {
            this.RemoveConverter(typeof(T));
        }

        /// <summary>
        ///     Gets the converter for the given <see cref="System.Type" />.
        /// </summary>
        /// <param name="type">The type to get the converter for.</param>
        /// <returns>The <see cref="ITypeConverter" /> for the given <see cref="System.Type" />.</returns>
        public ITypeConverter GetConverter(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (this.typeConverters.TryGetValue(type, out var typeConverter)) return typeConverter;

            if (typeof(Enum).IsAssignableFrom(type))
            {
                if (this.typeConverters.TryGetValue(typeof(Enum), out typeConverter))
                    // If the user has registered a converter for the generic Enum type,
                    // that converter will be used as a default for all enums. If a
                    // converter was registered for a specific enum type, it would be
                    // returned from above already.
                    return typeConverter;

                this.AddConverter(type, new EnumConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                this.AddConverter(type, new NullableConverter(type, this));
                return this.GetConverter(type);
            }

            if (type.IsArray)
            {
                this.AddConverter(type, new ArrayGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                this.AddConverter(type, new DictionaryGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                this.AddConverter(type, new DictionaryGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                this.AddConverter(type, new ListGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Collection<>))
            {
                this.AddConverter(type, new ListGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                this.AddConverter(type, new ListGenericConverter());
                return this.GetConverter(type);
            }

            if (type == typeof(Vector2))
            {
                this.AddConverter(type, new UnityVector2Converter());
                return this.GetConverter(type);
            }

            if (type == typeof(Vector3))
            {
                this.AddConverter(type, new UnityVector3Converter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                this.AddConverter(type, new ListGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                this.AddConverter(type, new ListGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,>))
            {
                this.AddConverter(type, new PairGenericConverter());
                return this.GetConverter(type);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Tuple<,,>))
            {
                this.AddConverter(type, new Tuple3GenericConverter());
                return this.GetConverter(type);
            }

            if (type.IsGenericType && this.typeConverters.TryGetValue(type.GetGenericTypeDefinition(), out var converter)) return converter;

            // A specific IEnumerable converter doesn't exist.
            if (typeof(IEnumerable).IsAssignableFrom(type)) return new EnumerableConverter();

            return new DefaultTypeConverter();
        }

        /// <summary>
        ///     Gets the converter for the given <see cref="System.Type" />.
        /// </summary>
        /// <typeparam name="T">The type to get the converter for.</typeparam>
        /// <returns>The <see cref="ITypeConverter" /> for the given <see cref="System.Type" />.</returns>
        public ITypeConverter GetConverter<T>()
        {
            return this.GetConverter(typeof(T));
        }

        private void CreateDefaultConverters()
        {
            this.AddConverter(typeof(bool), new BooleanConverter());
            this.AddConverter(typeof(byte), new ByteConverter());
            this.AddConverter(typeof(byte[]), new ByteArrayConverter());
            this.AddConverter(typeof(char), new CharConverter());
            this.AddConverter(typeof(DateTime), new DateConverter("o"));
            this.AddConverter(typeof(DateTimeOffset), new DateTimeOffsetConverter());
            this.AddConverter(typeof(decimal), new DecimalConverter());
            this.AddConverter(typeof(double), new DoubleConverter());
            this.AddConverter(typeof(float), new SingleConverter());
            this.AddConverter(typeof(Guid), new GuidConverter());
            this.AddConverter(typeof(short), new Int16Converter());
            this.AddConverter(typeof(int), new Int32Converter());
            this.AddConverter(typeof(long), new Int64Converter());
            this.AddConverter(typeof(sbyte), new SByteConverter());
            this.AddConverter(typeof(string), new StringConverter());
            this.AddConverter(typeof(ushort), new UInt16Converter());
            this.AddConverter(typeof(uint), new UInt32Converter());
            this.AddConverter(typeof(ulong), new UInt64Converter());
            this.AddConverter(typeof(Uri), new UriConverter());
            this.AddConverter(typeof(ReadOnlyCollection<>), new ReadonlyCollectionConverter());
        }
    }
}