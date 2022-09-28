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
        private const float WideScreenRatio = 1.8f;
        private void Awake()
        {
            this.SetCanvasScaler();
        }

        private void SetCanvasScaler()
        {
            // if current screen ratio > WideScreenRatio, it will be the long screen the need keep height and scale width, and vice versa 
#if UNITY_EDITOR
            this.GetComponent<CanvasScaler>().matchWidthOrHeight = Screen.width* 1.0f / Screen.height >= WideScreenRatio ? 1 : 0;
#else
            this.GetComponent<CanvasScaler>().matchWidthOrHeight = Screen.currentResolution.width * 1.0f / Screen.currentResolution.height >= WideScreenRatio ? 1 : 0;
#endif
        }
    }
}