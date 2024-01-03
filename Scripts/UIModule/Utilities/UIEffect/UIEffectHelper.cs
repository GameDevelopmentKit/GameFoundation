namespace UIModule.Utilities.UIEffect
{
    using UIModule.Utilities.UIEffect.ChangeGrayscale;
    using UnityEngine;

    public static class UIEffectHelper
    {
        public static void SetAllChildrenToGrayscale(this GameObject gameObject, bool value)
        {
            var changeGrayScale = gameObject.GetComponentsInChildren<IChangeGrayScale>();
            foreach (var grayScale in changeGrayScale)
            {
                if (value) grayScale.EnableGrayScale();
                else grayScale.DisableGrayScale();
            }
        }
    }
}