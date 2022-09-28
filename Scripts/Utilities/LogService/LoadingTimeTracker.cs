namespace GameFoundation.Scripts.Utilities.LogService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundation.Scripts.Utilities.Extension;
    using UniRx;
    using UnityEngine;

    public enum LoadTime
    {
        Login,
        LoadUserData,
        ReadUserData,
        LoadBlueprint,
        ReadBlueprint,
        LoadLocalization,
        ReadLocalization,
        InitS3Bundle,
        LoadBundle,
        LoadConfig,
        LoadScene,
        Total
    }

    public class LoadingTimeTracker
    {
        private static readonly Dictionary<LoadTime, float> TimeTracker   = new Dictionary<LoadTime, float>();
        private static readonly Dictionary<LoadTime, float> StartTracker  = new Dictionary<LoadTime, float>();
        public static readonly  List<float>                 StartPauseTime = new List<float>();
        public static readonly  List<float>                 EndPauseTime   = new List<float>();

        public static bool SentAnalytics;


        public static void StartTrack(LoadTime action)
        {
            try
            {
                if (StartTracker.ContainsKey(action))
                {
                    StartTracker[action] = Time.realtimeSinceStartup;
                    Debug.LogError($"[ERROR] Already tracking {action}");
                }
                else
                    StartTracker.Add(action, Time.realtimeSinceStartup);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static void EndTrack(LoadTime action)
        {
            MainThreadDispatcher.Send(_ =>
            {
                if (!StartTracker.ContainsKey(action) || TimeTracker.ContainsKey(action)) return;

                var timeNow        = Time.realtimeSinceStartup;
                var totalPauseTime = 0f;
                for (var i = 0; i < EndPauseTime.Count; i++)
                {
                    if (EndPauseTime[i] > timeNow) break;

                    if (i >= StartPauseTime.Count)
                    {
                        Debug.LogError($"Error here: {i} {StartPauseTime.ToJson()} {EndPauseTime.ToJson()}");
                        break;
                    }

//                    Debug.LogError($"Add pause period from {StartPauseTime[i]} to {EndPauseTime[i]}");
                    totalPauseTime += EndPauseTime[i] - StartPauseTime[i];
                }

                Debug.LogError($"{action} takes {timeNow - StartTracker[action] - totalPauseTime} {StartTracker[action]} -> {timeNow}, totalPauseTime={totalPauseTime}");
                TimeTracker.Add(action, timeNow - StartTracker[action] - totalPauseTime);
            }, null);
        }

        public static void Clear()
        {
            TimeTracker.Clear();
            StartTracker.Clear();
            StartPauseTime.Clear();
            EndPauseTime.Clear();
            SentAnalytics = false;
        }

        public static Dictionary<string, object> GenReport()
        {
            return TimeTracker.ToDictionary<KeyValuePair<LoadTime, float>, string, object>(t => t.Key.ToString().ToLower(), t => (int)(t.Value * 1000));
        }
    }
}