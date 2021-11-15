//#define FR2_DEBUG

namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class FR2_SplitView
    {
        private const float SPLIT_SIZE = 2f;


        private readonly IWindow window;
        private          bool    dirty;

        public FR2_SplitView(IWindow w) { this.window = w; }

        [Serializable]
        public class Info
        {
            public GUIContent title;

#if FR2_DEBUG
            public Color color;
#endif

            public Rect  rect;
            public float normWeight;
            public int   stIndex;

            public bool         visible = true;
            public float        weight  = 1f;
            public Action<Rect> draw;

            public void DoDraw()
            {
#if FR2_DEBUG
                GUI2.Rect(rect, Color.white, 0.1f);
#endif
                var drawRect = this.rect;
                if (this.title != null)
                {
                    var titleRect = new Rect(this.rect.x, this.rect.y, this.rect.width, 16f);
                    GUI2.Rect(titleRect, Color.black, 0.2f);

                    titleRect.xMin += 4f;
                    GUI.Label(titleRect, this.title, EditorStyles.boldLabel);
                    drawRect.yMin += 16f;
                }

                this.draw(drawRect);
            }
        }

        public bool       isHorz;
        public List<Info> splits = new List<Info>();

        public bool isVisible => this._visibleCount > 0;

        private int  _visibleCount;
        private Rect _rect;

        public void CalculateWeight()
        {
            this._visibleCount = 0;
            var _totalWeight = 0f;

            for (var i = 0; i < this.splits.Count; i++)
            {
                var info = this.splits[i];
                if (!info.visible) continue;

                info.stIndex =  this._visibleCount;
                _totalWeight += info.weight;

                this._visibleCount++;
            }

            if (this._visibleCount == 0 || _totalWeight == 0)
                //Debug.LogWarning("Nothing visible!");
                return;

            var cWeight = 0f;
            for (var i = 0; i < this.splits.Count; i++)
            {
                var info = this.splits[i];
                if (!info.visible) continue;

                cWeight         += info.weight;
                info.normWeight =  info.weight / _totalWeight;
            }
        }

        public void Draw(Rect rect)
        {
            if (rect.width > 0 || rect.height > 0) this._rect = rect;

            if (this.dirty)
            {
                this.dirty = false;
                this.CalculateWeight();
            }

            var sz = (this._visibleCount - 1) * SPLIT_SIZE;
            var dx = this._rect.x;
            var dy = this._rect.y;

            for (var i = 0; i < this.splits.Count; i++)
            {
                var info = this.splits[i];
                if (!info.visible) continue;

                var rr = new Rect
                (
                    dx, dy, this.isHorz ? (this._rect.width - sz) * info.normWeight : this._rect.width, this.isHorz ? this._rect.height : (this._rect.height - sz) * info.normWeight
                );

                if (rr.width > 0 && rr.height > 0) info.rect = rr;

                if (info.draw != null) info.DoDraw();

                if (info.stIndex < this._visibleCount - 1) this.DrawSpliter(i, this.isHorz ? info.rect.xMax : info.rect.yMax);

                if (this.isHorz)
                    dx += info.rect.width + SPLIT_SIZE;
                else
                    dy += info.rect.height + SPLIT_SIZE;
            }
        }

        public void DrawLayout()
        {
            var rect = this.StartLayout(this.isHorz);
            {
                this.Draw(rect);
            }
            this.EndLayout(this.isHorz);
        }

        private int resizeIndex = -1;


        private void RefreshSpliterPos(int index, float px)
        {
            var sp1 = this.splits[index];
            var sp2 = this.splits[index + 1];

            var r1 = sp1.rect;
            var r2 = sp2.rect;

            var w1 = sp1.weight;
            var w2 = sp2.weight;
            var tt = w1 + w2;

            var dd  = this.isHorz ? r2.xMax - r1.xMin : r2.yMax - r1.yMin - SPLIT_SIZE;
            var m   = this.isHorz ? Event.current.mousePosition.x - r1.x : Event.current.mousePosition.y - r1.y;
            var pct = Mathf.Min(0.9f, Mathf.Max(0.1f, m / dd));

            sp1.weight = tt * pct;
            sp2.weight = tt * (1 - pct);

            this.dirty = true;
            if (this.window != null) this.window.WillRepaint = true;
        }

        private void DrawSpliter(int index, float px)
        {
            var dRect = this._rect;

            if (this.isHorz)
            {
                dRect.x     = px + SPLIT_SIZE;
                dRect.width = SPLIT_SIZE;
            }
            else
            {
                dRect.y      = px;
                dRect.height = SPLIT_SIZE;
            }

            if (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseMove) GUI2.Rect(dRect, Color.black, 0.4f);

            var dRect2 = GUI2.Padding(dRect, -2f, -2f);

            EditorGUIUtility.AddCursorRect(dRect2, this.isHorz ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && dRect2.Contains(Event.current.mousePosition))
            {
                this.resizeIndex = index;
                this.RefreshSpliterPos(index, px);
            }

            if (this.resizeIndex == index) this.RefreshSpliterPos(index, px);

            if (Event.current.type == EventType.MouseUp) this.resizeIndex = -1;
        }

        private Rect StartLayout(bool horz)
        {
            return horz
                ? EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))
                : EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }

        private void EndLayout(bool horz)
        {
            if (horz)
                EditorGUILayout.EndHorizontal();
            else
                EditorGUILayout.EndVertical();
        }
    }
}