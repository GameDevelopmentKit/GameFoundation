namespace Localization.UnityLocalization
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Localization.Blueprint;
    using Localization.Interfaces;
    using Localization.Signals;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;
    using Zenject;

    public class UnityLocalizationServices : ILocalizationService, System.IDisposable
    {
        private readonly LocalizationDataOnline localizationDataOnline;
        private readonly SignalBus              signalBus;

        public LanguageInfo CurrentLanguage { private set; get; }

        private List<LanguageInfo> localizationData = new List<LanguageInfo>();

        public UnityLocalizationServices(SignalBus signalBus, LocalizationDataOnline localizationDataOnline)
        {
            this.localizationDataOnline = localizationDataOnline;
            this.signalBus              = signalBus;
        }

        public async UniTask InitializeAsync()
        {
            await LocalizationSettings.InitializationOperation.Task;

            var selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale != null)
            {
                this.CurrentLanguage = new LanguageInfo(selectedLocale, LocalizationSettings.AvailableLocales.Locales.IndexOf(selectedLocale));
            }
        }

        public void Dispose()
        {
        }

        public virtual async UniTaskVoid SetLocaleAsync(LanguageInfo language)
        {
            if (CurrentLanguage != null && CurrentLanguage.Index == language.Index) return;
            await LocalizationSettings.InitializationOperation.Task;
            CurrentLanguage                     = language;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[language.Index];
            this.signalBus.Fire(new LocaleChangedSignal(language));
        }

        public virtual async UniTask<string> GetLocalizedStringAsync(string tableName, string key)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return key;
            }

            var localData = await LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, key);

            if (string.IsNullOrEmpty(localData) || LocalizationHelper.IsDefaultNoTranslationMsg(localData))
            {
                var onlineData = this.GetRealtimeLocalizationOnline(key);
                if (!string.IsNullOrEmpty(onlineData))
                {
                    return onlineData;
                }

                return key;
            }

            return localData;
        }

        public virtual string GetLocalizedString(string tableName, string key)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return key;
            }

            var localData = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key);

            if (string.IsNullOrEmpty(localData) || LocalizationHelper.IsDefaultNoTranslationMsg(localData))
            {
                var onlineData = this.GetRealtimeLocalizationOnline(key);
                if (!string.IsNullOrEmpty(onlineData))
                {
                    return onlineData;
                }

                return key;
            }

            return localData;
        }

        private string GetRealtimeLocalizationOnline(string key)
        {
            if (this.localizationDataOnline.LocalizationDatas.Count == 0 || this.CurrentLanguage == null)
            {
                return string.Empty;
            }

            return this.localizationDataOnline.GetLocalizedText(this.CurrentLanguage.LanguageCode, key);
        }

        public List<LanguageInfo> GetAvailableLanguages()
        {
            if (this.localizationData.Count > 0)
            {
                return this.localizationData;
            }

            for (int index = 0; index < LocalizationSettings.AvailableLocales.Locales.Count; index++)
            {
                var locale = LocalizationSettings.AvailableLocales.Locales[index];
                this.localizationData.Add(new LanguageInfo(locale, index));
            }

            return this.localizationData;
        }


        public LanguageInfo GetCurrentSystemLanguageInfo()
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(Application.systemLanguage);
            if (locale == null)
            {
                Debug.LogWarning(
                    $"Locale not found for system language: {Application.systemLanguage}. Defaulting to English.");
                return null;
            }

            var index = LocalizationSettings.AvailableLocales.Locales.IndexOf(locale);
            return new LanguageInfo(locale, index);
        }
    }
}
