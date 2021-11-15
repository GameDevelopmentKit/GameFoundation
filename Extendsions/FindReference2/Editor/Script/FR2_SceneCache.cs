#if UNITY_2018_3_OR_NEWER
#define SUPPORT_NESTED_PREFAB
#endif

#if UNITY_2017_1_OR_NEWER
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
#if SUPPORT_NESTED_PREFAB
using UnityEditor.Experimental.SceneManagement;

#endif


namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class FR2_SceneCache
    {
        private static FR2_SceneCache                            _api;
        public static  Action                                    onReady;
        public static  bool                                      ready  = true;
        private        Dictionary<Component, HashSet<HashValue>> _cache = new Dictionary<Component, HashSet<HashValue>>();
        public         int                                       current;
        public         Dictionary<string, HashSet<Component>>    folderCache = new Dictionary<string, HashSet<Component>>();

        private List<GameObject> listGO;

        //public HashSet<string> prefabDependencies = new HashSet<string>();
        public Dictionary<GameObject, HashSet<string>> prefabDependencies =
            new Dictionary<GameObject, HashSet<string>>();

        public  int     total;
        private IWindow window;

        public FR2_SceneCache()
        {
#if UNITY_2018_1_OR_NEWER
            EditorApplication.hierarchyChanged -= this.OnSceneChanged;
            EditorApplication.hierarchyChanged += this.OnSceneChanged;
#else
			EditorApplication.hierarchyWindowChanged -= OnSceneChanged;
			EditorApplication.hierarchyWindowChanged += OnSceneChanged;
#endif

#if UNITY_2018_2_OR_NEWER
            EditorSceneManager.activeSceneChangedInEditMode -= this.OnSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += this.OnSceneChanged;
#endif

#if UNITY_2017_1_OR_NEWER
            SceneManager.activeSceneChanged -= this.OnSceneChanged;
            SceneManager.activeSceneChanged += this.OnSceneChanged;

            SceneManager.sceneLoaded -= this.OnSceneChanged;
            SceneManager.sceneLoaded += this.OnSceneChanged;

            Undo.postprocessModifications -= this.OnModify;
            Undo.postprocessModifications += this.OnModify;
#endif

#if SUPPORT_NESTED_PREFAB
            PrefabStage.prefabStageOpened  -= this.prefabOnpen;
            PrefabStage.prefabStageClosing += this.prefabClose;
            PrefabStage.prefabStageOpened  -= this.prefabOnpen;
            PrefabStage.prefabStageClosing += this.prefabClose;


#endif
        }

        public static FR2_SceneCache Api
        {
            get
            {
                if (_api == null) _api = new FR2_SceneCache();

                return _api;
            }
        }

        public bool Dirty { get; set; } = true;

        public Dictionary<Component, HashSet<HashValue>> cache
        {
            get
            {
                if (this._cache == null) this.refreshCache(this.window);

                return this._cache;
            }
        }

        public void refreshCache(IWindow window)
        {
            if (window == null) return;

            // if(!ready) return;
            this.window = window;

            this._cache             = new Dictionary<Component, HashSet<HashValue>>();
            this.folderCache        = new Dictionary<string, HashSet<Component>>();
            this.prefabDependencies = new Dictionary<GameObject, HashSet<string>>();

            ready = false;

            List<GameObject> listRootGO = null;

#if SUPPORT_NESTED_PREFAB

            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var rootPrefab                     = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                if (rootPrefab != null) listRootGO = new List<GameObject> { rootPrefab };
            }

#else
#endif
            if (listRootGO == null)
            {
                this.listGO = FR2_Unity.getAllObjsInCurScene().ToList();
            }
            else
            {
                this.listGO = new List<GameObject>();
                foreach (var item in listRootGO) this.listGO.AddRange(FR2_Unity.getAllChild(item, true));
            }

            this.total   = this.listGO.Count;
            this.current = 0;
            // Debug.Log("refresh cache total " + total);
            EditorApplication.update -= this.OnUpdate;
            EditorApplication.update += this.OnUpdate;

            // foreach (var item in FR2_Helper.getAllObjsInCurScene())
            // {
            //     // Debug.Log("object in scene: " + item.name);
            //     Component[] components = item.GetComponents<Component>();
            //     foreach (var com in components)
            //     {
            //         if(com == null) continue;
            //         SerializedObject serialized = new SerializedObject(com);
            //         SerializedProperty it = serialized.GetIterator().Copy();
            //         while (it.NextVisible(true))
            //         {

            //             if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
            //             if (it.objectReferenceValue == null) continue;

            // 			if(!_cache.ContainsKey(com)) _cache.Add(com, new HashSet<SerializedProperty>());
            // 			if(!_cache[com].Contains(it))
            // 				_cache[com].Add(it.Copy());
            //         }
            //     }
            // }
            this.Dirty = false;
        }

        private void OnUpdate()
        {
            for (var i = 0; i < 5 * FR2_Cache.priority; i++)
            {
                if (this.listGO == null || this.listGO.Count <= 0)
                {
                    //done
                    // Debug.Log("done");
                    EditorApplication.update -= this.OnUpdate;
                    ready                    =  true;
                    this.Dirty               =  false;
                    this.listGO              =  null;
                    if (onReady != null) onReady();

                    if (this.window != null) this.window.OnSelectionChange();

                    return;
                }

                var index = this.listGO.Count - 1;

                var go = this.listGO[index];
                if (go == null) continue;

                var prefabGUID = FR2_Unity.GetPrefabParent(go);
                if (!string.IsNullOrEmpty(prefabGUID))
                {
                    var parent = go.transform.parent;
                    while (parent != null)
                    {
                        var g = parent.gameObject;
                        if (!this.prefabDependencies.ContainsKey(g)) this.prefabDependencies.Add(g, new HashSet<string>());

                        this.prefabDependencies[g].Add(prefabGUID);
                        parent = parent.parent;
                    }
                }

                var components = go.GetComponents<Component>();

                foreach (var com in components)
                {
                    if (com == null) continue;

                    var serialized = new SerializedObject(com);
                    var it         = serialized.GetIterator().Copy();
                    while (it.NextVisible(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference) continue;

                        if (it.objectReferenceValue == null) continue;

                        var isSceneObject = true;
                        var path          = AssetDatabase.GetAssetPath(it.objectReferenceValue);
                        if (!string.IsNullOrEmpty(path))
                        {
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                isSceneObject = false;
                                if (!this.folderCache.ContainsKey(dir)) this.folderCache.Add(dir, new HashSet<Component>());

                                if (!this.folderCache[dir].Contains(com)) this.folderCache[dir].Add(com);
                            }
                        }

                        if (!this._cache.ContainsKey(com)) this._cache.Add(com, new HashSet<HashValue>());

                        this._cache[com].Add(new HashValue
                            { target = it.objectReferenceValue, isSceneObject = isSceneObject });

                        // if (!_cache.ContainsKey(com)) _cache.Add(com, new HashSet<SerializedProperty>());
                        // if (!_cache[com].Contains(it))
                        //     _cache[com].Add(it.Copy());
                        // string path = AssetDatabase.GetAssetPath(it.objectReferenceValue);

                        // if (string.IsNullOrEmpty(path)) continue;
                        // string dir = System.IO.Path.GetDirectoryName(path);
                        // if (string.IsNullOrEmpty(dir)) continue;
                        // if (!folderCache.ContainsKey(dir)) folderCache.Add(dir, new HashSet<Component>());
                        // if (!folderCache[dir].Contains(com))
                        //     folderCache[dir].Add(com);
                    }
                }

                this.listGO.RemoveAt(index);
                this.current++;
            }
        }

        private void OnSceneChanged()
        {
            if (!Application.isPlaying)
            {
                Api.refreshCache(this.window);
                return;
            }

            this.SetDirty();
        }

#if UNITY_2017_1_OR_NEWER
        private UndoPropertyModification[] OnModify(UndoPropertyModification[] modifications)
        {
            for (var i = 0; i < modifications.Length; i++)
                if (modifications[i].currentValue.objectReference != null)
                {
                    this.SetDirty();
                    break;
                }

            return modifications;
        }
#endif


        public void SetDirty() { this.Dirty = true; }


        public class HashValue
        {
            public bool isSceneObject;

            public Object target;
            //public SerializedProperty pro;

            //			public HashValue(SerializedProperty pro, bool isSceneObject)
            //			{
            //				//this.pro = pro;
            //				this.isSceneObject = isSceneObject;
            //			}
        }
#if SUPPORT_NESTED_PREFAB

        private void prefabClose(PrefabStage obj)
        {
            if (!Application.isPlaying)
            {
                Api.refreshCache(this.window);
                return;
            }

            this.SetDirty();
        }

        private void prefabOnpen(PrefabStage obj)
        {
            if (!Application.isPlaying)
            {
                Api.refreshCache(this.window);
                return;
            }

            this.SetDirty();
        }
#endif

#if UNITY_2017_1_OR_NEWER
        private void OnSceneChanged(Scene scene, LoadSceneMode mode) { this.OnSceneChanged(); }

        private void OnSceneChanged(Scene arg0, Scene arg1) { this.OnSceneChanged(); }
#endif
    }
}