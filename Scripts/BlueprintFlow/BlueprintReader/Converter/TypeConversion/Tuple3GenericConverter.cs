namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Runtime.CompilerServices;

    public class Tuple3GenericConverter : DefaultTypeConverter
    {
        private readonly char delimiterPair;

        public Tuple3GenericConverter(char delimiterPair = ':')
        {
            this.delimiterPair = delimiterPair;
        }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var item1Type   = typeInfo.GetGenericArguments()[0];
                var item2Type   = typeInfo.GetGenericArguments()[1];
                var item3Type   = typeInfo.GetGenericArguments()[2];
                var keyPairType = typeof(Tuple<,,>).MakeGenericType(item1Type, item2Type, item3Type);

                var item1Converter = CsvHelper.TypeConverterCache.GetConverter(item1Type);
                var item2Converter = CsvHelper.TypeConverterCache.GetConverter(item2Type);
                var item3Converter = CsvHelper.TypeConverterCache.GetConverter(item3Type);
                var itemsRawData   = text.Split(this.delimiterPair);
                var tuple = (ITuple)Activator.CreateInstance(keyPairType,
                    item1Converter.ConvertFromString(itemsRawData[0], item1Type),
                    item2Converter.ConvertFromString(itemsRawData[1], item2Type),
                    item3Converter.ConvertFromString(itemsRawData[2], item3Type));
                return tuple;
            }

            return null;
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            var item1Type   = typeInfo.GetGenericArguments()[0];
            var item2Type   = typeInfo.GetGenericArguments()[1];
            var item3Type   = typeInfo.GetGenericArguments()[2];
            var keyPairType = typeof(Tuple<,,>).MakeGenericType(item1Type, item2Type, item3Type);

            var item1Converter = CsvHelper.TypeConverterCache.GetConverter(item1Type);
            var item2Converter = CsvHelper.TypeConverterCache.GetConverter(item2Type);
            var item3Converter = CsvHelper.TypeConverterCache.GetConverter(item3Type);
            return string.Join(this.delimiterPair,
                item1Converter.ConvertToString(keyPairType.GetProperty("Item1").GetValue(value), item1Type),
                item2Converter.ConvertToString(keyPairType.GetProperty("Item2").GetValue(value), item2Type),
                item3Converter.ConvertToString(keyPairType.GetProperty("Item3").GetValue(value), item3Type));
        }
    }
}