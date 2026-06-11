namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using UnityEngine;

    public class UnityVector2Converter : DefaultTypeSpanConverter
    {
        private readonly char delimiter;
        public UnityVector2Converter(char delimiter = '|') { this.delimiter = delimiter; }

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

        public override object ConvertFromSpan(ReadOnlySpan<char> span, Type type)
        {
            var separator1 = span.IndexOf(this.delimiter);

            if (separator1 < 0)
                return default(Vector2);

            var xSpan = span[..separator1];

            var remain = span[(separator1 + 1)..];

            var separator2 = remain.IndexOf(this.delimiter);

            var ySpan =
                separator2 >= 0
                    ? remain[..separator2]
                    : remain;

            float.TryParse(xSpan, out var x);
            float.TryParse(ySpan, out var y);

            return new Vector2(x, y);
        }
    }
}