#if UNITY_5_3_OR_NEWER
#endif

namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // filter, ignore anh huong ket qua thi hien mau do
    // optimize lag duplicate khi use
    public class FR2_WindowAll : FR2_WindowBase, IHasCustomMenu
    {
        [MenuItem("Window/Find Reference 2")]
        private static void ShowWindow()
        {
            var _window = CreateInstance<FR2_WindowAll>();
            _window.InitIfNeeded();
            FR2_Unity.SetWindowTitle(_window, "FR2");
            _window.Show();
        }

        [NonSerialized] internal FR2_Bookmark       bookmark;
        [NonSerialized] internal FR2_Selection      selection;
        [NonSerialized] internal FR2_UsedInBuild    UsedInBuild;
        [NonSerialized] internal FR2_DuplicateTree2 Duplicated;
        [NonSerialized] internal FR2_RefDrawer      RefUnUse;

        [NonSerialized] internal FR2_RefDrawer UsesDrawer; // [Selected Assets] are [USING] (depends on / contains reference to) ---> those assets
        [NonSerialized] internal FR2_RefDrawer UsedByDrawer; // [Selected Assets] are [USED BY] <---- those assets 
        [NonSerialized] internal FR2_RefDrawer SceneToAssetDrawer; // [Selected GameObjects in current Scene] are [USING] ---> those assets

        [NonSerialized] internal FR2_RefDrawer RefInScene; // [Selected Assets] are [USED BY] <---- those components in current Scene 
        [NonSerialized] internal FR2_RefDrawer SceneUsesDrawer; // [Selected GameObjects] are [USING] ---> those components / GameObjects in current scene
        [NonSerialized] internal FR2_RefDrawer RefSceneInScene; // [Selected GameObjects] are [USED BY] <---- those components / GameObjects in current scene


        internal int     level;
        private  Vector2 scrollPos;
        private  string  tempGUID;
        private  Object  tempObject;

        protected bool lockSelection => this.selection != null && this.selection.isLock;

        private void OnEnable() { this.Repaint(); }

        protected void InitIfNeeded()
        {
            if (this.UsesDrawer != null) return;

            this.UsesDrawer = new FR2_RefDrawer(this)
            {
                messageEmpty = "[Selected Assets] are not [USING] (depends on / contains reference to) any other assets!"
            };

            this.UsedByDrawer = new FR2_RefDrawer(this)
            {
                messageEmpty = "[Selected Assets] are not [USED BY] any other assets!"
            };

            this.Duplicated = new FR2_DuplicateTree2(this);
            this.SceneToAssetDrawer = new FR2_RefDrawer(this)
            {
                messageEmpty = "[Selected GameObjects] (in current open scenes) are not [USING] any assets!"
            };

            this.RefUnUse                                 = new FR2_RefDrawer(this);
            this.RefUnUse.groupDrawer.hideGroupIfPossible = true;

            this.UsedInBuild = new FR2_UsedInBuild(this);
            this.bookmark    = new FR2_Bookmark(this);
            this.selection   = new FR2_Selection(this);

            this.SceneUsesDrawer = new FR2_RefDrawer(this)
            {
                messageEmpty = "[Selected GameObjects] are not [USING] any other GameObjects in scenes"
            };

            this.RefInScene = new FR2_RefDrawer(this)
            {
                messageEmpty = "[Selected Assets] are not [USED BY] any GameObjects in opening scenes!"
            };

            this.RefSceneInScene = new FR2_RefDrawer(this)
            {
                messageEmpty = "[Selected GameObjects] are not [USED BY] by any GameObjects in opening scenes!"
            };

#if UNITY_2018_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
#elif UNITY_2017_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += OnSceneChanged;
#endif

            FR2_Cache.onReady -= this.OnReady;
            FR2_Cache.onReady += this.OnReady;

            FR2_Setting.OnIgnoreChange -= this.OnIgnoreChanged;
            FR2_Setting.OnIgnoreChange += this.OnIgnoreChanged;

            this.Repaint();
        }

#if UNITY_2018_OR_NEWER
        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (IsFocusingFindInScene || IsFocusingSceneToAsset || IsFocusingSceneInScene)
            {
                OnSelectionChange();
            }
        }
#endif
        protected void OnIgnoreChanged()
        {
            this.RefUnUse.ResetUnusedAsset();
            this.UsedInBuild.SetDirty();

            this.OnSelectionChange();
        }
        protected void OnCSVClick()
        {
            FR2_Ref[] csvSource = null;
            var       drawer    = this.GetAssetDrawer();

            if (drawer != null) csvSource = drawer.source;

            if (this.IsFocusingUnused && csvSource == null)
                csvSource = this.RefUnUse.source;
            //if (csvSource != null) Debug.Log("d : " + csvSource.Length);

            if (this.IsFocusingUsedInBuild && csvSource == null)
                csvSource = FR2_Ref.FromDict(this.UsedInBuild.refs);
            //if (csvSource != null) Debug.Log("e : " + csvSource.Length);

            if (this.IsFocusingDuplicate && csvSource == null)
                csvSource = FR2_Ref.FromList(this.Duplicated.list);
            //if (csvSource != null) Debug.Log("f : " + csvSource.Length);

            FR2_Export.ExportCSV(csvSource);
        }

        protected void OnReady() { this.OnSelectionChange(); }

        public override void OnSelectionChange()
        {
            this.Repaint();

            isNoticeIgnore = false;
            if (!FR2_Cache.isReady) return;

            if (focusedWindow == null) return;

            if (this.SceneUsesDrawer == null) this.InitIfNeeded();

            if (this.UsesDrawer == null) this.InitIfNeeded();

            if (!this.lockSelection)
            {
                this.ids = FR2_Unity.Selection_AssetGUIDs;
                this.selection.Clear();

                //ignore selection on asset when selected any object in scene
                if (Selection.gameObjects.Length > 0 && !FR2_Unity.IsInAsset(Selection.gameObjects[0]))
                {
                    this.ids = new string[0];
                    this.selection.AddRange(Selection.gameObjects);
                }
                else
                {
                    this.selection.AddRange(this.ids);
                }

                this.level = 0;

                if (this.selection.isSelectingAsset)
                {
                    this.UsesDrawer.Reset(this.ids, true);
                    this.UsedByDrawer.Reset(this.ids, false);
                    this.RefInScene.Reset(this.ids, this as IWindow);
                }
                else
                {
                    this.RefSceneInScene.ResetSceneInScene(Selection.gameObjects);
                    this.SceneToAssetDrawer.Reset(Selection.gameObjects, true, true);
                    this.SceneUsesDrawer.ResetSceneUseSceneObjects(Selection.gameObjects);
                }

                // auto disable enable scene / asset
                if (this.IsFocusingUses)
                {
                    this.sp2.splits[0].visible = !this.selection.isSelectingAsset;
                    this.sp2.splits[1].visible = true;
                    this.sp2.CalculateWeight();
                }

                if (this.IsFocusingUsedBy)
                {
                    this.sp2.splits[0].visible = true;
                    this.sp2.splits[1].visible = this.selection.isSelectingAsset;
                    this.sp2.CalculateWeight();
                }
            }

            if (this.IsFocusingGUIDs)
            {
                //objs = new Object[ids.Length];
                this.objs = new Dictionary<string, Object>();
                var objects = Selection.objects;
                for (var i = 0; i < objects.Length; i++)
                {
                    var item = objects[i];

#if UNITY_2018_1_OR_NEWER
                    {
                        var  guid   = "";
                        long fileid = -1;
                        try
                        {
                            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item, out guid, out fileid))
                                this.objs.Add(guid + "/" + fileid, objects[i]);
                            //Debug.Log("guid: " + guid + "  fileID: " + fileid);
                        }
                        catch
                        {
                        }
                    }
#else
					{
						var path = AssetDatabase.GetAssetPath(item);
                        if (string.IsNullOrEmpty(path)) continue;
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        System.Reflection.PropertyInfo inspectorModeInfo =
                        typeof(SerializedObject).GetProperty("inspectorMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        SerializedObject serializedObject = new SerializedObject(item);
                        inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                        SerializedProperty localIdProp =
                            serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                        var localId = localIdProp.longValue;
                        if (localId <= 0)
                        {
                            localId = localIdProp.intValue;
                        }
                        if (localId <= 0)
                        {
                            continue;
                        }
                        if (!string.IsNullOrEmpty(guid)) objs.Add(guid + "/" + localId, objects[i]);
					}
#endif
                }
            }

            if (this.IsFocusingUnused) this.RefUnUse.ResetUnusedAsset();

            if (FR2_SceneCache.Api.Dirty && !Application.isPlaying) FR2_SceneCache.Api.refreshCache(this);

            EditorApplication.delayCall -= this.Repaint;
            EditorApplication.delayCall += this.Repaint;
        }


        public FR2_SplitView sp1; // container : Selection / sp2 / Bookmark 
        public FR2_SplitView sp2; // Scene / Assets

        private void InitPanes()
        {
            this.sp2 = new FR2_SplitView(this)
            {
                isHorz = false,
                splits = new List<FR2_SplitView.Info>
                {
                    new FR2_SplitView.Info { title = new GUIContent("Scene", FR2_Icon.Scene.image), draw  = this.DrawScene },
                    new FR2_SplitView.Info { title = new GUIContent("Assets", FR2_Icon.Asset.image), draw = this.DrawAsset }
                }
            };

            this.sp2.CalculateWeight();

            this.sp1 = new FR2_SplitView(this)
            {
                isHorz = true,
                splits = new List<FR2_SplitView.Info>
                {
                    new FR2_SplitView.Info { title = new GUIContent("Selection", FR2_Icon.Selection.image), weight = 0.4f, visible = false, draw = rect => this.selection.Draw(rect) },
                    new FR2_SplitView.Info
                    {
                        draw = r =>
                        {
                            if (this.IsFocusingUses || this.IsFocusingUsedBy)
                                this.sp2.Draw(r);
                            else
                                this.DrawTools(r);
                        }
                    },
                    new FR2_SplitView.Info
                    {
                        title = new GUIContent("Details", FR2_Icon.Details.image), weight = 0.4f, visible = true, draw = rect =>
                        {
                            var assetDrawer = this.GetAssetDrawer();
                            if (assetDrawer != null) assetDrawer.DrawDetails(rect);
                        }
                    },
                    new FR2_SplitView.Info { title = new GUIContent("Bookmark", FR2_Icon.Favorite.image), weight = 0.4f, visible = false, draw = rect => this.bookmark.Draw(rect) }
                }
            };

            this.sp1.CalculateWeight();
        }

        private FR2_TabView    tabs;
        private FR2_TabView    bottomTabs;
        private FR2_SearchView search;

        private void DrawScene(Rect rect)
        {
            var drawer = this.IsFocusingUses
                ? this.selection.isSelectingAsset ? null : this.SceneUsesDrawer
                : this.selection.isSelectingAsset
                    ? this.RefInScene
                    : this.RefSceneInScene;
            if (drawer == null) return;

            if (!FR2_SceneCache.ready)
            {
                var rr = rect;
                rr.height = 16f;

                int cur = FR2_SceneCache.Api.current, total = FR2_SceneCache.Api.total;
                EditorGUI.ProgressBar(rr, cur * 1f / total, string.Format("{0} / {1}", cur, total));
                this.WillRepaint = true;
                return;
            }

            drawer.Draw(rect);

            var refreshRect = new Rect(rect.xMax - 16f, rect.yMin - 14f, 18f, 18f);
            if (GUI2.ColorIconButton(refreshRect, FR2_Icon.Refresh.image,
                FR2_SceneCache.Api.Dirty ? (Color?)GUI2.lightRed : null))
                FR2_SceneCache.Api.refreshCache(drawer.window);
        }


        private FR2_RefDrawer GetAssetDrawer()
        {
            if (this.IsFocusingUses) return this.selection.isSelectingAsset ? this.UsesDrawer : this.SceneToAssetDrawer;

            if (this.IsFocusingUsedBy) return this.selection.isSelectingAsset ? this.UsedByDrawer : null;

            return null;
        }

        private void DrawAsset(Rect rect)
        {
            var drawer = this.GetAssetDrawer();
            if (drawer != null) drawer.Draw(rect);
        }

        private void DrawSearch()
        {
            if (this.search == null) this.search = new FR2_SearchView();
            this.search.DrawLayout();
        }

        protected override void OnGUI() { this.OnGUI2(); }

        protected bool CheckDrawImport()
        {
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Compiling scripts, please wait!", MessageType.Warning);
                this.Repaint();
                return false;
            }

            if (EditorApplication.isUpdating)
            {
                EditorGUILayout.HelpBox("Importing assets, please wait!", MessageType.Warning);
                this.Repaint();
                return false;
            }

            this.InitIfNeeded();

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorGUILayout.HelpBox("FR2 requires serialization mode set to FORCE TEXT!", MessageType.Warning);
                if (GUILayout.Button("FORCE TEXT")) EditorSettings.serializationMode = SerializationMode.ForceText;

                return false;
            }

            if (FR2_Cache.hasCache && !FR2_Cache.CheckSameVersion())
            {
                EditorGUILayout.HelpBox(
                    "Incompatible cache version found!!!\nFR2 will need a full refresh and this may take quite some time to finish but you would be able to work normally while the scan works in background!",
                    MessageType.Warning);
                FR2_Cache.DrawPriorityGUI();
                if (GUILayout.Button("Scan project"))
                {
                    FR2_Cache.DeleteCache();
                    FR2_Cache.CreateCache();
                }

                return false;
            }

            if (!FR2_Cache.isReady)
            {
                if (!FR2_Cache.hasCache)
                {
                    EditorGUILayout.HelpBox(
                        "FR2 cache not found!\nFirst scan may takes quite some time to finish but you would be able to work normally while the scan works in background...",
                        MessageType.Warning);

                    FR2_Cache.DrawPriorityGUI();

                    if (GUILayout.Button("Scan project"))
                    {
                        FR2_Cache.CreateCache();
                        this.Repaint();
                    }

                    return false;
                }

                FR2_Cache.DrawPriorityGUI();

                if (!this.DrawEnable()) return false;

                var api  = FR2_Cache.Api;
                var text = "Refreshing ... " + (int)(api.progress * api.workCount) + " / " + api.workCount;
                var rect = GUILayoutUtility.GetRect(1f, Screen.width, 18f, 18f);
                EditorGUI.ProgressBar(rect, api.progress, text);
                this.Repaint();
                return false;
            }

            if (!this.DrawEnable()) return false;

            return true;
        }

        protected bool IsFocusingUses        => this.tabs != null && this.tabs.current == 0;
        protected bool IsFocusingUsedBy      => this.tabs != null && this.tabs.current == 1;
        protected bool IsFocusingDuplicate   => this.tabs != null && this.tabs.current == 2;
        protected bool IsFocusingGUIDs       => this.tabs != null && this.tabs.current == 3;
        protected bool IsFocusingUnused      => this.tabs != null && this.tabs.current == 4;
        protected bool IsFocusingUsedInBuild => this.tabs != null && this.tabs.current == 5;

        private void OnTabChange()
        {
            if (this.deleteUnused != null) this.deleteUnused.hasConfirm = false;
            if (this.UsedInBuild != null) this.UsedInBuild.SetDirty();
        }

        private void InitTabs()
        {
            this.tabs = FR2_TabView.Create(this, false,
                "Uses", "Used By", "Duplicate", "GUIDs", "Unused Assets", "Uses in Build"
            );
            this.tabs.onTabChange = this.OnTabChange;
            this.tabs.callback = new DrawCallback
            {
                BeforeDraw = () =>
                {
                    if (GUI2.ToolbarToggle(ref this.selection.isLock, this.selection.isLock ? FR2_Icon.Lock.image : FR2_Icon.Unlock.image,
                        new Vector2(-1, 2), "Lock Selection"))
                        this.WillRepaint = true;
                },

                AfterDraw = () =>
                {
                    //GUILayout.Space(16f);

                    if (GUI2.ToolbarToggle(ref this.sp1.isHorz, FR2_Icon.Panel.image, Vector2.zero, "Layout"))
                    {
                        this.sp1.CalculateWeight();
                        this.Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref this.sp1.splits[0].visible, FR2_Icon.Selection.image, Vector2.zero, "Show / Hide Selection"))
                    {
                        this.sp1.CalculateWeight();
                        this.Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref this.sp2.splits[0].visible, FR2_Icon.Scene.image, Vector2.zero, "Show / Hide Scene References"))
                    {
                        this.sp2.CalculateWeight();
                        this.Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref this.sp2.splits[1].visible, FR2_Icon.Asset.image, Vector2.zero, "Show / Hide Asset References"))
                    {
                        this.sp2.CalculateWeight();
                        this.Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref this.sp1.splits[2].visible, FR2_Icon.Details.image, Vector2.zero, "Show / Hide Details"))
                    {
                        this.sp1.CalculateWeight();
                        this.Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref this.sp1.splits[3].visible, FR2_Icon.Favorite.image, Vector2.zero, "Show / Hide Bookmarks"))
                    {
                        this.sp1.CalculateWeight();
                        this.Repaint();
                    }
                }
            };
        }

        protected bool DrawHeader()
        {
            if (this.tabs == null) this.InitTabs();
            if (this.bottomTabs == null)
            {
                this.bottomTabs = FR2_TabView.Create(this, true,
                    new GUIContent(FR2_Icon.Setting.image, "Settings"),
                    new GUIContent(FR2_Icon.Ignore.image, "Ignore"),
                    new GUIContent(FR2_Icon.Filter.image, "Filter by Type")
                );
                this.bottomTabs.current = -1;
            }

            this.tabs.DrawLayout();

            return true;
        }


        protected bool DrawFooter()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                this.bottomTabs.DrawLayout();
                GUILayout.FlexibleSpace();
                this.DrawAssetViewSettings();
                GUILayout.FlexibleSpace();
                this.DrawViewModes();
            }
            GUILayout.EndHorizontal();
            return false;
        }

        private void DrawAssetViewSettings()
        {
            var isDisable = !this.sp2.splits[1].visible;
            EditorGUI.BeginDisabledGroup(isDisable);
            {
                GUI2.ToolbarToggle(ref FR2_Setting.s.displayAssetBundleName, FR2_Icon.AssetBundle.image, Vector2.zero, "Show / Hide Assetbundle Names");
#if UNITY_2017_1_OR_NEWER
                GUI2.ToolbarToggle(ref FR2_Setting.s.displayAtlasName, FR2_Icon.Atlas.image, Vector2.zero, "Show / Hide Atlas packing tags");
#endif
                GUI2.ToolbarToggle(ref FR2_Setting.s.showUsedByClassed, FR2_Icon.Material.image, Vector2.zero, "Show / Hide usage icons");
                GUI2.ToolbarToggle(ref FR2_Setting.s.displayFileSize, FR2_Icon.Filesize.image, Vector2.zero, "Show / Hide file size");

                if (GUILayout.Button("CSV", EditorStyles.toolbarButton)) this.OnCSVClick();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawViewModes()
        {
            var gMode = FR2_Setting.GroupMode;
            if (GUI2.EnumPopup(ref gMode, new GUIContent(FR2_Icon.Group.image, "Group by"), EditorStyles.toolbarPopup, GUILayout.Width(80f)))
            {
                FR2_Setting.GroupMode = gMode;
                this.markDirty();
            }

            GUILayout.Space(16f);

            var sMode = FR2_Setting.SortMode;
            if (GUI2.EnumPopup(ref sMode, new GUIContent(FR2_Icon.Sort.image, "Sort by"), EditorStyles.toolbarPopup, GUILayout.Width(50f)))
            {
                FR2_Setting.SortMode = sMode;
                this.RefreshSort();
            }
        }

        protected void OnGUI2()
        {
            if (!this.CheckDrawImport()) return;

            if (this.sp1 == null) this.InitPanes();

            this.DrawHeader();
            this.sp1.DrawLayout();
            this.DrawSettings();
            this.DrawFooter();

            if (this.WillRepaint) this.Repaint();
        }


        private FR2_DeleteButton deleteUnused;


        private void DrawTools(Rect rect)
        {
            if (this.IsFocusingDuplicate)
            {
                rect = GUI2.Padding(rect, 2f, 2f);

                GUILayout.BeginArea(rect);
                this.Duplicated.DrawLayout();
                GUILayout.EndArea();
                return;
            }

            if (this.IsFocusingUnused)
            {
                rect = GUI2.Padding(rect, 2f, 2f);

                if (this.RefUnUse.refs != null && this.RefUnUse.refs.Count == 0)
                {
                    GUILayout.BeginArea(rect);
                    {
                        EditorGUILayout.HelpBox("Wow! So clean!?", MessageType.Info);
                        EditorGUILayout.HelpBox("Your project does not has have any unused assets, or have you just hit DELETE ALL?", MessageType.Info);
                        EditorGUILayout.HelpBox("Your backups are placed at Library/FR2/ just in case you want your assets back!", MessageType.Info);
                    }
                    GUILayout.EndArea();
                }
                else
                {
                    rect.yMax -= 40f;
                    GUILayout.BeginArea(rect);
                    this.RefUnUse.DrawLayout();
                    GUILayout.EndArea();

                    var toolRect = rect;
                    toolRect.yMin = toolRect.yMax;

                    var lineRect = toolRect;
                    lineRect.height = 1f;

                    GUI2.Rect(lineRect, Color.black, 0.5f);

                    toolRect.xMin   += 2f;
                    toolRect.xMax   -= 2f;
                    toolRect.height =  40f;

                    if (this.deleteUnused == null)
                        this.deleteUnused = new FR2_DeleteButton
                        {
                            warningMessage = "It's absolutely safe to delete them all!\nA backup (.unitypackage) will be created so you can import it back later!",
                            deleteLabel    = new GUIContent("DELETE ASSETS", FR2_Icon.Delete.image),
                            confirmMessage = "Create backup at Library/FR2/"
                        };

                    GUILayout.BeginArea(toolRect);
                    this.deleteUnused.Draw(() => { FR2_Unity.BackupAndDeleteAssets(this.RefUnUse.source); });
                    GUILayout.EndArea();
                }

                return;
            }

            if (this.IsFocusingUsedInBuild)
            {
                this.UsedInBuild.Draw(rect);
                return;
            }

            if (this.IsFocusingGUIDs)
            {
                rect = GUI2.Padding(rect, 2f, 2f);

                GUILayout.BeginArea(rect);
                this.DrawGUIDs();
                GUILayout.EndArea();
            }
        }

        private void DrawSettings()
        {
            if (this.bottomTabs.current == -1) return;

            GUILayout.BeginVertical(GUILayout.Height(100f));
            {
                GUILayout.Space(2f);
                switch (this.bottomTabs.current)
                {
                    case 0:
                    {
                        FR2_Setting.s.DrawSettings();
                        break;
                    }

                    case 1:
                    {
                        if (AssetType.DrawIgnoreFolder()) this.markDirty();
                        break;
                    }

                    case 2:
                    {
                        if (AssetType.DrawSearchFilter()) this.markDirty();
                        break;
                    }
                }
            }
            GUILayout.EndVertical();

            var rect = GUILayoutUtility.GetLastRect();
            rect.height = 1f;
            GUI2.Rect(rect, Color.black, 0.4f);
        }

        protected void markDirty()
        {
            this.UsedByDrawer.SetDirty();
            this.UsesDrawer.SetDirty();
            this.Duplicated.SetDirty();
            this.SceneToAssetDrawer.SetDirty();
            this.RefUnUse.SetDirty();

            this.RefInScene.SetDirty();
            this.RefSceneInScene.SetDirty();
            this.SceneUsesDrawer.SetDirty();
            this.UsedInBuild.SetDirty();
            this.WillRepaint = true;
        }

        protected void RefreshSort()
        {
            this.UsedByDrawer.RefreshSort();
            this.UsesDrawer.RefreshSort();
            this.Duplicated.RefreshSort();
            this.SceneToAssetDrawer.RefreshSort();
            this.RefUnUse.RefreshSort();

            this.UsedInBuild.RefreshSort();
        }
        // public bool isExcludeByFilter;

        protected bool checkNoticeFilter()
        {
            var rsl = false;

            if (this.IsFocusingUsedBy && !rsl) rsl = this.UsedByDrawer.isExclueAnyItem();

            if (this.IsFocusingDuplicate) return this.Duplicated.isExclueAnyItem();

            if (this.IsFocusingUses && rsl == false) rsl = this.UsesDrawer.isExclueAnyItem();

            //tab use by
            return rsl;
        }

        protected bool checkNoticeIgnore()
        {
            var rsl = isNoticeIgnore;
            return rsl;
        }


        private Dictionary<string, Object> objs;
        private string[]                   ids;

        private void DrawGUIDs()
        {
            GUILayout.Label("GUID to Object", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            {
                var guid = EditorGUILayout.TextField(this.tempGUID ?? string.Empty);
                EditorGUILayout.ObjectField(this.tempObject, typeof(Object), false, GUILayout.Width(120f));

                if (GUILayout.Button("Paste", EditorStyles.miniButton, GUILayout.Width(70f))) guid = EditorGUIUtility.systemCopyBuffer;

                if (guid != this.tempGUID && !string.IsNullOrEmpty(guid))
                {
                    this.tempGUID = guid;

                    this.tempObject = FR2_Unity.LoadAssetAtPath<Object>
                    (
                        AssetDatabase.GUIDToAssetPath(this.tempGUID)
                    );
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            if (this.objs == null) // || ids == null)
                return;

            //GUILayout.Label("Selection", EditorStyles.boldLabel);
            //if (ids.Length == objs.Count)
            {
                this.scrollPos = GUILayout.BeginScrollView(this.scrollPos);
                {
                    //for (var i = 0; i < ids.Length; i++)
                    foreach (var item in this.objs)
                    {
                        //if (!objs.ContainsKey(ids[i])) continue;

                        GUILayout.BeginHorizontal();
                        {
                            //var obj = objs[ids[i]];
                            var obj = item.Value;

                            EditorGUILayout.ObjectField(obj, typeof(Object), false, GUILayout.Width(150));
                            var idi = item.Key;
                            GUILayout.TextField(idi, GUILayout.Width(240f));
                            if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(50f)))
                            {
                                this.tempObject = obj;
                                //EditorGUIUtility.systemCopyBuffer = tempGUID = item.Key;
                                this.tempGUID = item.Key;

                                //string guid = "";
                                //long file = -1;
                                //if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out file))
                                //{
                                //    EditorGUIUtility.systemCopyBuffer = tempGUID = idi + "/" + file;

                                //    if (!string.IsNullOrEmpty(tempGUID))
                                //    {
                                //        tempObject = obj;
                                //    }
                                //}  
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Merge Selection To")) FR2_Export.MergeDuplicate(this.tempGUID);

            EditorGUILayout.ObjectField(this.tempObject, typeof(Object), false, GUILayout.Width(120f));
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }
    }
}