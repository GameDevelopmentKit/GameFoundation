namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class DrawCallback
    {
        public Action BeforeDraw;
        public Action AfterDraw;
    }


    public class FR2_TabView
    {
        public int          current;
        public GUIContent[] labels;
        public IWindow      window;
        public Action       onTabChange;
        public DrawCallback callback;
        public bool         canDeselectAll; // can there be no active tabs

        public FR2_TabView(IWindow w, bool canDeselectAll)
        {
            this.window         = w;
            this.canDeselectAll = canDeselectAll;
        }

        public bool DrawLayout()
        {
            var result = false;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (this.callback != null && this.callback.BeforeDraw != null) this.callback.BeforeDraw();

                for (var i = 0; i < this.labels.Length; i++)
                {
                    var isActive = i == this.current;

                    var lb = this.labels[i];
                    var clicked = lb.image != null
                        ? GUI2.ToolbarToggle(ref isActive, lb.image, Vector2.zero, lb.tooltip)
                        : GUI2.Toggle(ref isActive, lb, EditorStyles.toolbarButton);

                    if (!clicked) continue;

                    this.current = !isActive && this.canDeselectAll ? -1 : i;
                    result       = true;

                    if (this.onTabChange != null) this.onTabChange();
                    if (this.window == null) continue;
                    this.window.OnSelectionChange(); // force refresh tabs
                    this.window.WillRepaint = true;
                }

                if (this.callback != null && this.callback.AfterDraw != null) this.callback.AfterDraw();
            }
            GUILayout.EndHorizontal();

            return result;
        }

        public static FR2_TabView FromEnum(Type enumType, IWindow w, bool canDeselectAll = false)
        {
            var values = Enum.GetValues(enumType);
            var labels = new List<GUIContent>();

            foreach (var item in values) labels.Add(new GUIContent(item.ToString()));

            return new FR2_TabView(w, canDeselectAll) { current = 0, labels = labels.ToArray() };
        }
        public static GUIContent GetGUIContent(object tex)
        {
            if (tex is GUIContent) return (GUIContent)tex;
            if (tex is Texture) return new GUIContent((Texture)tex);
            if (tex is string) return new GUIContent((string)tex);
            return GUIContent.none;
        }

        public static FR2_TabView Create(IWindow w, bool canDeselectAll = false, params object[] titles)
        {
            var labels = new List<GUIContent>();
            foreach (var item in titles) labels.Add(GetGUIContent(item));
            return new FR2_TabView(w, canDeselectAll) { current = 0, labels = labels.ToArray() };
        }
    }
}