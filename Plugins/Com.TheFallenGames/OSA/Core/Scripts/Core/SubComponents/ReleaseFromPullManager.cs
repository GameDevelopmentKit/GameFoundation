//#define DEBUG_COMPUTE_VISIBILITY

using frame8.Logic.Misc.Other.Extensions;
using UnityEngine;

namespace Com.ForbiddenByte.OSA.Core.SubComponents
{
    internal class ReleaseFromPullManager<TParams, TItemViewsHolder>
        where TParams : BaseParams
        where TItemViewsHolder : BaseItemViewsHolder
    {
        public bool inProgress;

        //public RectTransform.Edge pulledEdge;
        public double targetCTInsetFromVPS;

        private OSA<TParams, TItemViewsHolder> _Adapter;
        private ComputeVisibilityParams        _ComputeVisibilityParams_Reusable = new();

        public ReleaseFromPullManager(OSA<TParams, TItemViewsHolder> adapter)
        {
            this._Adapter = adapter;
        }

        public double CalculateFirstItemTargetInsetFromVPS()
        {
            return this.targetCTInsetFromVPS + this._Adapter._InternalState.paddingContentStart;
        }

        // Only call it if there ARE visible items
        public double CalculateFirstItemInsetFromVPS()
        {
            var firstVH = this._Adapter.GetItemViewsHolder(0);
            //float firstItemInsetFromVPS = _VisibleItems[0].root.GetInsetFromParentEdge(Parameters.content, _InternalState.startEdge);
            double firstItemInsetFromVPS                           = firstVH.root.GetInsetFromParentEdge(this._Adapter.Parameters.Content, this._Adapter._InternalState.startEdge);
            if (firstVH.itemIndexInView > 0) firstItemInsetFromVPS -= this._Adapter._InternalState.GetItemVirtualInsetFromParentStartUsingItemIndexInView(firstVH.itemIndexInView) - this._Adapter._InternalState.paddingContentStart;

            return firstItemInsetFromVPS;
        }

        // Only call this if in progress
        public void FinishNowByDraggingItems(bool computeVisibility)
        {
            //Debug.Log("FinishNowByDraggingItems: " + inProgress);

            if (!this.inProgress) return;

            var abstrDelta = this.CalculateFirstItemTargetInsetFromVPS() - this.CalculateFirstItemInsetFromVPS();
            if (abstrDelta != 0d) this._Adapter.DragVisibleItemsRangeUnchecked(0, this._Adapter.VisibleItemsCount, abstrDelta, true, computeVisibility);

            this.inProgress = false;
        }

        public void FinishNowBySettingContentInset(bool computeVisibility)
        {
            //Debug.Log("FinishNowBySettingContentInset: " + targetCTInsetFromVPS + ", " + _Adapter._InternalState.ctVirtualInsetFromVPS_Cached + ", " + computeVisibility);

            // Don't let it infer the delta, since we already know its value
            this._ComputeVisibilityParams_Reusable.overrideDelta = this.targetCTInsetFromVPS - this._Adapter._InternalState.ctVirtualInsetFromVPS_Cached;
            var contentPosChangeParams = new ContentSizeOrPositionChangeParams
            {
                cancelSnappingIfAny            = true,
                computeVisibilityParams        = computeVisibility ? this._ComputeVisibilityParams_Reusable : null,
                fireScrollPositionChangedEvent = true,
                keepVelocity                   = true,
            };

            bool _;
            this._Adapter.SetContentVirtualInsetFromViewportStart(this.targetCTInsetFromVPS, ref contentPosChangeParams, out _);

            this.inProgress = false;
        }

        /// <summary>This also returns true when CT is smaller than VP</summary>
        public bool IsPulled()
        {
            return this._Adapter._InternalState.ctVirtualInsetFromVPS_Cached > .00001d
                || this._Adapter._InternalState.CTVirtualInsetFromVPE_Cached > .00001d;
        }
    }
}