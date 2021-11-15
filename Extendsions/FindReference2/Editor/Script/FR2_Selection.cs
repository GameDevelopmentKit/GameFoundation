using UnityObject = UnityEngine.Object;

namespace vietlabs.fr2
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class FR2_Selection : IRefDraw
    {
        internal HashSet<string> guidSet = new HashSet<string>();
        internal HashSet<string> instSet = new HashSet<string>(); // Do not reference directly to SceneObject (which might be destroyed anytime)

        public int Count => this.guidSet.Count + this.instSet.Count;

        public bool Contains(string guidOrInstID) { return this.guidSet.Contains(guidOrInstID) || this.instSet.Contains(guidOrInstID); }

        public bool Contains(UnityObject sceneObject)
        {
            var id = sceneObject.GetInstanceID().ToString();
            return this.instSet.Contains(id);
        }

        public void Add(UnityObject sceneObject)
        {
            if (sceneObject == null) return;
            var id = sceneObject.GetInstanceID().ToString();
            this.instSet.Add(id); // hashset does not need to check exist before add
            this.dirty = true;
        }

        public void AddRange(params UnityObject[] sceneObjects)
        {
            foreach (var go in sceneObjects)
            {
                var id = go.GetInstanceID().ToString();
                this.instSet.Add(id); // hashset does not need to check exist before add	
            }

            this.dirty = true;
        }

        public void Add(string guid)
        {
            if (this.guidSet.Contains(guid)) return;
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("Invalid GUID: " + guid);
                return;
            }

            this.guidSet.Add(guid);
            this.dirty = true;
        }

        public void AddRange(params string[] guids)
        {
            foreach (var id in guids) this.Add(id);
            this.dirty = true;
        }

        public void Remove(UnityObject sceneObject)
        {
            if (sceneObject == null) return;
            var id = sceneObject.GetInstanceID().ToString();
            this.instSet.Remove(id);
            this.dirty = true;
        }

        public void Remove(string guidOrInstID)
        {
            this.guidSet.Remove(guidOrInstID);
            this.instSet.Remove(guidOrInstID);

            this.dirty = true;
        }

        public void Clear()
        {
            this.guidSet.Clear();
            this.instSet.Clear();
            this.dirty = true;
        }

        public bool isSelectingAsset => this.instSet.Count == 0;

        public void Add(FR2_Ref rf)
        {
            if (rf.isSceneRef)
                this.Add(rf.component);
            else
                this.Add(rf.asset.guid);
        }

        public void Remove(FR2_Ref rf)
        {
            if (rf.isSceneRef)
                this.Remove(rf.component);
            else
                this.Remove(rf.asset.guid);
        }

        // ------------ instance

        private          bool                        dirty;
        private readonly FR2_RefDrawer               drawer;
        internal         Dictionary<string, FR2_Ref> refs;
        internal         bool                        isLock;

        public FR2_Selection(IWindow window)
        {
            this.window                                 = window;
            this.drawer                                 = new FR2_RefDrawer(window);
            this.drawer.groupDrawer.hideGroupIfPossible = true;
            this.drawer.forceHideDetails                = true;
            this.drawer.level0Group                     = string.Empty;

            this.dirty = true;
            this.drawer.SetDirty();
        }

        public IWindow window { get; set; }

        public int ElementCount() { return this.refs == null ? 0 : this.refs.Count; }

        public bool DrawLayout()
        {
            if (this.dirty) this.RefreshView();
            return this.drawer.DrawLayout();
        }

        public bool Draw(Rect rect)
        {
            if (this.dirty) this.RefreshView();
            if (this.refs == null) return false;

            this.DrawLock(new Rect(rect.xMax - 12f, rect.yMin - 12f, 16f, 16f));

            return this.drawer.Draw(rect);
        }

        public void SetDirty() { this.drawer.SetDirty(); }

        private static readonly Color PRO   = new Color(0.8f, 0.8f, 0.8f, 1f);
        private static readonly Color INDIE = new Color(0.1f, 0.1f, 0.1f, 1f);

        public void DrawLock(Rect rect)
        {
            GUI2.ContentColor(() =>
            {
                var icon = this.isLock ? FR2_Icon.Lock : FR2_Icon.Unlock;
                if (GUI2.Toggle(rect, ref this.isLock, icon))
                {
                    this.window.WillRepaint = true;
                    this.window.OnSelectionChange();
                }
            }, GUI2.Theme(PRO, INDIE));
        }

        public void RefreshView()
        {
            if (this.refs == null) this.refs = new Dictionary<string, FR2_Ref>();
            this.refs.Clear();

            if (this.instSet.Count > 0)
                foreach (var instId in this.instSet)
                    this.refs.Add(instId, new FR2_SceneRef(0, EditorUtility.InstanceIDToObject(int.Parse(instId))));
            else
                foreach (var guid in this.guidSet)
                {
                    var asset = FR2_Cache.Api.Get(guid);
                    this.refs.Add(guid, new FR2_Ref(0, 0, asset, null)
                    {
                        isSceneRef = false
                    });
                }

            this.drawer.SetRefs(this.refs);
            this.dirty = false;
        }
    }
}