#if UNITY_EDITOR
using DA_Assets;
using UnityEditor;
using UnityEngine;

namespace DA_Assets
{
    public static class Console
    {
        public static string redColor = "red";
        public static string blackColor = "black";
        public static string whiteColor = "white";
        public static string violetColor = "#8b00ff";
        public static string orangeColor = "#ffa500";
        public static void Error(string log)
        {
            Debug.LogError(log.TextColor(redColor).TextBold());
        }
        public static void Warning(string log)
        {
            Debug.LogWarning(log.TextColor(orangeColor).TextBold());
        }

        public static void WriteLine(string log)
        {
            string color = EditorGUIUtility.isProSkin ? whiteColor : blackColor;
            Debug.Log(log.TextColor(color).TextBold());
        }

        public static void Success(string log)
        {
            Debug.Log(log.TextColor(whiteColor).TextBold());
        }
    }
}
#endif