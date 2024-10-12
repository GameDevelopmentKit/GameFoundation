namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using UnityEngine;

    public class UnityVector2Converter : DefaultTypeConverter
    {
        private readonly char delimiter;

        public UnityVector2Converter(char delimiter = '|')
        {
            this.delimiter = delimiter;
        }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var                 stringData = text.Split(this.delimiter);
                var                 x          = stringData[0];
                var                 y          = stringData[1];
                IEquatable<Vector2> vector     = new Vector2(float.Parse(x), float.Parse(y));
                return vector;
            }

            return null;
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            var vector = (Vector2)value;
            return $"{vector.x}{this.delimiter}{vector.y}";
        }
    }
}