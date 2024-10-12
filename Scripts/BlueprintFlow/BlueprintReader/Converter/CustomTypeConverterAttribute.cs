// Copyright 2009-2022 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

using System;

namespace BlueprintFlow.BlueprintReader.Converter
{
    using BlueprintFlow.BlueprintReader.Converter.TypeConversion;
    using Sylvan.Data.Csv;

    /// <summary>
    /// Use for custom read csv case
    /// </summary>
    public interface ICustomTypeConverter
    {
        /// <summary>
        ///     Converts the csv to an object.
        /// </summary>
        /// <param name="csvReader">The csv reader to convert to an object</param>
        /// <returns>The object created from the string.</returns>
        object ConvertFromCsv(CsvDataReader csvReader);

        /// <summary>
        ///     Converts the object to a string.
        /// </summary>
        /// <param name="value">The object to convert to a string.</param>
        /// <returns>The string representation of the object.</returns>
        string ConvertToString(object value);
    }

    /// <summary>
    /// Specifies the <see cref="TypeConverter"/> to use
    /// when converting the member to and from a CSV field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class CustomTypeConverterAttribute : Attribute
    {
        /// <summary>
        /// Gets the type converter.
        /// </summary>
        public ICustomTypeConverter TypeConverter { get; private set; }

        /// <summary>
        /// Specifies the <see cref="TypeConverter"/> to use
        /// when converting the member to and from a CSV field.
        /// </summary>
        /// <param name="typeConverterType">The type of the <see cref="ITypeConverter"/>.</param>
        public CustomTypeConverterAttribute(Type typeConverterType) : this(typeConverterType, new object[0])
        {
        }

        /// <summary>
        /// Specifies the <see cref="TypeConverter"/> to use
        /// when converting the member to and from a CSV field.
        /// </summary>
        /// <param name="typeConverterType">The type of the <see cref="ICustomTypeConverter"/>.</param>
        /// <param name="constructorArgs">Type constructor arguments for the type converter.</param>
        public CustomTypeConverterAttribute(Type typeConverterType, params object[] constructorArgs)
        {
            this.TypeConverter = Activator.CreateInstance(typeConverterType, constructorArgs) as ICustomTypeConverter ?? throw new ArgumentException($"Type '{typeConverterType.FullName}' does not implement {nameof(ITypeConverter)}");
        }
    }
}