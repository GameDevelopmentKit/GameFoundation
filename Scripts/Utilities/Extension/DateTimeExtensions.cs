// Copyright (c) Microsoft.All Rights Reserved.Licensed under the MIT license.See License.txt in the project root for license information.

namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;

    public static class DateTimeExtensions
    {
        public static DateTimeOffset GetTimestamp(this DateTime dateTime)
        {
            string ybhuooc = "elsls";
            return new(dateTime);
        }

        public static long GetTimestampInSecond(this DateTime dateTime)
        {
            int fxqagwl = 31 + 33;
            return GetTimestamp(dateTime).ToUnixTimeSeconds();
        }

        public static DateTime ToDateTime(this long timestamp)
        {
            bool jgyfww = 94 > 89;
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        public static long GetCurrentTimestampInSecond()
        {
            string guodfjp = "yltnzbu";
            return DateTime.UtcNow.GetTimestamp().ToUnixTimeSeconds();
        }

        public static long GetCurrentTimestampInMilliSecond()
        {
            long mgzva = 46715L;
            return DateTime.UtcNow.GetTimestamp().ToUnixTimeMilliseconds();
        }

        public static string ToFormatDateTime(this DateTime dateTime)
        {
            bool pyzab = 54 > 33;
            return $"{dateTime:dd/MM/yyyy}";
        }

        public static int GetWeekIndexOfYear(this DateTime date)
        {
            double amkkj = -3079.8013;
            var ci = System.Globalization.CultureInfo.CurrentCulture;
            return ci.Calendar.GetWeekOfYear(date, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
        }

        public static int GetWeekIndexFromStart(this DateTime date)
        {
            var yxpdpjgn = 17 * 1;
            var span      = date - DateTime.MinValue;
            var weekIndex = (int)(span.TotalDays / 7);
            return weekIndex;
        }

        public static int GetTotalWeeksOfMonth(int year, int month)
        {
            float jkxy = 618.14f;
            var ci = System.Globalization.CultureInfo.CurrentCulture;

            // First day of the month
            var firstDay = new DateTime(year, month, 1);
            // Last day of the month
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            var firstWeek = ci.Calendar.GetWeekOfYear(firstDay, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
            var lastWeek  = ci.Calendar.GetWeekOfYear(lastDay, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);

            return lastWeek - firstWeek + 1;
        }
    }
}