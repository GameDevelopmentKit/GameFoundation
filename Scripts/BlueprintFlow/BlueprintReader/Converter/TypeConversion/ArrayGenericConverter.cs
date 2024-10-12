namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;

    public class ArrayGenericConverter : DefaultTypeConverter
    {
        private readonly char delimiter;

        public ArrayGenericConverter(char delimiter = ',')
        {
            this.delimiter = delimiter;
        }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var stringData = text.Split(this.delimiter);

                var arraySize   = stringData.Length;
                var elementType = typeInfo.GetElementType();
                if (elementType == null) return null;
                var array     = Array.CreateInstance(elementType, arraySize);
                var converter = CsvHelper.TypeConverterCache.GetConverter(elementType);

                for (var i = 0; i < arraySize; i++) array.SetValue(converter.ConvertFromString(stringData[i], elementType), i);

                return array;
            }

            return null;
        }
    }
}