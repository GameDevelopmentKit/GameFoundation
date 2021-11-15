namespace vietlabs.fr2
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class AssetType
    {
        // ------------------------------- STATIC -----------------------------

        internal static readonly AssetType[] FILTERS =
        {
            new AssetType("Scene", ".unity"),
            new AssetType("Prefab", ".prefab"),
            new AssetType("Model", ".3df", ".3dm", ".3dmf", ".3dv", ".3dx", ".c5d", ".lwo", ".lws", ".ma", ".mb",
                ".mesh", ".vrl", ".wrl", ".wrz", ".fbx", ".dae", ".3ds", ".dxf", ".obj", ".skp", ".max", ".blend"),
            new AssetType("Material", ".mat", ".cubemap", ".physicsmaterial"),
            new AssetType("Texture", ".ai", ".apng", ".png", ".bmp", ".cdr", ".dib", ".eps", ".exif", ".ico", ".icon",
                ".j", ".j2c", ".j2k", ".jas", ".jiff", ".jng", ".jp2", ".jpc", ".jpe", ".jpeg", ".jpf", ".jpg", "jpw",
                "jpx", "jtf", ".mac", ".omf", ".qif", ".qti", "qtif", ".tex", ".tfw", ".tga", ".tif", ".tiff", ".wmf",
                ".psd", ".exr", ".rendertexture"),
            new AssetType("Video", ".asf", ".asx", ".avi", ".dat", ".divx", ".dvx", ".mlv", ".m2l", ".m2t", ".m2ts",
                ".m2v", ".m4e", ".m4v", "mjp", ".mov", ".movie", ".mp21", ".mp4", ".mpe", ".mpeg", ".mpg", ".mpv2",
                ".ogm", ".qt", ".rm", ".rmvb", ".wmv", ".xvid", ".flv"),
            new AssetType("Audio", ".mp3", ".wav", ".ogg", ".aif", ".aiff", ".mod", ".it", ".s3m", ".xm"),
            new AssetType("Script", ".cs", ".js", ".boo", ".h"),
            new AssetType("Text", ".txt", ".json", ".xml", ".bytes", ".sql"),
            new AssetType("Shader", ".shader", ".cginc"),
            new AssetType("Animation", ".anim", ".controller", ".overridecontroller", ".mask"),
            new AssetType("Unity Asset", ".asset", ".guiskin", ".flare", ".fontsettings", ".prefs"),
            new AssetType("Others") //
        };

        private static FR2_Ignore      _ignore;
        public         HashSet<string> extension;
        public         string          name;

        public AssetType(string name, params string[] exts)
        {
            this.name      = name;
            this.extension = new HashSet<string>();
            for (var i = 0; i < exts.Length; i++) this.extension.Add(exts[i]);
        }

        private static FR2_Ignore ignore
        {
            get
            {
                if (_ignore == null) _ignore = new FR2_Ignore();

                return _ignore;
            }
        }

        public static int GetIndex(string ext)
        {
            for (var i = 0; i < FILTERS.Length - 1; i++)
                if (FILTERS[i].extension.Contains(ext))
                    return i;

            return FILTERS.Length - 1; //Others
        }

        public static bool DrawSearchFilter()
        {
            var n      = FILTERS.Length;
            var nCols  = 4;
            var nRows  = Mathf.CeilToInt(n / (float)nCols);
            var result = false;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("All", EditorStyles.toolbarButton) && !FR2_Setting.IsIncludeAllType())
                {
                    FR2_Setting.IncludeAllType();
                    result = true;
                }

                if (GUILayout.Button("None", EditorStyles.toolbarButton) && FR2_Setting.GetExcludeType() != -1)
                {
                    FR2_Setting.ExcludeAllType();
                    result = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (var i = 0; i < nCols; i++)
            {
                GUILayout.BeginVertical();
                for (var j = 0; j < nRows; j++)
                {
                    var idx = i * nCols + j;
                    if (idx >= n) break;

                    var s  = !FR2_Setting.IsTypeExcluded(idx);
                    var s1 = GUILayout.Toggle(s, FILTERS[idx].name);
                    if (s1 != s)
                    {
                        result = true;
                        FR2_Setting.ToggleTypeExclude(idx);
                    }
                }

                GUILayout.EndVertical();
                if ((i + 1) * nCols >= n) break;
            }

            GUILayout.EndHorizontal();

            return result;
        }

        public static void SetDirtyIgnore() { ignore.SetDirty(); }

        public static bool DrawIgnoreFolder()
        {
            var change = false;
            ignore.Draw();


            // FR2_Helper.GuiLine();
            // List<string> lst = FR2_Setting.IgnoreFolder.ToList();
            // bool change = false;
            // pos = EditorGUILayout.BeginScrollView(pos);
            // for(int i =0; i < lst.Count; i++)
            // {
            // 	GUILayout.BeginHorizontal();
            // 	{
            // 		if(GUILayout.Button("X", GUILayout.Width(30)))
            // 		 {
            // 			 change = true;
            // 			 FR2_Setting.RemoveIgnore(lst[i]);
            // 		 }
            // 		 GUILayout.Label(lst[i]);
            // 	}GUILayout.EndHorizontal();
            // }
            // EditorGUILayout.EndScrollView();
            return change;
        }

        private class FR2_Ignore
        {
            public readonly FR2_TreeUI2.GroupDrawer     groupIgnore;
            private         bool                        dirty;
            private         Dictionary<string, FR2_Ref> refs;

            public FR2_Ignore()
            {
                this.groupIgnore                     = new FR2_TreeUI2.GroupDrawer(this.DrawGroup, this.DrawItem);
                this.groupIgnore.hideGroupIfPossible = false;

                this.ApplyFiter();
            }

            private void DrawItem(Rect r, string guid)
            {
                FR2_Ref rf;
                if (!this.refs.TryGetValue(guid, out rf)) return;

                if (rf.depth == 1) //mode != Mode.Dependency && 
                {
                    var c = GUI.color;
                    GUI.color = Color.blue;
                    GUI.DrawTexture(new Rect(r.x - 4f, r.y + 2f, 2f, 2f), EditorGUIUtility.whiteTexture);
                    GUI.color = c;
                }

                rf.asset.Draw(r, false,
                    true,
                    false, false, false, false);

                var drawR = r;
                drawR.x      =  drawR.x + drawR.width - 50f; // (groupDrawer.TreeNoScroll() ? 60f : 70f) ;
                drawR.width  =  30;
                drawR.y      += 1;
                drawR.height -= 2;

                if (GUI.Button(drawR, "X", EditorStyles.miniButton)) FR2_Setting.RemoveIgnore(rf.asset.assetPath);
            }

            private void DrawGroup(Rect r, string id, int childCound)
            {
                GUI.Label(r, id, EditorStyles.boldLabel);
                if (childCound <= 1) return;

                var drawR = r;
                drawR.x      =  drawR.x + drawR.width - 50f; // (groupDrawer.TreeNoScroll() ? 60f : 70f) ;
                drawR.width  =  30;
                drawR.y      += 1;
                drawR.height -= 2;
            }

            public void SetDirty() { this.dirty = true; }
            //private float sizeRatio {
            //    get{
            //        if(FR2_Window.window != null)
            //            return FR2_Window.window.sizeRatio;
            //        return .3f;
            //    }
            //}

            public void Draw()
            {
                if (this.dirty) this.ApplyFiter();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(4f);
                    var drops = GUI2.DropZone("Drag & Drop folders here to exclude", 100, 95);
                    if (drops != null && drops.Length > 0)
                        for (var i = 0; i < drops.Length; i++)
                        {
                            var path = AssetDatabase.GetAssetPath(drops[i]);
                            if (path.Equals(FR2_Cache.DEFAULT_CACHE_PATH)) continue;

                            FR2_Setting.AddIgnore(path);
                        }

                    this.groupIgnore.DrawLayout();
                }
                GUILayout.EndHorizontal();
            }


            private void ApplyFiter()
            {
                this.dirty = false;
                this.refs  = new Dictionary<string, FR2_Ref>();

                //foreach (KeyValuePair<string, List<string>> item in FR2_Setting.IgnoreFiltered)
                foreach (var item2 in FR2_Setting.s.listIgnore)
                {
                    var guid = AssetDatabase.AssetPathToGUID(item2);
                    if (string.IsNullOrEmpty(guid)) continue;

                    var asset = FR2_Cache.Api.Get(guid, true);
                    var r     = new FR2_Ref(0, 0, asset, null, "Ignore");
                    this.refs.Add(guid, r);
                }

                this.groupIgnore.Reset
                (this.refs.Values.ToList(),
                    rf => rf.asset != null ? rf.asset.guid : "", this.GetGroup, this.SortGroup
                );
            }

            private string GetGroup(FR2_Ref rf) { return "Ignore"; }

            private void SortGroup(List<string> groups) { }
        }
    }
}