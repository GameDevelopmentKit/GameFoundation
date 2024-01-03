namespace UIModule.Utilities.UIEffect.ChangeGrayscale
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Image))]
    public class ImageChangeGrayScale : ChangeGrayScaleBase
    {
        [SerializeField] private bool  activeOnStart;
        private                  Image image;
        private                  Image ImageComponent => this.image == null ? this.image = this.GetComponent<Image>() : this.image;

        private Material cacheMaterial;

        private static Material grayScaleMaterial;

        public static Material GrayScaleMaterial
        {
            get
            {
                if (grayScaleMaterial == null)
                {
                    var uiGrayScaleShader = Shader.Find("Custom/UI/Grayscale");
                    if (uiGrayScaleShader != null)
                    {
                        grayScaleMaterial = new Material(uiGrayScaleShader);
                    }
                }

                return grayScaleMaterial;
            }
        }

        private void Start()
        {
            if (this.activeOnStart)
            {
                this.EnableGrayScale();
            }
        }

        public override void EnableGrayScale()
        {
            if (this.ImageComponent.material != GrayScaleMaterial) this.cacheMaterial = this.ImageComponent.material;
            this.ImageComponent.material = GrayScaleMaterial;
        }

        public override void DisableGrayScale()
        {
            this.ImageComponent.material = this.cacheMaterial;
            this.cacheMaterial           = null;
        }
    }
}