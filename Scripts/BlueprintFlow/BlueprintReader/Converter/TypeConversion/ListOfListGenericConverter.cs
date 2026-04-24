using System;
using System.Collections;
using System.Collections.Generic;

namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    public class ListOfListGenericConverter : DefaultTypeConverter
    {
        private readonly char delimiterItem; // ,
        private readonly char delimiterPair; // ;

        public ListOfListGenericConverter(char delimiterItem = ',', char delimiterPair = ';')
        {
            this.delimiterItem = delimiterItem;
            this.delimiterPair = delimiterPair;
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            if (value == null) return null;

            var innerListType = typeInfo.GetGenericArguments()[0];
            var elementType   = innerListType.GetGenericArguments()[0];

            var converter = CsvHelper.TypeConverterCache.GetConverter(elementType);

            var outerList = (IList)value;
            var result    = new List<string>();

            foreach (IList innerList in outerList)
            {
                var inner = new List<string>();

                foreach (var item in innerList)
                {
                    inner.Add(converter.ConvertToString(item, elementType));
                }

                result.Add(string.Join(this.delimiterItem, inner));
            }

            return string.Join(this.delimiterPair, result);
        }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            var innerListType = typeInfo.GetGenericArguments()[0];
            var elementType   = innerListType.GetGenericArguments()[0];

            var outerList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(innerListType));

            if (string.IsNullOrEmpty(text))
                return outerList;

            var converter = CsvHelper.TypeConverterCache.GetConverter(elementType);

            var pairs = text.Split(this.delimiterPair);

            foreach (var pair in pairs)
            {
                var innerList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                if (!string.IsNullOrWhiteSpace(pair))
                {
                    var items = pair.Split(this.delimiterItem);

                    foreach (var item in items)
                    {
                        innerList.Add(converter.ConvertFromString(item.Trim(), elementType));
                    }
                }

                outerList.Add(innerList);
            }

            return outerList;
        }
    }
}