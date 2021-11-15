namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class FR2_TreeUI2
    {
        internal Drawer   drawer;
        private  Vector2  position;
        private  TreeItem rootItem;
        internal Rect     visibleRect;
        public   float    itemPaddingRight = 0f;

        public FR2_TreeUI2(Drawer drawer) { this.drawer = drawer; }

        public void Reset(params string[] root)
        {
            this.position = Vector2.zero;

            this.rootItem = new TreeItem
            {
                tree       = this,
                id         = "$root",
                height     = 0,
                depth      = -1,
                _isOpen    = true,
                highlight  = false,
                childCount = root.Length
            };

            this.rootItem.RefreshChildren(root);
            this.rootItem.DeepOpen();
        }

        public void Draw(Rect rect)
        {
            if (rect.width > 0) this.visibleRect = rect;

            var contentRect             = new Rect(0f, 0f, 1f, this.rootItem.childrenHeight);
            var noScroll                = contentRect.height < this.visibleRect.height;
            if (noScroll) this.position = Vector2.zero;

            var minY = (int)this.position.y;
            var maxY = (int)(this.position.y + this.visibleRect.height);
            contentRect.x -= FR2_Setting.TreeIndent;

            TreeItem.DrawCall   = 0;
            TreeItem.DrawRender = 0;
            this.position       = GUI.BeginScrollView(this.visibleRect, this.position, contentRect);
            {
                var r     = new Rect(0, 0, rect.width - (noScroll ? 4f : 18f) - this.itemPaddingRight, 16f);
                var index = 0;
                this.rootItem.Draw(ref index, ref r, minY, maxY);
            }

            GUI.EndScrollView();
        }

        public void DrawLayout()
        {
            var evtType = Event.current.type;
            var r       = GUILayoutUtility.GetRect(1f, Screen.width, 16f, Screen.height);

            if (evtType != EventType.Layout) this.visibleRect = r;

            this.Draw(this.visibleRect);
        }

        public bool NoScroll() { return this.rootItem.childrenHeight < this.visibleRect.height; }

        // ------------------------ DELEGATE --------------

        public class Drawer
        {
            public virtual int GetHeight(string id) { return 16; }

            public virtual int GetChildCount(string id) { return 0; }

            public virtual string[] GetChildren(string id) { return null; }

            public virtual void Draw(Rect r, TreeItem item) { GUI.Label(r, item.id); }
        }

        public class GroupDrawer : Drawer
        {
            public   Action<Rect, string, int>        drawGroup;
            public   Action<Rect, string>             drawItem;
            private  Dictionary<string, List<string>> groupDict;
            internal FR2_TreeUI2                      tree;

            public GroupDrawer(Action<Rect, string, int> drawGroup, Action<Rect, string> drawItem)
            {
                this.drawItem  = drawItem;
                this.drawGroup = drawGroup;
            }

            // ----------------- TREE WRAPPER ------------------
            public bool TreeNoScroll() { return this.tree.NoScroll(); }

            public bool hideGroupIfPossible;


            public void Reset<T>(List<T> items, Func<T, string> idFunc, Func<T, string> groupFunc,
                Action<List<string>> customGroupSort = null)
            {
                this.groupDict = new Dictionary<string, List<string>>();

                for (var i = 0; i < items.Count; i++)
                {
                    List<string> list;

                    var groupName = groupFunc(items[i]);
                    if (groupName == null) continue; // do not exclude groupName string.Empty

                    var itemId = idFunc(items[i]);
                    if (string.IsNullOrEmpty(itemId)) continue; // ignore items without id

                    if (!this.groupDict.TryGetValue(groupName, out list))
                    {
                        list = new List<string>();
                        this.groupDict.Add(groupName, list);
                    }

                    list.Add(itemId);
                }

                if (this.tree == null) this.tree = new FR2_TreeUI2(this);

                var groups = this.groupDict.Keys.ToList();

                if (this.hideGroupIfPossible && groups.Count == 1) //single group : Flat list
                {
                    var v = this.groupDict[groups[0]];
                    this.tree.Reset(v.ToArray());
                    this.groupDict.Clear();
                }
                else
                {
                    //multiple groups
                    if (customGroupSort != null)
                        customGroupSort(groups);
                    else
                        groups.Sort();

                    this.tree.Reset(groups.ToArray());
                }
            }

            public void Draw(Rect r)
            {
                if (this.tree != null) this.tree.Draw(r);
            }

            public bool hasChildren => this.tree != null && this.tree.rootItem.childCount > 0;

            public bool hasValidTree => this.groupDict != null && this.tree != null;

            public void DrawLayout()
            {
                if (this.tree != null) this.tree.DrawLayout();
            }

            // ----------------- DRAWER WRAPPER ------------------

            public override int GetChildCount(string id)
            {
                if (string.IsNullOrEmpty(id)) return 0;

                List<string> group;
                if (this.groupDict.TryGetValue(id, out group)) return @group.Count;

                return 0;
            }

            public override string[] GetChildren(string id)
            {
                List<string> group;
                if (this.groupDict.TryGetValue(id, out group)) return @group.ToArray();

                return null;
            }

            public override void Draw(Rect r, TreeItem item)
            {
                List<string> group;
                if (this.groupDict.TryGetValue(item.id, out group))
                {
                    this.drawGroup(r, item.id, item.childCount);
                    return;
                }

                this.drawItem(r, item.id);
            }
        }

        // ------------------------ TreeItem2 --------------

        public class TreeItem
        {
            public static int DrawCall;
            public static int DrawRender;

            internal bool _isOpen;

            public int            childCount;
            public List<TreeItem> children;
            public int            childrenHeight;
            public int            depth; // item depth

            public int  height;
            public bool highlight;

            public string id; // item id

            internal TreeItem parent;
            //static Color COLOR	= new Color(0f, 0f, 0f, 0.05f);

            internal FR2_TreeUI2 tree;

            public bool IsOpen
            {
                get => this._isOpen;
                set
                {
                    if (this._isOpen == value || this.childCount == 0) return;

                    this._isOpen = value;

                    if (this._isOpen)
                    {
                        if (this.children == null) this.RefreshChildren(this.tree.drawer.GetChildren(this.id));

                        //Update height for all parents
                        var p = this.parent;
                        while (p != null)
                        {
                            p.childrenHeight += this.childrenHeight;
                            p                =  p.parent;
                        }
                    }
                    else
                    {
                        //Update height for all parents
                        var p = this.parent;
                        while (p != null)
                        {
                            p.childrenHeight -= this.childrenHeight;
                            p                =  p.parent;
                        }
                    }
                }
            }

            internal void DeepOpen()
            {
                this.IsOpen = true;
                if (this.children == null) return;

                for (var i = 0; i < this.children.Count; i++) this.children[i].DeepOpen();
            }

            internal void Draw(ref int index, ref Rect rect, int minY, int maxY)
            {
                DrawCall++;

                // if (DrawCall < 10)
                // {
                // 	Debug.Log(index + ":" + rect + ":" + minY + ":" + maxY + ":" + height + ":" + childrenHeight);
                // }

                //var skipDraw = (rect.y >= maxY) || (height <=0);
                var min      = rect.y;
                var max      = rect.y + this.height;
                var interMin = min >= minY && min <= maxY;
                var interMax = max >= minY && max <= maxY;

                if (this.height > 0 && (interMin || interMax))
                {
                    DrawRender++;
                    rect.height = this.height;

                    if (index % 2 == 1 && FR2_Setting.AlternateRowColor)
                    {
                        var o = GUI.color;
                        GUI.color = FR2_Setting.RowColor;
                        // GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
                        GUI.DrawTexture(new Rect(rect.x - FR2_Setting.TreeIndent, rect.y, rect.width, rect.height),
                            EditorGUIUtility.whiteTexture);
                        GUI.color = o;
                    }

                    var x = (this.depth + 1) * 16f;
                    this.tree.drawer.Draw(new Rect(x, rect.y, rect.width - x, rect.height), this);

                    if (this.childCount > 0)
                        this.IsOpen = GUI.Toggle(new Rect(rect.x + x - 16f, rect.y, 16f, 16f), this.IsOpen, string.Empty,
                            EditorStyles.foldout);

                    index++;
                    rect.y += this.height;
                }
                else
                {
                    rect.y += this.height;
                }

                if (this._isOpen && rect.y <= maxY) //draw children
                    for (var i = 0; i < this.children.Count; i++)
                    {
                        this.children[i].Draw(ref index, ref rect, minY, maxY);
                        if (rect.y > maxY) break;
                    }
            }

            internal void RefreshChildren(string[] childrenIDs)
            {
                this.childCount     = childrenIDs.Length;
                this.childrenHeight = 0;
                this.children       = new List<TreeItem>();

                for (var i = 0; i < this.childCount; i++)
                {
                    var itemId = childrenIDs[i];

                    var item = new TreeItem
                    {
                        tree   = this.tree,
                        parent = this,

                        id        = itemId,
                        depth     = this.depth + 1,
                        highlight = false,

                        height     = this.tree.drawer.GetHeight(itemId),
                        childCount = this.tree.drawer.GetChildCount(itemId)
                    };

                    this.childrenHeight += item.height;
                    this.children.Add(item);
                }
            }
        }
    }
}