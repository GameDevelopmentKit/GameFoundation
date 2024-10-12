//#define DEBUG_COMPUTE_VISIBILITY

using UnityEngine;
using UnityEngine.EventSystems;

namespace Com.ForbiddenByte.OSA.Core.SubComponents
{
    internal class NestingManager<TParams, TItemViewsHolder> : IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
        where TParams : BaseParams
        where TItemViewsHolder : BaseItemViewsHolder
    {
        public bool SearchedParentAtLeastOnce     => this._SearchedAtLeastOnce;
        public bool CurrentDragCapturedByParent   => this._CurrentDragCapturedByParent;
        public bool CurrentScrollConsumedByParent => this._CurrentScrollConsumedByParent;

        private OSA<TParams, TItemViewsHolder>  _Adapter;
        private InternalState<TItemViewsHolder> _InternalState;
        private IInitializePotentialDragHandler parentInitializePotentialDragHandler;
        private IBeginDragHandler               parentBeginDragHandler;
        private IDragHandler                    parentDragHandler;
        private IEndDragHandler                 parentEndDragHandler;
        private IScrollHandler                  parentScrollHandler;
        private bool                            _SearchedAtLeastOnce;
        private bool                            _CurrentDragCapturedByParent;
        private bool                            _CurrentScrollConsumedByParent;

        public NestingManager(OSA<TParams, TItemViewsHolder> adapter)
        {
            this._Adapter       = adapter;
            this._InternalState = this._Adapter._InternalState;
        }

        public void FindAndStoreNestedParent()
        {
            this.parentInitializePotentialDragHandler = null;
            this.parentBeginDragHandler               = null;
            this.parentDragHandler                    = null;
            this.parentEndDragHandler                 = null;
            this.parentScrollHandler                  = null;

            var tr = this._Adapter.transform;
            // Find the first parent that implements all of the interfaces
            while ((tr = tr.parent) && this.parentInitializePotentialDragHandler == null)
            {
                this.parentInitializePotentialDragHandler = tr.GetComponent(typeof(IInitializePotentialDragHandler)) as IInitializePotentialDragHandler;
                if (this.parentInitializePotentialDragHandler == null) continue;

                this.parentBeginDragHandler = this.parentInitializePotentialDragHandler as IBeginDragHandler;
                if (this.parentBeginDragHandler == null)
                {
                    this.parentInitializePotentialDragHandler = null;
                    continue;
                }

                this.parentDragHandler = this.parentInitializePotentialDragHandler as IDragHandler;
                if (this.parentDragHandler == null)
                {
                    this.parentInitializePotentialDragHandler = null;
                    this.parentBeginDragHandler               = null;
                    continue;
                }

                this.parentEndDragHandler = this.parentInitializePotentialDragHandler as IEndDragHandler;
                if (this.parentEndDragHandler == null)
                {
                    this.parentInitializePotentialDragHandler = null;
                    this.parentBeginDragHandler               = null;
                    this.parentDragHandler                    = null;
                    continue;
                }
            }

            if (this.parentInitializePotentialDragHandler == null)
            {
                // Search for the scroll handler separately, if no drag handlers present
                tr = this._Adapter.transform;
                while ((tr = tr.parent) && this.parentScrollHandler == null) this.parentScrollHandler = tr.GetComponent(typeof(IScrollHandler)) as IScrollHandler;
            }
            else
                // Only allow the scroll handler to be taken from the drag handler, if any, so all handlers will come from the same object
            {
                this.parentScrollHandler = this.parentInitializePotentialDragHandler as IScrollHandler;
            }

            this._SearchedAtLeastOnce = true;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            this._CurrentDragCapturedByParent = false;

            if (!this._SearchedAtLeastOnce) this.FindAndStoreNestedParent();

            if (this.parentInitializePotentialDragHandler == null) return;

            this.parentInitializePotentialDragHandler.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (this.parentInitializePotentialDragHandler == null) return;

            if (this._Adapter.Parameters.DragEnabled)
            {
                var delta    = eventData.delta;
                var dyExcess = Mathf.Abs(delta.y) - Mathf.Abs(delta.x);

                this._CurrentDragCapturedByParent = this._InternalState.hor1_vertMinus1 * dyExcess >= 0f; // parents have priority when dx == dy, since they are supposed to be more important

                if (!this._CurrentDragCapturedByParent)
                    // The drag direction is bigger in the child adapter's scroll direction than in the perpendicular one,
                    // i.e. the drag is 'intended' for the child adapter.
                    // But if the child adapter is at boundary and ForwardDragSameDirectionAtBoundary, still forward the event to the parent
                    this._CurrentDragCapturedByParent = this.CheckForForwardingToParent(delta);
            }
            else
                // When the child ScrollView has its drag disabled, forward the event to the parent without further checks
            {
                this._CurrentDragCapturedByParent = true;
            }

            if (!this._CurrentDragCapturedByParent) return;

            this.parentBeginDragHandler.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (this.parentInitializePotentialDragHandler == null) return;

            this.parentDragHandler.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (this.parentInitializePotentialDragHandler == null) return;

            this.parentEndDragHandler.OnEndDrag(eventData);
            this._CurrentDragCapturedByParent = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            this._CurrentScrollConsumedByParent = false;

            if (!this._SearchedAtLeastOnce) this.FindAndStoreNestedParent();

            if (this.parentScrollHandler == null) return;

            if (this._Adapter.Parameters.ScrollEnabled)
            {
                var scrollDeltaRaw = eventData.scrollDelta;

                var scrollDeltaWithoutSensitivity = scrollDeltaRaw;
                scrollDeltaWithoutSensitivity.y *= -1f;
                var scrollDeltaWithSensitivity = scrollDeltaWithoutSensitivity;
                this._Adapter.Parameters.ApplyScrollSensitivityTo(ref scrollDeltaWithSensitivity);

                var scrollInChildDirectionExist_AfterSensitivity = scrollDeltaWithSensitivity[this._InternalState.hor0_vert1] != 0f;
                //bool b = scrollDeltaRaw[_InternalState.hor0_vert1] != 0f && scrollDelta[_InternalState.hor0_vert1] == 0f;
                if (scrollInChildDirectionExist_AfterSensitivity)
                {
                    // Scrolled in the child's orientation => forward if child adapter is at boundary
                    if (!this.CheckForForwardingToParent(scrollDeltaWithSensitivity)) return;
                }
                else
                {
                    // Sensivity in child's orientation disabled (it's set to 0) => forward the event to parent without further checks
                    var scrollInChildDirectionExist_BeforeSensitivity = scrollDeltaWithoutSensitivity[this._InternalState.hor0_vert1] != 0f;
                    if (scrollInChildDirectionExist_BeforeSensitivity)
                    {
                    }
                    else
                    {
                        // No scroll input in the child orientation

                        var scrollInChildTransversalDirectionExist_AfterSensitivity = scrollDeltaWithSensitivity[1 - this._InternalState.hor0_vert1] != 0f;
                        if (scrollInChildTransversalDirectionExist_AfterSensitivity)
                            // Child has priority, since it set a non-zero sensitivity for the received input axis
                            return;
                    }
                }
            }
            else
            {
                // When the child ScrollView has its scroll disabled, forward the event to the parent without further checks
            }
            this._CurrentScrollConsumedByParent = true;

            this.parentScrollHandler.OnScroll(eventData);
        }

        private bool CheckForForwardingToParent(Vector2 delta)
        {
            if (this._Adapter.Parameters.ForwardDragSameDirectionAtBoundary)
            {
                var deltaInScrollDir      = delta[this._InternalState.hor0_vert1];
                var abstrDeltaInScrollDir = deltaInScrollDir * this._InternalState.hor1_vertMinus1;

                var acceptedError = 3f; // UI units
                if (abstrDeltaInScrollDir < 0f)
                    // Delta would drag the Scroll View in a negative direction (towards end)
                    return this._Adapter.ContentVirtualInsetFromViewportEnd >= -acceptedError;
                else
                    // Postive direction (towards start)
                    return this._Adapter.ContentVirtualInsetFromViewportStart >= -acceptedError;
            }

            return false;
        }
    }
}