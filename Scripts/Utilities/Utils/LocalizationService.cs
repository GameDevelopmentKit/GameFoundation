namespace GameFoundation.Scripts.Utilities.Utils
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using I2.Loc;
    using TMPro;
    using UnityEngine;
    using GameFoundation.DI;

    public class LocalizationService
    {
        private readonly IGameAssets         gameAssets;
        private readonly SetLanguage         setLanguage;
        public static    LocalizationService Instance { get; private set; }
        public event Action                  OnLanguageChange;

        public LocalizationService(IGameAssets gameAssets, SetLanguage setLanguage)
        {
            this.gameAssets  = gameAssets;
            this.setLanguage = setLanguage;
            Instance         = this;
            //this.ChangeLanguage(gameFoundationLocalData.IndexSettingRecord.Language);
        }

        public string GetTextWithKey(string key, List<string> formation = null)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var output = LocalizationManager.TryGetTranslation(key, out var localization) ? localization : key;

            if (formation is { Count: > 0 })
            {
                output = string.Format(output, formation.ToArray());
            }

            if (output.Equals(key))
            {
                Debug.LogWarning($"{key} have no localization");
            }

            return output;
        }

        //todo for change language
        public void ChangeLanguage(string language)
        {
            this.setLanguage._Language = language;
            this.setLanguage.ApplyLanguage();
            this.OnLanguageChange?.Invoke();
        }

        //todo for change font asset
        public async UniTask<TMP_FontAsset> GetFontAsset()
        {
            TMP_FontAsset fontAsset   = null;
            const string  fontKey     = "!!FONT_SETTING";
            var           fontAddress = LocalizationManager.TryGetTranslation(fontKey, out var localization) ? localization : fontKey;
            fontAsset = await this.gameAssets.LoadAssetAsync<TMP_FontAsset>(fontAddress);

            return fontAsset;
        }

        public List<string> ListLanguages() { return LocalizationManager.GetAllLanguages(); }
    }
}