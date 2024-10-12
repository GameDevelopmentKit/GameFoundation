using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

namespace frame8.ThirdParty.UI
{
    /// <summary>
    /// ScrollRect that supports being nested inside another ScrollRect.
    /// BASED ON: https://forum.unity3d.com/threads/nested-scrollrect.268551/#post-1906953
    /// </summary>
    public class ScrollRectNested : ScrollRect
    {
        private ScrollRect _ParentScrollRect;
        private bool       _RouteToParent = false;

        protected override void Start()
        {
            base.Start();

            //if (!Application.isPlaying)
            //	return;

            var p                                                                    = this.transform;
            while (!this._ParentScrollRect && (p = p.parent)) this._ParentScrollRect = p.GetComponent<ScrollRect>();
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // Always route initialize potential drag event to parent
            if (this._ParentScrollRect) ((IInitializePotentialDragHandler)this._ParentScrollRect).OnInitializePotentialDrag(eventData);
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!this.horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
                this._RouteToParent = true;
            else if (!this.vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
                this._RouteToParent = true;
            else
                this._RouteToParent = false;

            if (this._RouteToParent)
            {
                if (this._ParentScrollRect) ((IBeginDragHandler)this._ParentScrollRect).OnBeginDrag(eventData);
            }
            else
            {
                base.OnBeginDrag(eventData);
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (this._RouteToParent)
            {
                if (this._ParentScrollRect) ((IDragHandler)this._ParentScrollRect).OnDrag(eventData);
            }
            else
            {
                base.OnDrag(eventData);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (this._RouteToParent)
            {
                if (this._ParentScrollRect) ((IEndDragHandler)this._ParentScrollRect).OnEndDrag(eventData);
            }
            else
            {
                base.OnEndDrag(eventData);
            }
            this._RouteToParent = false;
        }
    }
}