namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using UnityEngine;

    public class Fps : MonoBehaviour
    {
        private float    deltaTime = 0.0f;
        public  Color    c         = Color.red;
        private Rect     rect;
        private GUIStyle style = new GUIStyle();

        // Use this for initialization
        private void Start()
        {
#if !SHOW_FPS
            this.gameObject.SetActive(false);
#endif
            Init();
        }

        // Update is called once per frame
        void Update() { this.deltaTime += (Time.deltaTime - this.deltaTime) * 0.1f; }

        void Init()
        {
            rect                   = new Rect(0, 0, Screen.width, Screen.height * 2.0f / 100);
            style.alignment        = TextAnchor.UpperLeft;
            style.fontSize         = Screen.height * 2 / 100;
            style.normal.textColor = this.c;
        }

        void OnGUI()
        {
            string text = string.Concat((this.deltaTime * 1000.0f).ToString("0.0"), " ms (", (1.0f / this.deltaTime).ToString("0."), " fps)");
            GUI.Label(rect, text, style);
        }
    }
}