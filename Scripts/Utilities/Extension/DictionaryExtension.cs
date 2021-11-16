namespace GameFoundation.Scripts.Utilities.Extension
{
    using System.Collections.Generic;
    using System.Linq;

    public static class DictionaryExtension
    {
        public static string ToDebugString<TKey, TValue> (this IDictionary<TKey, TValue> dictionary)
        {
            return $"{{{string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray())}}}";
        }
    }
}