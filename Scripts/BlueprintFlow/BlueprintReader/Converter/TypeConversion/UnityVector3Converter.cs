namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using UnityEngine;

    public class UnityVector3Converter : DefaultTypeSpanConverter
    {
        private readonly char delimiter;
        public UnityVector3Converter(char delimiter = '|') { this.delimiter = delimiter; }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var                 stringData = text.Split(this.delimiter);
                var                 x          = stringData[0];
                var                 y          = stringData[1];
                var                 Z          = stringData[2];
                IEquatable<Vector3> vector     = new Vector3(float.Parse(x), float.Parse(y), float.Parse(Z));
                return vector;
            }

            return null;
        }

        public override string ConvertToString(object value, Type typeInfo)
        {
            var vector = (Vector3)value;
            return $"{vector.x}{this.delimiter}{vector.y}{this.delimiter}{vector.z}";
        }
        
        public override object ConvertFromSpan(ReadOnlySpan<char> span, Type type)
        {
            var comma1 = span.IndexOf(this.delimiter);

            if (comma1 < 0)
                return default(Vector3);

            var remain = span[(comma1 + 1)..];

            var comma2 = remain.IndexOf(this.delimiter);

            if (comma2 < 0)
                return default(Vector3);

            var x = float.Parse(span[..comma1]);

            var y = float.Parse(remain[..comma2]);

            var z = float.Parse(remain[(comma2 + 1)..]);

            return new Vector3(x, y, z);
        }
    }
}