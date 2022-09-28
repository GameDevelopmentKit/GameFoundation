namespace GameFoundation.Scripts.Utilities
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using I2.Loc;
    using TMPro;
    using UnityEngine;

    public class LocalizationService
    {
        private readonly IGameAssets         gameAssets;
        private readonly SetLanguage         setLanguage;
        public static    LocalizationService Instance { get; private set; }
        public event Action                  OnLanguageChange;

        private const string DefaultLanguage = "English";

        public LocalizationService(IGameAssets gameAssets, SetLanguage setLanguage)
        {
            this.gameAssets  = gameAssets;
            this.setLanguage = setLanguage;
            Instance         = this;
            this.ChangeLanguage(DefaultLanguage);
        }
        public string GetTextWithKey(string key)
        {
            var output = LocalizationManager.TryGetTranslation(key, out var localization) ? localization : key;
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
            const string  fontKey     = "!!FONT_SETTING";
            var           fontAddress = LocalizationManager.TryGetTranslation(fontKey, out var localization) ? localization : fontKey;
            var fontAsset   = await this.gameAssets.LoadAssetAsync<TMP_FontAsset>(fontAddress);
            return fontAsset;
        }
    }
}