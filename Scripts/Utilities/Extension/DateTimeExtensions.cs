// Copyright (c) Microsoft.All Rights Reserved.Licensed under the MIT license.See License.txt in the project root for license information.

namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;

    public static class DateTimeExtensions
    {
        public static DateTimeOffset GetTimestamp(this DateTime dateTime) { return new DateTimeOffset(dateTime); }

        public static long GetTimestampInSecond(this DateTime dateTime)
        {
            return GetTimestamp(dateTime).ToUnixTimeSeconds();
        }

        public static DateTime ToDateTime(this long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        public static long GetCurrentTimestampInSecond() { return DateTime.UtcNow.GetTimestamp().ToUnixTimeSeconds(); }

        public static long GetCurrentTimestampInMilliSecond()
        {
            return DateTime.UtcNow.GetTimestamp().ToUnixTimeMilliseconds();
        }

        public static string ToFormatDateTime(this DateTime dateTime) { return $"{dateTime:dd/MM/yyyy}"; }
        
        public static int GetWeekIndexOfYear(this DateTime date)
        {
            var ci = System.Globalization.CultureInfo.CurrentCulture;
            return ci.Calendar.GetWeekOfYear(date, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
        }
        
        public static int GetWeekIndexFromStart(this DateTime date)
        {
            var span      = date - DateTime.MinValue;
            var      weekIndex = (int)(span.TotalDays / 7);
            return weekIndex;
        }
    }
}