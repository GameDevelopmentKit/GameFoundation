namespace GameFoundation.Scripts.Utilities.LogService
{
    using System;
    using UnityEngine;
    using Color = UnityEngine.Color;

    public class LogService : ILogService
    {
        public void Log(string logContent, LogLevel logLevel = LogLevel.LOG)
        {
#if ENABLE_LOG || UNITY_EDITOR || DEVELOPMENT_BUILD
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
                case LogLevel.EXCEPTION:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
#endif
        }

        public void LogWithColor(string logContent, Color? c = null)
        {
#if ENABLE_LOG || UNITY_EDITOR || DEVELOPMENT_BUILD
            Color color = Color.white;
            if (c != null)
            {
                color = (Color)c;
            }
            Debug.Log($"<color=#{(byte)(color.r * 255f):X2}{(byte)(color.g * 255f):X2}{(byte)(color.b * 255f):X2}>{logContent}</color>");
#endif
        }

        public void Warning(string logContent)
        {
#if ENABLE_LOG || UNITY_EDITOR || DEVELOPMENT_BUILD
            this.Log(logContent, LogLevel.WARNING);
#endif
        }

        public void Error(string logContent)
        {
#if ENABLE_LOG || UNITY_EDITOR || DEVELOPMENT_BUILD
            this.Log(logContent, LogLevel.ERROR);
#endif
        }

        public void Exception(Exception exception)
        {
#if ENABLE_LOG || UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogException(exception);
#endif
        }

        public void Exception(Exception exception, string message)
        {
#if ENABLE_LOG || UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(message);
            Debug.LogException(exception);
#endif
        }
    }
}