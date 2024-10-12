using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.Editor.OSAWizard
{
    public abstract class BaseOSAWindow<TWindowParams> : EditorWindow where TWindowParams : BaseWindowParams
    {
        [SerializeField] protected TWindowParams _WindowParams = null;

        [NonSerialized] protected int       _CurrentFrameInSlowUpdateCycle;
        [NonSerialized] protected GUIStyle  _RootGUIStyle, _BoxGUIStyle;
        [NonSerialized] private   bool      _GUIResourcesInitialized;
        [NonSerialized] private   Texture2D _Icon;
        [NonSerialized] private   Texture2D _TopToBottomGradient;

        private string EDITORONLY_TEXTURES_PATH => OSAEditorConst.OSA_PATH_IN_PROJECT + "/Core/Editor/Textures";

        private Color _MainColor = new(219 / 255f, 195 / 255f, 166 / 255f, .2f);

        protected virtual string CompilingScriptsText => "Compiling scripts...";

        protected static bool BaseValidate(out string reasonIfNotValid)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reasonIfNotValid = "OSA wizard closed: Cannot be used in play mode";
                return false;
            }

            reasonIfNotValid = null;
            return true;
        }

        #region Unity methods

        protected void OnEnable()
        {
            if (this._WindowParams != null) // most probably, after a script re-compilation
                this.InitWithExistingParams();
        }

        protected void OnDisable()
        {
            this.ReleaseOnGUIResources();
        }

        protected void Update()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Not allowed in play mode
                CWiz.ShowNotification("Cannot use OSA wizard during play mode", false, false);
                this.Close();
                return;
            }

            this.UpdateImplWithoutChecks();

            // Wait for scripts recompilation
            if (EditorApplication.isCompiling) return;

            // It's ok to delay the starting of updates until the gui resources are initialized, in order to have averything prepared
            if (this._GUIResourcesInitialized) this.UpdateImpl();

            // SlowUpdate calling
            if (this._CurrentFrameInSlowUpdateCycle % CWiz.SLOW_UPDATE_SKIPPED_FRAMES == 0)
            {
                this._CurrentFrameInSlowUpdateCycle = 0;
                this.SlowUpdate();
            }
            else
                ++this._CurrentFrameInSlowUpdateCycle;
        }

        protected void OnGUI()
        {
            if (!this._GUIResourcesInitialized)
            {
                this.InitOnGUIResources();
                this._GUIResourcesInitialized = true;
            }

            var prevColor = GUI.color;
            GUI.color = this._MainColor;
            var r = this.position;
            r.position = Vector2.zero;
            GUI.DrawTexture(r, EditorGUIUtility.whiteTexture);
            GUI.color = prevColor;

            this.DrawIcon();

            // Wait for scripts recompilation
            if (EditorApplication.isCompiling)
            {
                var style = new GUIStyle();
                style.alignment        = TextAnchor.MiddleCenter;
                style.normal           = new();
                style.normal.textColor = Color.gray;
                EditorGUILayout.LabelField(this.CompilingScriptsText, style, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                return;
            }

            EditorGUILayout.BeginVertical(this._RootGUIStyle);
            {
                this.OnGUIImpl();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        protected virtual void InitWithNewParams(TWindowParams windowParams)
        {
            //title = GetType().Name.Replace("OSAWindow", " OSA");
            var titleString = "OSA Wizard";
            #if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4_OR_NEWER
            this.titleContent = new(titleString);
            #else
			title = titleString;
            #endif
            this.minSize       = windowParams.MinSize;
            this._WindowParams = windowParams;
        }

        protected virtual void InitWithExistingParams()
        {
        }

        protected virtual void InitOnGUIResources()
        {
            this._Icon                = AssetDatabase.LoadAssetAtPath(this.EDITORONLY_TEXTURES_PATH + "/osa-icon.png", typeof(Texture2D)) as Texture2D;
            this._TopToBottomGradient = AssetDatabase.LoadAssetAtPath(this.EDITORONLY_TEXTURES_PATH + "/gradient.png", typeof(Texture2D)) as Texture2D;

            this._RootGUIStyle           = new();
            this._RootGUIStyle.padding   = new(20, 20, 15, 25);
            this._RootGUIStyle.alignment = TextAnchor.UpperCenter;

            this._BoxGUIStyle                   = new(EditorStyles.textArea);
            this._BoxGUIStyle.normal            = new();
            this._BoxGUIStyle.normal.background = this._TopToBottomGradient;
            this._BoxGUIStyle.padding           = new(5, 5, 5, 5);
        }

        protected virtual void ReleaseOnGUIResources()
        {
            if (this._Icon)
            {
                Resources.UnloadAsset(this._Icon);
                this._Icon = null;
            }
            if (this._TopToBottomGradient)
            {
                Resources.UnloadAsset(this._TopToBottomGradient);
                this._TopToBottomGradient = null;
            }
        }

        protected virtual void UpdateImplWithoutChecks()
        {
        }

        protected virtual void UpdateImpl()
        {
        }

        protected abstract void OnGUIImpl();

        protected virtual void ConfigureScrollView(ScrollRect scrollRect, RectTransform viewport, params Transform[] objectsToSkipDisabling)
        {
            scrollRect.horizontal        = this._WindowParams.isHorizontal;
            scrollRect.vertical          = !scrollRect.horizontal;
            scrollRect.verticalScrollbar = scrollRect.horizontalScrollbar = null;

            if (!this._WindowParams.checkForMiscComponents) return;

            this.DisableOrNotifyAboutMiscComponents(scrollRect.gameObject, "ScrollRect", typeof(ScrollRect));
            foreach (Transform child in scrollRect.transform)
            {
                if (child.name == "Viewport") continue;
                if (child.GetComponent<ScrollbarFixer8>()) continue;

                if (child.gameObject.activeSelf)
                {
                    if (Array.IndexOf(objectsToSkipDisabling, child) != -1) continue;

                    var scrollbarFixer = typeof(ScrollbarFixer8).Name;
                    var isScrollbar    = child.name.ToLower().Contains("scrollbar") && child.GetComponent<Scrollbar>();
                    var suffix = !isScrollbar
                        ? "You can activate it back if it doesn't interfere with OSA"
                        : "This appears to be a Scrollbar, but it wasn't added by the OSA wizard. If you want to use it, activate it back "
                        + (child.GetComponent<ScrollbarFixer8>() != null
                            ? " and make sure its " + scrollbarFixer + " component is properly configured in inspector"
                            : ", add a " + scrollbarFixer + " component and make sure it's properly configured in inspector"
                        );
                    Debug.Log("OSA: De-activating ScrollRect's unknown child '" + child.name + "'. " + suffix);
                    child.gameObject.SetActive(false);
                }
            }

            this.DisableOrNotifyAboutMiscComponents(viewport.gameObject, "Viewport", typeof(Mask));
            foreach (Transform child in viewport.transform)
            {
                if (child == scrollRect.content) continue;

                if (child.gameObject.activeSelf)
                {
                    if (Array.IndexOf(objectsToSkipDisabling, child) != -1) continue;

                    Debug.Log("OSA: De-activating Viewport's unknown child '" + child.name + "'. You can activate it back if it doesn't interfere with OSA");
                    child.gameObject.SetActive(false);
                }
            }

            this.DisableOrNotifyAboutMiscComponents(scrollRect.content.gameObject, "Content");
            foreach (Transform child in scrollRect.content)
                if (child.gameObject.activeSelf)
                {
                    if (Array.IndexOf(objectsToSkipDisabling, child) != -1) continue;

                    Debug.Log("OSA: De-activating Content's unknown child '" + child.name + "'. You can activate it back if it doesn't interfere with OSA");
                    child.gameObject.SetActive(false);
                }
        }

        protected abstract void GetErrorAndWarning(out string error, out string warning);

        protected virtual void OnSubmitClicked()
        {
        }

        protected void SlowUpdate()
        {
            //if (FullyInitialized)
            this.Repaint();
        }

        protected void DrawIcon()
        {
            var iconSize          = 55f;
            var padding_icon      = 10f;
            var padding_text      = 0f;
            var r                 = new Rect();
            var labelHeight       = 15f;
            r.width    = r.height = iconSize;
            r.position = new(this.position.width - iconSize - padding_icon, this.position.height - iconSize - labelHeight - padding_icon);
            var prevColor = GUI.color;
            var newColor  = Color.white;
            newColor.a = .6f;
            GUI.color  = newColor;
            GUI.DrawTexture(r, this._Icon);
            r.position = new Vector3(r.position.x - padding_text / 2f, r.position.y + r.height + 3f);
            r.height   = labelHeight;
            var style = new GUIStyle();
            style.fontSize         = 9;
            style.fontStyle        = FontStyle.Bold;
            style.normal           = new();
            style.normal.textColor = Color.white;
            newColor.a             = .95f;
            GUI.color              = newColor;
            GUI.Label(r, "OSA v" + OSAConst.OSA_VERSION_STRING, style);
            GUI.color = prevColor;
        }

        protected void DrawSectionTitle(string title)
        {
            var titleStyle = new GUIStyle(EditorStyles.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize  = 20;
            EditorGUILayout.LabelField(title, titleStyle, GUILayout.Height(25f));

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        protected void DrawObjectWithPath<T>(GUIStyle style, string title, T objectToDraw) where T : UnityEngine.Object
        {
            EditorGUILayout.BeginVertical(style);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel, CWiz.LABEL_WIDTH);

                    if (objectToDraw)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(objectToDraw, typeof(T), true, CWiz.VALUE_WIDTH);
                        EditorGUILayout.LabelField("(ReadOnly)");
                        EditorGUI.EndDisabledGroup();
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (objectToDraw)
                {
                    var s = "";
                    s = objectToDraw.name;
                    var asGO                 = objectToDraw as GameObject;
                    var asComponent          = objectToDraw as Component;
                    var tr                   = asGO == null ? asComponent.transform : asGO.transform;
                    while (tr = tr.parent) s = tr.name + "/" + s;
                    s = "Path: " + s;

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextArea(s, EditorStyles.label);
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        protected void DrawSubmitButon(string title)
        {
            var buttonCRect = new Rect();
            buttonCRect.width  = 200f;
            buttonCRect.height = 30f;
            buttonCRect.x      = (this.position.width - buttonCRect.width) / 2;
            buttonCRect.y      = this.position.height - buttonCRect.height - this._RootGUIStyle.padding.bottom;
            string error, warning;
            this.GetErrorAndWarning(out error, out warning);
            var isError = !string.IsNullOrEmpty(error);
            if (isError) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (!string.IsNullOrEmpty(warning)) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            EditorGUI.BeginDisabledGroup(isError);
            {
                if (GUI.Button(buttonCRect, title)) this.OnSubmitClicked();
            }
            EditorGUI.EndDisabledGroup();
        }

        protected RectTransform CreateRTAndSetParent(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.transform as RectTransform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            return rt;
        }

        protected void DisableOrNotifyAboutMiscComponents(GameObject go, string nameToUse, params Type[] typesToIgnore)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp is Transform) continue;
                if (comp is CanvasRenderer) continue;
                if (comp is Image) continue;
                if (Array.Exists(typesToIgnore, t => OSAUtil.DotNETCoreCompat_IsAssignableFrom(t, comp.GetType()))) continue;
                var mb = comp as MonoBehaviour;
                if (mb)
                {
                    if (mb.enabled)
                    {
                        Debug.Log("OSA: Disabling unknown component " + mb.GetType().Name + " on " + nameToUse + ". You can enable it back if it doesn't interfere with OSA");
                        mb.enabled = false;
                    }
                    continue;
                }

                Debug.Log("OSA: Found unknown component '" + comp.GetType().Name + "' on " + nameToUse + ". Make sure it doesn't interfere with OSA");
            }
        }
    }

    [Serializable]
    public class BaseWindowParams
    {
        public bool isHorizontal;
        public bool checkForMiscComponents;

        public virtual Vector2 MinSize => new(480f, 300f);

        public int Hor0_Vert1 => this.isHorizontal ? 0 : 1;

        public virtual void ResetValues()
        {
            this.isHorizontal           = false;
            this.checkForMiscComponents = true;
        }
    }
}