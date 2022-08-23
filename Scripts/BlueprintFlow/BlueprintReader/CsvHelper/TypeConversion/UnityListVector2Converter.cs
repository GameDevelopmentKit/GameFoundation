namespace GameFoundation.Scripts.BlueprintFlow.BlueprintReader.CsvHelper.TypeConversion
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class UnityListVector2Converter : DefaultTypeConverter
    {
        private readonly char delimiter;
        public UnityListVector2Converter(char delimiter = '|') { this.delimiter = delimiter; }

        public override object ConvertFromString(string text, Type typeInfo)
        {
            var listVector = new List<Vector2>();
            if (string.IsNullOrEmpty(text)) return listVector;
            var stringData = text.Split(this.delimiter);
            foreach (var data in stringData)
            {
                var x = float.Parse(data.Split(",")[0]);
                var y = float.Parse(data.Split(",")[1]);
                listVector.Add(new Vector2(x, y));
            }
            return listVector;
        }
    }
}