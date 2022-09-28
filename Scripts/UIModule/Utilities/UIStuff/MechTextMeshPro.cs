namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using GameFoundation.Scripts.Utilities;
    using TMPro;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class MechTextMeshPro : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtText;
        private                  string          lastKey;
        private void Awake()
        {
            this.txtText ??= this.GetComponent<TextMeshProUGUI>();
            this.SetTextWithLocalization(this.txtText.text);
            LocalizationService.Instance.OnLanguageChange += this.OnLanguageChange;
        }
        private void OnLanguageChange() { this.SetTextWithLocalization(this.lastKey); }

        public void SetTextWithLocalization(string key, Color colorCode = default)
        {
            this.txtText      ??= this.GetComponent<TextMeshProUGUI>();
            this.txtText.text =   LocalizationService.Instance.GetTextWithKey(key);
            if (colorCode != default)
            {
                this.txtText.color = colorCode;
            }

            this.lastKey = key;

            //TOdo change font with language
            // this.txtText.font = await LocalizationService.Instance.GetFontAsset();
        }

    }
}