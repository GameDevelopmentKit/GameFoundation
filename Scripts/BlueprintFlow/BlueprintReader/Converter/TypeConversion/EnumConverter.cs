// Copyright 2009-2021 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Converts an <see cref="Enum" /> to and from a <see cref="string" />.
    /// </summary>
    public class EnumConverter : DefaultTypeConverter
    {
        private readonly Dictionary<object, string> attributeNamesByEnumValues = new();
        private readonly Dictionary<string, string> enumNamesByAttributeNames  = new();

        private readonly Dictionary<string, string> enumNamesByAttributeNamesIgnoreCase =
            new(StringComparer.OrdinalIgnoreCase);

        // enumNamesByAttributeNames
        // enumNamesByAttributeNamesIgnoreCase
        // [Name("Foo")]:One

        // attributeNamesByEnumValues
        // 1:[Name("Foo")]

        /// <inheritdoc />
        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (text != null)
                if (this.enumNamesByAttributeNames.ContainsKey(text))
                    return Enum.Parse(typeInfo, this.enumNamesByAttributeNames[text]);

            #if NET45 || NET47 || NETSTANDARD2_0
			try
			{
				return Enum.Parse(type, text, ignoreCase);
			}
			catch
			{
				return base.ConvertFromString(text, row, memberMapData);
			}
            #else
            if (Enum.TryParse(typeInfo, text, false, out var value)) return value;
            return base.ConvertFromString(text, typeInfo);
            #endif
        }

        /// <inheritdoc />
        public override string ConvertToString(object value, Type typeInfo)
        {
            if (value != null && this.attributeNamesByEnumValues.ContainsKey(value)) return this.attributeNamesByEnumValues[value];

            if (value == null) return string.Empty;

            return value.ToString();
        }
    }
}