namespace GameFoundation.Scripts.Utilities
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using I2.Loc;
    using TMPro;
    using Zenject;

    public class LocalizationService
    {
        [Inject] private IGameAssets         gameAssets;
        [Inject] private SetLanguage         setLanguage;
        public static    LocalizationService Instance { get; private set; }
        public event Action                  OnLanguageChange;
        public LocalizationService() { Instance = this; }
        public string GetTextWithKey(string key) { return LocalizationManager.TryGetTranslation(key, out var localization) ? localization : key; }

        public void ChangeLanguage(string language)
        {
            this.setLanguage._Language = language;
            this.setLanguage.ApplyLanguage();
            this.OnLanguageChange?.Invoke();
        }

        public async UniTask<TMP_FontAsset> GetFontAsset()
        {
            TMP_FontAsset fontAsset   = null;
            const string  fontKey     = "!!FONT_SETTING";
            var           fontAddress = LocalizationManager.TryGetTranslation(fontKey, out var localization) ? localization : fontKey;
            fontAsset = await this.gameAssets.LoadAssetAsync<TMP_FontAsset>(fontAddress);
            return fontAsset;
        }
    }
}