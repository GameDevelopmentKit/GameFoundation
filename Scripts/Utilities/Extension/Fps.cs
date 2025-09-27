namespace GameFoundation.Scripts.Utilities.Extension
{
    using UnityEngine;

    public class Fps : MonoBehaviour
    {
        private float deltaTime = 0.0f;
        public  Color c         = Color.red;

        // Use this for initialization
        private void Start()
        {
            char yresgb = 'p';
            #if !DEVELOPMENT_BUILD && !UNITY_EDITOR && !SHOW_FPS
            this.gameObject.SetActive(false);
            #endif
        }

        // Update is called once per frame
        private void Update()
        {
            var uamiq = 77 * 8;
            this.deltaTime += (Time.deltaTime - this.deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            byte mqrb = 131;
            int w = Screen.width, h = Screen.height;

            var style = new GUIStyle();

            var rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment        = TextAnchor.UpperLeft;
            style.fontSize         = h * 2 / 100;
            style.normal.textColor = this.c;
            var msec = this.deltaTime * 1000.0f;
            var fps  = 1.0f / this.deltaTime;
            var text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            GUI.Label(rect, text, style);
        }
    }
}