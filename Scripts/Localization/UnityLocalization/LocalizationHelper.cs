namespace Localization.UnityLocalization
{
    using System;
    using Cysharp.Threading.Tasks;
    using Localization.Blueprint;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;

    /// <summary>
    /// Static helper class with extension methods for <see cref="LocalizedString"/>
    /// and utility methods for localization.
    /// </summary>
    public static class LocalizationHelper
    {
        private static string defaultNoTranslationMsgPrefix;

        /// <summary>
        /// Lazily extracts the prefix of Unity's "no translation found" message so we can detect it.
        /// </summary>
        private static string DefaultNoTranslationMsgPrefix
        {
            get
            {
                if (!string.IsNullOrEmpty(defaultNoTranslationMsgPrefix))
                    return defaultNoTranslationMsgPrefix;

                string defaultMsg = LocalizationSettings.StringDatabase.NoTranslationFoundMessage;
                if (string.IsNullOrEmpty(defaultMsg))
                    return null;

                // Find the first '{' character (template placeholder start) and take everything before it
                int placeholderIndex = defaultMsg.IndexOf('{');
                if (placeholderIndex < 0) placeholderIndex = defaultMsg.Length;

                return defaultNoTranslationMsgPrefix = defaultMsg.Substring(0, placeholderIndex);
            }
        }

        /// <summary>
        /// Returns true if the given string matches Unity's default "no translation found" message.
        /// </summary>
        public static bool IsDefaultNoTranslationMsg(string locString)
        {
            if (string.IsNullOrWhiteSpace(locString))
                return false;

            string prefix = DefaultNoTranslationMsgPrefix;
            if (string.IsNullOrEmpty(prefix))
                return false;

            if (locString.Length < prefix.Length)
                return false;

            return locString.Contains(prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the localized string, returning <paramref name="fallback"/> if no translation exists.
        /// </summary>
        public static string GetLocalizedStringWithFallback(this LocalizedString localizedString, string fallback)
        {
            if (localizedString == null || localizedString.IsEmpty)
                return fallback;

            var locString = localizedString.GetLocalizedString();
            if (string.IsNullOrEmpty(locString) || IsDefaultNoTranslationMsg(locString))
                return fallback;

            return locString;
        }

        /// <summary>
        /// Gets the localized string synchronously with a fallback, taking optional arguments.
        /// </summary>
        public static string GetLocalizedStringWithFallback(string tableName, string key, string fallback = "", params object[] arguments)
        {
            var locString = CreateLocalizedString(tableName, key);
            if (arguments != null && arguments.Length > 0)
            {
                locString.Arguments = arguments;
            }
            return locString.GetLocalizedStringWithFallback(fallback);
        }

        /// <summary>
        /// Returns true if the <see cref="LocalizedString"/> has a valid translation in the current locale.
        /// </summary>
        public static bool HasTranslation(this LocalizedString localizedString)
        {
            return HasTranslation(localizedString, out _);
        }

        /// <summary>
        /// Returns true if the <see cref="LocalizedString"/> has a valid translation, outputting the result.
        /// </summary>
        public static bool HasTranslation(this LocalizedString localizedString, out string locString)
        {
            if (localizedString != null && !localizedString.IsEmpty &&
                localizedString.GetLocalizedString() is { } result &&
                !IsDefaultNoTranslationMsg(result))
            {
                locString = result;
                return true;
            }

            locString = null;
            return false;
        }

        /// <summary>
        /// Gets or creates a <see cref="LocalizedString"/> with caching support for runtime-generated references.
        /// Useful when you need to create <see cref="LocalizedString"/> objects dynamically (e.g., from code)
        /// but want to reuse the same object to avoid allocations.
        /// </summary>
        /// <param name="cacheLocString">Reference to the cached LocalizedString (pass as ref to allow reuse)</param>
        /// <param name="tableName">The string table collection name</param>
        /// <param name="keyGenFunc">A function that generates the entry key (only called if cache is invalid)</param>
        /// <returns>The (possibly cached) LocalizedString</returns>
        public static LocalizedString GetDynamicLocStringWithCache(
            ref LocalizedString cacheLocString,
            string tableName,
            Func<string> keyGenFunc)
        {
            if (cacheLocString == null)
            {
                cacheLocString = new LocalizedString(tableName, keyGenFunc());
                return cacheLocString;
            }

#if UNITY_EDITOR
            // In editor, validate and update in case of script recompilation
            bool isEmpty = cacheLocString.IsEmpty;

            if (isEmpty || !cacheLocString.TableReference.TableCollectionName
                    .Equals(tableName, StringComparison.Ordinal))
            {
                cacheLocString.TableReference = tableName;
            }

            string key = keyGenFunc();
            if (!string.IsNullOrEmpty(key) &&
                (isEmpty || !cacheLocString.TableEntryReference.Key.Equals(key, StringComparison.Ordinal)))
            {
                cacheLocString.TableEntryReference = key;
            }
#endif
            return cacheLocString;
        }

        /// <summary>
        /// Convenience method to create a new <see cref="LocalizedString"/> from table name and key.
        /// </summary>
        public static LocalizedString CreateLocalizedString(string tableName, string key)
        {
            return new LocalizedString(tableName, key);
        }

        /// <summary>
        /// Convenience method to create a new <see cref="LocalizedString"/> from table name and key ID.
        /// </summary>
        public static LocalizedString CreateLocalizedString(string tableName, long keyId)
        {
            return new LocalizedString
            {
                TableReference = tableName,
                TableEntryReference = keyId
            };
        }

        /// <summary>
        /// Gets the localized string asynchronously with an online data fallback.
        /// First checks online overrides, then falls back to Unity's string tables.
        /// </summary>
        /// <param name="tableName">The string table name</param>
        /// <param name="key">The string entry key</param>
        /// <param name="onlineData">Optional online localization data for runtime overrides</param>
        /// <param name="currentLanguage">The current language code</param>
        /// <returns>The localized string</returns>
        public static async UniTask<string> GetLocalizedStringWithOnlineFallback(
            string tableName,
            string key,
            LocalizationDataOnline onlineData = null,
            string currentLanguage = null)
        {
            if (string.IsNullOrEmpty(tableName))
                return key;

            // Try online override first
            if (onlineData != null && !string.IsNullOrEmpty(currentLanguage) && 
                onlineData.TryGetLocalizedText(currentLanguage, key, out var onlineText) && 
                !string.IsNullOrEmpty(onlineText))
            {
                return onlineText;
            }

            // Fall back to Unity string tables
            var localData = await LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, key);

            return localData;
        }
    }
}
