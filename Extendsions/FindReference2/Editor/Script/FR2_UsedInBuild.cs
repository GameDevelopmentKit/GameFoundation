namespace vietlabs.fr2
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class FR2_UsedInBuild : IRefDraw
    {
        private readonly FR2_TreeUI2.GroupDrawer groupDrawer;

        private          bool                        dirty;
        private readonly FR2_RefDrawer               drawer;
        internal         Dictionary<string, FR2_Ref> refs;

        public FR2_UsedInBuild(IWindow window)
        {
            this.window = window;
            this.drawer = new FR2_RefDrawer(window);
            this.dirty  = true;
            this.drawer.SetDirty();
        }

        public IWindow window { get; set; }


        public int ElementCount() { return this.refs == null ? 0 : this.refs.Count; }

        public bool Draw(Rect rect)
        {
            if (this.dirty) this.RefreshView();

            return this.drawer.Draw(rect);
        }

        public bool DrawLayout()
        {
            //Debug.Log("draw");
            if (this.dirty) this.RefreshView();

            return this.drawer.DrawLayout();
        }

        public void SetDirty()
        {
            this.dirty = true;
            this.drawer.SetDirty();
        }

        public void RefreshView()
        {
            var scenes = new HashSet<string>();
            // string[] scenes = new string[sceneCount];
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene == null) continue;

                if (scene.enabled == false) continue;

                var sce = AssetDatabase.AssetPathToGUID(scene.path);

                if (scenes.Contains(sce)) continue;

                scenes.Add(sce);
            }

            this.refs = FR2_Ref.FindUsage(scenes.ToArray());

            foreach (var VARIABLE in scenes)
            {
                FR2_Ref asset = null;
                if (!this.refs.TryGetValue(VARIABLE, out asset)) continue;


                asset.depth = 1;
            }

            var list  = FR2_Cache.Api.AssetList;
            var count = list.Count;

            for (var i = 0; i < count; i++)
            {
                var item = list[i];
                if (item.inEditor) continue;
                if (item.inPlugins)
                    if (item.type == FR2_AssetType.SCENE)
                        continue;

                if (item.inResources || item.inStreamingAsset || item.inPlugins)
                {
                    if (this.refs.ContainsKey(item.guid)) continue;

                    this.refs.Add(item.guid, new FR2_Ref(0, 1, item, null));
                }
            }

            // remove ignored items
            var vals = this.refs.Values.ToArray();
            foreach (var item in vals)
            foreach (var ig in FR2_Setting.s.listIgnore)
            {
                if (!item.asset.assetPath.StartsWith(ig)) continue;
                this.refs.Remove(item.asset.guid);
                //Debug.Log("Remove: " + item.asset.assetPath + "\n" + ig);
                break;
            }

            this.drawer.SetRefs(this.refs);
            this.dirty = false;
        }

        internal void RefreshSort() { this.drawer.RefreshSort(); }
    }
}