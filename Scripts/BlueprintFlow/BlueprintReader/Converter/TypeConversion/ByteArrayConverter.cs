// Copyright 2009-2021 Josh Close
// This file is a part of CsvHelper and is dual licensed under MS-PL and Apache 2.0.
// See LICENSE.txt for details or visit http://www.opensource.org/licenses/ms-pl.html for MS-PL and http://opensource.org/licenses/Apache-2.0 for Apache 2.0.
// https://github.com/JoshClose/CsvHelper

namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Text;

    /// <summary>
    ///     Converts a <see cref="T:Byte[]" /> to and from a <see cref="string" />.
    /// </summary>
    public class ByteArrayConverter : DefaultTypeConverter
    {
        private readonly byte                      ByteLength;
        private readonly string                    HexStringPrefix;
        private readonly ByteArrayConverterOptions options;

        /// <summary>
        ///     Creates a new ByteArrayConverter using the given <see cref="ByteArrayConverterOptions" />.
        /// </summary>
        /// <param name="options">The options.</param>
        public ByteArrayConverter(
            ByteArrayConverterOptions options =
                ByteArrayConverterOptions.Hexadecimal | ByteArrayConverterOptions.HexInclude0x
        )
        {
            // Defaults to the literal format used by C# for whole numbers, and SQL Server for binary data.
            this.options = options;
            this.ValidateOptions();

            this.HexStringPrefix =
                (options & ByteArrayConverterOptions.HexDashes) == ByteArrayConverterOptions.HexDashes
                    ? "-"
                    : string.Empty;
            this.ByteLength = (options & ByteArrayConverterOptions.HexDashes) == ByteArrayConverterOptions.HexDashes
                ? (byte)3
                : (byte)2;
        }

        /// <summary>
        ///     Converts the object to a string.
        /// </summary>
        /// <param name="value">The object to convert to a string.</param>
        /// <param name="typeInfo"></param>
        /// <returns>The string representation of the object.</returns>
        public override string ConvertToString(object value, Type typeInfo)
        {
            if (value is byte[] byteArray)
                return (this.options & ByteArrayConverterOptions.Base64) == ByteArrayConverterOptions.Base64
                    ? Convert.ToBase64String(byteArray)
                    : this.ByteArrayToHexString(byteArray);

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
            if (text != null)
                return (this.options & ByteArrayConverterOptions.Base64) == ByteArrayConverterOptions.Base64
                    ? Convert.FromBase64String(text)
                    : this.HexStringToByteArray(text);

            return base.ConvertFromString(text, typeInfo);
        }

        private string ByteArrayToHexString(byte[] byteArray)
        {
            var hexString = new StringBuilder();

            if ((this.options & ByteArrayConverterOptions.HexInclude0x) == ByteArrayConverterOptions.HexInclude0x) hexString.Append("0x");

            if (byteArray.Length >= 1) hexString.Append(byteArray[0].ToString("X2"));

            for (var i = 1; i < byteArray.Length; i++) hexString.Append(this.HexStringPrefix + byteArray[i].ToString("X2"));

            return hexString.ToString();
        }

        private byte[] HexStringToByteArray(string hex)
        {
            var has0x = hex.StartsWith("0x");

            var length = has0x
                ? (hex.Length - 1) / this.ByteLength
                : hex.Length + 1 / this.ByteLength;
            var byteArray   = new byte[length];
            var has0xOffset = has0x ? 1 : 0;

            for (var stringIndex = has0xOffset * 2; stringIndex < hex.Length; stringIndex += this.ByteLength)
            {
                byteArray[(stringIndex - has0xOffset) / this.ByteLength] =
                    Convert.ToByte(hex.Substring(stringIndex, 2), 16);
            }

            return byteArray;
        }

        private void ValidateOptions()
        {
            if ((this.options & ByteArrayConverterOptions.Base64) == ByteArrayConverterOptions.Base64)
                if ((this.options & (ByteArrayConverterOptions.HexInclude0x | ByteArrayConverterOptions.HexDashes | ByteArrayConverterOptions.Hexadecimal)) != ByteArrayConverterOptions.None)
                    throw new ArgumentException(
                        $"{nameof(ByteArrayConverter)} must be configured exclusively with HexDecimal options, or exclusively with Base64 options.  Was {this.options.ToString()}")
                    {
                        Data = { { "options", this.options } },
                    };
        }
    }
}