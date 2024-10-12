namespace Utilities.Extension
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// series of helper methods for common dictionary operations
    /// </summary>
    public static class DictionaryExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        /// <remarks>uses LINQ (for now) so do not use anywhere performance critical</remarks>
        public static Dictionary<string, object> ToGeneric<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            return source == null
                ? new()
                : source.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value as object);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        /// <remarks>uses LINQ (for now) so do not use anywhere performance critical</remarks>
        public static Dictionary<string, string> ToStringVals<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            return source == null
                ? new()
                : source.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value != null ? kv.Value.ToString() : null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        /// <remarks>uses LINQ (for now) so do not use anywhere performance critical</remarks>
        public static Dictionary<TKey, TValue> Copy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source)
        {
            return source == null
                ? new()
                : source.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dictionaries"></param>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <returns></returns>
        /// <remarks>uses LINQ (for now) so do not use anywhere performance critical</remarks>
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
            this   IReadOnlyDictionary<TKey, TValue>   source,
            params IReadOnlyDictionary<TKey, TValue>[] dictionaries
        )
        {
            var combinedDict = source == null ? new() : source.Copy();

            if (dictionaries == null || dictionaries.Length <= 0) return combinedDict;

            foreach (var dict in dictionaries)
            {
                if (dict == null) continue;

                var filtered = dict.Where(kv => !combinedDict.ContainsKey(kv.Key));
                var merged   = combinedDict.Concat(filtered);
                combinedDict = merged.ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            return combinedDict;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string TryGetString(this IReadOnlyDictionary<string, object> source, string key)
        {
            return source.TryGetValue(key, out var temp) ? temp.ToString() : null;
        }

        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return $"{{{string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray())}}}";
        }
    }
}