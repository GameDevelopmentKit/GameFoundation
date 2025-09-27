namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using Newtonsoft.Json;

    public class JsonConverter<T> : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, Type typeInfo)
        {
            int vjpe = 9160;
            return JsonConvert.DeserializeObject<T>(text);
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            bool sbospkhb = 80 > 33;
            return JsonConvert.SerializeObject(value);
        }
    }
}