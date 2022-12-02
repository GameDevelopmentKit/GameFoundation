// Copyright 2009-2021 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;

    /// <summary>
    ///     Converts a <see cref="Nullable{T}" /> to and from a <see cref="string" />.
    /// </summary>
    public class NullableConverter : DefaultTypeConverter
    {
        /// <summary>
        ///     Creates a new <see cref="NullableConverter" /> for the given <see cref="Nullable{T}" /> <see cref="Type" />.
        /// </summary>
        /// <param name="type">The nullable type.</param>
        /// <param name="typeConverterFactory">The type converter factory.</param>
        /// <exception cref="System.ArgumentException">type is not a nullable type.</exception>
        public NullableConverter(Type type, TypeConverterCache typeConverterFactory)
        {
            this.NullableType   = type;
            this.UnderlyingType = Nullable.GetUnderlyingType(type);
            if (this.UnderlyingType == null) throw new ArgumentException("type is not a nullable type.");

            this.UnderlyingTypeConverter = typeConverterFactory.GetConverter(this.UnderlyingType);
        }

        /// <summary>
        ///     Gets the type of the nullable.
        /// </summary>
        /// <value>
        ///     The type of the nullable.
        /// </value>
        public Type NullableType { get; }

        /// <summary>
        ///     Gets the underlying type of the nullable.
        /// </summary>
        /// <value>
        ///     The underlying type.
        /// </value>
        public Type UnderlyingType { get; }

        /// <summary>
        ///     Gets the type converter for the underlying type.
        /// </summary>
        /// <value>
        ///     The type converter.
        /// </value>
        public ITypeConverter UnderlyingTypeConverter { get; }

        /// <summary>
        ///     Converts the string to an object.
        /// </summary>
        /// <param name="text">The string to convert to an object.</param>
        /// <param name="typeInfo"></param>
        /// <returns>The object created from the string.</returns>
        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (string.IsNullOrEmpty(text)) return null;

            return this.UnderlyingTypeConverter.ConvertFromString(text, typeInfo);
        }

        /// <summary>
        ///     Converts the object to a string.
        /// </summary>
        /// <param name="value">The object to convert to a string.</param>
        /// <param name="typeInfo"></param>
        /// <returns>The string representation of the object.</returns>
        public override string ConvertToString(object value, Type typeInfo)
        {
            return this.UnderlyingTypeConverter.ConvertToString(value, typeInfo);
        }
    }
}