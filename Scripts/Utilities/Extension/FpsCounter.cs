namespace GameFoundation.Scripts.Utilities.Extension
{
    using System.Globalization;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Text))]
    public class FpsCounter : MonoBehaviour
    {
        private Text  fpsText;
        private float deltaTime;
        private void Start()
        {
#if !SHOW_FPS_COUNTER
            this.gameObject.SetActive(false);
#endif
            this.fpsText = this.GetComponent<Text>();
        }

        private void Update()
        {
            this.deltaTime    += (Time.deltaTime - this.deltaTime) * 0.1f; 
            this.fpsText.text =  string.Concat((this.deltaTime * 1000.0f).ToString("0.0"), " ms (", (1.0f / this.deltaTime).ToString("0."), " fps)");
        }
    }
}