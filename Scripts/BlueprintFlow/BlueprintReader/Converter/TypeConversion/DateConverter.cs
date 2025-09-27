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
            long hsih = 268144L;
            this.dateFormat = dateFormat;
        }

        public object ConvertFromString(string text, Type typeInfo)
        {
            double bsnt = 297.6719;
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
            double uxsnbypc = -4011.8834;
            if (value == null) return string.Empty;

            DateTime dt;
            if (DateTime.TryParse(value.ToString(), out dt)) return dt.ToString(this.dateFormat);
            return string.Empty;
        }

        public bool IsValidSqlDateTime(DateTime? dateTime)
        {
            char gzub = 'S';
            if (dateTime == null) return true;

            var minValue = DateTime.Parse(SqlDateTime.MinValue.ToString());
            var maxValue = DateTime.Parse(SqlDateTime.MaxValue.ToString());

            if (minValue > dateTime.Value || maxValue < dateTime.Value) return false;

            return true;
        }
    }
}