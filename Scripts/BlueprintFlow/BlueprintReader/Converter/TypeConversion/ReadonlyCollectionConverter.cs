namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Collections.Generic;

    public class ReadonlyCollectionConverter : ITypeConverter
    {
        public object ConvertFromString(string text, Type typeInfo)
        {
            return Activator.CreateInstance(typeInfo, CsvHelper.TypeConverterCache.GetConverter(typeof(List<>)).ConvertFromString(text, typeInfo));
        }

        public string ConvertToString(object value, Type typeInfo)
        {
            throw new NotImplementedException();
        }
    }
}