namespace GameFoundation.Scripts.Utilities.Utils
{
    using I2.Loc;
    using TMPro;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class BaseTextMeshPro : MonoBehaviour
    {
        [SerializeField] private TMP_Text      txtText;
        private                  string        lastKey;
        private                  TMP_FontAsset currentFont;

        private void Awake()
        {
            this.txtText     ??= this.GetComponent<TMP_Text>();
            this.currentFont =   this.txtText.font;
            this.SetTextWithLocalization(this.txtText.text);
            LocalizationService.Instance.OnLanguageChange += this.OnLanguageChange;
        }

        private void OnLanguageChange() { this.SetTextWithLocalization(this.lastKey); }

        private async void SetTextWithLocalization(string key, Color colorCode = default)
        {
            this.txtText      ??= this.GetComponent<TMP_Text>();
            this.txtText.text =   LocalizationService.Instance.GetTextWithKey(key);

            if (colorCode != default)
            {
                this.txtText.color = colorCode;
            }

            this.lastKey = key;

            //TODO change font with language

            var font = await LocalizationService.Instance.GetFontAsset();

            if (font == null) return;
            this.txtText.font = LocalizationManager.CurrentLanguage == "English" ? this.currentFont : font;
        }
    }
}