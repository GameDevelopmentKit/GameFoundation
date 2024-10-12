// Copyright 2009-2021 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///     Converts an <see cref="object" /> to and from a <see cref="string" />.
    /// </summary>
    public class DefaultTypeConverter : ITypeConverter
    {
        private static readonly JsonSerializerSettings JsonSetting = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        /// <inheritdoc />
        public virtual object ConvertFromString(string text, Type typeInfo)
        {
            try
            {
                return JsonConvert.DeserializeObject(text, typeInfo, JsonSetting);
            }
            catch (Exception)
            {
                var message =
                    $"The conversion cannot be performed.{Environment.NewLine}" + $"    Text: '{text}'{Environment.NewLine}" + $"    MemberType: {typeInfo.FullName}{Environment.NewLine}";
                throw new(message);
            }
        }

        /// <inheritdoc />
        public virtual string ConvertToString(object value, Type typeInfo)
        {
            if (value == null) return string.Empty;
            return JsonConvert.SerializeObject(value, JsonSetting);
        }
    }
}