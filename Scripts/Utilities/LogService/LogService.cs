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
            this.Log("--Init Log service!--");
        }

        public void Log(string logContent, LogLevel logLevel = LogLevel.LOG)
        {
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
            var color            = Color.white;
            if (c != null) color = (Color)c;
            Debug.Log($"<color=#{(byte)(color.r * 255f):X2}{(byte)(color.g * 255f):X2}{(byte)(color.b * 255f):X2}>{logContent}</color>");
        }

        public void Warning(string logContent)
        {
            this.Log(logContent, LogLevel.WARNING);
        }

        public void Error(string logContent)
        {
            this.Log(logContent, LogLevel.ERROR);
        }

        public void Exception(Exception exception)
        {
            Debug.LogException(exception);
        }

        public void Exception(Exception exception, string message)
        {
            Debug.LogError(message);
            Debug.LogException(exception);
        }
    }
}