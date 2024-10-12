namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class DictionaryGenericConverter : DefaultTypeConverter
    {
        private readonly char delimiterItem;
        private readonly char delimiterPair;

        public DictionaryGenericConverter(char delimiterItem = ',', char delimiterPair = ':')
        {
            this.delimiterItem = delimiterItem;
            this.delimiterPair = delimiterPair;
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            if (value != null)
            {
                var keyType        = typeInfo.GetGenericArguments()[0];
                var valueType      = typeInfo.GetGenericArguments()[1];
                var keyConverter   = CsvHelper.TypeConverterCache.GetConverter(keyType);
                var valueConverter = CsvHelper.TypeConverterCache.GetConverter(valueType);

                var list = new List<string>();
                foreach (DictionaryEntry itemData in (IDictionary)value)
                {
                    list.Add(string.Join(this.delimiterPair,
                        keyConverter.ConvertToString(itemData.Key, keyType),
                        valueConverter.ConvertToString(itemData.Value, valueType)));
                }

                return string.Join(this.delimiterItem, list);
            }

            return null;
        }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            var keyType        = typeInfo.GetGenericArguments()[0];
            var valueType      = typeInfo.GetGenericArguments()[1];
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var dictionary     = (IDictionary)Activator.CreateInstance(dictionaryType);

            if (!string.IsNullOrEmpty(text))
            {
                var keyConverter   = CsvHelper.TypeConverterCache.GetConverter(keyType);
                var valueConverter = CsvHelper.TypeConverterCache.GetConverter(valueType);
                var itemsRawData   = text.Split(this.delimiterItem);
                foreach (var itemRawData in itemsRawData)
                {
                    var itemData = itemRawData.Split(this.delimiterPair);
                    dictionary.Add(keyConverter.ConvertFromString(itemData[0], keyType),
                        valueConverter.ConvertFromString(itemData[1], valueType));
                }
            }

            return dictionary;
        }
    }
}