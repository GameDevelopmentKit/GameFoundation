namespace Localization.Blueprint
{
    using System.Collections.Generic;
    using Localization.Interfaces;

    public class LocalizationDataOnline : IBlueprintLocalizationData
    {
        public Dictionary<string, LocalizationDataModel> LocalizationDatas { get; set; } = new();

        public LocalizationDataModel GetLocalizationData(string languageCode)
        {
            return LocalizationDatas.GetValueOrDefault(languageCode);
        }

        public string GetLocalizedText(string languageCode, string key)
        {
            return LocalizationDatas.TryGetValue(languageCode, out var data) && data.LocalizedTexts.TryGetValue(key, out var localizedText) ?
                localizedText :
                string.Empty;
        }
        
        public bool TryGetLocalizedText(string languageCode, string key, out string localizedText)
        {
            localizedText = string.Empty;
            return LocalizationDatas.TryGetValue(languageCode, out var data) && data.LocalizedTexts.TryGetValue(key, out localizedText);

        }
    }
}
