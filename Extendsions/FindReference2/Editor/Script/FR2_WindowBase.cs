namespace vietlabs.fr2
{
    using UnityEditor;
    using UnityEngine;

    public interface IWindow
    {
        bool WillRepaint { get; set; }
        void Repaint();
        void OnSelectionChange();
    }

    internal interface IRefDraw
    {
        IWindow window { get; }
        int     ElementCount();
        bool    DrawLayout();
        bool    Draw(Rect rect);
    }

    public abstract class FR2_WindowBase : EditorWindow, IWindow
    {
        public    bool WillRepaint { get; set; }
        protected bool showFilter, showIgnore;

        //[NonSerialized] protected bool lockSelection;
        //[NonSerialized] internal List<FR2_Asset> Selected;

        public static bool isNoticeIgnore;

        public void AddItemsToMenu(GenericMenu menu)
        {
            var api = FR2_Cache.Api;
            if (api == null) return;

            menu.AddDisabledItem(new GUIContent("FR2 - v2.5.2"));
            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Enable"), !api.disabled, () => { api.disabled = !api.disabled; });
            menu.AddItem(new GUIContent("Refresh"), false, () =>
            {
                //FR2_Asset.lastRefreshTS = Time.realtimeSinceStartup;
                Resources.UnloadUnusedAssets();
                EditorUtility.UnloadUnusedAssetsImmediate();
                FR2_Cache.Api.Check4Changes(true);
                FR2_SceneCache.Api.SetDirty();
            });

#if FR2_DEV
            menu.AddItem(new GUIContent("Refresh Usage"), false, () => FR2_Cache.Api.Check4Usage());
            menu.AddItem(new GUIContent("Refresh Selected"), false, ()=> FR2_Cache.Api.RefreshSelection());
            menu.AddItem(new GUIContent("Clear Cache"), false, () => FR2_Cache.Api.Clear());
#endif
        }

        public abstract    void OnSelectionChange();
        protected abstract void OnGUI();

#if UNITY_2018_OR_NEWER
        protected void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (IsFocusingFindInScene || IsFocusingSceneToAsset || IsFocusingSceneInScene)
            {
                OnSelectionChange();
            }
        }
#endif

        protected bool DrawEnable()
        {
            var api = FR2_Cache.Api;
            if (api == null) return false;

            var v = api.disabled;
            if (v)
            {
                EditorGUILayout.HelpBox("Find References 2 is disabled!", MessageType.Warning);

                if (GUILayout.Button("Enable"))
                {
                    api.disabled = !api.disabled;
                    this.Repaint();
                }

                return !api.disabled;
            }

            return !api.disabled;
        }
    }
}