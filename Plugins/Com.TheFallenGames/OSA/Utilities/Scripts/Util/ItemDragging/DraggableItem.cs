//#define DEBUG_EVENTS

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.Util.ItemDragging
{
    /// <summary>
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class DraggableItem : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler //, ICancelHandler
    {
        public float longClickTime = .7f;

        public IDragDropListener  dragDropListener;
        public StateEnum          State                                 => this._State;
        public RectTransform      RT                                    => this._RT;
        public OrphanedItemBundle OrphanedBundle                        => this._OrphanedBundle;
        public Vector2            CurrentOnDragEventWorldPosition       => this._CurrentOnDragEventWorldPosition;
        public Vector2            DistancePointerToDraggedInCanvasSpace => this._DistancePointerToDraggedInCanvasSpace;
        public Camera             CurrentPressEventCamera               => this._CurrentPressEventCamera;

        private IInitializePotentialDragHandler _ParentToDelegateDragEventsTo;
        private Vector2                         _CurrentOnDragEventWorldPosition;
        private Vector2                         _DistancePointerToDraggedInCanvasSpace;
        private Camera                          _CurrentPressEventCamera;
        private RectTransform                   _RT;
        private Canvas                          _Canvas;
        private GraphicRaycaster                _GraphicRaycaster;
        private RectTransform                   _CanvasRT;
        private Vector2                         _CurrentPressEventWorldPosition;
        private float                           _PressedTime;
        private StateEnum                       _State;

        private OrphanedItemBundle _OrphanedBundle;

        //int _PointerID;
        private EventSystem _EventSystem;

        private EventSystem GetOrFindEventSystem()
        {
            if (this._EventSystem == null) this._EventSystem = FindObjectOfType<EventSystem>();

            return this._EventSystem;
        }

        private void Start()
        {
            this._RT = this.transform as RectTransform;
        }

        private void Update()
        {
            if (this._State == StateEnum.PRESSING__WAITING_FOR_LONG_CLICK)
                if (Time.unscaledTime - this._PressedTime >= this.longClickTime)
                    this.OnLongClick();
        }

        private void OnLongClick()
        {
            this.EnterState_AfterLongClickDragAccepted_WaitingToBeginDrag();
            var evSystem = this.GetOrFindEventSystem();
            if (!evSystem)
            {
                this.EnterState_WaitingForPress();
                return;
            }

            var canvas         = this.GetComponentInParent<Canvas>();
            var raycaster      = canvas.GetComponentInParent<GraphicRaycaster>();
            var raycastResults = new List<RaycastResult>();
            var pev            = new PointerEventData(evSystem);
            pev.position = this._CurrentPressEventWorldPosition;
            raycaster.Raycast(pev, raycastResults);
            var foundThis = false;
            foreach (var res in raycastResults)
            {
                if (res.gameObject == this.gameObject)
                {
                    foundThis = true;
                    break;
                }
            }

            // Happens if the object is moved externally while the pointer remains still
            if (!foundThis)
            {
                this.EnterState_AfterLongClickDragDeclined_WaitingToBeginDrag();
                return;
            }

            this._Canvas           = canvas;
            this._CanvasRT         = this._Canvas.transform as RectTransform;
            this._GraphicRaycaster = raycaster;
            var pos = this.RT.position;
            this.RT.SetParent(this._CanvasRT, false);
            this.RT.position = pos; // preserving the pos

            this.SetVisualMode(VisualMode.OVER_OWNER_OR_OUTSIDE);
            if (this.dragDropListener != null && !this.dragDropListener.OnPrepareToDragItem(this)) this.EnterState_AfterLongClickDragDeclined_WaitingToBeginDrag();
        }

        public void CancelDragSilently()
        {
            this.EnterState_WaitingForPress();
        }

        private void SetVisualMode(VisualMode mode)
        {
            var intMode = (int)mode;
            var euler   = this.RT.localEulerAngles;
            euler.x                  = 10f * intMode;
            euler.z                  = 4f * intMode;
            this.RT.localEulerAngles = euler;
        }

        private void EnterState_WaitingForPress()
        {
            this._ParentToDelegateDragEventsTo          = null;
            this._DistancePointerToDraggedInCanvasSpace = Vector2.zero;
            this._CurrentPressEventCamera               = null;
            this._Canvas                                = null;
            this._CanvasRT                              = null;
            this._GraphicRaycaster                      = null;
            this.SetVisualMode(VisualMode.NONE);
            this._State = StateEnum.WAITING_FOR_PRESS;
        }

        private void EnterState_AfterLongClickDragAccepted_WaitingToBeginDrag()
        {
            this._State = StateEnum.AFTER_LONG_CLICK_DRAG_ACCEPTED__WAITING_TO_BEGIN_DRAG;
        }

        private void EnterState_AfterLongClickDragDeclined_WaitingToBeginDrag()
        {
            this._State = StateEnum.AFTER_LONG_CLICK_DRAG_DECLINED__WAITING_TO_BEGIN_DRAG;
        }

        private void EnterState_BusyDelegatingDragEventToParent()
        {
            this._State = StateEnum.BUSY_DELEGATING_DRAG_TO_PARENT;
        }

        #region Callbacks from Unity UI event handlers

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("OnPointerDown: " + _State);
            #endif
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (this._State != StateEnum.WAITING_FOR_PRESS) return;

            //_PointerID = eventData.pointerId;

            this._CurrentPressEventWorldPosition = eventData.position;
            this._State                          = StateEnum.PRESSING__WAITING_FOR_LONG_CLICK;
            this._PressedTime                    = Time.unscaledTime;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("OnPointerUp: " + _State);
            #endif
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (this._State == StateEnum.PRESSING__WAITING_FOR_LONG_CLICK)
            {
                this.EnterState_WaitingForPress();
            }
            else if (this._State == StateEnum.DRAGGING || this._State == StateEnum.AFTER_LONG_CLICK_DRAG_ACCEPTED__WAITING_TO_BEGIN_DRAG)
            {
                var raycaster = this._GraphicRaycaster;
                this.EnterState_WaitingForPress();
                this.DropAndCheckForOrphaned(eventData, raycaster);
            }
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("OnInitializePotentialDrag: " + _State);
            #endif
            this._ParentToDelegateDragEventsTo = null;
            if (!this.RT.parent) return;
            this._ParentToDelegateDragEventsTo = this.RT.parent.GetComponentInParent(typeof(IInitializePotentialDragHandler)) as IInitializePotentialDragHandler;
            if (this._ParentToDelegateDragEventsTo != null) this._ParentToDelegateDragEventsTo.OnInitializePotentialDrag(eventData);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("OnBeginDrag: " + _State);
            #endif
            if (eventData.button != PointerEventData.InputButton.Left
                || this._State != StateEnum.AFTER_LONG_CLICK_DRAG_ACCEPTED__WAITING_TO_BEGIN_DRAG)
            {
                if (
                    // A child was pressed, which forwarded the event to us
                    this._State == StateEnum.WAITING_FOR_PRESS
                    // Long-click canceled
                    || this._State == StateEnum.PRESSING__WAITING_FOR_LONG_CLICK
                    // The OnPrepareToDragItem returned false (the listener declined the drag when the long click happened) or the item could not be dragged due to other reasons
                    || this._State == StateEnum.AFTER_LONG_CLICK_DRAG_DECLINED__WAITING_TO_BEGIN_DRAG
                )
                {
                    var casted = this._ParentToDelegateDragEventsTo as IBeginDragHandler;
                    if (casted != null)
                    {
                        this.EnterState_BusyDelegatingDragEventToParent(); // keep sending the current started drag event
                        casted.OnBeginDrag(eventData);
                    }
                    else
                    {
                        this.EnterState_WaitingForPress();
                    }
                }

                return;
            }

            this._CurrentPressEventCamera = eventData.pressEventCamera;
            var draggedVHPosScreen = frame8.Logic.Misc.Other.UIUtils8.Instance.WorldToScreenPointForCanvas(this._Canvas, eventData.pressEventCamera, this.RT.position);
            this._DistancePointerToDraggedInCanvasSpace = draggedVHPosScreen - eventData.position;

            this._State = StateEnum.DRAGGING;
            if (this.dragDropListener == null)
            {
                if (this._OrphanedBundle == null) Debug.Log("OnBeginDrag: dragDropListener is null, but the item is not orphaned (_OrphanedBundle is null)");

                return;
            }

            this.dragDropListener.OnBeginDragItem(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            //Debug.Log("OnDrag" + eventData.button);
            if (eventData.button != PointerEventData.InputButton.Left
                || this._State != StateEnum.DRAGGING)
            {
                if (this._State != StateEnum.BUSY_DELEGATING_DRAG_TO_PARENT) return;

                var casted = this._ParentToDelegateDragEventsTo as IDragHandler;
                if (casted != null) casted.OnDrag(eventData);

                return;
            }

            this._CurrentOnDragEventWorldPosition = eventData.position;

            Vector3 worldPoint;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(this._CanvasRT,
                this.CurrentOnDragEventWorldPosition + this.DistancePointerToDraggedInCanvasSpace,
                eventData.pressEventCamera,
                out worldPoint
            );
            this.RT.position = worldPoint;

            if (this.dragDropListener == null && this._OrphanedBundle == null)
            {
                Debug.Log("OnBeginDrag: dragDropListener is null, but the item is not orphaned (_OrphanedBundle is null)");
                return;
            }

            var results = this.RaycastForDragDropListeners(this._GraphicRaycaster, eventData);
            if (results.Count > 0)
            {
                // Just a visual feedback that another listener may accept this item
                if (this.dragDropListener == null || !results.Contains(this.dragDropListener))
                    this.SetVisualMode(VisualMode.OVER_POTENTIAL_NEW_OWNER);
                else
                    this.SetVisualMode(VisualMode.OVER_OWNER_OR_OUTSIDE);
            }
            else
            {
                this.SetVisualMode(VisualMode.OVER_OWNER_OR_OUTSIDE);
            }

            if (this.dragDropListener != null) this.dragDropListener.OnDraggedItem(eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("OnEndDrag: " + _State);
            #endif
            if (eventData.button != PointerEventData.InputButton.Left
                || this._State != StateEnum.DRAGGING)
            {
                if (this._State != StateEnum.BUSY_DELEGATING_DRAG_TO_PARENT) return;

                var casted = this._ParentToDelegateDragEventsTo as IEndDragHandler;
                this.EnterState_WaitingForPress(); // prevent setting _ParentToDelegateDragEventsTo to null
                if (casted != null) casted.OnEndDrag(eventData);

                return;
            }

            var raycaster = this._GraphicRaycaster;
            this.EnterState_WaitingForPress();
            this.DropAndCheckForOrphaned(eventData, raycaster);
        }

        #endregion

        private void DropAndCheckForOrphaned(PointerEventData eventData, GraphicRaycaster raycaster)
        {
            if (this.dragDropListener == null && this._OrphanedBundle == null) Destroy(this.gameObject);

            var wasOrphanedBeforeDrag = this.dragDropListener == null;
            if (wasOrphanedBeforeDrag || (this._OrphanedBundle = this.dragDropListener.OnDroppedItem(eventData)) != null)
            {
                if (this.dragDropListener != null) throw new InvalidOperationException("When orphaned, dragDropListener should be set to null");

                // Find a listener among the raycasted ones, other that the current listener (since this is the listener that has orphaned the item anyway)
                var results  = this.RaycastForDragDropListeners(raycaster, eventData);
                var accepted = false;
                foreach (var listener in results)
                {
                    if (!wasOrphanedBeforeDrag && this._OrphanedBundle.previousOwner != null && listener == this._OrphanedBundle.previousOwner) continue;
                    accepted = listener.OnDroppedExternalItem(eventData, this);
                    if (!accepted) continue;
                }

                if (accepted)
                {
                    if (this.dragDropListener == null) throw new InvalidOperationException("When adopting an orphaned item, dragDropListener should be set to the new owner");

                    this._OrphanedBundle = null;
                }
                else
                {
                    // Just wait for another press event
                }
            }
        }

        private List<IDragDropListener> RaycastForDragDropListeners(GraphicRaycaster raycaster, PointerEventData eventData)
        {
            var listeners = new List<IDragDropListener>();
            var results   = new List<RaycastResult>();
            raycaster.Raycast(eventData, results);
            // Find a listener among the raycasted ones
            IDragDropListener listener;
            foreach (var res in results)
            {
                listener = res.gameObject.GetComponent(typeof(IDragDropListener)) as IDragDropListener;
                if (listener == null) continue;
                listeners.Add(listener);
            }

            return listeners;
        }

        public enum StateEnum
        {
            WAITING_FOR_PRESS,
            BUSY_DELEGATING_DRAG_TO_PARENT,
            PRESSING__WAITING_FOR_LONG_CLICK,
            AFTER_LONG_CLICK_DRAG_DECLINED__WAITING_TO_BEGIN_DRAG,
            AFTER_LONG_CLICK_DRAG_ACCEPTED__WAITING_TO_BEGIN_DRAG,
            DRAGGING,
        }

        private enum VisualMode
        {
            NONE                     = 0,
            OVER_OWNER_OR_OUTSIDE    = 1,
            OVER_POTENTIAL_NEW_OWNER = -1,
        }

        public class OrphanedItemBundle
        {
            public IDragDropListener previousOwner;
            public object            views;
            public object            model;
        }

        /// <summary>Interface to implement by the class that'll handle the drag events</summary>
        public interface IDragDropListener
        {
            /// <summary> Returns if the item drag was accepted </summary>
            bool OnPrepareToDragItem(DraggableItem item);

            void OnBeginDragItem(PointerEventData eventData);
            void OnDraggedItem(PointerEventData   eventData);

            /// <summary> Returns null if the object was accepted. Otherwise, an <see cref="OrphanedItemBundle"/> </summary>
            OrphanedItemBundle OnDroppedItem(PointerEventData eventData);

            /// <summary> Returns if the item was accepted </summary>
            bool OnDroppedExternalItem(PointerEventData eventData, DraggableItem orphanedItemWithBundle);
        }
    }
}