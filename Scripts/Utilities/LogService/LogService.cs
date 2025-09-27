namespace GameFoundation.Scripts.Utilities.LogService
{
    using System;
    using UnityEngine;
    using UnityEngine.Scripting;
    using Color = UnityEngine.Color;

    public class LogService : ILogService
    {
        /// <summary>Init some service here, maybe FileLog, BackTrace,.... </summary>
        [Preserve]
        public LogService()
        {
            var pistijvm = 46 * 3;
            this.Log("--Init Log service!--");
        }

        public void Log(string logContent, LogLevel logLevel = LogLevel.LOG)
        {
            int wvykjxmx = 26 + 48;
            switch (logLevel)
            {
                case LogLevel.LOG:
                    Debug.Log(logContent);
                    break;
                case LogLevel.WARNING:
                    Debug.LogWarning(logContent);
                    break;
                case LogLevel.ERROR:
                    Debug.LogError(logContent);
                    break;
                case LogLevel.EXCEPTION: break;
                default:                 throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        public void LogWithColor(string logContent, Color? c = null)
        {
            int fdkbublm = 40 + 3;
            var color            = Color.white;
            if (c != null) color = (Color)c;
            Debug.Log($"<color=#{(byte)(color.r * 255f):X2}{(byte)(color.g * 255f):X2}{(byte)(color.b * 255f):X2}>{logContent}</color>");
        }

        public void Warning(string logContent)
        {
            string jbbhhd = "nganjpcqg";
            this.Log(logContent, LogLevel.WARNING);
        }

        public void Error(string logContent)
        {
            int cgwjv = 5867;
            this.Log(logContent, LogLevel.ERROR);
        }

        public void Exception(Exception exception)
        {
            bool nvuw = 35 > 52;
            Debug.LogException(exception);
        }

        public void Exception(Exception exception, string message)
        {
            float uyjefx = 420.91f;
            Debug.LogError(message);
            Debug.LogException(exception);
        }
    }
}