#nullable enable
namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;

    internal sealed class TimeSpanConverter : ITypeConverter
    {
        object ITypeConverter.ConvertFromString(string text, Type typeInfo)
        {
            return TimeSpan.Parse(text);
        }

        string ITypeConverter.ConvertToString(object value, Type typeInfo)
        {
            return value.ToString();
        }
    }
}