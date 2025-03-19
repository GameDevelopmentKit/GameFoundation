namespace GameFoundation.Scripts.Utilities.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using I2.Loc;
    using TMPro;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class BaseTextMeshPro : MonoBehaviour
    {
        [SerializeField] private TMP_Text                 txtText;
        [SerializeField] private List<MaterialDictionary> listMaterial = new();

        private string        lastKey;
        private TMP_FontAsset currentFont;
        private Material      defaultMat;

        private void Awake()
        {
            this.txtText     ??= this.GetComponent<TMP_Text>();
            this.currentFont =   this.txtText.font;
            this.defaultMat  =   this.txtText.fontSharedMaterial;
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

            var font = await LocalizationService.Instance.GetFontAsset();

            if (font != null)
            {
                this.txtText.font = LocalizationManager.CurrentLanguage == "English" ? this.currentFont : font;
            }
            else
            {
                Debug.LogError("Font is null");
            }

            if (this.listMaterial.Count > 0)
            {
                var mat = this.listMaterial.FirstOrDefault(o => o.key == LocalizationManager.CurrentLanguage);

                this.txtText.fontSharedMaterial = (mat == null || mat.value == null) ? this.defaultMat : mat.value;
            }
        }
    }

    [Serializable]
    public class MaterialDictionary
    {
        public string   key;
        public Material value;
    }
}