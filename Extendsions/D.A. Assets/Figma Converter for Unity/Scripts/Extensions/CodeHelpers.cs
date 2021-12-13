///D.A. Code Helpers v1.0
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets
{
    public static class CodeHelpers
    {
        /// <summary>
        /// Makes random bool.
        /// </summary>
        public static bool RandomBool
        {
            get
            {
                return UnityEngine.Random.value > 0.5f;
            }
        }
        /// <summary>
        /// Removes all childs from Transform
        /// </summary>
        public static void ClearChilds(this Transform transform)
        {
            foreach (Transform child in transform)
            {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(child.gameObject);
#else
            UnityEngine.Object.Destroy(child.gameObject);
#endif
            }
        }
        /// <summary>
        /// Removes all HTML tags from string
        /// <para><see href="https://stackoverflow.com/a/18154046"/></para>
        /// </summary>
        public static string RemoveHTML(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty);
        }
        /// <summary>
        /// Removing string between two strings
        /// <para><see href="https://stackoverflow.com/q/51891661"/></para>
        /// </summary>
        public static string RemoveBetween(this string text, string startTag, string endTag)
        {
            Regex regex = new Regex(string.Format("{0}(.*?){1}", Regex.Escape(startTag), Regex.Escape(endTag)), RegexOptions.RightToLeft);
            string result = regex.Replace(text, startTag + endTag);
            return result;
        }
        /// <summary>
        /// Get part of string between two strings
        /// <para><see href="https://stackoverflow.com/a/17252672"/></para>
        /// </summary>
        public static string GetBetween(this string text, string startTag, string endTag)
        {
            int pFrom = text.IndexOf(startTag) + startTag.Length;
            int pTo = text.LastIndexOf(endTag);
            string result = text.Substring(pFrom, pTo - pFrom);
            return result;
        }
        /// <summary>
        /// This method will removed everything but letters, numbers and spaces. It will also remove any ' or " followed by the character s.
        /// <para><see href="https://stackoverflow.com/a/4418510"/></para>
        /// </summary>
        public static string RemoveSpecChars(this string input)
        {
            Regex regex = new Regex("(?:[^a-z0-9 ]|(?<=['\"])s)",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant |
                RegexOptions.Compiled);

            return regex.Replace(input, string.Empty);
        }

        /// <summary>
        /// Remap value from these interval to another interval with saving proportion
        /// <para><see href="https://forum.unity.com/threads/re-map-a-number-from-one-range-to-another.119437/"/></para>
        /// </summary>
        /// <param name="value">Value for remapping</param>
        /// <param name="from1">Min value of 1th interval</param>
        /// <param name="to1">Max value of 2th interval</param>
        /// <param name="from2">Min value of 1th interval</param>
        /// <param name="to2">Max value of 2th interval</param>
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
        /// <summary>
        /// Split's list to some chunks
        /// <para><see href="https://stackoverflow.com/a/419063"/></para>
        /// </summary>
        public static List<List<T>> ToChunks<T>(this IEnumerable<T> array, int shunkSize)
        {
            int i = 0;
            List<List<T>> result = array.GroupBy(s => i++ / shunkSize).Select(g => g.ToList()).ToList();
            return result;
        }
        /// <summary>
        /// Adds dictionary to another dictionary
        /// <para><see href="https://stackoverflow.com/a/3982463"/></para>
        /// </summary>
        public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            foreach (T element in source)
                target.Add(element);
        }
        /// <summary>
        /// Destroying unity object as mehtod in editor
        /// </summary>
        public static void DestroyImmediate(this UnityEngine.Object unityObject)
        {
            UnityEngine.Object.DestroyImmediate(unityObject);
        }
        /// <summary>
        /// Destroying unity object as mehtod
        /// </summary>
        public static void Destroy(this UnityEngine.Object unityObject)
        {
            UnityEngine.Object.Destroy(unityObject);
        }
        /// <summary>
        /// Getting description of enum.
        /// <para><see href="https://stackoverflow.com/a/11959512"/></para>
        public static string GetDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }
        /// <summary>
        /// Debug.Log(log), but as method
        /// </summary>
        public static void Log(this string log)
        {
            Debug.Log(log);
        }
        /// <summary>
        /// Debug.LogError(log), but as method
        /// </summary>
        public static void LogError(this string error)
        {
            Debug.LogError(error);
        }
        /// <summary>
        /// Line break if the lineLength is exceeded.
        /// <para><see href="https://stackoverflow.com/a/16727478"/></para>
        /// </summary>
        public static string Splice(this string text, int lineLength)
        {
            int charCount = 0;
            IEnumerable<string> lines = text.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                            .GroupBy(w => (charCount += w.Length + 1) / lineLength)
                            .Select(g => string.Join(" ", g));

            return string.Join("\n", lines.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrollRect"></param>
        public static Vector2 GetSnapToPositionToBringChildIntoView(this ScrollRect instance, RectTransform child)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 viewportLocalPosition = instance.viewport.localPosition;
            Vector2 childLocalPosition = child.localPosition;
            Vector2 result = new Vector2(
                0 - (viewportLocalPosition.x + childLocalPosition.x),
                0 - (viewportLocalPosition.y + childLocalPosition.y)
            );
            return result;
        }

        /// <summary>
        /// <para>Example: "#ff000099".ToColor() red with alpha ~50%</para>
        /// <para>Example: "ffffffff".ToColor() white with alpha 100%</para>
        /// <para>Example: "00ff00".ToColor() green with alpha 100%</para>
        /// <para>Example: "0000ff00".ToColor() blue with alpha 0%</para>
        /// <para><see href="https://github.com/smkplus/KamaliDebug"/></para>
        /// </summary>
        public static Color ToColor(this string color)
        {
            if (color.StartsWith("#", StringComparison.InvariantCulture))
            {
                color = color.Substring(1); // strip #
            }

            if (color.Length == 6)
            {
                color += "FF"; // add alpha if missing
            }

            uint hex = Convert.ToUInt32(color, 16);
            float r = ((hex & 0xff000000) >> 0x18) / 255f;
            float g = ((hex & 0xff0000) >> 0x10) / 255f;
            float b = ((hex & 0xff00) >> 8) / 255f;
            float a = (hex & 0xff) / 255f;

            return new Color(r, g, b, a);
        }

        public static float Round(this float value)
        {
            return Mathf.Floor(0.5f + value);
        }

        public static string[] Split(this string text, string separator)
        {
            return text.Split(new string[] { separator }, StringSplitOptions.None);
        }

        /// <summary>
        /// Replaces text in a file.
        /// <para><see href="https://stackoverflow.com/a/58377834/8265642"/></para>
        /// </summary>
        /// <param name="filePath">Path of the text file.</param>
        /// <param name="searchText">Text to search for.</param>
        /// <param name="replaceText">Text to replace the search text.</param>
        static public void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            StreamReader reader = new StreamReader(filePath);
            string content = reader.ReadToEnd();
            reader.Close();

            content = Regex.Replace(content, searchText, replaceText);

            StreamWriter writer = new StreamWriter(filePath);
            writer.Write(content);
            writer.Close();
        }
        /// <summary>
        /// Substring with max lenght.
        /// <para><see href="https://codereview.stackexchange.com/a/101023"/></para>
        /// </summary>
        public static string Substring(this string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }
    }


    /// <summary>
    /// <para><see href="https://forum.unity.com/threads/easy-text-format-your-debug-logs-rich-text-format.906464/"/></para>
    /// </summary>
    public static class StringExtension
    {
        public static string TextBold(this string str) => "<b>" + str + "</b>";
        public static string TextColor(this string str, string clr) => string.Format("<color={0}>{1}</color>", clr, str);
        public static string TextItalic(this string str) => "<i>" + str + "</i>";
        public static string TextSize(this string str, int size) => string.Format("<size={0}>{1}</size>", size, str);
    }
}
#endif