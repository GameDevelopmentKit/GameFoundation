namespace GameFoundation.Scripts.UIModule.Utilities
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(CanvasScaler))]
    public class ScaleScreenRatio : MonoBehaviour
    {
        /// <summary>
        /// Ratio of wide screen = 16/9, approx. 1.8
        /// </summary>
        private const float LandscapeStandardScreenRatio = 1.8f;

        private const float PortraitStandardScreenRatio = 0.56f;

        private void Awake()
        {
            this.SetCanvasScaler();
        }

        private void SetCanvasScaler()
        {
            // if current screen ratio > WideScreenRatio, it will be the long screen the need keep height and scale width, and vice versa 
            #if UNITY_EDITOR
            var standardScreenRatio = Screen.width > Screen.height ? LandscapeStandardScreenRatio : PortraitStandardScreenRatio;
            this.GetComponent<CanvasScaler>().matchWidthOrHeight = Screen.width * 1.0f / Screen.height >= standardScreenRatio ? 1 : 0;
            #else
            var standardScreenRatio = Screen.width > Screen.height ? LandscapeStandardScreenRatio : PortraitStandardScreenRatio;
            this.GetComponent<CanvasScaler>().matchWidthOrHeight = Screen.currentResolution.width * 1.0f / Screen.currentResolution.height >= standardScreenRatio ? 1 : 0;
            #endif
        }
    }
}