namespace UIModule.Utilities.UIEffect.ChangeGrayscale
{
    using TMPro;
    using UnityEngine;

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextChangeGrayScale : ChangeGrayScaleBase {
        private Color cacheColor = Color.white;

        public override void EnableGrayScale() {
            var cpn = this.GetComponent<TextMeshProUGUI>();
            if (cpn.color != Color.grey) {
                this.cacheColor = cpn.color;
                cpn.color  = Color.grey;
            }
        }

        public override void DisableGrayScale() {
            var cpn = this.GetComponent<TextMeshProUGUI>();
            if (cpn.color == Color.grey)
                cpn.color = this.cacheColor;
        }
    }
}