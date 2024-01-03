namespace UIModule.Utilities.UIEffect.ChangeGrayscale
{
    using UnityEngine;

    public abstract class ChangeGrayScaleBase : MonoBehaviour, IChangeGrayScale {
        public abstract void EnableGrayScale();

        public abstract void DisableGrayScale();

        public void SetGrayScale(bool grayscale) {
            if(grayscale) this.EnableGrayScale();
            else this.DisableGrayScale();
        }
    }
}