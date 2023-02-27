namespace UIModule.Utilities
{
    using System;
    using TMPro;

    public static class FormatNumberHelper
    {
        public static string ConvertToTimeElapsed(this object obj, long endTime, long startTime)
        {
            const string minuteLow  = "minute";
            const string minuteLows = "minutes";
            const string ago        = "ago";
            const string hourLow    = "hour";
            const string hourLows   = "hours";
            const string dayLow     = "day";
            const string dayLows    = "days";

            var currentTime = obj.CurrentTimeSecond();
            var elapsedTime = TimeSpan.FromSeconds(currentTime - (endTime));

            return elapsedTime.TotalHours switch
            {
                < 1 => $"{elapsedTime.ToString("%m")} {(Math.Abs(elapsedTime.TotalMinutes - 1) < 1f ? minuteLow : minuteLows)} {ago}",
                < 24 => $"{elapsedTime.ToString("%h")} {(Math.Abs(elapsedTime.TotalHours - 1) < 1f ? hourLow : hourLows)} {ago}",
                _ => $"{elapsedTime.ToString("%d")} {(Math.Abs(elapsedTime.TotalDays - 1) < 1f ? dayLow : dayLows)} {ago}"
            };
        }

        public static void SetTimeRemain(this TextMeshProUGUI txtCoolDown, long currentTime, long nextTimeRemain)
        {
            var remainTime = nextTimeRemain - currentTime;
            var hours      = TimeSpan.FromMilliseconds(remainTime).Hours;
            var minuses    = TimeSpan.FromMilliseconds(remainTime).Minutes;
            var second     = TimeSpan.FromMilliseconds(remainTime).Seconds;
            var s          = "00:00:00";

            if (remainTime >= 0)
            {
                s = $"{hours:00}:{minuses:00}:{second:00}";
            }

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

            if (remainTime >= 0)
            {
                s = $"{hours:00}h:{minuses:00}m:{second:00}s";
            }

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

            if (remainTime >= 0)
            {
                s = $"{originString}{day:00}d:{hours:00}h:{minuses:00}m:{second:00}s";
            }

            txtCoolDown.text = s;
        }

        public static void FormatNumberToMMHHDD(this TextMeshProUGUI txt, long time, string originstring = "")
        {
            var hours  = TimeSpan.FromSeconds(time).Hours;
            var minus  = TimeSpan.FromSeconds(time).Minutes;
            var second = TimeSpan.FromSeconds(time).Seconds;
            var s      = "00:00:00";

            if (time >= 0)
            {
                s = $"{hours:00}:{minus:00}:{second:00}";
            }

            txt.text = originstring + s;
        }

        public static string ConvertSecondToTime(this long second, bool useSemiColon = true, bool space = false) => TimeSpan.FromSeconds(second).ToTimeString(useSemiColon, space);

        public static string ConvertSecondToShortTime(this long second, bool useSemiColon = true) => TimeSpan.FromSeconds(second).ToShortTimeString(useSemiColon);

        public static string ToTimeString(this TimeSpan timeSpan, bool useSemiColon = false, bool space = false)
        {
            if (timeSpan.Days >= 1)
                return useSemiColon ? (space ? timeSpan.ToString(@"dd\:\ hh") : timeSpan.ToString(@"dd\:hh")) : (space ? timeSpan.ToString(@"dd\d\ hh\h") : timeSpan.ToString(@"dd\dhh\h"));

            if (timeSpan.Hours >= 1)
                return useSemiColon ? (space ? timeSpan.ToString(@"hh\:\ mm") : timeSpan.ToString(@"hh\:mm")) : (space ? timeSpan.ToString(@"hh\h\ mm\m") : timeSpan.ToString(@"hh\hmm\m"));

            return useSemiColon ? (space ? timeSpan.ToString(@"mm\:\ ss") : timeSpan.ToString(@"mm\:ss")) : (space ? timeSpan.ToString(@"mm\m\ ss\s") : timeSpan.ToString(@"mm\mss\s"));
        }

        public static string ToShortTimeString(this TimeSpan timeSpan, bool useSemiColon = false)
        {
            if (timeSpan.Days >= 1)
                return useSemiColon ? timeSpan.ToString(@"dd") : timeSpan.ToString(@"dd\d");

            if (timeSpan.Hours >= 1)
                return useSemiColon ? timeSpan.ToString(@"hh") : timeSpan.ToString(@"hh\h");

            if (timeSpan.Minutes >= 1)
                return useSemiColon ? timeSpan.ToString(@"mm") : timeSpan.ToString(@"mm\m");

            return useSemiColon ? timeSpan.ToString(@"ss") : timeSpan.ToString(@"ss\s");
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

        private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis(this object obj) { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); }

        public static long CurrentTimeSecond(this object obj) { return (long)((DateTime.UtcNow - Jan1St1970).TotalSeconds); }
    }
}