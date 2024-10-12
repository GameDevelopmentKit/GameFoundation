namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Runtime.CompilerServices;

    public class PairGenericConverter : DefaultTypeConverter
    {
        private readonly char delimiterPair;

        public PairGenericConverter(char delimiterPair = ':')
        {
            this.delimiterPair = delimiterPair;
        }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var keyType     = typeInfo.GetGenericArguments()[0];
                var valueType   = typeInfo.GetGenericArguments()[1];
                var keyPairType = typeof(Tuple<,>).MakeGenericType(keyType, valueType);

                var keyConverter   = CsvHelper.TypeConverterCache.GetConverter(keyType);
                var valueConverter = CsvHelper.TypeConverterCache.GetConverter(valueType);
                var itemsRawData   = text.Split(this.delimiterPair);

                var tuple = (ITuple)Activator.CreateInstance(keyPairType,
                    keyConverter.ConvertFromString(itemsRawData[0], keyType),
                    valueConverter.ConvertFromString(itemsRawData[1], valueType));
                return tuple;
            }

            return null;
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            var item1Type   = typeInfo.GetGenericArguments()[0];
            var item2Type   = typeInfo.GetGenericArguments()[1];
            var keyPairType = typeof(Tuple<,>).MakeGenericType(item1Type, item2Type);

            var item1Converter = CsvHelper.TypeConverterCache.GetConverter(item1Type);
            var item2Converter = CsvHelper.TypeConverterCache.GetConverter(item2Type);
            return string.Join(this.delimiterPair,
                item1Converter.ConvertToString(keyPairType.GetProperty("Item1").GetValue(value), item1Type),
                item2Converter.ConvertToString(keyPairType.GetProperty("Item2").GetValue(value), item2Type));
        }
    }
}