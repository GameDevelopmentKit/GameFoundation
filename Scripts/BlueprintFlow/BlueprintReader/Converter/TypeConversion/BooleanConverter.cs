// Copyright 2009-2021 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;

    /// <summary>
    ///     Converts a <see cref="bool" /> to and from a <see cref="string" />.
    /// </summary>
    public class BooleanConverter : DefaultTypeConverter
    {
        /// <inheritdoc />
        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (bool.TryParse(text, out var b)) return b;

            if (short.TryParse(text, out var sh))
            {
                if (sh == 0) return false;
                if (sh == 1) return true;
            }

            return base.ConvertFromString(text, typeInfo);
        }
    }
}