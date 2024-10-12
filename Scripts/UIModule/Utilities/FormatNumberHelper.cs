namespace UIModule.Utilities
{
    using System;
    using TMPro;

    public static class FormatNumberHelper
    {
        public static string ConvertToTimeElapsed(long totalSecond)
        {
            const string minuteLow  = "minute";
            const string minuteLows = "minutes";
            const string ago        = "ago";
            const string hourLow    = "hour";
            const string hourLows   = "hours";
            const string dayLow     = "day";
            const string dayLows    = "days";

            var elapsedTime = TimeSpan.FromSeconds(totalSecond);

            return elapsedTime.TotalHours switch
            {
                < 1  => $"{elapsedTime.ToString("%m")} {(Math.Abs(elapsedTime.TotalMinutes - 1) < 1f ? minuteLow : minuteLows)} {ago}",
                < 24 => $"{elapsedTime.ToString("%h")} {(Math.Abs(elapsedTime.TotalHours - 1) < 1f ? hourLow : hourLows)} {ago}",
                _    => $"{elapsedTime.ToString("%d")} {(Math.Abs(elapsedTime.TotalDays - 1) < 1f ? dayLow : dayLows)} {ago}",
            };
        }

        public static string ConvertToTimeElapsed(DateTime endTime)
        {
            return ConvertToTimeElapsed((long)(endTime - DateTime.UtcNow).TotalSeconds);
        }

        public static void SetTimeRemain(this TextMeshProUGUI txtCoolDown, long currentTime, long endTime)
        {
            var remainTime = endTime - currentTime;
            var hours      = TimeSpan.FromMilliseconds(remainTime).Hours;
            var minuses    = TimeSpan.FromMilliseconds(remainTime).Minutes;
            var second     = TimeSpan.FromMilliseconds(remainTime).Seconds;
            var s          = "00:00:00";

            if (remainTime >= 0) s = $"{hours:00}:{minuses:00}:{second:00}";

            txtCoolDown.text = s;
        }

        public static void SetTimeRemainWithHHMMSS(this TextMeshProUGUI txtCoolDown, long currentTime, long endTime)
        {
            var remainTime = endTime - currentTime;
            var day        = TimeSpan.FromMilliseconds(remainTime).Days;
            var hours      = day * 24 + TimeSpan.FromMilliseconds(remainTime).Hours;
            var minuses    = TimeSpan.FromMilliseconds(remainTime).Minutes;
            var second     = TimeSpan.FromMilliseconds(remainTime).Seconds;
            var s          = "00h:00m:00s";

            if (remainTime >= 0) s = $"{hours:00}h:{minuses:00}m:{second:00}s";

            txtCoolDown.text = s;
        }

        public static void SetTimeRemainWithDDHHMMSS(this TextMeshProUGUI txtCoolDown, long currentTime, long endTime, string originString = "")
        {
            var remainTime = endTime - currentTime;
            var day        = TimeSpan.FromMilliseconds(remainTime).Days;
            var hours      = TimeSpan.FromMilliseconds(remainTime).Hours;
            var minuses    = TimeSpan.FromMilliseconds(remainTime).Minutes;
            var second     = TimeSpan.FromMilliseconds(remainTime).Seconds;
            var s          = "00d:00h:00m:00s";

            if (remainTime >= 0) s = $"{originString}{day:00}d:{hours:00}h:{minuses:00}m:{second:00}s";

            txtCoolDown.text = s;
        }

        public static void FormatNumberToMMHHDD(this TextMeshProUGUI txt, long time, string originstring = "")
        {
            var hours  = TimeSpan.FromSeconds(time).Hours;
            var minus  = TimeSpan.FromSeconds(time).Minutes;
            var second = TimeSpan.FromSeconds(time).Seconds;
            var s      = "00:00:00";

            if (time >= 0) s = $"{hours:00}:{minus:00}:{second:00}";

            txt.text = originstring + s;
        }

        public static string ToTimeString(this float time, string delimiter = ":", bool showZeroHour = false, bool showZeroMinus = false)
        {
            var    hours  = TimeSpan.FromSeconds(time).Hours;
            var    minus  = TimeSpan.FromSeconds(time).Minutes;
            var    second = TimeSpan.FromSeconds(time).Seconds;
            string result;

            if (hours > 0 || showZeroHour)
                result = $"{hours:00}{delimiter}{minus:00}{delimiter}{second:00}";
            else if (minus > 0 || showZeroMinus)
                result = $"{minus:00}{delimiter}{second:00}";
            else
                result = $"{second:00}";

            return result;
        }

        public static string ToTimeString(this long time, string delimiter = ":")
        {
            return ToTimeString((float)time, delimiter);
        }

        public static string ToTimeString(this int time, string delimiter = ":")
        {
            return ToTimeString((float)time, delimiter);
        }

        public static string ToTimeString(this TimeSpan timeSpan, bool useSemiColon = false, bool space = false)
        {
            if (timeSpan.Days >= 1) return useSemiColon ? space ? timeSpan.ToString(@"dd\:\ hh") : timeSpan.ToString(@"dd\:hh") : space ? timeSpan.ToString(@"dd\d\ hh\h") : timeSpan.ToString(@"dd\dhh\h");

            if (timeSpan.Hours >= 1) return useSemiColon ? space ? timeSpan.ToString(@"hh\:\ mm") : timeSpan.ToString(@"hh\:mm") : space ? timeSpan.ToString(@"hh\h\ mm\m") : timeSpan.ToString(@"hh\hmm\m");

            return useSemiColon ? space ? timeSpan.ToString(@"mm\:\ ss") : timeSpan.ToString(@"mm\:ss") : space ? timeSpan.ToString(@"mm\m\ ss\s") : timeSpan.ToString(@"mm\mss\s");
        }

        public static string ToShortTimeString(this TimeSpan timeSpan)
        {
            if (timeSpan.Days >= 1) return timeSpan.Days > 1 ? $"{timeSpan.Days} days" : $"{timeSpan.Days} day";

            if (timeSpan.Hours >= 1) return timeSpan.Hours > 1 ? $"{timeSpan.Hours} hours" : $"{timeSpan.Hours} hour";

            if (timeSpan.Minutes >= 1) return timeSpan.Minutes > 1 ? $"{timeSpan.Minutes} minutes" : $"{timeSpan.Minutes} minute";

            return timeSpan.Seconds > 1 ? $"{timeSpan.Seconds} seconds" : $"{timeSpan.Seconds} second";
        }
    }

    public static class GlobalCountDown
    {
        private static DateTime timeStarted;
        private static TimeSpan totalTime;

        public static void StartCountDown(TimeSpan time)
        {
            timeStarted = DateTime.UtcNow;
            totalTime   = time;
        }

        public static TimeSpan TimeLeft
        {
            get
            {
                var result = totalTime - (DateTime.UtcNow - timeStarted);

                return result.TotalSeconds <= 0 ? TimeSpan.Zero : result;
            }
        }

        private static readonly DateTime Jan1St1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1St1970).TotalMilliseconds;
        }

        public static long CurrentTimeSecond()
        {
            return (long)(DateTime.UtcNow - Jan1St1970).TotalSeconds;
        }
    }
}