namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ListGenericConverter : DefaultTypeConverter
    {
        private readonly char delimiter;

        public ListGenericConverter(char delimiter = ',')
        {
            this.delimiter = delimiter;
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            if (value != null)
            {
                var type      = typeInfo.GetGenericArguments()[0];
                var converter = CsvHelper.TypeConverterCache.GetConverter(type);
                var list      = new List<string>();

                foreach (var s in (IList)value) list.Add(converter.ConvertToString(s, type));

                return string.Join(this.delimiter, list);
            }

            return null;
        }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            var type = typeInfo.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));

            if (!string.IsNullOrEmpty(text))
            {
                var converter  = CsvHelper.TypeConverterCache.GetConverter(type);
                var stringData = text.Split(this.delimiter);

                foreach (var s in stringData) list.Add(converter.ConvertFromString(s, type));
            }

            return list;
        }
    }
}