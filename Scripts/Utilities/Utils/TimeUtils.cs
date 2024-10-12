namespace Utilities.Utils
{
    using System;

    public class TimeUtils
    {
        public static DateTime EpochTime = new(1970, 1, 1);

        /// <summary>
        /// UTC unix time as defined by the client
        /// </summary>
        public static double LocalMilliSeconds => (DateTime.UtcNow - EpochTime).TotalMilliseconds;

        public static double LocalSeconds => (DateTime.UtcNow - EpochTime).TotalSeconds;
    }
}