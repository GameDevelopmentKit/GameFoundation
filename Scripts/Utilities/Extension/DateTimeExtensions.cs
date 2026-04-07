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
        
        /// <summary>
        /// Calculates the nearest period-aligned time from a reference point.
        /// Used by PurchaseOptionData to align refresh timers to period boundaries.
        /// Handles daily (86400s), weekly (604800s), monthly (2592000s), and yearly (31536000s)
        /// periods with UTC day-start alignment, plus arbitrary sub-day periods.
        /// </summary>
        /// <param name="lastTime">Reference time. DateTime.MinValue is treated as UtcNow.</param>
        /// <param name="period">Period in seconds. Returns lastTime unchanged if &lt;= 0.</param>
        /// <returns>The most recent period boundary before UtcNow.</returns>
        public static DateTime GetNearestTimeFromPeriod(this DateTime lastTime, long period)
        {
            if (period <= 0)
            {
                return lastTime;
            }

            if (lastTime.Equals(DateTime.MinValue))
            {
                lastTime = DateTime.UtcNow;
            }

            if (period % 86400 == 0)
            {
                var lastTimeUtcDate = lastTime.Date;

                if (period == 86400)
                {
                    lastTime = lastTimeUtcDate;
                }
                else if (period == 604800)
                {
                    int daysToSubtract = (int)lastTimeUtcDate.DayOfWeek;
                    lastTime = lastTimeUtcDate.AddDays(-daysToSubtract);
                }
                else if (period == 2592000)
                {
                    lastTime = new DateTime(lastTimeUtcDate.Year, lastTimeUtcDate.Month, 1);
                }
                else if (period == 31536000)
                {
                    lastTime = new DateTime(lastTimeUtcDate.Year, 1, 1);
                }

                var intervalDays       = period / 86400;
                int totalPeriodsPassed = (int)((DateTime.UtcNow - lastTime).TotalDays / intervalDays);
                return lastTime.AddDays(intervalDays * totalPeriodsPassed);
            }

            var now           = DateTime.UtcNow;
            var periodsPassed = (int)(now - lastTime).TotalSeconds / (int)period;
            return lastTime.AddSeconds(periodsPassed * period);
        }
    }
}