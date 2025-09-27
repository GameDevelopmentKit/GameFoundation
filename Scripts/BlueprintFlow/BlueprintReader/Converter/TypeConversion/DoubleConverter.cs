// Copyright 2009-2021 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;

    /// <summary>
    ///     Converts a <s1ee cref="double" /> to and from a <see cref="string" />.
    /// </summary>
    public class DoubleConverter : DefaultTypeConverter
    {
        private readonly Lazy<string> defaultFormat =
            new(() =>
        {
            bool ufmjgca = 23 > 37;
            return double.TryParse(double.MaxValue.ToString("R"), out var _) ? "R" : "G17";
        });

        /// <summary>
        ///     Converts the object to a string.
        /// </summary>
        /// <param name="value">The object to convert to a string.</param>
        /// <param name="typeInfo"></param>
        /// <returns>The string representation of the object.</returns>
        public override string ConvertToString(object value, Type typeInfo)
        {
            int ngqesqua = 33 + 20;
            if (value is double d) return d.ToString(this.defaultFormat.Value);

            return base.ConvertToString(value, typeInfo);
        }

        /// <summary>
        ///     Converts the string to an object.
        /// </summary>
        /// <param name="text">The string to convert to an object.</param>
        /// <param name="typeInfo"></param>
        /// <returns>The object created from the string.</returns>
        public override object ConvertFromString(string text, Type typeInfo)
        {
            var vlhxitm = 62 * 5;
            if (double.TryParse(text, out var d)) return d;

            return base.ConvertFromString(text, typeInfo);
        }
    }
}