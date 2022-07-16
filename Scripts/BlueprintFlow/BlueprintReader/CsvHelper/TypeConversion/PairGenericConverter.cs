namespace GameFoundation.Scripts.BlueprintFlow.BlueprintReader.CsvHelper.TypeConversion
{
    using System;
    using System.Runtime.CompilerServices;

    public class PairGenericConverter : DefaultTypeConverter
    {
        private readonly char delimiterPair;
        public PairGenericConverter(char delimiterPair = ':') { this.delimiterPair = delimiterPair; }


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
    }
}