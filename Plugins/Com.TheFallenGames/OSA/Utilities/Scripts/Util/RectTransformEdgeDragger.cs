using UnityEngine;
using System;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.EventSystems;
using frame8.Logic.Misc.Other;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Util
{
    public class RectTransformEdgeDragger : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public event Action TargetDragged;

        [FormerlySerializedAs("draggedRectTransform")] [SerializeField] private RectTransform      _DraggedRectTransform = null;
        [FormerlySerializedAs("draggedEdge")] [SerializeField]          private RectTransform.Edge _DraggedEdge          = RectTransform.Edge.Left;
        [SerializeField]                                                private RectTransform      _StartPoint           = null;
        [SerializeField]                                                private RectTransform      _EndPoint             = null;

        [SerializeField]
        [Tooltip("Set to false if the dragger will be automatically dragged by the exact same amount, as a result of being a direct child of the dragged recttransform")]
        private bool _DragSelf = true;

        [SerializeField] private float _DraggedRectTransformMinSize = 1f;
        [SerializeField] private float _DraggedRectTransformMaxSize = 0f;

        public RectTransform DraggedRectTransform => this._DraggedRectTransform;
        public float         DragNormalizedAmount => this.GetNormPosOnDraggingSegment(this.GetVEndpointStartToMe());

        private float DragAreaSize => Vector3.Distance(this._StartPoint.localPosition, this._EndPoint.localPosition);

        private RectTransform _RT;
        private RectTransform _MyParent;

        private Vector2 _StartDragPosInMySpace;

        //Vector2 _MyInitialLocalPos;
        private float _MyInitialInset;

        private float _DraggedRTInitialInset;

        //float _DraggedRTStartInset;
        //float _DraggedRTStartSize;
        private float  _DraggedRTInitialSize;
        private Canvas _Canvas;
        private bool   _Dragging;

        private void Awake()
        {
            this._RT       = this.transform as RectTransform;
            this._MyParent = this._RT.parent as RectTransform;

            if (this._StartPoint.parent != this._MyParent || this._EndPoint.parent != this._MyParent) throw new UnityException("_StartPoint and _EndPoint should have the same parent as the dragger");

            // Get the root canvas
            var c = this._Canvas = this.transform.parent.GetComponentInParent<Canvas>();
            while (c && c.transform.parent)
            {
                this._Canvas = c;
                c            = c.transform.parent.GetComponentInParent<Canvas>();
            }
        }

        private void Start()
        {
            //_MyInitialLocalPos = _RT.localPosition;
            this.Reinitialize();

            //SetNormalizedPosition(0);
        }

        public void Reinitialize()
        {
            this._MyInitialInset        = this.GetMyCurrentInsetFromDraggedEdge();
            this._DraggedRTInitialInset = this.GetDraggedRTCurrentInsetFromDraggedEdge();
            this._DraggedRTInitialSize  = this.GetRTSize(this._DraggedRectTransform);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData ped)
        {
            var localPos = UIUtils8.Instance.WorldToCanvasLocalPosition(this._Canvas, this._RT.parent as RectTransform, Camera.main, this._RT.position);
            this._Dragging = localPos != null;
            if (!this._Dragging) return;

            this._StartDragPosInMySpace = localPos.Value;

            if (!this._DragSelf) this.Reinitialize();
            //_DraggedRTStartInset = GetDraggedRTCurrentInsetFromDraggedEdge();
            //_DraggedRTStartSize = GetRTSize(_DraggedRectTransform);
        }

        void IDragHandler.OnDrag(PointerEventData ped)
        {
            if (!this._Dragging) return;

            var     cam = ped.pressEventCamera;
            Vector2 posInMySpace;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(this._MyParent, ped.position, cam, out posInMySpace)) return;

            Vector2 pressPosInMySpace;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(this._MyParent, ped.pressPosition, cam, out pressPosInMySpace)) return;

            var dragVectorInMySpace = posInMySpace - pressPosInMySpace;

            var     parentOfDragged = this._DraggedRectTransform.parent as RectTransform;
            Vector2 posInDraggedRTSpace;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentOfDragged, ped.position, cam, out posInDraggedRTSpace)) return;

            Vector2 pressPosInDraggedRTSpace;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentOfDragged, ped.pressPosition, cam, out pressPosInDraggedRTSpace)) return;

            //var dragVectorInDraggedRTSpace = posInDraggedRTSpace - pressPosInDraggedRTSpace;

            var rtNewPos          = this._StartDragPosInMySpace;
            var rtNewPosUnclamped = this._StartDragPosInMySpace;
            rtNewPosUnclamped += dragVectorInMySpace;

            //float amount;
            //float rectMoveAmount;
            float _DraggedRTInsetDelta;
            if (this._DraggedEdge == RectTransform.Edge.Left || this._DraggedEdge == RectTransform.Edge.Right)
            {
                rtNewPos.x           += dragVectorInMySpace.x;
                _DraggedRTInsetDelta =  dragVectorInMySpace.x * (this._DraggedEdge == RectTransform.Edge.Left ? 1f : -1f);
            }
            else
            {
                rtNewPos.y           += dragVectorInMySpace.y;
                _DraggedRTInsetDelta =  dragVectorInMySpace.y * (this._DraggedEdge == RectTransform.Edge.Bottom ? 1f : -1f);
            }
            var normPos = this.GetNormPosOnDraggingSegment(this.GetVEndPointStartTo(rtNewPosUnclamped));
            if (this._DragSelf)
            {
                this.SetNormalizedPosition(normPos, true);
            }
            else
            {
                //Debug.Log(normPos);
                //// TODO see why normPos reports at boundary when it actually isn't
                //if (normPos == 0f)
                //{
                //	if (_DraggedRTInsetDelta > 0)
                //		return;
                //}
                //else if (normPos == 1f)
                //{
                //	if (_DraggedRTInsetDelta < 0)
                //		return;
                //}

                var newInset = this._DraggedRTInitialInset + _DraggedRTInsetDelta;
                var newSize  = this._DraggedRTInitialSize - _DraggedRTInsetDelta;
                if (newSize < this._DraggedRectTransformMinSize)
                {
                    var excess = this._DraggedRectTransformMinSize - newSize;
                    newInset -= excess;
                    newSize  += excess;
                }

                if (this._DraggedRectTransformMaxSize != 0f && newSize > this._DraggedRectTransformMaxSize)
                {
                    var excess = newSize - this._DraggedRectTransformMaxSize;
                    newInset += excess;
                    newSize  -= excess;
                }

                this.SetDraggedRTInsetAndSize(newInset, newSize);
            }

            if (this.TargetDragged != null) this.TargetDragged();
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (!this._Dragging) return;
            // TODO test if this is still needed
            //Reinitialize();
        }

        private Vector2 GetVEndpointStartToMe()
        {
            return this.GetVEndPointStartTo(this._RT.localPosition);
        }

        private Vector2 GetVEndPointStartTo(Vector2 localPoint)
        {
            Vector2 endV2 = this._EndPoint.localPosition;
            return localPoint - endV2;
        }

        /// <summary>
        /// Segment start is the end point (it was at the bottom on the moment of the implementation and it was easier to visualize this way)
        /// </summary>
        private float GetNormPosOnDraggingSegment(Vector2 vSegmentStartToPoint)
        {
            Vector2 endV2   = this._EndPoint.localPosition;
            Vector2 startV2 = this._StartPoint.localPosition;

            // O = end point, A = my pos, B = start point
            var oa = vSegmentStartToPoint;
            var ob = startV2 - endV2;
            return this.GetNormPosOnSegment(ob, oa);
        }

        private float GetNormPosOnSegment(Vector2 segmentVector, Vector2 vSegmentStartToPoint)
        {
            var oa          = vSegmentStartToPoint;
            var ob          = segmentVector;
            var obNorm      = ob / ob.magnitude;
            var oaInOBSpace = oa / ob.magnitude; // i.e. considering ob as unit vector

            //float dot = Vector2.Dot(oaNorm, obNorm);
            var dot = Vector2.Dot(obNorm, oaInOBSpace);

            var normPos = 1f - Mathf.Clamp01(dot);

            return normPos;
        }

        public void SetNormalizedPosition(float normalizedPos, bool updateDraggedRT)
        {
            //var prevLocalPos = transform.localPosition;
            //Debug.Log("SetNormalizedPosition " + normalizedPos);
            this.transform.position = Vector3.Lerp(this._StartPoint.position, this._EndPoint.position, normalizedPos);
            if (updateDraggedRT) this.UpdateDraggedRTFromDraggerPos();

            // Commented: doesn't work very well in the current form
            //if (!_DragSelf)
            //	transform.localPosition = prevLocalPos;
        }

        private void UpdateDraggedRTFromDraggerPos()
        {
            var myCurrentInset = this.GetMyCurrentInsetFromDraggedEdge();
            var deltaInset     = myCurrentInset - this._MyInitialInset;
            this.SetDraggedRTInsetAndSize(this._DraggedRTInitialInset + deltaInset, this._DraggedRTInitialSize - deltaInset);
        }

        private float GetDraggedRTCurrentInsetFromDraggedEdge()
        {
            return this.GetRTCurrentInsetFromDraggedEdge(this._DraggedRectTransform);
        }

        private float GetMyCurrentInsetFromDraggedEdge()
        {
            return this.GetRTCurrentInsetFromDraggedEdge(this._RT);
        }

        private float GetRTCurrentInsetFromDraggedEdge(RectTransform rt)
        {
            return rt.GetInsetFromParentEdge(rt.parent as RectTransform, this._DraggedEdge);
        }

        private float GetRTSize(RectTransform rt)
        {
            float s;
            if (this._DraggedEdge == RectTransform.Edge.Left || this._DraggedEdge == RectTransform.Edge.Right)
                s = this._DraggedRectTransform.rect.width;
            else
                s = this._DraggedRectTransform.rect.height;

            return s;
        }

        private void SetDraggedRTInsetAndSize(float inset, float size)
        {
            this._DraggedRectTransform.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(this._DraggedEdge, inset, size);
        }
    }
}