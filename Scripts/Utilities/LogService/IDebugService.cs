namespace GameFoundation.Scripts.Utilities.LogService
{
    using System;
    using UnityEngine;

    public enum LogLevel
    {
        LOG       = 1,
        WARNING   = 2,
        ERROR     = 3,
        EXCEPTION = 4,
    }

    /// <summary>Wrapped debug for unity, maybe will do some extra  </summary>
    public interface ILogService
    {
        void Log(string logContent, LogLevel logLevel = LogLevel.LOG);

        void LogWithColor(string logContent, Color? c = null);

        void Warning(string logContent);

        void Error(string logContent);

        void Exception(Exception exception);

        void Exception(Exception exception, string message);
    }
}