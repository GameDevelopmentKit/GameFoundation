namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class FR2_SceneRef : FR2_Ref
    {
        internal static Dictionary<string, Type> CacheType = new Dictionary<string, Type>();


        // ------------------------- Ref in scene
        private static Action<Dictionary<string, FR2_Ref>> onFindRefInSceneComplete;
        private static Dictionary<string, FR2_Ref>         refs = new Dictionary<string, FR2_Ref>();
        private static string[]                            cacheAssetGuids;
        public         string                              sceneFullPath = "";
        public         string                              scenePath     = "";
        public         string                              targetType;
        public         HashSet<string>                     usingType = new HashSet<string>();

        public FR2_SceneRef(int index, int depth, FR2_Asset asset, FR2_Asset by) : base(index, depth, asset, by) { this.isSceneRef = false; }

        //		public override string ToString()
        //		{
        //			return "SceneRef: " + sceneFullPath;
        //		}

        public FR2_SceneRef(int depth, Object target) : base(0, depth, null, null)
        {
            this.component  = target;
            this.depth      = depth;
            this.isSceneRef = true;
            var obj = target as GameObject;
            if (obj == null)
            {
                var com              = target as Component;
                if (com != null) obj = com.gameObject;
            }

            this.scenePath = FR2_Unity.GetGameObjectPath(obj, false);
            if (this.component == null) return;

            this.sceneFullPath = this.scenePath + this.component.name;
            this.targetType    = this.component.GetType().Name;
        }

        public static IWindow window { get; set; }

        public override bool isSelected() { return this.component == null ? false : FR2_Bookmark.Contains(this.component); }

        public void Draw(Rect r, IWindow window, bool showDetails)
        {
            var selected = this.isSelected();
            this.DrawToogleSelect(r);

            var margin = 2;
            var left   = new Rect(r);
            left.width = r.width / 3f;

            var right = new Rect(r);
            right.xMin += left.width + margin;

            //Debug.Log("draw scene "+ selected);
            if ( /* FR2_Setting.PingRow && */ Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                var pingRect = FR2_Setting.PingRow
                    ? new Rect(0, r.y, r.x + r.width, r.height)
                    : left;

                if (pingRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.control || Event.current.command)
                    {
                        if (selected)
                            FR2_Bookmark.Remove(this);
                        else
                            FR2_Bookmark.Add(this);
                        if (window != null) window.Repaint();
                    }
                    else
                    {
                        EditorGUIUtility.PingObject(this.component);
                    }

                    Event.current.Use();
                }
            }

            EditorGUI.ObjectField(showDetails ? left : r, GUIContent.none, this.component, typeof(GameObject), true);
            if (!showDetails) return;

            var drawPath  = FR2_Setting.GroupMode != FR2_RefDrawer.Mode.Folder;
            var pathW     = drawPath ? EditorStyles.miniLabel.CalcSize(new GUIContent(this.scenePath)).x : 0;
            var assetName = this.component.name;
            // if(usingType!= null && usingType.Count > 0)
            // {
            // 	assetName += " -> ";
            // 	foreach(var item in usingType)
            // 	{
            // 		assetName += item + " - ";
            // 	}
            // 	assetName = assetName.Substring(0, assetName.Length - 3);
            // }
            var cc = FR2_Cache.Api.setting.SelectedColor;

            var lableRect = new Rect(
                right.x,
                right.y,
                pathW + EditorStyles.boldLabel.CalcSize(new GUIContent(assetName)).x,
                right.height);

            if (selected)
            {
                var c = GUI.color;
                GUI.color = cc;
                GUI.DrawTexture(lableRect, EditorGUIUtility.whiteTexture);
                GUI.color = c;
            }

            if (drawPath)
            {
                GUI.Label(this.LeftRect(pathW, ref right), this.scenePath, EditorStyles.miniLabel);
                right.xMin -= 4f;
                GUI.Label(right, assetName, EditorStyles.boldLabel);
            }
            else
            {
                GUI.Label(right, assetName);
            }


            if (!FR2_Setting.ShowUsedByClassed || this.usingType == null) return;

            float sub = 10;
            var   re  = new Rect(r.x + r.width - sub, r.y, 20, r.height);
            Type  t   = null;
            foreach (var item in this.usingType)
            {
                var name = item;
                if (!CacheType.TryGetValue(item, out t))
                {
                    t = FR2_Unity.GetType(name);
                    // if (t == null)
                    // {
                    // 	continue;
                    // } 
                    CacheType.Add(item, t);
                }

                GUIContent content;
                var        width = 0.0f;
                if (!FR2_Asset.cacheImage.TryGetValue(name, out content))
                {
                    if (t == null)
                    {
                        content = new GUIContent(name);
                    }
                    else
                    {
                        var text = EditorGUIUtility.ObjectContent(null, t).image;
                        if (text == null)
                            content = new GUIContent(name);
                        else
                            content = new GUIContent(text, name);
                    }


                    FR2_Asset.cacheImage.Add(name, content);
                }

                if (content.image == null)
                    width = EditorStyles.label.CalcSize(content).x;
                else
                    width = 20;

                re.x     -= width;
                re.width =  width;

                GUI.Label(re, content);
                re.x -= margin; // margin;
            }


            // var nameW = EditorStyles.boldLabel.CalcSize(new GUIContent(assetName)).x;
        }

        private Rect LeftRect(float w, ref Rect rect)
        {
            rect.x     += w;
            rect.width -= w;
            return new Rect(rect.x - w, rect.y, w, rect.height);
        }

        // ------------------------- Scene use scene objects
        public static Dictionary<string, FR2_Ref> FindSceneUseSceneObjects(GameObject[] targets)
        {
            var results = new Dictionary<string, FR2_Ref>();
            var objs    = Selection.gameObjects;
            for (var i = 0; i < objs.Length; i++)
            {
                if (FR2_Unity.IsInAsset(objs[i])) continue;

                var key = objs[i].GetInstanceID().ToString();
                if (!results.ContainsKey(key)) results.Add(key, new FR2_SceneRef(0, objs[i]));

                var coms       = objs[i].GetComponents<Component>();
                var SceneCache = FR2_SceneCache.Api.cache;
                for (var j = 0; j < coms.Length; j++)
                {
                    HashSet<FR2_SceneCache.HashValue> hash = null;
                    if (coms[j] == null) continue; // missing component

                    if (SceneCache.TryGetValue(coms[j], out hash))
                        foreach (var item in hash)
                            if (item.isSceneObject)
                            {
                                var obj  = item.target;
                                var key1 = obj.GetInstanceID().ToString();
                                if (!results.ContainsKey(key1)) results.Add(key1, new FR2_SceneRef(1, obj));
                            }
                }
            }

            return results;
        }

        // ------------------------- Scene in scene
        public static Dictionary<string, FR2_Ref> FindSceneInScene(GameObject[] targets)
        {
            var results = new Dictionary<string, FR2_Ref>();
            var objs    = Selection.gameObjects;
            for (var i = 0; i < objs.Length; i++)
            {
                if (FR2_Unity.IsInAsset(objs[i])) continue;

                var key = objs[i].GetInstanceID().ToString();
                if (!results.ContainsKey(key)) results.Add(key, new FR2_SceneRef(0, objs[i]));


                foreach (var item in FR2_SceneCache.Api.cache)
                foreach (var item1 in item.Value)
                {
                    // if(item.Key.gameObject.name == "ScenesManager")
                    // Debug.Log(item1.objectReferenceValue);
                    GameObject ob = null;
                    if (item1.target is GameObject)
                    {
                        ob = item1.target as GameObject;
                    }
                    else
                    {
                        var com = item1.target as Component;
                        if (com == null) continue;

                        ob = com.gameObject;
                    }

                    if (ob == null) continue;

                    if (ob != objs[i]) continue;

                    key = item.Key.GetInstanceID().ToString();
                    if (!results.ContainsKey(key)) results.Add(key, new FR2_SceneRef(1, item.Key));

                    (results[key] as FR2_SceneRef).usingType.Add(item1.target.GetType().FullName);
                }
            }

            return results;
        }

        public static Dictionary<string, FR2_Ref> FindRefInScene(string[] assetGUIDs, bool depth,
            Action<Dictionary<string, FR2_Ref>> onComplete, IWindow win)
        {
            // var watch = new System.Diagnostics.Stopwatch();
            // watch.Start();
            window                   = win;
            cacheAssetGuids          = assetGUIDs;
            onFindRefInSceneComplete = onComplete;
            if (FR2_SceneCache.ready)
            {
                FindRefInScene();
            }
            else
            {
                FR2_SceneCache.onReady -= FindRefInScene;
                FR2_SceneCache.onReady += FindRefInScene;
            }

            return refs;
        }

        private static void FindRefInScene()
        {
            refs = new Dictionary<string, FR2_Ref>();
            for (var i = 0; i < cacheAssetGuids.Length; i++)
            {
                var asset = FR2_Cache.Api.Get(cacheAssetGuids[i]);
                if (asset == null) continue;

                Add(refs, asset, 0);

                ApplyFilter(refs, asset);
            }

            if (onFindRefInSceneComplete != null) onFindRefInSceneComplete(refs);

            FR2_SceneCache.onReady -= FindRefInScene;
            //    UnityEngine.Debug.Log("Time find ref in scene " + watch.ElapsedMilliseconds);
        }

        private static void FilterAll(Dictionary<string, FR2_Ref> refs, Object obj, string targetPath)
        {
            // ApplyFilter(refs, obj, targetPath);
        }

        private static void ApplyFilter(Dictionary<string, FR2_Ref> refs, FR2_Asset asset)
        {
            var targetPath = AssetDatabase.GUIDToAssetPath(asset.guid);
            if (string.IsNullOrEmpty(targetPath)) return; // asset not found - might be deleted!

            //asset being moved!
            if (targetPath != asset.assetPath) asset.MarkAsDirty();

            var target = AssetDatabase.LoadAssetAtPath(targetPath, typeof(Object));
            if (target == null)
                //Debug.LogWarning("target is null");
                return;

            var targetIsGameobject = target is GameObject;

            if (targetIsGameobject)
                foreach (var item in FR2_Unity.getAllObjsInCurScene())
                    if (FR2_Unity.CheckIsPrefab(item))
                    {
                        var itemGUID = FR2_Unity.GetPrefabParent(item);
                        // Debug.Log(item.name + " itemGUID: " + itemGUID);
                        // Debug.Log(target.name + " asset.guid: " + asset.guid);
                        if (itemGUID == asset.guid) Add(refs, item, 1);
                    }

            var dir = Path.GetDirectoryName(targetPath);
            if (FR2_SceneCache.Api.folderCache.ContainsKey(dir))
                foreach (var item in FR2_SceneCache.Api.folderCache[dir])
                    if (FR2_SceneCache.Api.cache.ContainsKey(item))
                        foreach (var item1 in FR2_SceneCache.Api.cache[item])
                            if (targetPath == AssetDatabase.GetAssetPath(item1.target))
                                Add(refs, item, 1);
        }

        private static void Add(Dictionary<string, FR2_Ref> refs, FR2_Asset asset, int depth)
        {
            var targetId = asset.guid;
            if (!refs.ContainsKey(targetId)) refs.Add(targetId, new FR2_Ref(0, depth, asset, null));
        }

        private static void Add(Dictionary<string, FR2_Ref> refs, Object target, int depth)
        {
            var targetId = target.GetInstanceID().ToString();
            if (!refs.ContainsKey(targetId)) refs.Add(targetId, new FR2_SceneRef(depth, target));
        }
    }

    public class FR2_Ref
    {
        private static int CSVSorter(FR2_Ref item1, FR2_Ref item2)
        {
            var r = item1.depth.CompareTo(item2.depth);
            if (r != 0) return r;

            var t = item1.type.CompareTo(item2.type);
            if (t != 0) return t;

            return item1.index.CompareTo(item2.index);
        }


        public static FR2_Ref[] FromDict(Dictionary<string, FR2_Ref> dict)
        {
            if (dict == null || dict.Count == 0) return null;

            var result = new List<FR2_Ref>();

            foreach (var kvp in dict)
            {
                if (kvp.Value == null) continue;
                if (kvp.Value.asset == null) continue;

                result.Add(kvp.Value);
            }

            result.Sort(CSVSorter);


            return result.ToArray();
        }

        public static FR2_Ref[] FromList(List<FR2_Ref> list)
        {
            if (list == null || list.Count == 0) return null;

            list.Sort(CSVSorter);
            var result = new List<FR2_Ref>();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].asset == null) continue;
                result.Add(list[i]);
            }

            return result.ToArray();
        }

        public FR2_Asset addBy;
        public FR2_Asset asset;
        public Object    component;
        public int       depth;
        public string    group;
        public int       index;

        public bool isSceneRef;
        public int  matchingScore;
        public int  type;

        public override string ToString()
        {
            if (this.isSceneRef)
            {
                var sr = (FR2_SceneRef)this;
                return sr.scenePath;
            }

            return this.asset.assetPath;
        }

        public FR2_Ref(int index, int depth, FR2_Asset asset, FR2_Asset by)
        {
            this.index = index;
            this.depth = depth;

            this.asset = asset;
            if (asset != null) this.type = AssetType.GetIndex(asset.extension);

            this.addBy = by;
            // isSceneRef = false;
        }

        public FR2_Ref(int index, int depth, FR2_Asset asset, FR2_Asset by, string group) : this(index, depth, asset,
            by)
        {
            this.group = group;
            // isSceneRef = false;
        }

        public string GetSceneObjId()
        {
            if (this.component == null) return string.Empty;

            return this.component.GetInstanceID().ToString();
        }

        public virtual bool isSelected() { return FR2_Bookmark.Contains(this.asset.guid); }
        public virtual void DrawToogleSelect(Rect r)
        {
            var s = this.isSelected();
            r.width = 16f;

            if (!GUI2.Toggle(r, ref s)) return;

            if (s)
                FR2_Bookmark.Add(this);
            else
                FR2_Bookmark.Remove(this);
        }

        // public FR2_Ref(int depth, UnityEngine.Object target)
        // {
        // 	this.component = target;
        // 	this.depth = depth;
        // 	// isSceneRef = true;
        // }
        internal List<FR2_Ref> Append(Dictionary<string, FR2_Ref> dict, params string[] guidList)
        {
            var result = new List<FR2_Ref>();

            if (FR2_Cache.Api.disabled) return result;

            if (!FR2_Cache.isReady)
            {
                Debug.LogWarning("Cache not yet ready! Please wait!");
                return result;
            }

            //filter to remove items that already in dictionary
            for (var i = 0; i < guidList.Length; i++)
            {
                var guid = guidList[i];
                if (dict.ContainsKey(guid)) continue;

                var child = FR2_Cache.Api.Get(guid);
                if (child == null) continue;

                var r = new FR2_Ref(dict.Count, this.depth + 1, child, this.asset);
                if (!this.asset.IsFolder) dict.Add(guid, r);

                result.Add(r);
            }

            return result;
        }

        internal void AppendUsedBy(Dictionary<string, FR2_Ref> result, bool deep)
        {
            // var list = Append(result, FR2_Asset.FindUsedByGUIDs(asset).ToArray());
            // if (!deep) return;

            // // Add next-level
            // for (var i = 0;i < list.Count;i ++)
            // {
            // 	list[i].AppendUsedBy(result, true);
            // }

            var h    = this.asset.UsedByMap;
            var list = deep ? new List<FR2_Ref>() : null;

            if (this.asset.UsedByMap == null) return;

            foreach (var kvp in h)
            {
                var guid = kvp.Key;
                if (result.ContainsKey(guid)) continue;

                var child = FR2_Cache.Api.Get(guid);
                if (child == null) continue;

                if (child.IsMissing) continue;

                var r = new FR2_Ref(result.Count, this.depth + 1, child, this.asset);
                if (!this.asset.IsFolder) result.Add(guid, r);

                if (deep) list.Add(r);
            }

            if (!deep) return;

            foreach (var item in list) item.AppendUsedBy(result, true);
        }

        internal void AppendUsage(Dictionary<string, FR2_Ref> result, bool deep)
        {
            var h    = this.asset.UseGUIDs;
            var list = deep ? new List<FR2_Ref>() : null;

            foreach (var kvp in h)
            {
                var guid = kvp.Key;
                if (result.ContainsKey(guid)) continue;

                var child = FR2_Cache.Api.Get(guid);
                if (child == null) continue;

                if (child.IsMissing) continue;

                var r = new FR2_Ref(result.Count, this.depth + 1, child, this.asset);
                if (!this.asset.IsFolder) result.Add(guid, r);

                if (deep) list.Add(r);
            }

            if (!deep) return;

            foreach (var item in list) item.AppendUsage(result, true);
        }

        // --------------------- STATIC UTILS -----------------------

        internal static Dictionary<string, FR2_Ref> FindRefs(string[] guids, bool usageOrUsedBy, bool addFolder)
        {
            var dict = new Dictionary<string, FR2_Ref>();
            var list = new List<FR2_Ref>();

            for (var i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                if (dict.ContainsKey(guid)) continue;

                var asset = FR2_Cache.Api.Get(guid);
                if (asset == null) continue;

                var r = new FR2_Ref(i, 0, asset, null);
                if (!asset.IsFolder || addFolder) dict.Add(guid, r);

                list.Add(r);
            }

            for (var i = 0; i < list.Count; i++)
                if (usageOrUsedBy)
                    list[i].AppendUsage(dict, true);
                else
                    list[i].AppendUsedBy(dict, true);

            //var result = dict.Values.ToList();
            //result.Sort((item1, item2)=>{
            //	return item1.index.CompareTo(item2.index);
            //});

            return dict;
        }


        public static Dictionary<string, FR2_Ref> FindUsage(string[] guids) { return FindRefs(guids, true, true); }

        public static Dictionary<string, FR2_Ref> FindUsedBy(string[] guids) { return FindRefs(guids, false, true); }

        public static Dictionary<string, FR2_Ref> FindUsageScene(GameObject[] objs, bool depth)
        {
            var dict = new Dictionary<string, FR2_Ref>();
            // var list = new List<FR2_Ref>();

            for (var i = 0; i < objs.Length; i++)
            {
                if (FR2_Unity.IsInAsset(objs[i])) continue; //only get in scene 

                //add selection
                if (!dict.ContainsKey(objs[i].GetInstanceID().ToString())) dict.Add(objs[i].GetInstanceID().ToString(), new FR2_SceneRef(0, objs[i]));

                foreach (var item in FR2_Unity.GetAllRefObjects(objs[i])) AppendUsageScene(dict, item);

                if (depth)
                    foreach (var child in FR2_Unity.getAllChild(objs[i]))
                    foreach (var item2 in FR2_Unity.GetAllRefObjects(child))
                        AppendUsageScene(dict, item2);
            }

            return dict;
        }

        private static void AppendUsageScene(Dictionary<string, FR2_Ref> dict, Object obj)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return;

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;

            if (dict.ContainsKey(guid)) return;

            var asset = FR2_Cache.Api.Get(guid);
            if (asset == null) return;

            var r = new FR2_Ref(0, 1, asset, null);
            dict.Add(guid, r);
        }
    }


    public class FR2_RefDrawer : IRefDraw
    {
        public enum Mode
        {
            Dependency,
            Depth,
            Type,
            Extension,
            Folder,
            Atlas,
            AssetBundle,

            None
        }

        public enum Sort
        {
            Type,
            Path,
            Size
        }

        public static GUIStyle toolbarSearchField;
        public static GUIStyle toolbarSearchFieldCancelButton;
        public static GUIStyle toolbarSearchFieldCancelButtonEmpty;

        internal readonly FR2_TreeUI2.GroupDrawer groupDrawer;
        private readonly  bool                    showSearch    = true;
        public            bool                    caseSensitive = false;

        // STATUS
        private bool dirty;
        private int  excludeCount;

        public string level0Group;
        public bool   forceHideDetails;

        public   string                      Lable;
        internal List<FR2_Ref>               list;
        internal Dictionary<string, FR2_Ref> refs;

        // FILTERING
        private readonly string searchTerm = string.Empty;
        private          bool   selectFilter;
        private          bool   showIgnore;


        // ORIGINAL
        internal FR2_Ref[] source => FR2_Ref.FromList(this.list);


        public FR2_RefDrawer(IWindow window)
        {
            this.window      = window;
            this.groupDrawer = new FR2_TreeUI2.GroupDrawer(this.DrawGroup, this.DrawAsset);
        }

        public string messageNoRefs = "Do select something!";
        public string messageEmpty  = "It's empty!";

        public IWindow window { get; set; }

        private void DrawEmpty(Rect rect, string text)
        {
            rect        = GUI2.Padding(rect, 2f, 2f);
            rect.height = 40f;

            EditorGUI.HelpBox(rect, text, MessageType.Info);
        }
        public bool Draw(Rect rect)
        {
            if (this.refs == null || this.refs.Count == 0)
            {
                this.DrawEmpty(rect, this.messageNoRefs);
                return false;
            }

            if (this.dirty || this.list == null) this.ApplyFilter();

            if (!this.groupDrawer.hasChildren)
                this.DrawEmpty(rect, this.messageEmpty);
            else
                this.groupDrawer.Draw(rect);
            return false;
        }

        public bool DrawLayout()
        {
            if (this.refs == null || this.refs.Count == 0) return false;

            if (this.dirty || this.list == null) this.ApplyFilter();

            this.groupDrawer.DrawLayout();
            return false;
        }

        public int ElementCount()
        {
            if (this.refs == null) return 0;

            return this.refs.Count;
            // return refs.Where(x => x.Value.depth != 0).Count();
        }
        public void SetRefs(Dictionary<string, FR2_Ref> dictRefs)
        {
            this.refs  = dictRefs;
            this.dirty = true;
        }

        private void SetBookmarkGroup(string groupLabel, bool willbookmark)
        {
            var ids  = this.groupDrawer.GetChildren(groupLabel);
            var info = this.GetBMInfo(groupLabel);

            for (var i = 0; i < ids.Length; i++)
            {
                FR2_Ref rf;
                if (!this.refs.TryGetValue(ids[i], out rf)) continue;

                if (willbookmark)
                    FR2_Bookmark.Add(rf);
                else
                    FR2_Bookmark.Remove(rf);
            }

            info.count = willbookmark ? info.total : 0;
        }

        internal class BookmarkInfo
        {
            public int count;
            public int total;
        }

        private readonly Dictionary<string, BookmarkInfo> gBookmarkCache = new Dictionary<string, BookmarkInfo>();

        private BookmarkInfo GetBMInfo(string groupLabel)
        {
            BookmarkInfo info = null;
            if (!this.gBookmarkCache.TryGetValue(groupLabel, out info))
            {
                var ids = this.groupDrawer.GetChildren(groupLabel);

                info = new BookmarkInfo();
                for (var i = 0; i < ids.Length; i++)
                {
                    FR2_Ref rf;
                    if (!this.refs.TryGetValue(ids[i], out rf)) continue;
                    info.total++;

                    var isBM = FR2_Bookmark.Contains(rf);
                    if (isBM) info.count++;
                }

                this.gBookmarkCache.Add(groupLabel, info);
            }

            return info;
        }

        private void DrawToggleGroup(Rect r, string groupLabel)
        {
            var info      = this.GetBMInfo(groupLabel);
            var selectAll = info.count == info.total;
            r.width = 16f;
            if (GUI2.Toggle(r, ref selectAll)) this.SetBookmarkGroup(groupLabel, selectAll);

            if (!selectAll && info.count > 0)
            {
                //GUI.DrawTexture(r, EditorStyles.
            }
        }

        private void DrawGroup(Rect r, string label, int childCount)
        {
            if (FR2_Setting.GroupMode == Mode.Folder)
            {
                var tex = AssetDatabase.GetCachedIcon("Assets");
                GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, 16f, 16f), tex);
                r.xMin += 16f;
            }

            this.DrawToggleGroup(r, label);
            r.xMin += 18f;
            GUI.Label(r, label + " (" + childCount + ")", EditorStyles.boldLabel);

            var hasMouse = Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition);
            if (hasMouse && Event.current.button == 1)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Bookmark"), false, () => { this.SetBookmarkGroup(label, true); });
                menu.AddItem(new GUIContent("Remove Bookmark"), false, () => { this.SetBookmarkGroup(label, false); });

                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        public List<FR2_Asset> highlight = new List<FR2_Asset>();

        public void DrawDetails(Rect rect)
        {
            var r = rect;
            r.xMin   += 18f;
            r.height =  18f;

            for (var i = 0; i < this.highlight.Count; i++)
            {
                this.highlight[i].Draw(r, false, false, false, false, false, false, this.window);
                r.y    += 18f;
                r.xMin += 18f;
            }
        }

        private void DrawAsset(Rect r, string guid)
        {
            FR2_Ref rf;
            if (!this.refs.TryGetValue(guid, out rf)) return;

            if (rf.isSceneRef)
            {
                if (rf.component == null) return;

                var re = rf as FR2_SceneRef;
                if (re != null)
                {
                    r.x -= 16f;
                    rf.DrawToogleSelect(r);
                    r.xMin += 32f;
                    re.Draw(r, this.window, !this.forceHideDetails);
                }
            }
            else
            {
                r.xMin -= 16f;
                rf.DrawToogleSelect(r);
                r.xMin += 32f;

                var w2      = (r.x + r.width) / 2f;
                var rRect   = new Rect(w2, r.y, w2, r.height);
                var isClick = Event.current.type == EventType.MouseDown && Event.current.button == 0;

                if (isClick && rRect.Contains(Event.current.mousePosition))
                {
                    this.highlight.Clear();
                    this.highlight.Add(rf.asset);

                    var p   = rf.addBy;
                    var cnt = 0;

                    while (p != null && this.refs.ContainsKey(p.guid))
                    {
                        this.highlight.Add(p);

                        var fr2_ref            = this.refs[p.guid];
                        if (fr2_ref != null) p = fr2_ref.addBy;

                        if (++cnt > 100)
                        {
                            Debug.LogWarning("Break on depth 1000????");
                            break;
                        }
                    }

                    this.highlight.Sort((item1, item2) =>
                    {
                        var d1 = this.refs[item1.guid].depth;
                        var d2 = this.refs[item2.guid].depth;
                        return d1.CompareTo(d2);
                    });

                    // Debug.Log("Highlight: " + highlight.Count + "\n" + string.Join("\n", highlight.ToArray()));
                    Event.current.Use();
                }

                var isHighlight = this.highlight.Contains(rf.asset);
                if (isHighlight)
                {
                    var hlRect = new Rect(-20, r.y, 15f, r.height);
                    GUI2.Rect(hlRect, GUI2.darkGreen);
                }

                rf.asset.Draw(r,
                    isHighlight,
                    !this.forceHideDetails && FR2_Setting.GroupMode != Mode.Folder,
                    !this.forceHideDetails && FR2_Setting.s.displayFileSize,
                    !this.forceHideDetails && FR2_Setting.s.displayAssetBundleName,
                    !this.forceHideDetails && FR2_Setting.s.displayAtlasName,
                    !this.forceHideDetails && FR2_Setting.s.showUsedByClassed, this.window
                );
            }
        }

        private string GetGroup(FR2_Ref rf)
        {
            if (rf.depth == 0) return this.level0Group;

            if (FR2_Setting.GroupMode == Mode.None) return "(no group)";

            FR2_SceneRef sr = null;
            if (rf.isSceneRef)
            {
                sr = rf as FR2_SceneRef;
                if (sr == null) return null;
            }

            if (!rf.isSceneRef)
                if (rf.asset.IsExcluded)
                    return null; // "(ignored)"

            switch (FR2_Setting.GroupMode)
            {
                case Mode.Extension: return rf.isSceneRef ? sr.targetType : rf.asset.extension;
                case Mode.Type:
                {
                    return rf.isSceneRef ? sr.targetType : AssetType.FILTERS[rf.type].name;
                }

                case Mode.Folder: return rf.isSceneRef ? sr.scenePath : rf.asset.assetFolder;

                case Mode.Dependency:
                {
                    return rf.depth == 1 ? "Direct Usage" : "Indirect Usage";
                }

                case Mode.Depth:
                {
                    return "Level " + rf.depth;
                }

                case Mode.Atlas:       return rf.isSceneRef ? "(not in atlas)" : string.IsNullOrEmpty(rf.asset.AtlasName)             ? "(not in atlas)" : rf.asset.AtlasName;
                case Mode.AssetBundle: return rf.isSceneRef ? "(not in assetbundle)" : string.IsNullOrEmpty(rf.asset.AssetBundleName) ? "(not in assetbundle)" : rf.asset.AssetBundleName;
            }

            return "(others)";
        }

        private void SortGroup(List<string> groups)
        {
            groups.Sort((item1, item2) =>
            {
                if (item1.Contains("(")) return 1;
                if (item2.Contains("(")) return -1;

                return item1.CompareTo(item2);
            });
        }

        public FR2_RefDrawer Reset(string[] assetGUIDs, bool isUsage)
        {
            //Debug.Log("Reset :: " + assetGUIDs.Length + "\n" + string.Join("\n", assetGUIDs));
            this.gBookmarkCache.Clear();

            if (isUsage)
                this.refs = FR2_Ref.FindUsage(assetGUIDs);
            else
                this.refs = FR2_Ref.FindUsedBy(assetGUIDs);

            this.dirty = true;
            if (this.list != null) this.list.Clear();

            return this;
        }

        public FR2_RefDrawer Reset(GameObject[] objs, bool findDept, bool findPrefabInAsset)
        {
            this.refs = FR2_Ref.FindUsageScene(objs, findDept);

            var guidss    = new List<string>();
            var dependent = FR2_SceneCache.Api.prefabDependencies;
            foreach (var gameObject in objs)
            {
                HashSet<string> hash;
                if (!dependent.TryGetValue(gameObject, out hash)) continue;

                foreach (var guid in hash) guidss.Add(guid);
            }

            var usageRefs1 = FR2_Ref.FindUsage(guidss.ToArray());
            foreach (var kvp in usageRefs1)
            {
                if (this.refs.ContainsKey(kvp.Key)) continue;

                if (guidss.Contains(kvp.Key)) kvp.Value.depth = 1;

                this.refs.Add(kvp.Key, kvp.Value);
            }


            if (findPrefabInAsset)
            {
                var guids = new List<string>();
                for (var i = 0; i < objs.Length; i++)
                {
                    var guid = FR2_Unity.GetPrefabParent(objs[i]);
                    if (string.IsNullOrEmpty(guid)) continue;

                    guids.Add(guid);
                }

                var usageRefs = FR2_Ref.FindUsage(guids.ToArray());
                foreach (var kvp in usageRefs)
                {
                    if (this.refs.ContainsKey(kvp.Key)) continue;

                    if (guids.Contains(kvp.Key)) kvp.Value.depth = 1;

                    this.refs.Add(kvp.Key, kvp.Value);
                }
            }

            this.dirty = true;
            if (this.list != null) this.list.Clear();

            return this;
        }

        //ref in scene
        public FR2_RefDrawer Reset(string[] assetGUIDs, IWindow window)
        {
            this.refs  = FR2_SceneRef.FindRefInScene(assetGUIDs, true, this.SetRefInScene, window);
            this.dirty = true;
            if (this.list != null) this.list.Clear();

            return this;
        }

        private void SetRefInScene(Dictionary<string, FR2_Ref> data)
        {
            this.refs  = data;
            this.dirty = true;
            if (this.list != null) this.list.Clear();
        }

        //scene in scene
        public FR2_RefDrawer ResetSceneInScene(GameObject[] objs)
        {
            this.refs  = FR2_SceneRef.FindSceneInScene(objs);
            this.dirty = true;
            if (this.list != null) this.list.Clear();

            return this;
        }

        public FR2_RefDrawer ResetSceneUseSceneObjects(GameObject[] objs)
        {
            this.refs  = FR2_SceneRef.FindSceneUseSceneObjects(objs);
            this.dirty = true;
            if (this.list != null) this.list.Clear();

            return this;
        }

        public FR2_RefDrawer ResetUnusedAsset()
        {
            var lst = FR2_Cache.Api.ScanUnused();

            this.refs  = lst.ToDictionary(x => x.guid, x => new FR2_Ref(0, 1, x, null));
            this.dirty = true;
            if (this.list != null) this.list.Clear();

            return this;
        }

        public void RefreshSort()
        {
            if (this.list == null) return;

            if (this.list.Count > 0 && this.list[0].isSceneRef == false && FR2_Setting.SortMode == Sort.Size)
                this.list = this.list.OrderByDescending(x => x.asset != null ? x.asset.fileSize : 0).ToList();
            else
                this.list.Sort((r1, r2) =>
                {
                    var isMixed = r1.isSceneRef ^ r2.isSceneRef;
                    if (isMixed)
                    {
#if FR2_DEBUG
						var sb = new StringBuilder();
						sb.Append("r1: " + r1.ToString());
						sb.AppendLine();
						sb.Append("r2: " +r2.ToString());
						Debug.LogWarning("Mixed compared!\n" + sb.ToString());
#endif

                        var v1 = r1.isSceneRef ? 1 : 0;
                        var v2 = r2.isSceneRef ? 1 : 0;
                        return v2.CompareTo(v1);
                    }

                    if (r1.isSceneRef)
                    {
                        var rs1 = (FR2_SceneRef)r1;
                        var rs2 = (FR2_SceneRef)r2;

                        return this.SortAsset(rs1.sceneFullPath, rs2.sceneFullPath,
                            rs1.targetType, rs2.targetType,
                            FR2_Setting.SortMode == Sort.Path);
                    }

                    return this.SortAsset(
                        r1.asset.assetPath, r2.asset.assetPath,
                        r1.asset.extension, r2.asset.extension,
                        false
                    );
                });

            // clean up list
            var invalidCount = 0;
            for (var i = this.list.Count - 1; i >= 0; i--)
            {
                var item = this.list[i];

                if (item.isSceneRef)
                {
                    if (string.IsNullOrEmpty(item.GetSceneObjId()))
                    {
                        invalidCount++;
                        this.list.RemoveAt(i);
                    }

                    continue;
                }

                if (item.asset == null)
                {
                    invalidCount++;
                    this.list.RemoveAt(i);
                }
            }

#if FR2_DEBUG
			if (invalidCount > 0) Debug.LogWarning("Removed [" + invalidCount + "] invalid assets / objects");
#endif

            this.groupDrawer.Reset(this.list,
                rf =>
                {
                    if (rf == null) return null;
                    if (rf.isSceneRef) return rf.GetSceneObjId();
                    return rf.asset == null ? null : rf.asset.guid;
                }, this.GetGroup, this.SortGroup);
        }

        public bool isExclueAnyItem() { return this.excludeCount > 0; }

        private void ApplyFilter()
        {
            this.dirty = false;

            if (this.refs == null) return;

            if (this.list == null)
                this.list = new List<FR2_Ref>();
            else
                this.list.Clear();

            var minScore = this.searchTerm.Length;

            var term1                      = this.searchTerm;
            if (!this.caseSensitive) term1 = term1.ToLower();

            var term2 = term1.Replace(" ", string.Empty);

            this.excludeCount = 0;

            foreach (var item in this.refs)
            {
                var r = item.Value;

                if (FR2_Setting.IsTypeExcluded(r.type))
                {
                    this.excludeCount++;
                    continue; //skip this one
                }

                if (!this.showSearch || string.IsNullOrEmpty(this.searchTerm))
                {
                    r.matchingScore = 0;
                    this.list.Add(r);
                    continue;
                }

                //calculate matching score
                var name1                      = r.isSceneRef ? (r as FR2_SceneRef).sceneFullPath : r.asset.assetName;
                if (!this.caseSensitive) name1 = name1.ToLower();

                var name2 = name1.Replace(" ", string.Empty);

                var score1 = FR2_Unity.StringMatch(term1, name1);
                var score2 = FR2_Unity.StringMatch(term2, name2);

                r.matchingScore = Mathf.Max(score1, score2);
                if (r.matchingScore > minScore) this.list.Add(r);
            }

            this.RefreshSort();
        }

        public void SetDirty() { this.dirty = true; }

        private int SortAsset(string term11, string term12, string term21, string term22, bool swap)
        {
            //			if (term11 == null) term11 = string.Empty;
            //			if (term12 == null) term12 = string.Empty;
            //			if (term21 == null) term21 = string.Empty;
            //			if (term22 == null) term22 = string.Empty;
            var v1 = string.Compare(term11, term12, StringComparison.Ordinal);
            var v2 = string.Compare(term21, term22, StringComparison.Ordinal);
            return swap ? v1 == 0 ? v2 : v1 : v2 == 0 ? v1 : v2;
        }

        public Dictionary<string, FR2_Ref> getRefs() { return this.refs; }
    }
}