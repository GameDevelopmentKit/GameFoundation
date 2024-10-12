namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    using System;
    using System.Data.SqlTypes;
    using System.Globalization;

    public class DateConverter : ITypeConverter
    {
        private readonly string dateFormat;

        public DateConverter(string dateFormat)
        {
            this.dateFormat = dateFormat;
        }

        public object ConvertFromString(string text, Type typeInfo)
        {
            if (!string.IsNullOrEmpty(text))
            {
                DateTime.TryParseExact(text,
                    this.dateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal,
                    out var result);
                if (this.IsValidSqlDateTime(result)) return result;
            }

            return null;
        }

        public string ConvertToString(object value, Type typeInfo)
        {
            if (value == null) return string.Empty;

            DateTime dt;
            if (DateTime.TryParse(value.ToString(), out dt)) return dt.ToString(this.dateFormat);
            return string.Empty;
        }

        public bool IsValidSqlDateTime(DateTime? dateTime)
        {
            if (dateTime == null) return true;

            var minValue = DateTime.Parse(SqlDateTime.MinValue.ToString());
            var maxValue = DateTime.Parse(SqlDateTime.MaxValue.ToString());

            if (minValue > dateTime.Value || maxValue < dateTime.Value) return false;

            return true;
        }
    }
}