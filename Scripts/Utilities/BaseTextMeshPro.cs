namespace Utilities
{
    using GameFoundation.Scripts.Utilities;
    using I2.Loc;
    using TMPro;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class BaseTextMeshPro : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtText;
        private                  string          lastKey;
        private                  TMP_FontAsset   currentFont;

        private void Awake()
        {
            this.txtText     ??= this.GetComponent<TextMeshProUGUI>();
            this.currentFont =   this.txtText.font;
            this.BeforeSetLocalization();
            this.SetTextWithLocalization(this.txtText.text);
            LocalizationService.Instance.OnLanguageChange += this.OnLanguageChange;
        }

        protected virtual void BeforeSetLocalization() { }

        private void OnLanguageChange() { this.SetTextWithLocalization(this.lastKey); }

        public async void SetTextWithLocalization(string key, Color colorCode = default)
        {
            this.txtText      ??= this.GetComponent<TextMeshProUGUI>();
            this.txtText.text =   LocalizationService.Instance.GetTextWithKey(key);

            if (colorCode != default)
            {
                this.txtText.color = colorCode;
            }

            this.lastKey = key;

            var font = await LocalizationService.Instance.GetFontAsset();

            if (font == null) return;
            this.txtText.font = LocalizationManager.CurrentLanguage == "English" ? this.currentFont : font;
        }
    }
}