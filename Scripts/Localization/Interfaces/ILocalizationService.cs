using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

namespace Localization.Interfaces
{

    /// <summary>
    /// Abstract interface for localization services to allow providers to be swapped out if needed.
    /// Current: UnityLocalizationServices. Future: I2LocalizationService, etc.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        ///  Initializes the localization service asynchronously, loading necessary data and setting up event listeners.
        /// </summary>
        /// <returns></returns>
        UniTask InitializeAsync();
        
        /// <summary>
        /// Gets the localized string asynchronously.
        /// </summary>
        UniTask<string> GetLocalizedStringAsync(string tableName, string key);
        
        /// <summary>
        ///  Gets the localized string synchronously.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetLocalizedString(string tableName, string key);

        /// <summary>
        /// Sets the active locale asynchronously.
        /// </summary>
        UniTaskVoid SetLocaleAsync(LanguageInfo language);

        /// <summary>
        /// Gets the currently active language code.
        /// </summary>
        LanguageInfo CurrentLanguage { get; }

        /// <summary>
        /// Gets the list of available languages with their codes and display names.
        /// </summary>
        List<LanguageInfo> GetAvailableLanguages();

        LanguageInfo GetCurrentSystemLanguageInfo();
    }

    public class LanguageInfo
    {
        public string LanguageCode;
        public int    Index;
        public string DisplayName;
        public LanguageInfo(Locale locale, int index)
        {
            this.LanguageCode = locale.Identifier.Code;
            this.DisplayName  = locale.LocaleName.Split(' ')[0]; // Use the first part of the locale name as the display name (e.g., "English" from "English (United States)")
            this.Index        = index;
        }
    }
}
