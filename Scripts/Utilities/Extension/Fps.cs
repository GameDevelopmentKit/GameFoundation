namespace GameFoundation.Scripts.Utilities.Extension
{
    using UnityEngine;

    public class Fps : MonoBehaviour
    {
        float        deltaTime = 0.0f;
        public Color c         = Color.red;

        // Use this for initialization
        private void Start()
        {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR && !SHOW_FPS
            this.gameObject.SetActive(false);
#endif
        }

        // Update is called once per frame
        void Update() { this.deltaTime += (Time.deltaTime - this.deltaTime) * 0.1f; }

        void OnGUI()
        {
            int w = Screen.width, h = Screen.height;

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment        = TextAnchor.UpperLeft;
            style.fontSize         = h * 2 / 100;
            style.normal.textColor = this.c;
            float  msec = this.deltaTime * 1000.0f;
            float  fps  = 1.0f / this.deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            GUI.Label(rect, text, style);
        }
    }
}