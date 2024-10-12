//#define ALLOW_DEBUG_OUTSIDE_EDITOR

#if UNITY_EDITOR || ALLOW_DEBUG_OUTSIDE_EDITOR
//#define DEBUG_COMPUTE_VISIBILITY_TWIN
//#define DEBUG_CHANGE_COUNT
//#define DEBUG_UPDATE
//#define DEBUG_INDICES
//#define DEBUG_CONTENT_VISUALLY
//#define DEBUG_ADD_VHS
//#define DEBUG_LOOPING
//#define DEBUG_ON_SIZES_CHANGED_EXTERNALLY
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using Com.ForbiddenByte.OSA.Core.SubComponents;
using Com.ForbiddenByte.OSA.Core.Data;
using Com.ForbiddenByte.OSA.Core.Data.Gallery;
using Com.ForbiddenByte.OSA.Core.Data.Animations;

namespace Com.ForbiddenByte.OSA.Core
{
    public abstract partial class OSA<TParams, TItemViewsHolder> : MonoBehaviour, IOSA
        where TParams : BaseParams
        where TItemViewsHolder : BaseItemViewsHolder
    {
        #if DEBUG_UPDATE
		public bool debug_Update;
        #endif

        #if DEBUG_CONTENT_VISUALLY
		public bool debug_ContentVisually;
        #endif

        #if DEBUG_INDICES
		public bool debug_Indices;
        #endif

        private ComputeVisibilityParams _ComputeVisibilityParams_Reusable_Empty = new(),
            _ComputeVisibilityParams_Reusable_DragUnchecked                     = new(),
            _ComputeVisibilityParams_Reusable_SetNormalizedScrollPos            = new();

        private void Drag(
            double                        abstrDeltaInCTSpace,
            AllowContentOutsideBoundsMode allowOutsideBoundsMode,
            bool                          cancelSnappingIfAny
        )
        {
            bool _, __;
            this.Drag(abstrDeltaInCTSpace, allowOutsideBoundsMode, cancelSnappingIfAny, out _, out __);
        }

        /// <summary></summary>
        /// <param name="abstrDeltaInCTSpace">
        /// diff in positions, raw value, local space (content's space). 
        /// Represented as normalized is: 
        ///		start=1, end=0
        ///		, and translated in local space: 
        ///			1) vert: 1=top, 0=bottom; 
        ///			2) hor: inversely
        ///	</param>
        private void Drag(
                double                        abstrDeltaInCTSpace,
                AllowContentOutsideBoundsMode allowOutsideBoundsMode,
                bool                          cancelSnappingIfAny,
                out bool                      done,
                out bool                      looped
            )
            //bool cancelSnappingIfAny,
            //bool updateCachedCTVirtualInset = true,
            //double? updateCachedCTVirtualInset_ContentInsetOverride = null)
        {
            done   = false;
            looped = false;

            //Debug.Log("Dragging by abstrDelta " + abstrDeltaInCTSpace);
            // Commented: Drag functions correctly in theory even if there are no visible items; it'll just update the content's inset, which is what we want in this case
            //if (_VisibleItemsCount == 0)
            //	return false;
            // TODO think if it eases the looping or not to clamp using the last item's inset instead of the content's. 
            // R(after more thinking): yes
            var    dragCoefficient = 1d;
            double curPullSignDistance, newPullDistance;
            double absAbstractDelta;
            //bool isVirtualizing = _InternalState.VirtualScrollableArea > 0;
            if (abstrDeltaInCTSpace > 0d) // going to start
            {
                //if (isVirtualizing)
                curPullSignDistance = this._InternalState.ctVirtualInsetFromVPS_Cached;
                //else
                //	// Not virtualizing means the content's allowed start edge is not at VPS
                //	curPullSignDistance = _InternalState.GetContentInferredRealInsetFromVPS(_VisibleItems[0]);

                absAbstractDelta = abstrDeltaInCTSpace;
            }
            else // going to end
            {
                //if (isVirtualizing)
                curPullSignDistance = this._InternalState.CTVirtualInsetFromVPE_Cached;
                //else
                //	// Not virtualizing means the content's allowed end edge position is not at VPE
                //	curPullSignDistance = _InternalState.GetContentInferredRealInsetFromVPE(_VisibleItems[_VisibleItemsCount-1]);
                absAbstractDelta = -abstrDeltaInCTSpace;
            }

            newPullDistance = curPullSignDistance + absAbstractDelta;

            bool allowOutsideBounds;
            if (newPullDistance >= 0d) // is pulled beyond bounds
            {
                allowOutsideBounds = allowOutsideBoundsMode == AllowContentOutsideBoundsMode.ALLOW
                    || (allowOutsideBoundsMode == AllowContentOutsideBoundsMode.ALLOW_IF_OUTSIDE_AMOUNT_SHRINKS
                        && newPullDistance < curPullSignDistance);

                var currentIsAlreadyPulledOutOfBoundsOrIsAtLimit = curPullSignDistance >= 0d;
                if ( /*!_Params.elasticMovement || _Params.effects.LoopItems || */!allowOutsideBounds)
                {
                    if (currentIsAlreadyPulledOutOfBoundsOrIsAtLimit) return; // nothing more to pull

                    var maxAllowedAbsDelta = -curPullSignDistance;
                    abstrDeltaInCTSpace = Math.Sign(abstrDeltaInCTSpace) * Math.Min(absAbstractDelta, maxAllowedAbsDelta);
                }

                if (this._Params.effects.ElasticMovement && currentIsAlreadyPulledOutOfBoundsOrIsAtLimit)
                {
                    var curPullSignDistance01                             = curPullSignDistance / this._InternalState.vpSize;
                    if (curPullSignDistance01 > 1d) curPullSignDistance01 = 1d;
                    dragCoefficient = this._Params.effects.PullElasticity * (1d - curPullSignDistance01);
                }
            }
            else
                allowOutsideBounds = allowOutsideBoundsMode != AllowContentOutsideBoundsMode.DO_NOT_ALLOW;

            var finalAbstractDelta = abstrDeltaInCTSpace * dragCoefficient;
            //DragVisibleItemsRangeUnchecked(0, _VisibleItemsCount, finalAbstractDelta, false);
            //UpdateCTVrtInsetFromVPS(
            //	new ContentSizeOrPositionChangeParams {
            //		cancelSnappingIfAny = true,
            //		computeVisibilityNowIfSuccess = true,
            //		computeVisibilityNowIfSuccess_OverrideDelta = finalAbstractDelta,
            //		fireScrollPositionChangedEvent = true,
            //		keepVelocity = true,
            //		allowOutsideBounds = allowOutsideBounds
            //	}
            //);

            looped = this.DragVisibleItemsRangeUnchecked(0, this.VisibleItemsCount, finalAbstractDelta, true, true, allowOutsideBounds, cancelSnappingIfAny);
            //bool looped = DragVisibleItemsRangeUnchecked(0, _VisibleItemsCount, finalAbstractDelta, updateCachedCTVirtualInset, true, allowOutsideBounds, cancelSnappingIfAny, updateCachedCTVirtualInset_ContentInsetOverride);

            //double newInset = currentInset + finalAbstractDelta;
            //SetContentVirtualInsetFromViewportStart2(newInset, true, false, true, true, true, allowOutsideBounds);
            done = true;
        }

        // Returns whether looped or not
        internal bool DragVisibleItemsRangeUnchecked(
            int    vhStartIndex,
            int    vhEndIndexExcl,
            double abstractDelta,
            bool   updateCachedCTVirtualInset,
            bool   updateCachedCTVirtualInset_ComputeVisibility,
            bool   updateCachedCTVirtualInset_AllowOutsideBounds  = true,
            bool   updateCachedCTVirtualInset_CancelSnappingIfAny = true
            //double? updateCachedCTVirtualInset_ContentInsetOverride = null
        )
        {
            //Debug.Log("DragVisibleItemsRangeUnchecked: (count="+Math.Max(0, vhEndIndexExcl - vhStartIndex)+") start=" + vhStartIndex + ", endExcl=" + vhEndIndexExcl + ", abstrDelta=" + abstractDelta);
            double localDelta = (float)(abstractDelta * this._InternalState.hor1_vertMinus1);
            for (var i = vhStartIndex; i < vhEndIndexExcl; ++i)
            {
                var vh       = this._VisibleItems[i];
                var localPos = vh.root.localPosition;
                //localPos[_InternalState.hor0_vert1] += transformedLocalDelta[_InternalState.hor0_vert1];
                localPos[this._InternalState.hor0_vert1] = (float)(localDelta + localPos[this._InternalState.hor0_vert1]);
                vh.root.localPosition                    = localPos;
            }

            if (updateCachedCTVirtualInset)
            {
                this._ComputeVisibilityParams_Reusable_DragUnchecked.overrideDelta = abstractDelta;
                double? contentInsetOverride = null;
                if (this.VisibleItemsCount == 0) // nothing to infer the content size from => use the cached one
                    contentInsetOverride = this._InternalState.ctVirtualInsetFromVPS_Cached + abstractDelta;

                var p = new ContentSizeOrPositionChangeParams
                {
                    cancelSnappingIfAny            = updateCachedCTVirtualInset_CancelSnappingIfAny,
                    allowOutsideBounds             = updateCachedCTVirtualInset_AllowOutsideBounds,
                    computeVisibilityParams        = updateCachedCTVirtualInset_ComputeVisibility ? this._ComputeVisibilityParams_Reusable_DragUnchecked : null,
                    fireScrollPositionChangedEvent = true,
                    keepVelocity                   = true,
                    contentInsetOverride           = contentInsetOverride,
                    //contentInsetOverride = updateCachedCTVirtualInset_ContentInsetOverride
                };

                return this.UpdateCTVrtInsetFromVPS(ref p);
            }

            return false;
        }

        private IEnumerator SmoothScrollProgressCoroutine(
            int               itemIndex,
            double            duration,
            double            normalizedOffsetFromViewportStart  = 0f,
            double            normalizedPositionOfItemPivotToUse = 0f,
            Func<float, bool> onProgress                         = null,
            Action            onDone                             = null
        )
        {
            //Debug.Log("Started routine");
            var vsa = this._InternalState.VirtualScrollableArea;
            // Negative/zero values indicate CT is smallerthan/sameas VP, so no scrolling can be done
            if (vsa <= 0d)
            {
                // This is dependent on the case. sometimes is needed, sometimes not
                //if (duration > 0f)
                //{
                //	if (_Params.UseUnscaledTime)
                //		yield return new WaitForSecondsRealtime(duration);
                //	else
                //		yield return new WaitForSeconds(duration);
                //}

                this._SmoothScrollCoroutine = null;

                if (onProgress != null) onProgress(1f);

                if (onDone != null) onDone();
                //Debug.Log("stop 1f");
                yield break;
            }

            // Ignoring OnScrollViewValueChanged during smooth scrolling
            var ignorOnScroll_lastValue = this._SkipComputeVisibilityInUpdateOrOnScroll;
            this._SkipComputeVisibilityInUpdateOrOnScroll = true;

            this.StopMovement();

            //Canvas.ForceUpdateCanvases();
            if (this._Params.optimization.ForceLayoutRebuildOnBeginSmoothScroll) this._InternalState.RebuildLayoutImmediateCompat(this._Params.ScrollViewRT);

            Func<double> getTargetVrtInset = () =>
            {
                // This needs to be updated regularly (if looping/twin pass, but it doesn't add too much overhead, so it's ok to re-calculate it each time)
                vsa = this._InternalState.VirtualScrollableArea;

                return this.ScrollToHelper_GetContentStartVirtualInsetFromViewportStart(
                    vsa,
                    itemIndex,
                    normalizedOffsetFromViewportStart,
                    normalizedPositionOfItemPivotToUse
                );
            };

            // Keep track of the value of _SmoothScrollCoroutine, since _SmoothScrollToCoroutine could change if the onProgress() returns false,
            // and we need to know whether to set _SmoothScrollToCoroutine back to null (when no other coroutine has stated) or not (when another coroutine started).
            // Also, in order to have _SmoothScrollCoroutine non-null, we need to wait 1 frame;
            yield return null;
            var mySmoothScrollCoroutine = this._SmoothScrollCoroutine;

            if (this._SmoothScrollCoroutine == null) throw new InvalidOperationException();

            this._SmoothScrollOnDone = onDone;

            double initialVrtInsetFromParent   = -1d,       targetVrtInsetFromParent   = -1d; // setting a value because of compiler, but it's initialized at least once in the loop below
            bool   needToCalculateInitialInset = true,      needToCalculateTargetInset = true, notCanceledByCaller = true;
            double startTime                   = this.Time, elapsedTime;
            double localProgress = 0d, // used in calculations
                reportedProgress,      // the "real" progress, as needed for the caller of this function
                value;
            var endOfFrame = new WaitForEndOfFrame();

            var contentPosChangeParams = new ContentSizeOrPositionChangeParams
            {
                computeVisibilityParams        = this._ComputeVisibilityParams_Reusable_Empty,
                fireScrollPositionChangedEvent = true,
                allowOutsideBounds             = true,
            };

            var looped = false;
            Action<double> setInsetAndUpdateLocalsFn = inset =>
            {
                //Debug.Log("vrtinset="+_InternalState.ContentPanelVirtualInsetFromViewportStart + ", i="+ initialVirtualInsetFromParent + ", t="+ targetInsetFromParent + ", v="+value);
                contentPosChangeParams.allowOutsideBounds = this._Params.effects.LoopItems && this._InternalState.VirtualScrollableArea > 0d;
                this.SetContentVirtualInsetFromViewportStart(inset, ref contentPosChangeParams, out looped);
            };
            var lerpFn = OSAMath.GetLerpFunction(this._Params.Animation.SmoothScrollType);

            double time;
            double originalStartTime = startTime, originalDuration = duration;
            //bool neededToRecalculateInitialInset;
            //bool atLeastOneTwinPassDetected = false;
            var atLeastOneSizeChangeDetected               = false;
            var sizeChanges_LastKnown                      = this._InternalState.totalNumberOfSizeChanges;
            var atLeastOneTwinPassOrItemSizeChangeDetected = false;
            Action detectTwinPassOrItemSizeChanges = () =>
            {
                //atLeastOneTwinPassDetected = atLeastOneTwinPassDetected || _InternalState.lastComputeVisibilityHadATwinPass;
                if (!atLeastOneSizeChangeDetected)
                    if (sizeChanges_LastKnown != this._InternalState.totalNumberOfSizeChanges)
                    {
                        sizeChanges_LastKnown        = this._InternalState.totalNumberOfSizeChanges;
                        atLeastOneSizeChangeDetected = true;
                    }

                atLeastOneTwinPassOrItemSizeChangeDetected = atLeastOneSizeChangeDetected;
                //atLeastOneTwinPassOrItemSizeChangeDetected = atLeastOneTwinPassDetected || atLeastOneSizeChangeDetected;
            };

            do
            {
                yield return null;
                detectTwinPassOrItemSizeChanges();
                //hadTwinPass = hadTwinPass || _InternalState.lastComputeVisibilityHadATwinPass;
                yield return endOfFrame;
                detectTwinPassOrItemSizeChanges();

                time        = this.Time;
                elapsedTime = time - startTime;

                if (elapsedTime >= duration)
                    reportedProgress = localProgress = 1d;
                else
                {
                    //progress = (elapsedTime / duration);
                    var ta01 = elapsedTime / duration;
                    var tb01 = (time - originalStartTime) / originalDuration;
                    localProgress    = lerpFn(ta01);
                    reportedProgress = lerpFn(tb01);
                }

                //neededToRecalculateInitialInset = needToCalculateInitialInset;
                if (needToCalculateInitialInset)
                {
                    initialVrtInsetFromParent = this._InternalState.ctVirtualInsetFromVPS_Cached;

                    startTime =  time;
                    duration  -= elapsedTime;
                }

                if (needToCalculateTargetInset) targetVrtInsetFromParent = getTargetVrtInset();
                //if (!neededToRecalculateInitialInset)
                //{
                //}
                value = initialVrtInsetFromParent * (1d - localProgress) + targetVrtInsetFromParent * localProgress; // Lerp for double
                //Debug.Log(
                //	"t=" + progress.ToString("0.####") +
                //	", i=" + initialVrtInsetFromParent.ToString("0") +
                //	", t=" + targetVrtInsetFromParent.ToString("0") +
                //	", t-i=" + (targetVrtInsetFromParent - initialVrtInsetFromParent).ToString("0") +
                //	", toSet=" + value.ToString("0"));

                // If finished earlier => don't make additional unnecesary steps
                if (Math.Abs(targetVrtInsetFromParent - value) < .01d)
                {
                    value            = targetVrtInsetFromParent;
                    reportedProgress = localProgress = 1d;
                }

                // Values that that would cause the ctStart to be placed AFTER vpStart should indicate the scrolling has ended (can't go past it)
                // Only allowed if looping
                if (value > 0d && !this._Params.effects.LoopItems)
                {
                    reportedProgress = localProgress = 1d; // end; last loop
                    value            = 0d;
                }
                else
                {
                    setInsetAndUpdateLocalsFn(value);
                    detectTwinPassOrItemSizeChanges();

                    if (this._Params.effects.LoopItems)
                        needToCalculateInitialInset = needToCalculateTargetInset = true;
                    else
                        //needToCalculateInitialInset = needToCalculateTargetInset = atLeastOneTwinPassDetected;
                        needToCalculateInitialInset = needToCalculateTargetInset = atLeastOneTwinPassOrItemSizeChangeDetected;

                    //if (_InternalState.lastComputeVisibilityHadATwinPass)
                    //	Debug.Log(_InternalState.lastComputeVisibilityHadATwinPass);
                    //if (false && looped)
                    //{
                    //	needToCalculateInitialInset = true;
                    //	needToCalculateTargetInset = true;
                    //}
                }
            } while (reportedProgress < 1d && (onProgress == null || (notCanceledByCaller = onProgress((float)reportedProgress))));

            if (notCanceledByCaller)
            {
                detectTwinPassOrItemSizeChanges();
                // Assures the end result is the expected one
                setInsetAndUpdateLocalsFn(getTargetVrtInset());
                detectTwinPassOrItemSizeChanges();

                // Bugfix when new items request a twin pass which may displace the content, or if the content simply looped (this is the same correction as done in the loop above)
                //if (looped || atLeastOneTwinPassDetected)
                if (looped || atLeastOneTwinPassOrItemSizeChangeDetected) setInsetAndUpdateLocalsFn(getTargetVrtInset());

                //if (false && looped)
                //{
                //	needToCalculateInitialInset = true;
                //	needToCalculateTargetInset = true;
                //}

                //// This is a semi-hack-lazy hot-fix because when the duration is 0 (or near 0), sometimes the visibility isn't computed well
                //// Same thing is done in ScrollTo method above
                //ComputeVisibilityForCurrentPosition(false, -.1);
                //ComputeVisibilityForCurrentPosition(true, +.1);
                ////ScrollTo(itemIndex, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse);

                this._SmoothScrollCoroutine = null;
                this._SmoothScrollOnDone    = null;

                if (onProgress != null) onProgress(1f);

                //Debug.Log("stop natural");
            }
            else
            {
                // Only reset _SmoothScrollCoroutine if no other coroutine has started meanwhile
                if (mySmoothScrollCoroutine == this._SmoothScrollCoroutine) this._SmoothScrollCoroutine = null;
                if (onDone == this._SmoothScrollOnDone) this._SmoothScrollOnDone                        = null;
                //	Debug.Log("routine cancelled");
            }

            // This should be restored even if the scroll was cancelled by the caller. 
            // When the routine is stopped via StopCoroutine, this line won't be executed because the execution point won't pass the previous yield instruction.
            // It's assumed that _SkipComputeVisibilityInUpdateOrOnScroll is manually set to false whenever that happpens
            this._SkipComputeVisibilityInUpdateOrOnScroll = ignorOnScroll_lastValue;

            if (notCanceledByCaller)
                if (onDone != null)
                    onDone();
        }

        /// <summary> It assumes that the content is bigger than the viewport </summary>
        private double ScrollToHelper_GetContentStartVirtualInsetFromViewportStart(double vsa, int itemIndex, double normalizedItemOffsetFromStart, double normalizedPositionOfItemPivotToUse)
        {
            var itemViewIndex                  = this._ItemsDesc.GetItemViewIndexFromRealIndexChecked(itemIndex);
            var itemVrtInsetFromStart          = this._InternalState.GetItemVirtualInsetFromParentStartUsingItemIndexInView(itemViewIndex);
            var itemSize                       = this._ItemsDesc[itemViewIndex];
            var insetToAddFromFineTunedOffsets = this._InternalState.vpSize * normalizedItemOffsetFromStart - itemSize * normalizedPositionOfItemPivotToUse;
            var looping                        = this._Params.effects.LoopItems;

            // The standard ct inset calculation, i.e. go towards start if target real index is smaller, else towards end
            var ctInsetFromStart_NonLooping = -itemVrtInsetFromStart + insetToAddFromFineTunedOffsets;

            if (looping)
            {
                // When looping, we try to loop to the closer item
                var ctInsetFromStart_ShorterPathIfExists = this._ScrollToHelper_GetContentStartVirtualInsetFromViewportStart_Looping_ShorterPathIfExists(
                    itemViewIndex,
                    itemVrtInsetFromStart,
                    normalizedItemOffsetFromStart,
                    normalizedPositionOfItemPivotToUse,
                    insetToAddFromFineTunedOffsets
                );

                if (ctInsetFromStart_ShorterPathIfExists != null) return ctInsetFromStart_ShorterPathIfExists.Value;

                //return ctInsetFromStart_ShorterPathIfExists ?? ctInsetFromStart_NonLooping;
            }

            // If looping, there's no need to clamp. In addition, clamping would cancel a scrollTo if the content is exactly at start or end
            var defaultAllowedOutsideBoundary = this._InternalState.vpSize / 2d;
            var maxContentInsetFromVPAllowed  = this._Params.effects.LoopItems && vsa > 0d ? defaultAllowedOutsideBoundary : 0d;

            //double maxContentInsetFromVPAllowed = 0d;

            var minContentVirtualInsetFromVPAllowed = -vsa - maxContentInsetFromVPAllowed;

            var ctInsetFromStart_Clamped = this.ClampDouble(ctInsetFromStart_NonLooping, minContentVirtualInsetFromVPAllowed, maxContentInsetFromVPAllowed);

            //Debug.Log("siz=" + itemSize + ", -itemVrtInsetFromStart=" + (-itemVrtInsetFromStart) + ", insetToAdd=" + insetToAdd + ", ctInsetFromStart_Clamped=" + ctInsetFromStart_Clamped);

            return ctInsetFromStart_Clamped;
        }

        // IMGDOC <!image url="$(SolutionDir)\Docs\img\OSA\SmoothScroll-Shortest-Path-Looping.png" scale=".86"/>
        private double? _ScrollToHelper_GetContentStartVirtualInsetFromViewportStart_Looping_ShorterPathIfExists(int itemViewIndex, double itemVrtInsetFromStart, double normalizedItemOffsetFromStart, double normalizedPositionOfItemPivotToUse, double insetToAddFromFineTunedOffsets)
        {
            if (this._VisibleItemsCount == 0)
                //Debug.Log("Visible 0");
                return null;

            var ctSize                  = this._InternalState.ctVirtualSize;
            var vpSize                  = this._InternalState.vpSize;
            var itemSize                = this._ItemsDesc[itemViewIndex];
            var ctCurrentInsetFromStart = this._InternalState.ctVirtualInsetFromVPS_Cached;
            var itemEndEdgeDistToCTS    = itemVrtInsetFromStart + itemSize;
            var ctAmountBeforeVP        = -ctCurrentInsetFromStart;
            var isItemBeforeVP          = itemEndEdgeDistToCTS < ctAmountBeforeVP;

            // Measured in 'distance from CTS', or the more commonly used 'inset from CTS'
            var travelBeginDefaultPoint = itemVrtInsetFromStart + normalizedPositionOfItemPivotToUse * itemSize;

            if (isItemBeforeVP)
            {
                var contentBeforeTravelBeginDefaultPoint = travelBeginDefaultPoint;

                var travelBeforeViewportAmount = ctAmountBeforeVP - travelBeginDefaultPoint;
                var travelInsideViewportAmount = vpSize * normalizedItemOffsetFromStart;
                var totalDefaultTravel         = travelBeforeViewportAmount + travelInsideViewportAmount;
                var travelEndPoint             = travelBeginDefaultPoint + totalDefaultTravel;

                var remainingContentAfterTravelEndPoint = ctSize - travelEndPoint;

                // How much would we travel if we'd scroll in the other direction and a loop will have been happened
                var totalAlternativeTravel = remainingContentAfterTravelEndPoint + contentBeforeTravelBeginDefaultPoint;

                if (totalDefaultTravel < totalAlternativeTravel)
                    // Default item scrolling will already use the shortest direction
                    return null;

                return ctCurrentInsetFromStart - totalAlternativeTravel;
            }

            var ctAmountBeforeVpPlusInsideVP = ctAmountBeforeVP + vpSize;
            var isItemAfterVP                = itemVrtInsetFromStart > ctAmountBeforeVpPlusInsideVP;
            if (isItemAfterVP)
            {
                var contentAfterTravelBeginDefaultPoint = ctSize - travelBeginDefaultPoint;

                var ctAmountAfterVP = ctSize - ctAmountBeforeVP - vpSize;

                var travelAfterViewportAmount  = ctAmountAfterVP - contentAfterTravelBeginDefaultPoint;
                var travelInsideViewportAmount = vpSize * (1f - normalizedItemOffsetFromStart);
                var totalDefaultTravel         = travelAfterViewportAmount + travelInsideViewportAmount;
                var travelEndPoint             = travelBeginDefaultPoint - totalDefaultTravel;

                var remainingContentBeforeTravelEndPoint = travelEndPoint;

                // How much would we travel if we'd scroll in the other direction and a loop will have been happened
                var totalAlternativeTravel = remainingContentBeforeTravelEndPoint + contentAfterTravelBeginDefaultPoint;

                if (totalDefaultTravel < totalAlternativeTravel)
                    // Default item scrolling will already use the shortest direction
                    return null;

                return ctCurrentInsetFromStart + totalAlternativeTravel;
            }

            // If item is inside vp, no special treatment is needed
            return null;
        }

        /// <summary><paramref name="virtualInset"/> should be a valid value. See how it's clamped in <see cref="ScrollTo(int, float, float)"/></summary>
        internal double SetContentVirtualInsetFromViewportStart(double virtualInset, ref ContentSizeOrPositionChangeParams p, out bool looped)
        {
            this._ReleaseFromPull.inProgress = false;

            var  deltaInset = virtualInset - this._InternalState.ctVirtualInsetFromVPS_Cached;
            bool _;

            if (!p.keepVelocity) this.StopMovement();

            this.Drag(
                deltaInset,
                this._Params.effects.LoopItems && this._InternalState.VirtualScrollableArea > 0
                    ? AllowContentOutsideBoundsMode.ALLOW
                    : AllowContentOutsideBoundsMode.ALLOW_IF_OUTSIDE_AMOUNT_SHRINKS,
                p.cancelSnappingIfAny,
                out _,
                out looped
            );

            this.CorrectPositionsOfVisibleItems(false, p.fireScrollPositionChangedEvent);

            return deltaInset;
        }

        private void RecycleAllVisibleViewsHolders()
        {
            while (this.VisibleItemsCount > 0) this.RecycleOrStealViewsHolder(0, false);
        }

        private void RecycleOrStealViewsHolder(int vhIndex, bool steal)
        {
            var vh = this._VisibleItems[vhIndex];
            if (!steal)
            {
                this.OnBeforeRecycleOrDisableViewsHolder(vh, -1); // -1 means it'll be disabled, not re-used ATM
                this.SetViewsHolderDisabled(vh);
            }

            this._VisibleItems.RemoveAt(vhIndex);
            --this.VisibleItemsCount;
            if (steal)
            {
                //_StolenItems.Add(vh);
            }
            else
                this._RecyclableItems.Add(vh);
        }

        // Returns whether the scrollview looped or not
        private bool UpdateCTVrtInsetFromVPS(ref ContentSizeOrPositionChangeParams p)
        {
            var ctInsetBefore = this._InternalState.ctVirtualInsetFromVPS_Cached;

            var itemVirtualInset    = this._InternalState.paddingContentStart;
            var contentVirtualInset = 0d;
            if (p.contentInsetOverride != null)
                contentVirtualInset = p.contentInsetOverride.Value;
            else if (this.VisibleItemsCount > 0)
            {
                var vh                        = this._VisibleItems[0];
                var indexInViewOfFirstVisible = vh.itemIndexInView;

                if (indexInViewOfFirstVisible > 0) itemVirtualInset += this._ItemsDesc.GetItemSizeCumulative(indexInViewOfFirstVisible - 1, false) + indexInViewOfFirstVisible * this._InternalState.spacing;

                double itemRealInset = vh.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.startEdge);

                contentVirtualInset = itemRealInset - itemVirtualInset;
            }

            var ignoreOnScroll_valueBefore = this._SkipComputeVisibilityInUpdateOrOnScroll;
            this._SkipComputeVisibilityInUpdateOrOnScroll = true;

            if (p.cancelSnappingIfAny && this._Params.Snapper) this._Params.Snapper.CancelSnappingIfInProgress();

            if (!p.keepVelocity) this.StopMovement();

            this._InternalState.UpdateCachedCTVirtInsetFromVPS(contentVirtualInset, p.allowOutsideBounds);
            var looped                                    = false;
            if (p.computeVisibilityParams != null) looped = this.ComputeVisibilityForCurrentPosition(p.computeVisibilityParams);
            if (p.fireScrollPositionChangedEvent) this.OnScrollPositionChangedInternal();

            this._SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;

            if (this._Params.effects.HasContentVisual)
            {
                var ctInsetDelta = this._InternalState.ctVirtualInsetFromVPS_Cached - ctInsetBefore;
                ctInsetDelta = ctInsetDelta + this._Params.effects.ContentVisualParallaxEffect * ctInsetDelta;

                var uvRect = this._Params.effects.ContentVisual.uvRect;

                var pos                                            = uvRect.position;
                var curVal                                         = pos[this._InternalState.hor0_vert1];
                var dragToVPSizeRatio                              = ctInsetDelta / this._InternalState.vpSize;
                var dragToVPSizeRatio_FractionalPartPositiveLooped = dragToVPSizeRatio - Math.Floor(dragToVPSizeRatio);

                var dragDeltaInUVSpace                          = dragToVPSizeRatio_FractionalPartPositiveLooped * -this._InternalState.hor1_vertMinus1;
                if (dragDeltaInUVSpace < 0d) dragDeltaInUVSpace = 1d + dragDeltaInUVSpace;

                var newUVPos = curVal + dragDeltaInUVSpace;
                newUVPos = newUVPos - Math.Floor(newUVPos);
                if (newUVPos < 0d) newUVPos = 1d + newUVPos;

                pos[this._InternalState.hor0_vert1]       = (float)newUVPos;
                uvRect.position                           = pos;
                this._Params.effects.ContentVisual.uvRect = uvRect;
            }

            return looped;
        }

        private void ShiftViewsHolderItemIndexAndFireEvent(TItemViewsHolder vh, int shift, bool wasInsert, int insertOrRemoveIndex)
        {
            var prev = vh.ItemIndex;
            vh.ShiftIndex(shift, this._ItemsDesc.itemsCount);
            this.OnItemIndexChangedDueInsertOrRemove(vh, prev, wasInsert, insertOrRemoveIndex);
        }

        private void ShiftViewsHolderItemIndexInView(TItemViewsHolder vh, int shift)
        {
            vh.ShiftIndexInView(shift, this._ItemsDesc.itemsCount);
        }

        /// <summary> 
        /// Make sure to only call this from <see cref="ChangeItemsCount(ItemCountChangeMode, int, int, bool, bool)"/>, because implementors may override it to catch the "pre-item-count-change" event
        /// </summary>
        private void ChangeItemsCountInternal(
            ItemCountChangeMode changeMode,
            int                 count,
            int                 indexIfInsertingOrRemoving,
            bool                contentPanelEndEdgeStationary,
            bool                keepVelocity,
            bool                stealInsteadOfRecycle
        )
        {
            if (!this._Initialized && !this._SkipInitializationChecks) throw new OSAException("ChangeItemsCountInternal: OSA not initialized. Before using it, make sure the GameObject is active in hierarchy, the OSA component is enabled, and Start has been called. If you overrode OnInitialized, please call base.OnInitialized() on the first line of your function");

            var prevCount = this._ItemsDesc.itemsCount;
            if (changeMode == ItemCountChangeMode.INSERT)
            {
                if (indexIfInsertingOrRemoving < 0 || indexIfInsertingOrRemoving > prevCount) throw new ArgumentOutOfRangeException("indexIfInsertingOrRemoving", indexIfInsertingOrRemoving, "should be >=0 and <= than itemsCount(=" + prevCount + ")");

                var newCountLong = (long)prevCount + count;
                if (newCountLong > OSAConst.MAX_ITEMS) throw new ArgumentOutOfRangeException("newCount", newCountLong, "should be <= MAX_COUNT(=" + OSAConst.MAX_ITEMS + ")");
            }
            else if (changeMode == ItemCountChangeMode.REMOVE)
            {
                if (indexIfInsertingOrRemoving < 0 || indexIfInsertingOrRemoving >= prevCount) throw new ArgumentOutOfRangeException("indexIfInsertingOrRemoving", indexIfInsertingOrRemoving, "should be >=0 and < than itemsCount(=" + prevCount + ")");

                if (count < 1) throw new ArgumentOutOfRangeException("count", count, "should be > 0");

                if (indexIfInsertingOrRemoving + count > prevCount) throw new ArgumentOutOfRangeException("indexIfInsertingOrRemoving + count", count, "indexIfInsertingOrRemoving+count = " + (indexIfInsertingOrRemoving + count) + " should be <= itemsCount(=" + this._ItemsDesc.itemsCount + ")");
            }

            var loopItems = this._Params.effects.LoopItems;
            if (loopItems)
                if (changeMode != ItemCountChangeMode.RESET)
                    throw new OSAException("ChangeItemsCountInternal: At the moment, only ItemCountChangeMode.RESET is supported when looping. Use ResetItems()");
            //				if (changeMode == ItemCountChangeMode.REMOVE)
            //				{
            //					if (count > 1)
            //						throw new ArgumentOutOfRangeException(
            //							"count", 
            //							count, 
            //							"Looping is enabled. Removing more than 1 item at once is not yet supported. " +
            //								"Use ResetItems instead, or simply remove them 1 by 1 (if feasible)"
            //						);
            //				}
            //				if (contentPanelEndEdgeStationary)
            //				{
            //#if UNITY_EDITOR
            //					Debug.Log("OSA.ChangeItemsCountInternal: When looping is active, contentPanelEndEdgeStationary parameter is ignored");
            //#endif
            //					contentPanelEndEdgeStationary = false;
            //				}
            //OnItemsCountWillChange(itemsCount);
            var cancelAnim = this._Params.Animation.Cancel;
            this.CancelAnimations(true, cancelAnim.SmoothScroll.OnCountChanges, cancelAnim.UserAnimations.OnCountChanges);

            if (this._ReleaseFromPull.inProgress)
            {
                // Bugfix 15-Jul.2019: if there are no items visible, there's nothing to drag
                if (this.VisibleItemsCount > 0)
                    this._ReleaseFromPull.FinishNowByDraggingItems(false);
                else
                    this._ReleaseFromPull.FinishNowBySettingContentInset(false);
            }

            //if (_ReleaseFromPullCurrentState.inProgress && changeMode != ItemCountChangeMode.RESET)
            //{
            //	Debug.Log("ChangeItemsCountInternal: _ReleaseFromPullCurrentState.inProgress and removing/inserting. TODO clamp current items before, in case of negative VSA");
            //}

            //_ReleasingFromOutsideBoundsPull = false;

            var ignoreOnScroll_valueBefore = this._SkipComputeVisibilityInUpdateOrOnScroll;
            this._SkipComputeVisibilityInUpdateOrOnScroll = true;

            var indexInViewIfInsertingOrRemoving = -1;
            if (prevCount > 0 && changeMode != ItemCountChangeMode.RESET)
            {
                if (indexIfInsertingOrRemoving == prevCount)
                    // If inserting at end, GetItemViewIndexFromRealIndex(<count>) will return 0, since the item count was not yet changed
                    indexInViewIfInsertingOrRemoving = this._ItemsDesc.GetItemViewIndexFromRealIndexChecked(indexIfInsertingOrRemoving - 1) + 1;
                else
                    indexInViewIfInsertingOrRemoving = this._ItemsDesc.GetItemViewIndexFromRealIndexChecked(indexIfInsertingOrRemoving);
            }

            var velocity = this._Velocity;
            if (!keepVelocity) this.StopMovement();

            var ctSizeBefore = this._InternalState.CalculateContentVirtualSize();
            //if (_InternalState.layoutRebuildPendingDueToScrollViewSizeChangeEvent)
            //	Canvas.ForceUpdateCanvases();

            #if DEBUG_INDICES
			string debugIndicesString;
			if (GetDebugIndicesString(out debugIndicesString))
				Debug.Log("ChangeCountBef vhs " + _VisibleItemsCount + ". Indices: " + debugIndicesString);
            #endif
            //int oldCount = _ItemsDesc.itemsCount;
            this.CollectItemsSizes(changeMode, count, indexIfInsertingOrRemoving, this._ItemsDesc);
            var newCount = this._ItemsDesc.itemsCount;

            var     newCTSize = this._InternalState.CalculateContentVirtualSize();
            var     deltaSize = newCTSize - ctSizeBefore;
            double? _;
            var     additionalCTDragAbstrDelta = 0d; // only provided if shrinking
            this._InternalState.CorrectParametersOnCTSizeChange(contentPanelEndEdgeStationary, out _, ref additionalCTDragAbstrDelta, newCTSize, deltaSize);
            var emptyAreaWhenCTSmallerThanVP   = -this._InternalState.VirtualScrollableArea;
            var vrtContentPanelIsAtOrBeforeEnd = this._InternalState.CTVirtualInsetFromVPE_Cached >= 0d;

            // Re-build the content: mark all currentViews as recyclable
            // _RecyclableItems.Count must be zero;
            if (this.GetNumExcessRecycleableItems() > 0) throw new OSAException("ChangeItemsCountInternal: GetNumExcessObjects() > 0 when calling ChangeItemsCountInternal(); this may be due ComputeVisibility not being finished executing yet");

            // TODO see if it makes sense to optimize by keeping the items that will continue to be visible, in case of insert/remove. Currently, all of them are being recycled
            // , case in which the more of the items will be dragged, not only the first one for ctinset calculation

            // DragVisibleItemsRangeUnchecked is called only to compute content inset start accordingly, as all the vhs are made into recyclable after, anyway
            double? reportedScrollDeltaOverride         = null;
            double? ctInsetFromVPSOverrideToPassAsParam = null;
            ////bool allowOutsideBounds = Parameters.elasticMovement;
            //// Outside bounds should be always allowed in case of insert/remove, because otherwise the content's inset is clamped due to its pivot when smaller than vp, but
            //// in ComputeVisibility that's called below the item's inset from CTS is being inferred from the new value 
            //// (thus, placing newly added items at tail over the existing ones, the more the pivot is towards bottom)
            var allowOutsideBounds = false;

            TItemViewsHolder vh;
            // TODO see if setting ctInsetFromVPSOverrideToPassAsParam instead of auto-inferring-from-first-vh is necessary (maybe it does more harm than good)
            //Debug.Log("TODO see if setting ctInsetFromVPSOverrideToPassAsParam for REMOVE/INSERT instead of auto-inferring-from-first-vh is necessary (maybe it does more harm than good)");
            bool recycleAllViewsHolders   = false, correctionMayBeNeeded = false;
            var  vhIndexForInsertOrRemove = -1;
            if (this.VisibleItemsCount > 0 && changeMode != ItemCountChangeMode.RESET)
            {
                var firstVH                          = this._VisibleItems[0];
                var firstVHIndexInViewBeforeShifting = firstVH.itemIndexInView;
                vhIndexForInsertOrRemove = indexInViewIfInsertingOrRemoving - firstVHIndexInViewBeforeShifting;
            }

            switch (changeMode)
            {
                // IMGDOC <!image url="$(SolutionDir)\Docs\img\OSA\Insert-Remove-Items.jpg" scale=".86"/>
                case ItemCountChangeMode.INSERT:
                {
                    int vhIndex;

                    if (this.VisibleItemsCount > 0)
                        // TODO test
                        // Items with higer indices can be BEFORE the insertIndex, if looping. They need to be increased.
                        // Increasing indices that are bigger, i.e. ignoring the HEAD(if looping) and items after.
                        // This covers both the looping case and the normal case, to avoid multiple loops. vCount is not that big anyway
                        // The indexInView of items before the inserted ones should remain the same
                        for (vhIndex = 0; vhIndex < this.VisibleItemsCount; ++vhIndex)
                        {
                            vh = this._VisibleItems[vhIndex];
                            if (vh.itemIndexInView >= indexInViewIfInsertingOrRemoving)
                                //if (loopItems && (vhIndexForInsertOrRemove < 0 || indexIfInsertingOrRemoving == oldCount))
                                //{
                                //	// If inserting before viewport when looping, the items will preserve their indexInView and they will also remain stationary
                                //	// This is a easier way of handling looping in this case and avoiding some edge cases
                                //}
                                //else
                                this.ShiftViewsHolderItemIndexInView(vh, count);

                            if (vh.ItemIndex >= indexIfInsertingOrRemoving) this.ShiftViewsHolderItemIndexAndFireEvent(vh, count, true, indexIfInsertingOrRemoving);
                        }

                    //allowOutsideBounds = true;
                    //// no looping 
                    if (contentPanelEndEdgeStationary)
                    {
                        // commented: additionalCTDragAbstrDelta is 0 if expanding the size
                        //ctInsetFromVPSOverrideToPassAsParam = _InternalState.contentPanelVirtualInsetFromViewportStart_Cached - deltaSize + additionalCTDragAbstrDelta;

                        // TODO see if setting ctInsetFromVPSOverrideToPassAsParam should be done here instead of only inside one if branch
                        if (emptyAreaWhenCTSmallerThanVP > 0)
                        {
                            ctInsetFromVPSOverrideToPassAsParam = this._InternalState.ctVirtualInsetFromVPS_Cached - deltaSize;
                            allowOutsideBounds                  = true;
                        }

                        if (this.VisibleItemsCount > 0)
                        {
                            // Important: if you insert at X, that translates to inserting BEFORE x, meaning between x and x-1, which
                            // means X should stay in place and only items before it should be shifted towards start.

                            int vhEndIndex, vhEndIndexMinus1;
                            vhEndIndex       = vhIndexForInsertOrRemove;
                            vhEndIndexMinus1 = vhEndIndex - 1;

                            reportedScrollDeltaOverride = -.1d;

                            // TODO test
                            //// Only shifting indices that are bigger, i.e. ignoring the HEAD(if looping) and items after.
                            //// Items with higer indices can be BEFORE the insertIndex, if looping. They need to be increased.
                            //// This covers both the looping case and the normal case, to avoid multiple loops. vCount is not that big anyway
                            //for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                            //{
                            //	vh = _VisibleItems[vhIndex];
                            //	if (vh.ItemIndex < indexIfInsertingOrRemoving)
                            //		continue;
                            //	ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                            //}

                            if (vhEndIndexMinus1 < 0)
                                // The views holders to be shifted are all before vp => only shift the indices of all visible items
                                reportedScrollDeltaOverride = .1d;
                            //for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                            //{
                            //	vh = _VisibleItems[vhIndex];
                            //	if (loopItems && vh.ItemIndex < indexIfInsertingOrRemoving)
                            //		continue;
                            //	ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                            //}
                            else
                            {
                                //if (__t_OverrideCTInsetWhenInsertRemove)
                                //	ctInsetFromVPSOverrideToPassAsParam = _InternalState.ctVirtualInsetFromVPS_Cached - deltaSize;
                                //else if (emptyAreaWhenCTSmallerThanVP > 0)
                                //{
                                //	ctInsetFromVPSOverrideToPassAsParam = _InternalState.ctVirtualInsetFromVPS_Cached - deltaSize;
                                //	allowOutsideBounds = true;
                                //}

                                if (vhEndIndex >= this.VisibleItemsCount)
                                    // The new items will be added after LV, so all the currently visible ones will be shifted towards start
                                    this.DragVisibleItemsRangeUnchecked(0, this.VisibleItemsCount, -deltaSize, false, false);
                                //// If looping, only shifting items with bigger ItemIndex, which can be before insertIndex, i.e. shifting indices of the TAIL and everything before it
                                //if (loopItems)
                                //{
                                //	for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                                //	{
                                //		vh = _VisibleItems[vhIndex];
                                //		if (vh.ItemIndex < indexIfInsertingOrRemoving)
                                //			continue;
                                //		ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                                //	}
                                //}
                                ////for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                                ////	ShiftViewsHolderIndex(_VisibleItems[vhIndex], count, true, indexIfInsertingOrRemoving);
                                else
                                {
                                    double insetFromEndForNextNewVH = this._VisibleItems[vhEndIndexMinus1].root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.endEdge);

                                    // Drag towards start the ones before the new items
                                    this.DragVisibleItemsRangeUnchecked(0, vhEndIndex, -deltaSize, false, false);

                                    //for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                                    //{
                                    //	vh = _VisibleItems[vhIndex];
                                    //	if (vh.ItemIndex < indexIfInsertingOrRemoving) 
                                    //		continue;
                                    //	ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                                    //}

                                    // Find the next before viewport (to be recycled, and all vhs before it)
                                    double vhInsetFromEnd;
                                    int    idxOfFirstVHToRecycle;
                                    for (idxOfFirstVHToRecycle = vhEndIndexMinus1; idxOfFirstVHToRecycle >= 0; --idxOfFirstVHToRecycle)
                                    {
                                        vh             = this._VisibleItems[idxOfFirstVHToRecycle];
                                        vhInsetFromEnd = vh.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.endEdge);

                                        if (vhInsetFromEnd > this._InternalState.vpSize) break;
                                    }

                                    if (idxOfFirstVHToRecycle >= 0)              // at least 1 item to recycle that went before VP (otherwise, this would be -1)
                                        vhEndIndex -= idxOfFirstVHToRecycle + 1; // since the vhs from the beginning will be recycled, their position changes in the _VisibleItems array

                                    // Recycle all items that now are before viewport
                                    while (idxOfFirstVHToRecycle >= 0) this.RecycleOrStealViewsHolder(idxOfFirstVHToRecycle--, stealInsteadOfRecycle);

                                    // Extract from the recycler or create new items, until the viewport is filled (not necesarily <count> new items will be shown)
                                    var indexInViewOfFirstItemToBeInserted = indexInViewIfInsertingOrRemoving - 1 + count;
                                    var sizeAddedToContent                 = this.AddViewsHoldersAndMakeVisible(insetFromEndForNextNewVH, this._InternalState.endEdge, vhEndIndex, indexInViewOfFirstItemToBeInserted, count, 0, -1);

                                    // The content needs to be shifted towards start with the same amout it grew, so its end edge will be stationary
                                    //if (true || _InternalState.computeVisibilityTwinPassScheduled)
                                    //{
                                    if (ctInsetFromVPSOverrideToPassAsParam == null)
                                        ctInsetFromVPSOverrideToPassAsParam = this._InternalState.ctVirtualInsetFromVPS_Cached - sizeAddedToContent;
                                    else
                                        ctInsetFromVPSOverrideToPassAsParam -= sizeAddedToContent;
                                    allowOutsideBounds = true;
                                    //}
                                }
                            }
                        }
                    }
                    //// possible looping
                    else
                    {
                        if (emptyAreaWhenCTSmallerThanVP > 0)
                        {
                            ctInsetFromVPSOverrideToPassAsParam = this._InternalState.ctVirtualInsetFromVPS_Cached;
                            allowOutsideBounds                  = true;
                        }

                        if (this.VisibleItemsCount > 0)
                        {
                            int vhStartIndex;
                            vhStartIndex = vhIndexForInsertOrRemove;

                            //// Shift items having their ItemIndex >= insertIndex
                            //for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                            //{
                            //	vh = _VisibleItems[vhIndex];
                            //	if (vh.ItemIndex < indexIfInsertingOrRemoving)
                            //		continue;
                            //	ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                            //}

                            if (vhStartIndex < this.VisibleItemsCount)
                            {
                                if (vhStartIndex < 0)
                                {
                                    // The first item will be inserted before the first VH

                                    reportedScrollDeltaOverride = .1d;
                                    //if (loopItems) 
                                    //{
                                    //	// Keeping items stationary when looping. Their indexInView is also preserved at the begining of this switch case
                                    //}
                                    //else
                                    //{
                                    // The first inserted item may not become visible => shift the existing items towards end and the ComputeVisibility will fill the gaps that'll form at start

                                    this.DragVisibleItemsRangeUnchecked(0, this.VisibleItemsCount, deltaSize, false, false);
                                    //}

                                    //for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                                    //{
                                    //	vh = _VisibleItems[vhIndex];
                                    //	if (loopItems && vh.ItemIndex < indexIfInsertingOrRemoving)
                                    //		continue;
                                    //	ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                                    //}
                                }
                                else
                                {
                                    //if (loopItems && vhStartIndex == 0 && indexIfInsertingOrRemoving == oldCount)
                                    //{

                                    //}
                                    //else
                                    //{
                                    //if (loopItems)
                                    //{
                                    //	for (vhIndex = 0; vhIndex < vhStartIndex; ++vhIndex)
                                    //	{
                                    //		vh = _VisibleItems[vhIndex];
                                    //		if (vh.ItemIndex < indexIfInsertingOrRemoving)
                                    //			continue;
                                    //		ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                                    //	}
                                    //}

                                    double insetFromStartForNextNewVH = this._VisibleItems[vhStartIndex].root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.startEdge);
                                    this.DragVisibleItemsRangeUnchecked(vhStartIndex, this.VisibleItemsCount, deltaSize, false, false);
                                    // Find the next after viewport (to be recycled, and all vhs after it) 
                                    // (update: this is now done for all cases at once at the begining)while also shifting indices of the ones that will remain visible
                                    double vhInsetFromStart;
                                    for (vhIndex = vhStartIndex; vhIndex < this.VisibleItemsCount; ++vhIndex)
                                    {
                                        vh               = this._VisibleItems[vhIndex];
                                        vhInsetFromStart = vh.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.startEdge);

                                        if (vhInsetFromStart > this._InternalState.vpSize) break; // the current and all after it will be recycled.

                                        //// If looping and found an item with ItemIndex<insertIndex, we've eached the "head", and all the following items will have ItemIndex smaller than insertIndex
                                        //if (loopItems && vh.ItemIndex < indexIfInsertingOrRemoving)
                                        //	continue;

                                        //ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                                    }

                                    // Include all items after viewport into the recycle bin
                                    while (vhIndex < this.VisibleItemsCount) this.RecycleOrStealViewsHolder(vhIndex, stealInsteadOfRecycle);

                                    // Extract from the recycler or create new items, until the viewport is filled (not necesarily <count> new items will be shown)
                                    this.AddViewsHoldersAndMakeVisible(insetFromStartForNextNewVH, this._InternalState.startEdge, vhStartIndex, indexInViewIfInsertingOrRemoving, count, 1, 1);
                                    //}
                                }
                            }
                            else
                                // All items to be added are after vp
                                //// Visible items may have indices bigger than insertIndex, if looping
                                //if (loopItems)
                                //{
                                //	for (vhIndex = 0; vhIndex < _VisibleItemsCount; ++vhIndex)
                                //	{
                                //		vh = _VisibleItems[vhIndex];
                                //		if (vh.ItemIndex < indexIfInsertingOrRemoving)
                                //			continue;
                                //		ShiftViewsHolderIndex(vh, count, true, indexIfInsertingOrRemoving);
                                //	}
                                //}
                                reportedScrollDeltaOverride = -.1d;
                        }
                    }

                    if (!allowOutsideBounds)
                        // Fixes bug where if an item is inserted during a pull from the extremity, the content position is corrected, i.e it jumps, which doesn't look right. 
                        // The correction needs to be postponed until after the drag has ended. The ReleaseFromPullManager takes care of that.
                        // TODO also think about the ItemCountChangeMode.REMOVE case - it's a bit more complex
                        if (this._Params.effects.ElasticMovement && this.IsDragging && !(this._Params.ForwardDragToParents && this._NestingManager.CurrentDragCapturedByParent))
                            allowOutsideBounds = true;
                }
                    break;

                // IMGDOC <!image url="$(SolutionDir)\Docs\img\OSA\Insert-Remove-Items-Remove.jpg" scale=".81"/>
                case ItemCountChangeMode.REMOVE:
                {
                    //if (emptyAreaWhenCTSmallerThanVP > 0)
                    //{
                    //	ctInsetFromVPSOverrideToPassAsParam = _InternalState.ctVirtualInsetFromVPS_Cached;
                    //	allowOutsideBounds = true;
                    //}

                    //allowOutsideBounds = true;
                    if (this.VisibleItemsCount > 0)
                    {
                        var startVHIndex              = vhIndexForInsertOrRemove;
                        var endVHIndexExcl            = startVHIndex + count;
                        var endVHIndex                = endVHIndexExcl - 1;
                        var vhsToRemove               = 0; // guaranteed to be >= 0
                        var itemsOutsideVPToBeRemoved = 0;
                        int vhStationaryStartIndex, vhStationaryEndIndexExcl; // stationary in the sense that they don't get moved by deltaSize, only by the correction amount

                        //// No looping
                        if (contentPanelEndEdgeStationary)
                        {
                            if (emptyAreaWhenCTSmallerThanVP > 0)
                            {
                                ctInsetFromVPSOverrideToPassAsParam = this._InternalState.ctVirtualInsetFromVPS_Cached - deltaSize + additionalCTDragAbstrDelta;
                                allowOutsideBounds                  = true;
                            }

                            //int endVHIndexClamped; // last to be removed
                            int startVHIndexClamped;
                            vhStationaryEndIndexExcl = this.VisibleItemsCount;

                            if (endVHIndexExcl > this.VisibleItemsCount) // some are after vp
                            {
                                if (startVHIndex < 0) // the rest are some inside + some before vp => all vhs will be recycled =>  treat is as the RESET case
                                    goto case ItemCountChangeMode.RESET;

                                reportedScrollDeltaOverride = .1d;

                                if (startVHIndex >= this.VisibleItemsCount) // all are after vp
                                {
                                    startVHIndexClamped = this.VisibleItemsCount;
                                    vhsToRemove         = 0;
                                }
                                else // the rest are inside
                                {
                                    startVHIndexClamped = startVHIndex;
                                    vhsToRemove         = this.VisibleItemsCount - startVHIndexClamped;

                                    correctionMayBeNeeded = true;
                                }
                            }
                            else // none are after vp
                            {
                                if (startVHIndex < 0) // some of items are before vp
                                {
                                    startVHIndexClamped = 0;
                                    if (endVHIndex < 0) // all are before vp
                                        vhsToRemove = 0;
                                    else // .. and some are inside vp
                                    {
                                        vhsToRemove = endVHIndexExcl;

                                        reportedScrollDeltaOverride = .1d;
                                        correctionMayBeNeeded       = true;
                                    }
                                }
                                else // all items are inside
                                {
                                    startVHIndexClamped = startVHIndex;
                                    vhsToRemove         = endVHIndexExcl - startVHIndexClamped;

                                    reportedScrollDeltaOverride = .1d;
                                    correctionMayBeNeeded       = true;
                                }
                            }
                            //endVHIndexClamped = startVHIndexClamped + vhsToRemove - 1;

                            // Recycle the removed items 
                            // Note: after this, <startVHIndexClamped> will be the index of the first item after the removed ones
                            while (vhsToRemove-- > 0) this.RecycleOrStealViewsHolder(startVHIndexClamped, stealInsteadOfRecycle);

                            // Drag the ones before the removed items
                            this.DragVisibleItemsRangeUnchecked(0, startVHIndexClamped, -deltaSize + additionalCTDragAbstrDelta, false, false);

                            // Drag any 'stationary' items (i.e. not affected by sizeDelta change - the items after the ones removed) to account for the correction, if any, + shift their indices
                            if (startVHIndexClamped < this.VisibleItemsCount)
                            {
                                if (additionalCTDragAbstrDelta != 0d) this.DragVisibleItemsRangeUnchecked(startVHIndexClamped, this.VisibleItemsCount, additionalCTDragAbstrDelta, false, false);

                                //// TODO remove this check temporarily, until removing with endStat while looping will be implemented
                                //if (!loopItems) // looping is handled for the whole REMOVE case, at the very end of the switch
                                //{
                                for (var i = startVHIndexClamped; i < this.VisibleItemsCount; ++i)
                                {
                                    vh = this._VisibleItems[i];
                                    this.ShiftViewsHolderItemIndexInView(vh, -count);
                                    this.ShiftViewsHolderItemIndexAndFireEvent(vh, -count, false, indexIfInsertingOrRemoving);
                                }
                                //}
                            }
                        }
                        //// Possible looping
                        else
                        {
                            if (emptyAreaWhenCTSmallerThanVP > 0d)
                            {
                                ctInsetFromVPSOverrideToPassAsParam = this._InternalState.ctVirtualInsetFromVPS_Cached + additionalCTDragAbstrDelta;
                                allowOutsideBounds                  = true;
                            }

                            int startVHIndexClamped; // first to be removed
                            vhStationaryStartIndex = 0;

                            var atLeastOneItemsToBeRemovedIsBeforeViewport = startVHIndex < 0;
                            if (atLeastOneItemsToBeRemovedIsBeforeViewport) // some items are before vp
                            {
                                startVHIndexClamped = 0;
                                if (endVHIndex < 0) // all are before vp
                                    itemsOutsideVPToBeRemoved = count;
                                else
                                {
                                    if (endVHIndex < this.VisibleItemsCount) // the rest are inside vp
                                    {
                                        itemsOutsideVPToBeRemoved = -startVHIndex;
                                        correctionMayBeNeeded     = true;
                                    }
                                    else // the rest are some inside + some after vp => all vhs will be recycled =>  treat is as the RESET case
                                        goto case ItemCountChangeMode.RESET;
                                }

                                reportedScrollDeltaOverride = -.1d;
                            }
                            else // none are before vp
                            {
                                if (startVHIndex < this.VisibleItemsCount) // some are inside vp
                                {
                                    startVHIndexClamped = startVHIndex;
                                    if (endVHIndexExcl > this.VisibleItemsCount) // .. and some are after vp
                                        itemsOutsideVPToBeRemoved = endVHIndexExcl - this.VisibleItemsCount;
                                    else // are all inside
                                    {
                                    }

                                    reportedScrollDeltaOverride = -.1d;
                                    correctionMayBeNeeded       = true;
                                }
                                else // all are after vp
                                {
                                    itemsOutsideVPToBeRemoved = count;
                                    startVHIndexClamped       = this.VisibleItemsCount; // no vh will be removed
                                }
                            }
                            vhStationaryEndIndexExcl = startVHIndexClamped;

                            // Add the removed visible vhs to recycle bin
                            vhsToRemove = count - itemsOutsideVPToBeRemoved;
                            while (vhsToRemove-- > 0) this.RecycleOrStealViewsHolder(startVHIndexClamped, stealInsteadOfRecycle);

                            // Drag the stationary items by the correction amount
                            if (additionalCTDragAbstrDelta != 0d) this.DragVisibleItemsRangeUnchecked(vhStationaryStartIndex, vhStationaryEndIndexExcl, additionalCTDragAbstrDelta, false, false);

                            // Drag the items following the removed ones by the size delta + the correction amount, if any, & shift their indices
                            // The one after the last vh to be removed will have the same vhIndex as the first removed
                            if (startVHIndexClamped < this.VisibleItemsCount)
                            {
                                this.DragVisibleItemsRangeUnchecked(startVHIndexClamped, this.VisibleItemsCount, deltaSize + additionalCTDragAbstrDelta, false, false);

                                //if (!loopItems) // looping is handled for the whole REMOVE case, at the very end of the switch
                                //{
                                for (var i = startVHIndexClamped; i < this.VisibleItemsCount; i++)
                                {
                                    vh = this._VisibleItems[i];
                                    this.ShiftViewsHolderItemIndexInView(vh, -count);
                                    this.ShiftViewsHolderItemIndexAndFireEvent(vh, -count, false, indexIfInsertingOrRemoving);
                                }
                                //}
                            }
                        }

                        //if (loopItems)
                        //{
                        //	// Decrementing bigger indices, if present
                        //	// Keeping indexInView for items before the removal indexInView
                        //	for (int i = 0; i < _VisibleItemsCount; i++)
                        //	{
                        //		vh = _VisibleItems[i];
                        //		if (vh.itemIndexInView > indexInViewIfInsertingOrRemoving)
                        //			ShiftViewsHolderItemIndexInView(vh, -count);

                        //		if (vh.ItemIndex < indexIfInsertingOrRemoving)
                        //			continue;

                        //		ShiftViewsHolderItemIndexAndFireEvent(vh, -count, false, indexIfInsertingOrRemoving);
                        //	}

                        //	//correctionMayBeNeeded = true;
                        //}
                    }
                }
                    break;

                case ItemCountChangeMode.RESET:
                    recycleAllViewsHolders = true;
                    if (contentPanelEndEdgeStationary)
                        ctInsetFromVPSOverrideToPassAsParam = this._InternalState.ctVirtualInsetFromVPS_Cached - deltaSize + additionalCTDragAbstrDelta;
                    else
                        ctInsetFromVPSOverrideToPassAsParam = this._InternalState.ctVirtualInsetFromVPS_Cached + additionalCTDragAbstrDelta;
                    break;
            }

            #if DEBUG_CHANGE_COUNT
			double ctInsetEndBefOnSizeChange = _InternalState.CTVirtualInsetFromVPE_Cached;
            #endif

            #if DEBUG_INDICES
			if (GetDebugIndicesString(out debugIndicesString))
				Debug.Log("ChangeCountAft vhs " + _VisibleItemsCount + (recycleAllViewsHolders ? "(allWillBeRecycled)" : "") + ". Indices: " + debugIndicesString);
            #endif
            //Debug.Log("ctInsetFromVPSOverrideToPassAsParam: " + ctInsetFromVPSOverrideToPassAsParam);
            var p = new ContentSizeOrPositionChangeParams
            {
                keepVelocity             = keepVelocity,
                contentEndEdgeStationary = contentPanelEndEdgeStationary,
                contentInsetOverride     = ctInsetFromVPSOverrideToPassAsParam,
                allowOutsideBounds       = allowOutsideBounds,
                // Commented: this is done by ComputeVisibility below
                //fireScrollPositionChangedEvent = true
            };
            this.OnCumulatedSizesOfAllItemsChanged(ref p);

            if (newCount == 0)
            {
                recycleAllViewsHolders = this._Params.optimization.KeepItemsPoolOnEmptyList;
                if (!recycleAllViewsHolders)
                {
                    // If the itemsCount is 0, then in most cases it makes sense to destroy all the views, instead of marking them as recyclable. 
                    // Maybe the ChangeItemCountTo(0) was called in order to clear the current contents
                    this.ClearVisibleItems();
                    this.ClearCachedRecyclableItems();
                }
            }

            if (recycleAllViewsHolders) this.RecycleAllVisibleViewsHolders();

            // TODO check this, the same way as for when changing item's size
            double reportedScrollDelta;
            if (reportedScrollDeltaOverride != null)
                reportedScrollDelta = reportedScrollDeltaOverride.Value;
            else
            {
                if (prevCount == 0)
                    reportedScrollDelta = 0d; // helps with the initial displacement of the content when using CSF and preferEndEdge=false
                else if (contentPanelEndEdgeStationary)
                    reportedScrollDelta = .1d;
                else
                {
                    // If start edge is stationary, either if the content shrinks or expands the reportedDelta should be negative, 
                    // indicating that a fake "slight scroll towards end" was done. This triggers a virtualization of the the content's position correctly to compensate for the new ctEnd 
                    // and makes any item after it be visible again (in the shirnking case) if it was after viewport
                    reportedScrollDelta = -.1d;

                    // ..but if the ctEnd is fully visible, the content will act as it was shrinking with itemEndEdgeStationary=true, because the content's end can't go before vpEnd
                    if (vrtContentPanelIsAtOrBeforeEnd) reportedScrollDelta = .1d;
                }
            }

            // Update 18.02.2019: added "emptyAreaWhenCTSmallerThanVP_After >= 0" because when this is true, additionalCTDragAbstrDelta will be 0, which
            // caused some items to disappear if the suddenly a great number of items were removed from start from index 1 or after (For example, having 128 items, and using RemoveItems(1, 125), 
            // would not compute visibility as needed)
            var emptyAreaWhenCTSmallerThanVP_After = -this._InternalState.VirtualScrollableArea;
            var computeBothWays                    = correctionMayBeNeeded && (additionalCTDragAbstrDelta != 0d || emptyAreaWhenCTSmallerThanVP_After >= 0d);
            var twinPassScheduled                  = this._InternalState.ConsumeFlag_computeVisibilityTwinPassScheduled();
            if (twinPassScheduled)
            {
                //if(_Params.effects.LoopItems && changeMode != ItemCountChangeMode.RESET)
                //{
                //	throw new OSAException(
                //		"OSA.ChangeItemCountInternal: Looping is enabled and twin pass scheduled (you're probably using ContentSizeFitter or similar), but changeMode is " + changeMode +
                //		". In this particular case, only ResetItems can be used to change the count"
                //		);
                //}

                var preferEndStat = this._InternalState.ConsumeFlag_preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass();

                #if DEBUG_CHANGE_COUNT
				double ctInsetBeforeTwinPass = _InternalState.ctVirtualInsetFromVPS_Cached;
				//double ctSizeBeforeTwinPass = _InternalState.ctVirtualSize;
				string str =
					"ctInsetEndBefOnSizeChange " + ctInsetEndBefOnSizeChange.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					", ctInsetFromVPSOverrideToPassAsParam " + ctInsetFromVPSOverrideToPassAsParam +
					", allowOutsideBounds " + allowOutsideBounds +
					", lastVHInsetEnd " + _VisibleItems[_VisibleItemsCount - 1].root.GetInsetFromParentEdge(_Params.Content, _InternalState.endEdge).ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					", ctInsetEnd " + _InternalState.CTVirtualInsetFromVPE_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
                #endif
                this.ComputeVisibilityTwinPass(preferEndStat);

                #if DEBUG_CHANGE_COUNT
				double ctInsetDeltaFromTwinPass = _InternalState.ctVirtualInsetFromVPS_Cached - ctInsetBeforeTwinPass;
				double ctSizeDeltaFromTwinPass = _InternalState.ctVirtualSize;
				str += ", lastVHInsetEnd aft " + _VisibleItems[_VisibleItemsCount - 1].root.GetInsetFromParentEdge(_Params.Content, _InternalState.endEdge).ToString(OSAConst.DEBUG_FLOAT_FORMAT);
				str += ", ctInsetEnd aft " + _InternalState.CTVirtualInsetFromVPE_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
				str +=
					"\n(ctInsetDelta " + ctInsetDeltaFromTwinPass.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					", ctSizeDelta " + ctSizeDeltaFromTwinPass.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					"), reportedDelta " + reportedScrollDelta.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					", computeBothWays " + computeBothWays;

				Debug.Log(str);
                #endif
            }
            //else
            //	_InternalState.lastComputeVisibilityHadATwinPass = false;

            //Debug.Log("correctionMayBeNeeded " + correctionMayBeNeeded +
            //	", computeBothWays " + computeBothWays +
            //	", ctDelta " + additionalCTDragAbstrDelta +
            //	", reportedScrollDelta " + reportedScrollDelta
            //	);

            // Bugfix items displaced when cutting huge amounts from content, bringing it smaller than vp, and gravity != start
            if (correctionMayBeNeeded) this.CorrectPositionsOfVisibleItems(true, false);
            //CorrectPositionsOfVisibleItemsUsingDefaultSizeRetrievingMethod(true);

            this.ComputeVisibilityForCurrentPositionRawParams(false, true, reportedScrollDelta);
            if (computeBothWays) this.ComputeVisibilityForCurrentPositionRawParams(false, true, -reportedScrollDelta);
            //Debug.Log(str);

            // Correcting & firing PosChanged event
            this.CorrectPositionsOfVisibleItems(true, true);

            //if (changeMode == ItemCountChangeMode.INSERT || changeMode == ItemCountChangeMode.REMOVE)
            //	CorrectPositionsOfVisibleItemsUsingDefaultSizeRetrievingMethod(true);

            if (keepVelocity) this._Velocity = velocity;

            this.OnItemsRefreshed(prevCount, newCount);
            if (this.ItemsRefreshed != null) this.ItemsRefreshed(prevCount, newCount);

            this._SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;
        }

        /// <summary>Called by MonoBehaviour.Update</summary>
        private void MyUpdate()
        {
            if (this._InternalState.computeVisibilityTwinPassScheduled) throw new OSAException(OSAConst.EXCEPTION_SCHEDULE_TWIN_PASS_CALL_ALLOWANCE);

            var rebuildNeeded = this._ForceRebuildLayoutScheduled || this._InternalState.HasScrollViewSizeChanged;
            if (rebuildNeeded)
            {
                this.ForceRebuildLayout();
                return;
            }

            this._NavigationManager.OnUpdate();

            ////bool startSnappingIfNeeded = !IsDragging && !_SkipComputeVisibilityInUpdateOrOnScroll && _Params.Snapper;
            //if (_InternalState.updateRequestPending)
            //{
            //	// TODO See if need to skip modifying updateRequestPending if _SkipComputeVisibility is true

            //	// ON_SCROLL is the only case when we don't regularly update and are using only onScroll event to ComputeVisibility
            //	_InternalState.updateRequestPending = _Params.optimization.updateMode != BaseParams.UpdateModeEnum.ON_SCROLL;
            //	if (!_SkipComputeVisibilityInUpdateOrOnScroll)
            //	{
            //		ComputeVisibilityForCurrentPosition(false, false);

            //		//startSnappingIfNeeded = _Params.Snapper != null;
            //		//if (_Params.Snapper)// && !scrollviewSizeChanged)
            //		//	_Params.Snapper.StartSnappingIfNeeded();
            //	}
            //}
            ////if (startSnappingIfNeeded)
            ////	_Params.Snapper.StartSnappingIfNeeded();
        }

        #if DEBUG_UPDATE
		string prev_UpdateDebugString;
        #endif

        #if DEBUG_INDICES
		string prev_IndicesDebugString;

		bool GetDebugIndicesString(out string debugIndicesString)
		{
			debugIndicesString = 
					//"ctSize " + _InternalState.ctVirtualSize.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					"RFirst " + _ItemsDesc.realIndexOfFirstItemInView +
					//", cumuSizeAll " + _ItemsDesc.CumulatedSizeOfAllItems.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					"; " + _ItemsDesc.itemsCount + " items: ";

			var phantomVHS = new List<TItemViewsHolder>(_VisibleItems);
			for (int i = 0; i < Math.Min(20, _ItemsDesc.itemsCount); i++)
			{
				bool vis = false;
				//int visIdx = -1;
				int selfR = -1;
				for (int j = 0; j < _VisibleItemsCount; j++)
				{
					var vh = _VisibleItems[j];
					if (vh.itemIndexInView == i)
					{
						phantomVHS.Remove(vh);
						selfR = vh.ItemIndex;
						vis = true;
						//visIdx = j;
						break;
					}
				}

				int r = _ItemsDesc.GetItemRealIndexFromViewIndex(i);
				debugIndicesString += (vis ? "<b>" : "") + i + "R" + r + 
					(vis ? (/*"V"+ visIdx +*/ (selfR == r ? "" : "<color=red>SR</color>" + selfR) + "</b>, ") : ", "
				);
				// + "insetV " + _InternalState.GetItemVirtualInsetFromParentStartUsingItemIndexInView(i).ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
				//", insetR " + _InternalState.GetItemInferredRealInsetFromParentStart(i).ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
				//", size " + _ItemsDesc[i].ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
				//", cumuSize " + _ItemsDesc.GetItemSizeCumulative(i).ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			}
			if (phantomVHS.Count > 0)
			{
				debugIndicesString += "|| PhantomVHs: ";
				for (int i = 0; i < phantomVHS.Count; i++)
					debugIndicesString += phantomVHS[i] + ", ";
			}

			if (debugIndicesString == prev_IndicesDebugString)
			{
				debugIndicesString = null;
				return false;
			}
			prev_IndicesDebugString = debugIndicesString;
			return true;
		}
        #endif

        #if DEBUG_CONTENT_VISUALLY
		RectTransform _ContentPanelVisualization;
        #endif

        private void MyLateUpdate()
        {
            var releasingFromOutsideBoundsPull_wasInProgress = this._ReleaseFromPull.inProgress;
            var vsa                                          = this._InternalState.VirtualScrollableArea;
            var emptyAreaWhenCTSmallerThanVP                 = -vsa;
            var ctSmallerThanVP                              = emptyAreaWhenCTSmallerThanVP > 0d;
            var emptyAreaWhenCTSmallerThanVPClamped          = Math.Max(0d, emptyAreaWhenCTSmallerThanVP);

            var snapper                     = this._Params.Snapper;
            var currentFrameHadNoSizeChages = this._InternalState.totalNumberOfSizeChangesLastFrame == this._InternalState.totalNumberOfSizeChanges;
            var startSnappingIfNeeded =
                !ctSmallerThanVP
                && !releasingFromOutsideBoundsPull_wasInProgress
                && !this.IsDragging
                && !this._ReleaseFromPull.IsPulled()
                && !this._SkipComputeVisibilityInUpdateOrOnScroll
                && currentFrameHadNoSizeChages
                && snapper;
            var isSnapping = false;
            if (startSnappingIfNeeded)
            {
                snapper.StartSnappingIfNeeded();
                isSnapping = snapper.SnappingInProgress;
            }

            this.UpdateGalleryEffectIfNeeded(true);

            var dt                     = this.DeltaTime;
            var canChangeVelocity      = true;
            var velocity               = this._Velocity[this._InternalState.hor0_vert1];
            var allowOutsideBoundsMode = AllowContentOutsideBoundsMode.DO_NOT_ALLOW;
            var dragUnchecked          = false;
            // TODO think if it eases the looping or not to clamp using the last item's inset instead of the content's. 

            #if DEBUG_UPDATE
			string debugString = null;
			if (debug_Update)
			{
				debugString = 
					"vNormPos " + GetVirtualAbstractNormalizedScrollPosition() +
					"vsa " + vsa +
					", ctSize " + _InternalState.ctVirtualSize.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					", ctInsetCached " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					", realIndexOfFirst " + _ItemsDesc.realIndexOfFirstItemInView +
					", cumuSizeAll " + _ItemsDesc.CumulatedSizeOfAllItems.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
					", " + _ItemsDesc.itemsCount + " items: ";

				for (int i = 0; i < Math.Min(20, _ItemsDesc.itemsCount); i++)
				{
					debugString +=
						"\n" + i + "(R" + _ItemsDesc.GetItemRealIndexFromViewIndex(i) +
						"): insetV " + _InternalState.GetItemVirtualInsetFromParentStartUsingItemIndexInView(i).ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
						", insetR " + _InternalState.GetItemInferredRealInsetFromParentStart(i).ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
						", size " + _ItemsDesc[i].ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
						", cumuSize " + _ItemsDesc.GetItemSizeCumulative(i).ToString(OSAConst.DEBUG_FLOAT_FORMAT);
				}

				debugString += "\n-- "+ _VisibleItemsCount + " vhs: ";
				if (_VisibleItemsCount > 0)
				{
					for (int i = 0; i < Math.Min(10, _VisibleItemsCount); i++)
					{
						var itemIndexInView = _VisibleItems[i].itemIndexInView;
						debugString += 
							"\n" + itemIndexInView + "(R" + _ItemsDesc.GetItemRealIndexFromViewIndex(itemIndexInView) + 
							"): insetV " + _InternalState.GetItemVirtualInsetFromParentStartUsingItemIndexInView(itemIndexInView).ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
							", insetR " + _InternalState.GetItemInferredRealInsetFromParentStart(itemIndexInView).ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
							", size " + _ItemsDesc[itemIndexInView].ToString(OSAConst.DEBUG_FLOAT_FORMAT);
					}
				}
			}
            #endif

            #if DEBUG_INDICES
			string debugIndicesString = null;
			if (debug_Indices)
			{
				if (GetDebugIndicesString(out debugIndicesString))
					Debug.Log(debugIndicesString);
			}
            #endif

            if (ctSmallerThanVP)
            {
                this._ReleaseFromPull.targetCTInsetFromVPS = this._InternalState.GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP(emptyAreaWhenCTSmallerThanVPClamped);

                if (this._IsDragging)
                {
                    this._ReleaseFromPull.inProgress               = false;
                    this._Velocity[this._InternalState.hor0_vert1] = 0f;
                }
                else
                {
                    dragUnchecked = true;
                    //var firstVH = _VisibleItems[0];
                    //float firstItemInsetFromVPS = _VisibleItems[0].root.GetInsetFromParentEdge(Parameters.content, _InternalState.startEdge);

                    // Bugfix 07.05.2021: When there are no items, we still want to be able to drag the content. For ex.,
                    // maybe the user listens to OSA's position changes and moves external objects based on that, even
                    // when it's empy (hint: the OSAContentDecorator component does this)
                    float smoothDampCurrentValueToGive;
                    float smoothDampTargetValueToGive;
                    if (this.VisibleItemsCount > 0)
                    {
                        smoothDampCurrentValueToGive = (float)this._ReleaseFromPull.CalculateFirstItemInsetFromVPS();
                        smoothDampTargetValueToGive  = (float)this._ReleaseFromPull.CalculateFirstItemTargetInsetFromVPS();
                    }
                    else
                    {
                        smoothDampCurrentValueToGive = (float)this._InternalState.ctVirtualInsetFromVPS_Cached;
                        smoothDampTargetValueToGive  = (float)this._ReleaseFromPull.targetCTInsetFromVPS;
                    }
                    this._ReleaseFromPull.inProgress = Math.Abs(smoothDampCurrentValueToGive - smoothDampTargetValueToGive) >= 1d;

                    if (this._Params.effects.ElasticMovement)
                    {
                        var velocityAbstr = velocity * this._InternalState.hor1_vertMinus1;

                        /*float nextCTInsetF = */
                        Mathf.SmoothDamp(smoothDampCurrentValueToGive, smoothDampTargetValueToGive, ref velocityAbstr, this._Params.effects.ReleaseTime, float.PositiveInfinity, dt);
                        //Debug.Log(velocity);
                        // End if the drag distance would be close to zero
                        if (this._ReleaseFromPull.inProgress)
                            velocity = velocityAbstr * this._InternalState.hor1_vertMinus1;
                        else
                            velocity = 0f;
                    }
                    else
                    {
                        if (this._ReleaseFromPull.inProgress)
                        {
                            // Bugfix 10.01.2022: if there are no items visible, there's nothing to drag (thanks to 'BAIZOR')
                            if (this.VisibleItemsCount > 0)
                                this._ReleaseFromPull.FinishNowByDraggingItems(
                                    // Bugfix xx.yy.2018: Disappearing items that are outside vp on pointer up
                                    true
                                );
                            else
                                this._ReleaseFromPull.FinishNowBySettingContentInset(false);
                        }
                        velocity = 0f;
                    }

                    this._Velocity[this._InternalState.hor0_vert1] = velocity;
                }
            }
            else
            {
                if (this._IsDragging || isSnapping)
                    this._ReleaseFromPull.inProgress = false;
                else
                {
                    var    currentInset = this._InternalState.ctVirtualInsetFromVPS_Cached;
                    double absDisplacement;
                    var    displacedFromStart = currentInset > 0d;
                    if (displacedFromStart)
                    {
                        absDisplacement                            = currentInset;
                        this._ReleaseFromPull.targetCTInsetFromVPS = 0d;
                    }
                    else
                    {
                        var currentInsetEnd  = this._InternalState.CTVirtualInsetFromVPE_Cached;
                        var displacedFromEnd = currentInsetEnd > 0d;
                        if (displacedFromEnd)
                        {
                            absDisplacement                            = currentInsetEnd;
                            this._ReleaseFromPull.targetCTInsetFromVPS = -vsa;
                        }
                        else
                            absDisplacement = 0d;
                    }
                    var displacementExists = absDisplacement > 0d;

                    var clampManually = false;
                    this._ReleaseFromPull.inProgress = displacementExists;
                    if (this._Params.effects.ElasticMovement)
                    {
                        var zeroVelocity = false;
                        if (this._ReleaseFromPull.inProgress)
                        {
                            allowOutsideBoundsMode = AllowContentOutsideBoundsMode.ALLOW_IF_OUTSIDE_AMOUNT_SHRINKS;
                            canChangeVelocity      = false;

                            // If statement commented: This wasn't necessary in testing (it also cut the release-from-pull animation),
                            // but can be uncommented back if future bugs are found
                            //if (_VisibleItemsCount > 0)
                            //{
                            var    pullDistanceF = absDisplacement > float.MaxValue ? float.MaxValue : absDisplacement;
                            double smoothDampCurrentValueToGive;
                            if (displacedFromStart)
                                // Exemplifying the horizontal with pull from start: the content needs to be shifted to the left => velocity decrease => smoothDampCurrentValueToGive is 
                                // set as positive in order to obtain a negative velocity with SmoothDamp (from positive to 0 you need a negative velocity)
                                smoothDampCurrentValueToGive = pullDistanceF * this._InternalState.hor1_vertMinus1;
                            else
                                smoothDampCurrentValueToGive = -pullDistanceF * this._InternalState.hor1_vertMinus1;

                            var nextPullDistanceF = Mathf.SmoothDamp((float)smoothDampCurrentValueToGive, 0f, ref velocity, this._Params.effects.ReleaseTime, float.PositiveInfinity, dt);

                            // Clamp to zero inset start or end if the distance is close to zero
                            this._ReleaseFromPull.inProgress = Mathf.Abs(nextPullDistanceF) >= 1f;
                            if (this._ReleaseFromPull.inProgress)
                                this._Velocity[this._InternalState.hor0_vert1] = velocity;
                            else
                                clampManually = zeroVelocity = true;
                            //}
                            //else
                            //{
                            //	clampManually = zeroVelocity = true;
                            //}
                        }
                        else
                        {
                            zeroVelocity = releasingFromOutsideBoundsPull_wasInProgress;
                            // In case the applied velocity made the pull distance too negative (ideally, it'll be 0)
                            clampManually     = releasingFromOutsideBoundsPull_wasInProgress && absDisplacement < .1d;
                            canChangeVelocity = !clampManually;
                            if (!clampManually) allowOutsideBoundsMode = AllowContentOutsideBoundsMode.ALLOW;
                        }

                        if (zeroVelocity) this._Velocity[this._InternalState.hor0_vert1] = velocity = 0f;
                    }
                    else if (this._Params.effects.LoopItems)
                    {
                        allowOutsideBoundsMode = AllowContentOutsideBoundsMode.ALLOW;
                        if (displacementExists)
                        {
                            // All items are visible and they're scrollable (the ct size bigger than vp => don't clamp them)
                            if (this.VisibleItemsCount == this._ItemsDesc.itemsCount && vsa > 0d)
                            {
                            }
                            else
                                //// Bugfix: on fast scrolling and/or on low-framerate, sometimes all vhs go outside the vp, 
                                //// so even if looping, the content needs to be clamped & computevisibility needs to correct the positions
                                //if (_VisibleItemsCount == 0 && _ItemsDesc.itemsCount > 0)
                                clampManually = true;
                        }
                    }
                    else
                    {
                        if (displacementExists) clampManually = true;
                    }

                    if (clampManually)
                        //canDrag = false; // no dragging
                        this._ReleaseFromPull.FinishNowBySettingContentInset(true);
                }
            }

            if (this._Params.effects.Inertia && !isSnapping)
            {
                var velocityFactor = Mathf.Pow(1f - this._Params.effects.InertiaDecelerationRate, dt);
                if (this._IsDragging)
                {
                    // The longer the drag lasts, the less previous velocity will be added up to the curent on drag end
                    this._VelocityToAddOnDragEnd *= velocityFactor;

                    var magVelocityToAdd = this._VelocityToAddOnDragEnd.magnitude;
                    if (magVelocityToAdd < 1f)
                        this._VelocityToAddOnDragEnd = Vector2.zero;
                    else
                    {
                        var magVelocityToAddToMaxVelocity                                    = magVelocityToAdd / this._Params.effects.MaxSpeed;
                        if (magVelocityToAddToMaxVelocity > 1f) this._VelocityToAddOnDragEnd /= magVelocityToAddToMaxVelocity;
                    }
                }
                else if (canChangeVelocity)
                {
                    if (Mathf.Abs(velocity) < 2f)
                    {
                        this._Velocity[this._InternalState.hor0_vert1] = 0f;

                        // The content's speed decreases with each second, according to inertiaDecelerationRate
                        var transvIdx = 1 - this._InternalState.hor0_vert1;
                        this._Velocity[transvIdx] *= velocityFactor;
                    }
                    else
                        // The content's speed decreases with each second, according to inertiaDecelerationRate
                        this._Velocity *= velocityFactor;
                }
            }
            #if DEBUG_UPDATE
			if (debug_Update)
			{
				float velocityAbstr = velocity * _InternalState.hor1_vertMinus1;
				float dragPerFrame = velocityAbstr * dt;
				debugString +=
					"\n_IsDragging " + _IsDragging +
					", velocityAbstr " + velocityAbstr +
					", dragPerFrame " + dragPerFrame +
					", nvelocityToAddOnDragEnd " + _VelocityToAddOnDragEnd;
			}
            #endif
            this.Velocity = this._Velocity; // will clamp it
            velocity      = this._Velocity[this._InternalState.hor0_vert1];

            if (!this._IsDragging && !isSnapping && velocity != 0f)
            {
                var velocityAbstr = velocity * this._InternalState.hor1_vertMinus1;
                var dragPerFrame  = velocityAbstr * (double)dt;
                //if (Math.Abs(dragPerFrame) > .001d)
                if (Math.Abs(velocityAbstr) > .001d)
                {
                    #if DEBUG_UPDATE
					if (debug_Update)
					{
						debugString +=
							"\nvelocityAbstr " + velocityAbstr.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
							", dragPerFrame " + dragPerFrame.ToString(OSAConst.DEBUG_FLOAT_FORMAT) + 
							", dragUnchecked " + dragUnchecked;
					}
                    #endif

                    if (dragUnchecked)
                        this.DragVisibleItemsRangeUnchecked(0,
                            this.VisibleItemsCount,
                            dragPerFrame,
                            true,
                            true); // bugfix for disappearing items that are outside vp on pointer up
                    else
                    {
                        bool _, __;
                        this.Drag(dragPerFrame, allowOutsideBoundsMode, false, out _, out __);
                    }
                }
            }

            //// Bugfix: when removing large amounts of items and gravity != start, the remaining items are displaced
            //if (releasingFromOutsideBoundsPull_wasInProgress)
            //	CorrectPositionsOfVisibleItemsUsingDefaultSizeRetrievingMethod(true);

            this._InternalState.totalNumberOfSizeChangesLastFrame = this._InternalState.totalNumberOfSizeChanges;

            #if DEBUG_UPDATE
			if (debug_Update && debugString != prev_UpdateDebugString)
				Debug.Log(prev_UpdateDebugString = debugString);
            #endif

            #if DEBUG_CONTENT_VISUALLY
			if (debug_ContentVisually)
			{
				if (_ContentPanelVisualization == null)
				{
					_ContentPanelVisualization = new GameObject("ContentVisualization").AddComponent<RectTransform>();
					var img = _ContentPanelVisualization.gameObject.AddComponent<Image>();
					img.CrossFadeAlpha(.15f, 1f, true);
					_ContentPanelVisualization.SetParent(_Params.ScrollViewRT, false);
					_ContentPanelVisualization.SetAsFirstSibling();
				}
				else if (!_ContentPanelVisualization.gameObject.activeSelf)
					_ContentPanelVisualization.gameObject.SetActive(true);

				var ins = _InternalState.ctVirtualInsetFromVPS_Cached + 
					(_Params.Viewport == _Params.ScrollViewRT ? 
						0d
						: _Params.Viewport.GetInsetFromParentEdge(_Params.ScrollViewRT, _InternalState.startEdge)
					);
				_ContentPanelVisualization.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(
					_InternalState.startEdge,
					ins > float.MaxValue ? float.MaxValue : (float)ins,
					_InternalState.ctVirtualSize > float.MaxValue ? float.MaxValue : (float)_InternalState.ctVirtualSize
				);
			}
			else if (_ContentPanelVisualization != null && _ContentPanelVisualization.gameObject.activeSelf)
				_ContentPanelVisualization.gameObject.SetActive(false);
            #endif
        }

        private void MyOnDisable()
        {
            // Bugfix 11.04.2019 (thanks justtime (Unity forum)).
            // Disabling the GameObject or the script should clear the animation coroutines and other types of animations
            this.CancelAllAnimations();

            // Bugfix: if the routine is stopped, this is not restored back. Setting it to false is the best thing we can do
            this._SkipComputeVisibilityInUpdateOrOnScroll = false;
        }

        private void OnScrollPositionChangedInternal()
        {
            this.UpdateGalleryEffectIfNeeded(false);

            var normPos = this.GetNormalizedPosition();
            this.OnScrollPositionChanged(normPos);

            if (this.ScrollPositionChanged != null) this.ScrollPositionChanged(normPos);
        }

        private double GetDeltaForComputeVisibility()
        {
            return this._InternalState.ctVirtualInsetFromVPS_Cached - this._InternalState.lastProcessedCTVirtualInsetFromVPS;
        }

        private bool ComputeVisibilityForCurrentPosition(ComputeVisibilityParams p)
        {
            if (p.overrideDelta != null) return this.ComputeVisibilityForCurrentPositionRawParams(p.forceFireScrollPositionChangedEvent, p.potentialTwinPassCTEndStationaryPrioritizeUserPreference, p.overrideDelta.Value);

            return this.ComputeVisibilityForCurrentPosition(p.forceFireScrollPositionChangedEvent, p.potentialTwinPassCTEndStationaryPrioritizeUserPreference);
        }

        private bool ComputeVisibilityForCurrentPositionRawParams(bool forceFireScrollViewPositionChangedEvent, bool potentialTwinPassCTEndStationaryPrioritizeUserPreference, double overrideScrollingDelta)
        {
            var curInset = this._InternalState.ctVirtualInsetFromVPS_Cached;
            this._InternalState.lastProcessedCTVirtualInsetFromVPS = curInset - overrideScrollingDelta;
            return this.ComputeVisibilityForCurrentPosition(forceFireScrollViewPositionChangedEvent, potentialTwinPassCTEndStationaryPrioritizeUserPreference);
        }

        private bool ComputeVisibilityForCurrentPosition(bool forceFireScrollViewPositionChangedEvent, bool potentialTwinPassCTEndStationaryPrioritizeUserPreference)
        {
            if (this._InternalState.computeVisibilityTwinPassScheduled) throw new OSAException(OSAConst.EXCEPTION_SCHEDULE_TWIN_PASS_CALL_ALLOWANCE);

            var delta = this.GetDeltaForComputeVisibility();

            //if (forcePreTwinPass)
            //{
            //	ComputeVisibilityTwinPass(delta);
            //	GetDeltaForComputeVisibility();
            //}

            var velocityToSet = this._Velocity;

            var looped                                 = false;
            if (this._Params.effects.LoopItems) looped = this.LoopIfNeeded(delta);

            this._ComputeVisibilityManager.ComputeVisibility(delta);
            var twinPassScheduled = this._InternalState.ConsumeFlag_computeVisibilityTwinPassScheduled();
            if (twinPassScheduled)
            {
                var preferEndStat            = this._InternalState.ConsumeFlag_preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass();
                var contentEndEdgeStationary = potentialTwinPassCTEndStationaryPrioritizeUserPreference || delta == 0d ? preferEndStat : delta > 0d;

                #if DEBUG_COMPUTE_VISIBILITY_TWIN
				string debugString =
					"preferEndStat " + preferEndStat +
					", endEdgeStatFinal " + contentEndEdgeStationary + (preferEndStat != contentEndEdgeStationary ? "(delta " + delta.ToString(OSAConst.DEBUG_FLOAT_FORMAT) + ")" : "") +
					", ctInsetBef " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
				Debug.Log("|---PreTwinPass: " + debugString);
                #endif
                if (delta == 0d) delta = -.1d;

                bool ctSizeChanged;
                var  maxIterations = 20;
                var  iter          = 0;
                do
                {
                    this.ComputeVisibilityTwinPass(contentEndEdgeStationary);
                    ctSizeChanged = false;
                    var ctSizeBef = this._InternalState.ctVirtualSize;
                    this._ComputeVisibilityManager.ComputeVisibility(delta);
                    this._ComputeVisibilityManager.ComputeVisibility(-delta);
                    var twinPassScheduledInner = this._InternalState.ConsumeFlag_computeVisibilityTwinPassScheduled();
                    if (twinPassScheduledInner)
                    {
                        this.ComputeVisibilityTwinPass(contentEndEdgeStationary);
                        // Ignore subsequent twin pass requests for the current function call
                        this._InternalState.ConsumeFlag_computeVisibilityTwinPassScheduled();
                    }

                    ctSizeChanged = ctSizeBef != this._InternalState.ctVirtualSize;

                    ++iter;
                    if (iter == maxIterations)
                        throw new OSAException(
                            "Max iterations (" + maxIterations + ") reached for TwinPass. \n" + "If you're using ContentSizeFitter, make sure the DefaultItemSize is smaller than the size of any generated item.\n" + "If you're also using BaseParamsWithPrefab for the params, DefaultItemSize will be automatically set to the prefab's size, " + "so in this case make the prefab as small as possible instead."
                        );
                } while (ctSizeChanged);
            }
            //else
            //	_InternalState.lastComputeVisibilityHadATwinPass = false;

            this._InternalState.UpdateLastProcessedCTVirtualInsetFromVPStart();

            if (!this.IsDragging) // if dragging, the velocity is not needed
                this._Velocity = velocityToSet;

            if (forceFireScrollViewPositionChangedEvent || delta != 0d) this.OnScrollPositionChangedInternal();

            return looped;
        }

        private void ComputeVisibilityTwinPass(bool contentEndEdgeStationary)
        {
            if (this.VisibleItemsCount == 0) throw new OSAException("computeVisibilityTwinPassScheduled, but there are no visible items." + OSAConst.EXCEPTION_SCHEDULE_TWIN_PASS_CALL_ALLOWANCE);

            var itCount = this.GetItemsCount();
            if (this._Params.effects.LoopItems && itCount > OSAConst.MAX_ITEMS_WHILE_LOOPING_TO_ALLOW_TWIN_PASS)
                throw new OSAException(
                    "If looping is enabled, ComputeVisibilityTwinPass can only be used if item count is less than " + OSAConst.MAX_ITEMS_WHILE_LOOPING_TO_ALLOW_TWIN_PASS + " (currently having " + itCount + "). This prevents UI overlaps due to rounding errors"
                );

            // Prevent onValueChanged callbacks from being processed when setting inset and size of content
            var ignoreOnScroll_valueBefore = this._SkipComputeVisibilityInUpdateOrOnScroll;
            this._SkipComputeVisibilityInUpdateOrOnScroll = true;

            //Canvas.ForceUpdateCanvases();

            // Caching the sizes before disabling the CSF, because Unity 2017.2 suddenly decided that's a good idea to resize the item to its original size after the CSF is disabled
            var                      sizes = new double[this.VisibleItemsCount];
            TItemViewsHolder         v;
            Action<TItemViewsHolder> sizeChangeCallback;

            if (this._Params.IsHorizontal)
                sizeChangeCallback = this.OnItemWidthChangedPreTwinPass;
            else
                sizeChangeCallback = this.OnItemHeightChangedPreTwinPass;

            #if DEBUG_COMPUTE_VISIBILITY_TWIN
			string debugString = "|---TwinPass: ";
            #endif
            for (var i = 0; i < this.VisibleItemsCount; ++i)
            {
                v = this._VisibleItems[i];
                #if DEBUG_COMPUTE_VISIBILITY_TWIN
				debugString += "\n" + i + ": " + v.root.rect.size[_InternalState.hor0_vert1].ToString(OSAConst.DEBUG_FLOAT_FORMAT) + " -> ";
                #endif
                sizes[i] = this.UpdateItemSizeOnTwinPass(v);
                #if DEBUG_COMPUTE_VISIBILITY_TWIN
				debugString += sizes[i].ToString(OSAConst.DEBUG_FLOAT_FORMAT);
                #endif
                sizeChangeCallback(v);
            }

            ////bool endEdgeStationary = delta > 0d;
            //bool preferEndStat = _InternalState.preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass;
            ////bool contentEndEdgeStationary = delta == 0d ? preferEndStat : delta > 0d;
            //bool contentEndEdgeStationary = preferEndStat;

            #if DEBUG_COMPUTE_VISIBILITY_TWIN
			debugString +=
				"\ncontentEndEdgeStationary " + contentEndEdgeStationary +
				", ctInsetBef " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
				", ctInsetEndBef " + _InternalState.CTVirtualInsetFromVPE_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
            #endif

            this.OnItemsSizesChangedExternally(this._VisibleItems, sizes, contentEndEdgeStationary);

            #if DEBUG_COMPUTE_VISIBILITY_TWIN
			debugString +=
				", ctInsetAft " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
				", ctInsetEndAft " + _InternalState.CTVirtualInsetFromVPE_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			Debug.Log(debugString);
            #endif

            this._SkipComputeVisibilityInUpdateOrOnScroll = ignoreOnScroll_valueBefore;

            //_InternalState.lastComputeVisibilityHadATwinPass = true;
        }

        /// <summary>
        /// Should only be called once, in ComputeVisibilityForCurrentPosition()!
        /// Assigns pev to the pointer event data, if a pointer was touching the scroll view before virtualizing. 
        /// Will return false if it did not try to retrieve the pev
        /// </summary>
        private bool LoopIfNeeded(double delta)
        {
            if (delta == 0d) return false;

            var vsa = this._InternalState.VirtualScrollableArea;
            if (vsa <= 0d) // nothing to loop through, since ctsize<=vpsize
                return false;

            ContentSizeOrPositionChangeParams p;
            if (this.VisibleItemsCount == 0)
            {
                if (this._ItemsDesc.itemsCount == 0) return false;

                //double ctAmountOutside = -_InternalState.ctVirtualInsetFromVPS_Cached;

                // Because of high jumps that are optimized by recycling all visible items (confirmed) or a very high speed (not confirmed, but seems similar), 
                // vhs can end up being outside the viewport. In this case, we wait for them to appear in next frames
                return false;

                //double targetCTInsetFromVPS = _InternalState.paddingContentStart;
                //p = new ContentSizeOrPositionChangeParams
                //{
                //	allowOutsideBounds = true,
                //	contentInsetOverride = targetCTInsetFromVPS,
                //	keepVelocity = true
                //};
            }
            else
            {
                p = new()
                {
                    allowOutsideBounds = true,
                    keepVelocity       = true,
                    // Commented: this is done by CorrectPositions at the end of this method
                    //fireScrollPositionChangedEvent = true
                };

                var negativeScroll       = delta <= 0d;
                var firstVH              = this._VisibleItems[0];
                var lastVH               = this._VisibleItems[this.VisibleItemsCount - 1];
                int firstVH_IndexInView  = firstVH.itemIndexInView, lastVH_IndexInView = lastVH.itemIndexInView;
                var firstVHIsFirstInView = firstVH_IndexInView == 0;
                var lastVHIsLastInView   = lastVH_IndexInView == this._ItemsDesc.itemsCount - 1;

                double firstVisibleItemAmountOutside = 0d, lastVisibleItemAmountOutside = 0d;
                int    newRealIndexOfFirstItemInView;

                if (negativeScroll) // going towards end
                {
                    // There are more items after the last
                    if (!lastVHIsLastInView) return false;

                    //// Commented: this blocks scrolling when there's a high speed drag
                    // Only loop if there's at least 1 item that's not visible
                    if (firstVHIsFirstInView)
                    {
                        // Even if the first vh is last in view, it may be outside the viewport completely due to a high speed drag
                        firstVisibleItemAmountOutside = -firstVH.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.startEdge);
                        if (firstVisibleItemAmountOutside <= 0d) return false;
                    }

                    // Only loop after the last item is completely inside the viewport
                    lastVisibleItemAmountOutside = -lastVH.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.endEdge);
                    if (lastVisibleItemAmountOutside > 0d) return false;

                    newRealIndexOfFirstItemInView = firstVH.ItemIndex;
                    //newRealIndexOfFirstItemInView = _InternalState.GetItemRealIndexFromViewIndex(0);

                    // Adjust the itemIndexInView for the visible items. they'll be the last ones, so the last one of them will have, for example, viewIndex = itemsCount-1
                    for (var i = 0; i < this.VisibleItemsCount; ++i) this._VisibleItems[i].itemIndexInView = i;
                }
                else // going towards start
                {
                    // There are more items before the first
                    if (!firstVHIsFirstInView) return false;

                    // Only loop if there's at least 1 item that's entirely not visible
                    if (lastVHIsLastInView)
                    {
                        // Even if the last vh is last in view, it may be outside the viewport completely due to a high speed drag
                        lastVisibleItemAmountOutside = -lastVH.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.endEdge);
                        if (lastVisibleItemAmountOutside <= 0d) return false;
                    }

                    // Only loop after the first item is completely inside the viewport
                    firstVisibleItemAmountOutside = -firstVH.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.startEdge);
                    if (firstVisibleItemAmountOutside > 0d) return false;

                    // The next item after this will become the first one in view
                    newRealIndexOfFirstItemInView = this._ItemsDesc.GetItemRealIndexFromViewIndex(lastVH_IndexInView + 1);
                    //newRealIndexOfFirstItemInView = _InternalState.GetItemRealIndexFromViewIndex(_ItemsDescriptor.itemsCount - 1);

                    // Adjust the itemIndexInView for the visible items
                    for (var i = 0; i < this.VisibleItemsCount; ++i) this._VisibleItems[i].itemIndexInView = this._ItemsDesc.itemsCount - this.VisibleItemsCount + i;
                }

                this._ItemsDesc.RotateItemsSizesOnScrollViewLooped(newRealIndexOfFirstItemInView);

                #if DEBUG_LOOPING
			string debugString = null;
			debugString += 
				"Looped: vhs " + _VisibleItemsCount + 
				(lastVHIsLastInView ? 
					", lastVHIsLastInView, amountOutside " + lastVisibleItemAmountOutside.ToString(OSAConst.DEBUG_FLOAT_FORMAT)  
					: ", firstVHIsFirstInView, amountOutside " + firstVisibleItemAmountOutside.ToString(OSAConst.DEBUG_FLOAT_FORMAT)) +
				", newRealIndexofFirst " + newRealIndexOfFirstItemInView +
				", ctInsetCached_Before " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT) +
				", cumuSizeAll " + _ItemsDesc.CumulatedSizeOfAllItems.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
                #endif
            }

            this.UpdateCTVrtInsetFromVPS(ref p);

            #if DEBUG_LOOPING
			debugString += ", ctInsetCached_After " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			Debug.Log(debugString);
            #endif

            // The visible items are displaced now, so correct their positions
            //CorrectPositionsOfVisibleItemsUsingDefaultSizeRetrievingMethod(true);
            this.CorrectPositionsOfVisibleItems(true, true);

            this._InternalState.UpdateLastProcessedCTVirtualInsetFromVPStart();

            return true;
        }

        /// <summary>Don't abuse this method. See why in the description of <see cref="InternalState{TItemViewsHolder}.CorrectPositions(List{TItemViewsHolder}, bool)"/></summary>
        private void CorrectPositionsOfVisibleItems(bool alsoCorrectTransversalPositioning, bool fireScrollPositionChangedEvent) //bool itemEndEdgeStationary)
        {
            // Update the positions of the visible items so they'll retain their position relative to the viewport
            if (this.VisibleItemsCount > 0) this._InternalState.CorrectPositions(this._VisibleItems, alsoCorrectTransversalPositioning); //, itemEndEdgeStationary);

            if (fireScrollPositionChangedEvent) this.OnScrollPositionChangedInternal();
        }

        internal TItemViewsHolder ExtractRecyclableViewsHolderOrCreateNew(int indexOfItemThatWillBecomeVisible, double sizeOfItem)
        {
            // First choice recycleable VHs
            var vh = this.TryExtractRecyclableViewsHolderFrom(this._RecyclableItems, indexOfItemThatWillBecomeVisible, sizeOfItem);

            // Second choice: buffered recycleable VHs
            if (vh == null) vh = this.TryExtractRecyclableViewsHolderFrom(this._BufferredRecyclableItems, indexOfItemThatWillBecomeVisible, sizeOfItem);

            // The only remaining choice: create it
            if (vh == null) vh = this.CreateViewsHolder(indexOfItemThatWillBecomeVisible);

            return vh;
        }

        private TItemViewsHolder TryExtractRecyclableViewsHolderFrom(IList<TItemViewsHolder> vhsToChooseFrom, int indexOfItemThatWillBecomeVisible, double sizeOfItem)
        {
            var i = 0;
            while (i < vhsToChooseFrom.Count)
            {
                var vh = vhsToChooseFrom[i];
                if (this.IsRecyclable(vh, indexOfItemThatWillBecomeVisible, sizeOfItem))
                {
                    this.OnBeforeRecycleOrDisableViewsHolder(vh, indexOfItemThatWillBecomeVisible);

                    // Commented: not needed for now. Current tests show no misplacements
                    //// This prepares the item to be further adjusted. If the item is way too far outside the content's panel,
                    //// some floatpoint precision can be lost when modifying its anchor[Min/Max]
                    //vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(_InternalState.startEdge, 0f, (float)sizeOfItem);

                    vhsToChooseFrom.RemoveAt(i);
                    return vh;
                }
                ++i;
            }

            return null;
        }

        internal void AddViewsHolderAndMakeVisible(TItemViewsHolder vh, int vhIndex, int itemIndex, int itemIndexInView, double realInsetFromEdge, RectTransform.Edge insetEdge, double size)
        {
            // Add it in list at [end]
            this._VisibleItems.Insert(vhIndex, vh);
            ++this.VisibleItemsCount;

            // Update its index
            if (itemIndexInView < 0 || itemIndexInView >= this._ItemsDesc.itemsCount) throw new OSAException("OSA internal error: itemIndexInView " + itemIndexInView + ", while itemsCount is " + this._ItemsDesc.itemsCount);

            vh.ItemIndex       = itemIndex;
            vh.itemIndexInView = itemIndexInView;

            // Make sure it's parented to content panel
            var nlvRT = vh.root;
            //if (size > 190)
            //	Debug.LogWarning("size "+ size +", "+ nlvRT.rect.height + ", " + vh.ItemIndex);
            //if (nlvRT.rect.height > 190)
            //	throw new Exception(nlvRT.rect.height + ", " + vh.ItemIndex);
            nlvRT.SetParent(this._Params.Content, false);
            //if (nlvRT.rect.height > 190)
            //	throw new Exception(nlvRT.rect.height + ", " + vh.ItemIndex);

            // Make sure its GO is activated
            this.SetViewsHolderEnabled(vh);

            // Update its views
            this.UpdateViewsHolder(vh);

            // GO should remain activated
            if (!this.IsViewsHolderEnabled(vh))
            {
                var midSentence = this._Params.optimization.ScaleToZeroInsteadOfDisable ? "have a zero scale" : "be disabled";
                throw new OSAException(
                    "AddViewsHolderAndMakeVisible: VH detected to "
                    + midSentence
                    + " after UpdateViewsHolder() was called on it. This is not allowed. "
                    + vh.root);
            }

            // Make sure it's left-top anchored (the need for this arose together with the feature for changind an item's size 
            // (an thus, the content's size) externally, using RequestChangeItemSizeAndUpdateLayout)
            nlvRT.anchorMin = nlvRT.anchorMax = this._InternalState.layoutInfo.constantAnchorPosForAllItems;

            // TODO make it as a parameter, turned off as default. Maybe the users want to see the views holders in order in hierarchy
            //if (negativeScroll) nlvRT.SetAsLastSibling();
            //else nlvRT.SetAsFirstSibling();
            if (this._Params.optimization.KeepItemsSortedInHierarchy)
                if (vhIndex < this._Params.Content.childCount) // even if not found while testing, taking additional measures in case vhIndex may be bigger
                    nlvRT.SetSiblingIndex(vhIndex);

            //if (negativeScroll)
            //	currentVirtualInsetFromCTSToUseForNLV = negCurrentVrtInsetFromCTSToUseForNLV_posCurrentVrtInsetFromCTEToUseForNLV;
            //else
            //	currentVirtualInsetFromCTSToUseForNLV = _InternalState.contentPanelVirtualSize - nlvSize - negCurrentVrtInsetFromCTSToUseForNLV_posCurrentVrtInsetFromCTEToUseForNLV;

            //float inset = nlvRT.GetInsetFromParentEdge(_Params.Content, insetEdge);
            //string bef = "inset " + inset+ ", h " + nlvRT.rect.height + ", anchorMin " + nlvRT.anchorMin + ", anchorMax " + nlvRT.anchorMax;
            nlvRT.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(
                insetEdge,
                (float)realInsetFromEdge,
                (float)size
            );

            //inset = nlvRT.GetInsetFromParentEdge(_Params.Content, insetEdge);
            //if (nlvRT.rect.height > 190)
            //	throw new Exception("inset " + inset + ", h " + nlvRT.rect.height + ", " + vh.ItemIndex + ", size " + size + ", anchorMin " + nlvRT.anchorMin + ", anchorMax " + nlvRT.anchorMax +
            //		"\n bef: " + bef);

            // Commented: using cumulative sizes
            //negCurrentInsetFromCTSToUseForNLV_posCurrentInsetFromCTEToUseForNLV += nlvSizePlusSpacing;
            float tInsetStartToUse;
            float tSizeToUse;
            this._InternalState.GetTransversalInsetStartAndSizeToUse(vh, out tInsetStartToUse, out tSizeToUse);
            //Assure transversal size and transversal position based on parent's padding and width settings
            nlvRT.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(this._InternalState.transvStartEdge,
                tInsetStartToUse,
                tSizeToUse
            );
        }

        /// <summary>Returns the content size delta, even if less than 'maxCount' vhs were added </summary>
        private double AddViewsHoldersAndMakeVisible(
            double             firstVHInsetFromEdge,
            RectTransform.Edge insetEdge,
            int                vhStartIndex,
            int                startIndexInView,
            int                maxCount,
            int                vhIndexIncrement,
            int                itemIndexInViewIncrementSign
        )
        {
            #if DEBUG_ADD_VHS
			string debugString = "AddingVHs: start View"+ startIndexInView + "VH" + vhStartIndex + ", incr View" + itemIndexInViewIncrementSign + "VH" + vhIndexIncrement + ": ";
            #endif
            TItemViewsHolder vhToUse = null;
            int              indexOfItemThatWillBecomeVisible;
            //double vhInsetFromEdge = firstVHInsetFromEdge, itemSize = _Params.DefaultItemSize;
            double vhInsetFromEdge    = firstVHInsetFromEdge, itemSize;
            var    sizeAddedToContent = 0d;
            //int numberOfKnownSizes = 0;
            for (int iAbs = 0, vhIndex = vhStartIndex, iIdxView = startIndexInView;
                vhInsetFromEdge <= this._InternalState.vpSize && iAbs < maxCount;
                ++iAbs, vhIndex += vhIndexIncrement, iIdxView += itemIndexInViewIncrementSign)
            {
                itemSize = this._ItemsDesc[iIdxView];

                indexOfItemThatWillBecomeVisible = this._ItemsDesc.GetItemRealIndexFromViewIndex(iIdxView);
                vhToUse                          = this.ExtractRecyclableViewsHolderOrCreateNew(indexOfItemThatWillBecomeVisible, itemSize);
                this.AddViewsHolderAndMakeVisible(vhToUse, vhIndex, indexOfItemThatWillBecomeVisible, iIdxView, vhInsetFromEdge, insetEdge, itemSize);
                vhInsetFromEdge += itemSize + this._InternalState.spacing;

                //sizeAddedToContent += itemSize + _InternalState.spacing;
                //++numberOfKnownSizes;

                #if DEBUG_ADD_VHS
				debugString += "i" + iAbs + "VH" + vhIndex + "View" + iIdxView + "R" + indexOfItemThatWillBecomeVisible;
                #endif
            }

            #if DEBUG_ADD_VHS
			Debug.Log(debugString);
            #endif
            var sizeCumulativeUntilStartIncl                       = 0d;
            if (startIndexInView > 0) sizeCumulativeUntilStartIncl = this._ItemsDesc.GetItemSizeCumulative(startIndexInView - 1, false);

            var sizeCumulativeUntilEndIncl = this._ItemsDesc.GetItemSizeCumulative(startIndexInView + maxCount - 1, false);
            var itemSizesCumulativeDelta   = sizeCumulativeUntilEndIncl - sizeCumulativeUntilStartIncl;

            // Update: now sizes are retrieved from ItemsDesc directly, since non-default item sizes could've been set in CollectItemsSizes
            //// Add the remaining size using the default item size
            //sizeAddedToContent += (_Params.DefaultItemSize + _InternalState.spacing) * (maxCount - numberOfKnownSizes);
            sizeAddedToContent = itemSizesCumulativeDelta + this._InternalState.spacing * maxCount;

            return sizeAddedToContent;
        }

        internal int GetNumExcessRecycleableItems()
        {
            // It's important to keep this at 1, because
            // 1. the original reason (at least 1 item cached)
            // 2. because of how the item stealing works (it inserts an item at the head of the list)
            if (this._RecyclableItems.Count > 1)
            {
                var maxToKeepInMemory = this.GetMaxNumObjectsToKeepInMemory();
                var excess            = this._RecyclableItems.Count + this.VisibleItemsCount - maxToKeepInMemory;
                if (excess > 0) return excess;
            }

            return 0;
        }

        private int GetMaxNumObjectsToKeepInMemory()
        {
            var binCapacity = this._Params.optimization.RecycleBinCapacity;
            if (binCapacity > 0) return binCapacity + this.VisibleItemsCount;

            return this._ItemsDesc.maxVisibleItemsSeenSinceLastScrollViewSizeChange + this._ItemsDesc.destroyedItemsSinceLastScrollViewSizeChange + 1;
        }

        /// <summary>
        /// Utility method to create buffered recycleable items (which aren't directly destroyed).
        /// It simply calls <see cref="CreateViewsHolder(int)"/> <paramref name="count"/> times.
        /// <para><paramref name="indexToPass"/> can be specified in case you want additional 
        /// information to be passed to <see cref="CreateViewsHolder(int)"/> during this. 
        /// Use negative values, to distinguish it from the regular calls OSA does to <see cref="CreateViewsHolder(int)"/>. If not specified, -1 is passed.</para>
        /// <para>Make sure you adapt your code in <see cref="CreateViewsHolder(int)"/> to support a negative index being passed!</para>
        /// <para>An example where different negative values for the index are useful is when you have multiple prefabs and want to distinguish between them</para>
        /// <para>Pass the returned list to <see cref="AddBufferredRecycleableItems(IList{TItemViewsHolder})"/></para>
        /// </summary>
        protected internal IList<TItemViewsHolder> CreateBufferredRecycleableItems(int count, int indexToPass = -1)
        {
            var vhs = new List<TItemViewsHolder>(count);
            for (var i = 0; i < count; i++)
            {
                var vh = this.CreateViewsHolder(indexToPass);
                vh.root.SetParent(this._Params.Content, false);
                this.SetViewsHolderDisabled(vh);
                vhs.Add(vh);
            }

            return vhs;
        }

        /// <summary>See <see cref="CreateBufferredRecycleableItems(int, int)"/>. You can also pass the buffered Views Holders directly, if you make sure to initialize them properly</summary>
        protected internal void AddBufferredRecycleableItems(IList<TItemViewsHolder> vhs)
        {
            foreach (var vh in vhs) this.AddBufferredRecycleableItem(vh);
        }

        /// <summary>Same as <see cref="AddBufferredRecycleableItems(IList{TItemViewsHolder})"/></summary>
        protected internal void AddBufferredRecycleableItem(TItemViewsHolder vh)
        {
            this._BufferredRecyclableItems.Add(vh);
        }

        private void UpdateGalleryEffectIfNeeded(bool onlyIfEffectAmountChanged)
        {
            var sameAmount = this._PrevGalleryEffectAmount == this._Params.effects.Gallery.OverallAmount;
            if (sameAmount && onlyIfEffectAmountChanged) return;

            if (this._Params.effects.Gallery.OverallAmount == 0f)
            {
                if (sameAmount) return;

                // Make sure the items in the recycle bin don't preserve the local scale from the gallery effect
                this.RemoveGalleryEffectFromItems(this._RecyclableItems, false);
                this.RemoveGalleryEffectFromItems(this._BufferredRecyclableItems, false);
                this.RemoveGalleryEffectFromItems(this._VisibleItems, true);
            }
            else
            {
                if (this.VisibleItemsCount == 0 || this._ItemsDesc.itemsCount == 0) return;

                //double halfVPSize = _InternalState.vpSize / 2;
                //double vpPivotInsetFromStart = _Params.effects.Gallery.Scale.ViewportPivot * _InternalState.vpSize;
                for (var i = 0; i < this.VisibleItemsCount; i++)
                {
                    var    vh                           = this._VisibleItems[i];
                    double vhRealInsetStart             = vh.root.GetInsetFromParentEdge(this._Params.Content, this._InternalState.startEdge);
                    var    vhCenterRealInsetFromStart   = vhRealInsetStart + this._ItemsDesc[vh.itemIndexInView] / 2d;
                    var    vhCenterRealInsetFromStart01 = vhCenterRealInsetFromStart / this._InternalState.vpSize;
                    vhCenterRealInsetFromStart01 = Math.Min(1d, Math.Max(0d, vhCenterRealInsetFromStart01));

                    double vhDistFromVPPivot01Signed;
                    vh.root.localScale       = this.ComposeGalleryEffectFinalAmount(this._Params.effects.Gallery.Scale, vhCenterRealInsetFromStart01, out vhDistFromVPPivot01Signed);
                    vh.root.localEulerAngles = this.ComposeGalleryEffectFinalAmount(this._Params.effects.Gallery.Rotation, vhCenterRealInsetFromStart01, out vhDistFromVPPivot01Signed);

                    // [-.5f, .5f] => [-1f, 1f] for easier processing
                    var itemCenterDistFromStart01            = vhCenterRealInsetFromStart01;
                    var itemCenterPosRelativeToPivot01Signed = vhDistFromVPPivot01Signed * 2f;
                    this.OnApplyCustomGalleryEffects(vh, itemCenterDistFromStart01, itemCenterPosRelativeToPivot01Signed);
                }
            }
            this._PrevGalleryEffectAmount = this._Params.effects.Gallery.OverallAmount;
        }

        private Vector3 ComposeGalleryEffectFinalAmount(GalleryAnimation effectParams, double vhCenterRealInsetFromStart01, out double vhDistFromVPPivot01Signed)
        {
            var vpPivot             = effectParams.ViewportPivot;
            var diff                = vhCenterRealInsetFromStart01 - vpPivot;
            var diffSign            = Math.Sign(diff);
            var vhDistFromVPPivot01 = Math.Abs(diff);
            vhDistFromVPPivot01       = Math.Min(2d, vhDistFromVPPivot01);
            vhDistFromVPPivot01Signed = diffSign * vhDistFromVPPivot01;

            // vhDistFromVPPivot01 needs to be scaled to [0, 1] space, if it's not already, so it needs a divider.
            // Function for the divider: y = |x - .5| + .5, where x = vpPivot
            // Table:
            // -1 -> 2
            // 0  -> 1
            // .5 -> .5
            // 1  -> 1
            // 2  -> 2
            var divider = Math.Abs(vpPivot - .5d) + .5d;
            vhDistFromVPPivot01 /= divider;
            var effectFactor01 = 1f - (float)vhDistFromVPPivot01;

            var exp = Mathf.Clamp(effectParams.Exponent, 1f, GalleryAnimation.MAX_EFFECT_EXPONENT);
            effectFactor01 = Mathf.Pow(effectFactor01, exp);
            effectFactor01 = Mathf.Clamp01(effectFactor01);
            var effAmount = effectParams.Amount * this._Params.effects.Gallery.OverallAmount;
            if (effectParams == this._Params.effects.Gallery.Scale)
            {
                var regularValue = Vector3.one;
                var value        = effectParams.TransformSpace.Transform(effectFactor01);
                var minValue     = this._Params.effects.Gallery.Scale.MinValue;
                value = new(Mathf.Max(minValue, value.x), Mathf.Max(minValue, value.y), Mathf.Max(minValue, value.z));
                return Vector3.Lerp(regularValue, value, effAmount);
            }
            else
            {
                var regularValue = Vector3.zero;
                var value        = Quaternion.Lerp(Quaternion.Euler(effectParams.TransformSpace.From), Quaternion.Euler(effectParams.TransformSpace.To), effectFactor01);
                var euler        = value.eulerAngles;
                value = Quaternion.Lerp(Quaternion.Euler(regularValue), value, effAmount);
                return value.eulerAngles;
            }
        }

        private void RemoveGalleryEffectFromItems(IList<TItemViewsHolder> vhs, bool areEnabled)
        {
            if (vhs == null) return;

            var scaleToZeroInsteadOfDisable                   = this._Params.optimization.ScaleToZeroInsteadOfDisable;
            var targetLocalScaleIfScaleToZeroInsteadOfDisable = areEnabled ? Vector3.one : Vector3.zero;
            foreach (var vh in vhs)
                if (vh != null && vh.root)
                {
                    if (scaleToZeroInsteadOfDisable)
                    {
                        if (vh.root.localScale != targetLocalScaleIfScaleToZeroInsteadOfDisable) vh.root.localScale = targetLocalScaleIfScaleToZeroInsteadOfDisable;
                    }
                    else
                        vh.root.localScale = Vector3.one;
                    vh.root.localEulerAngles = Vector3.zero;

                    this.OnRemoveCustomGalleryEffects(vh);
                }
        }

        // TODO merge this with UpdateCTInset...
        private void OnCumulatedSizesOfAllItemsChanged(ref ContentSizeOrPositionChangeParams p)
        {
            this._InternalState.ctVirtualSize = this._InternalState.CalculateContentVirtualSize();

            //Debug.Log("OnCumulatedSizesOfAllItemsChanged: verify _ReleaseFromPullCurrentState.inProgress");
            this._ReleaseFromPull.inProgress = false;
            this.UpdateCTVrtInsetFromVPS(ref p);

            this._InternalState.totalNumberOfSizeChanges++;
        }

        /// <summary><paramref name="viewsHolder"/> will be null if the item is not visible</summary>
        /// <returns>the resolved size, as this may be a bit different than the passed <paramref name="requestedSize"/> for huge data sets (>100k items)</returns>
        private double ChangeItemSizeAndUpdateContentSizeAccordingly(TItemViewsHolder viewsHolder, int itemIndexInView, double curSize, double requestedSize, bool itemEndEdgeStationary)
        {
            var     deltaSize = requestedSize - curSize;
            var     newCTSize = this._InternalState.ctVirtualSize + deltaSize;
            double? _;
            var     additionalCTDragAbstrDelta = 0d;
            this._InternalState.CorrectParametersOnCTSizeChange(itemEndEdgeStationary, out _, ref additionalCTDragAbstrDelta, newCTSize, deltaSize);

            var resolvedSize = requestedSize;

            // Index of VH if visible; 
            // Otherwise, -1 if it's before the viewport, or VisibleItemsCount if it's after the viewport
            int indexOfVHIfVisible_ElseExtremityExcl;

            if (viewsHolder == null)
            {
                //resolvedSize = requestedSize;

                // Bugfix 27.08.2019 (thanks Gladyon (Unity forum)): resizing items outside viewport didn't work properly sometimes
                var firstVisibleItemIndexInView = this._VisibleItems[0].itemIndexInView;
                if (itemIndexInView < firstVisibleItemIndexInView)
                    indexOfVHIfVisible_ElseExtremityExcl = -1;
                else
                {
                    var lastVisibleItemIndexInView = this._VisibleItems[this.VisibleItemsCount - 1].itemIndexInView;
                    if (itemIndexInView > lastVisibleItemIndexInView)
                        indexOfVHIfVisible_ElseExtremityExcl = this.VisibleItemsCount;
                    else
                        throw new OSAException(
                            "ChangeItemSizeAndUpdateContentSizeAccordingly: the item with itemIndexInView " + itemIndexInView + " is neither visible, nor before, nor after the visible items?"
                        );
                }
            }
            else
            {
                if (viewsHolder.root == null)
                    throw new OSAException(
                        "ChangeItemSizeAndUpdateContentSizeAccordingly: Unexpected state: ViewsHolder not found among visible items. " + "Shouldn't happen if implemented according to documentation/examples"
                    );

                RectTransform.Edge edge;
                float              realInsetToSet;
                if (itemEndEdgeStationary)
                {
                    edge = this._InternalState.endEdge;
                    //realInsetToSet = (float)(_InternalState.GetItemInferredRealInsetFromParentEnd(itemIndexInView) + additionalCTDragAbstrDelta);
                    realInsetToSet = (float)(viewsHolder.root.GetInsetFromParentEdge(this._Params.Content, edge) - additionalCTDragAbstrDelta);
                }
                else
                {
                    edge = this._InternalState.startEdge;
                    //realInsetToSet = (float)(_InternalState.GetItemInferredRealInsetFromParentStart(itemIndexInView) - additionalCTDragAbstrDelta);
                    realInsetToSet = (float)(viewsHolder.root.GetInsetFromParentEdge(this._Params.Content, edge) + additionalCTDragAbstrDelta);
                }
                viewsHolder.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(edge, realInsetToSet, (float)requestedSize);

                //// Even though we know the desired size, the one actually set by the UI system may be different, so we cache that one
                //resolvedSize = _InternalState.getRTCurrentSizeFn(viewsHolder.root);
                ////viewsHolder.cachedSize = resolvedSize;

                indexOfVHIfVisible_ElseExtremityExcl = this._VisibleItems.IndexOf(viewsHolder);
            }

            // All other items need to be moved(in the mose general case), because most of them won't get recycled
            if (itemEndEdgeStationary)
            {
                this.DragVisibleItemsRangeUnchecked(0, indexOfVHIfVisible_ElseExtremityExcl, -deltaSize + additionalCTDragAbstrDelta, false, false);

                if (additionalCTDragAbstrDelta != 0d) this.DragVisibleItemsRangeUnchecked(indexOfVHIfVisible_ElseExtremityExcl + 1, this.VisibleItemsCount, additionalCTDragAbstrDelta, false, false);
            }
            else
            {
                this.DragVisibleItemsRangeUnchecked(indexOfVHIfVisible_ElseExtremityExcl + 1, this.VisibleItemsCount, deltaSize + additionalCTDragAbstrDelta, false, false);

                if (additionalCTDragAbstrDelta != 0d) this.DragVisibleItemsRangeUnchecked(0, indexOfVHIfVisible_ElseExtremityExcl, additionalCTDragAbstrDelta, false, false);
            }

            this._ItemsDesc.BeginChangingItemsSizes(itemIndexInView);
            this._ItemsDesc[itemIndexInView] = resolvedSize;
            this._ItemsDesc.EndChangingItemsSizes();

            var p = new ContentSizeOrPositionChangeParams
            {
                computeVisibilityParams        = this._ComputeVisibilityParams_Reusable_Empty,
                fireScrollPositionChangedEvent = true,
                keepVelocity                   = true,
                allowOutsideBounds             = true,
                contentEndEdgeStationary       = itemEndEdgeStationary,
                //contentInsetOverride = ctInsetFromVPSOverride
            };
            this.OnCumulatedSizesOfAllItemsChanged(ref p);

            return resolvedSize;
        }

        /// <summary>
        /// Assuming that vhs.Count is > 0. IMPORTANT: vhs should be in order (their itemIndexInView 
        /// should be in ascending order - not necesarily consecutive)
        /// </summary>
        private void OnItemsSizesChangedExternally(List<TItemViewsHolder> vhs, double[] sizes, bool itemEndEdgeStationary)
        {
            if (this._ItemsDesc.itemsCount == 0) throw new OSAException("Cannot change item sizes externally if the items count is 0!");

            var              vhsCount = vhs.Count;
            int              viewIndex;
            TItemViewsHolder vh;
            //var insetEdge = itemEndEdgeStationary ? endEdge : startEdge;
            //float currentSize;
            var ctSizeBefore = this._InternalState.CalculateContentVirtualSize();

            var firstVHIndexInView = vhs[0].itemIndexInView;

            #if DEBUG_INDICES
			string debugIndicesString;
			if (GetDebugIndicesString(out debugIndicesString))
				Debug.Log("OnExtCh " + vhs.Count + ", firstIdx "+ firstVHIndexInView + ". Indices: " + debugIndicesString);
            #endif

            //bool doAnotherPass;
            //int i = 0;
            //int iterations = 0;
            //do
            //{
            //	doAnotherPass = false;
            //	int prevViewIndex = -1;
            //	_ItemsDesc.BeginChangingItemsSizes(firstVHIndexInView + i);
            //	for (; i < vhsCount; ++i)
            //	{
            //		vh = vhs[i];
            //		viewIndex = vh.itemIndexInView;

            //		if (viewIndex < prevViewIndex) // looping and found the HEAD after some of the items at the begining => do another pass
            //		{
            //			//throw new OSAException(
            //			//	"OSA.OnItemsSizesChangedExternally: Internal exception. Please report this. Looping=" + _Params.effects.LoopItems);

            //			doAnotherPass = true;
            //			break;
            //		}
            //		// Commented: adapting to Unity 2017.2 breaking the ContentSizeFitter for us... when it's disabled, the object's size returns to the one before resizing. Pretty bad. Oh well..
            //		// Now the sizes are retrieved before disabling the CSF and passed to this method
            //		//currentSize = _GetRTCurrentSizeFn(vh.root);
            //		//_ItemsDesc[viewIndex] = currentSize;

            //		try
            //		{
            //			_ItemsDesc[viewIndex] = sizes[i];

            //		}
            //		catch
            //		{
            //			int x = 0;
            //			Debug.LogError("asd");
            //		}
            //		prevViewIndex = viewIndex;
            //	}
            //	_ItemsDesc.EndChangingItemsSizes();
            //	++iterations;

            //	if (iterations > 2)
            //		throw new OSAException(
            //			"OSA.OnItemsSizesChangedExternally: Internal exception. Please report this. Done " + iterations +
            //			" iterations for changing items' sizes, while only 2 should've been done. Looping=" + _Params.effects.LoopItems);
            //} while (doAnotherPass);

            this._ItemsDesc.BeginChangingItemsSizes(firstVHIndexInView);
            for (var i = 0; i < vhsCount; ++i)
            {
                vh        = vhs[i];
                viewIndex = vh.itemIndexInView;
                // Commented: adapting to Unity 2017.2 breaking the ContentSizeFitter for us... when it's disabled, the object's size returns to the one before resizing. Pretty bad. Oh well..
                // Now the sizes are retrieved before disabling the CSF and passed to this method
                //currentSize = _GetRTCurrentSizeFn(vh.root);
                //_ItemsDesc[viewIndex] = currentSize;
                this._ItemsDesc[viewIndex] = sizes[i];
            }
            this._ItemsDesc.EndChangingItemsSizes();

            var     ctSizeAfter = this._InternalState.CalculateContentVirtualSize();
            var     deltaSize   = ctSizeAfter - ctSizeBefore;
            double? _;
            var     additionalCTDragAbstrDelta = 0d;
            this._InternalState.CorrectParametersOnCTSizeChange(itemEndEdgeStationary, out _, ref additionalCTDragAbstrDelta, ctSizeAfter, deltaSize);

            var newCTInset = this._InternalState.ctVirtualInsetFromVPS_Cached;

            // Preparing the first visible item to be used in calculating the new ctInsetStart
            // if ct top is stationary, ctinsetStart won't need to be modified
            if (itemEndEdgeStationary) newCTInset -= deltaSize;
            //DragVisibleItemsRangeUnchecked(0, 1, -deltaSize + additionalCTDragAbstrDelta);
            //else
            //{
            //	//DragVisibleItemsRangeUnchecked(0, 1, -deltaSize + additionalCTDragAbstrDelta);
            //}

            newCTInset += additionalCTDragAbstrDelta;

            #if DEBUG_ON_SIZES_CHANGED_EXTERNALLY
			string str = "OnSizesChExt: additionalCTDragAbstrDelta " + additionalCTDragAbstrDelta.ToString("###################0.####");
			str += "\nlastVHInsetEnd before OnCumuChanged " + _VisibleItems[_VisibleItemsCount - 1].root.GetInsetFromParentEdge(_Params.Content, _InternalState.endEdge).ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			str += "\nctInsetEnd before OnSizesChanged " + _InternalState.CTVirtualInsetFromVPE_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			str += "\nctInset before OnSizesChanged " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			str += "\nnewCTInsetToSet " + newCTInset.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			str += "\nctSize Before BeginChanging " + ctSizeBefore.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			str += ", ctSize After EndChanging " + ctSizeAfter.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
            #endif

            var p = new ContentSizeOrPositionChangeParams
            {
                cancelSnappingIfAny      = true,
                keepVelocity             = true,
                allowOutsideBounds       = true,
                contentEndEdgeStationary = itemEndEdgeStationary,
                contentInsetOverride     = newCTInset,
                // Commented: this is done by CorrectPositionsOfVisibleItems below
                //fireScrollPositionChangedEvent = true
            };
            this.OnCumulatedSizesOfAllItemsChanged(ref p);

            #if DEBUG_ON_SIZES_CHANGED_EXTERNALLY
			str += ", lastVHInsetEnd after OnSizesChanged " + _VisibleItems[_VisibleItemsCount - 1].root.GetInsetFromParentEdge(_Params.Content, _InternalState.endEdge).ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			str += ", ctInsetEnd after OnSizesChanged " + _InternalState.CTVirtualInsetFromVPE_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
			str += ", ctInset after OnSizesChanged " + _InternalState.ctVirtualInsetFromVPS_Cached.ToString(OSAConst.DEBUG_FLOAT_FORMAT);
            #endif

            //CorrectPositionsOfVisibleItems(true, indexInView => sizes[indexInView - firstVHIndexInView]);
            this.CorrectPositionsOfVisibleItems(true, true);

            #if DEBUG_ON_SIZES_CHANGED_EXTERNALLY
			str += ", lastVHInsetEnd after CorrectPos " + _VisibleItems[_VisibleItemsCount - 1].root.GetInsetFromParentEdge(_Params.Content, _InternalState.endEdge);
			Debug.Log(str);
            #endif
        }

        /// <summary>Needed so <see cref="ScrollViewSizeChanged"/> is called before everything else</summary>
        private void OnScrollViewSizeChangedBase()
        {
            if (this.ScrollViewSizeChanged != null) this.ScrollViewSizeChanged();

            this.OnScrollViewSizeChanged();
        }

        /// <summary>
        /// This is just a shortcut that detects if there are gaps at start/end of the viewport that should be filled with items.
        /// Ideally, we shouldn't need this, but as OSA grew more complex over time it became much harder to prevent some subtile 
        /// glitches than to "patch" them. In any case, performance is still a priority, so most of the time this method has no overhead.
        /// This was initially needed to solve some gaps at start/end of the viewport when caling 
        /// <see cref="SetVirtualAbstractNormalizedScrollPosition(double, bool, out bool, bool)"/> and having items of different sizes.
        /// </summary>
        private void RecomputeVisibility_Auto(out bool looped)
        {
            looped = false;

            // CT smaller than VP => nothing to recompute
            if (this._InternalState.VirtualScrollableArea <= 0f) return;

            if (this.GetItemsCount() == 0) return;

            bool computeNegative = false, computePositive = false;
            if (this.VisibleItemsCount == 0)
                computeNegative = computePositive = true;
            else
            {
                var firstVH                                                                               = this._VisibleItems[0];
                var firstVHRealInsetStart                                                                 = this.GetItemRealInsetFromParentStart(firstVH.root);
                if (firstVHRealInsetStart > this._InternalState.paddingContentStart + 1f) computePositive = true;

                if (this.GetItemsCount() > 1)
                {
                    var lastVH                                                                           = this._VisibleItems[this.VisibleItemsCount - 1];
                    var lastVHRealInsetEnd                                                               = this.GetItemRealInsetFromParentEnd(lastVH.root);
                    if (lastVHRealInsetEnd > this._InternalState.paddingContentEnd + 1f) computeNegative = true;
                }
            }

            if (computeNegative) looped = this.ComputeVisibilityForCurrentPositionRawParams(false, true, -.1f);

            if (computePositive) looped = this.ComputeVisibilityForCurrentPositionRawParams(false, true, +.1f) || looped;
        }

        private void ForceRebuildLayout()
        {
            if (this._InternalState.computeVisibilityTwinPassScheduled) throw new OSAException(OSAConst.EXCEPTION_SCHEDULE_TWIN_PASS_CALL_ALLOWANCE);

            this._ForceRebuildLayoutScheduled = false;
            this._Rebuilding                  = true;
            //_InternalState.layoutIsBeingRebuildDueToScrollViewSizeChangeEvent = true;
            this.OnScrollViewSizeChangedBase();
            this.RebuildLayoutDueToScrollViewSizeChange();

            this.Refresh(false, true);
            this._Rebuilding = false;

            this.PostRebuildLayoutDueToScrollViewSizeChange();
            //ChangeItemsCount(ItemCountChangeMode.RESET, GetItemsCount(), -1, false, true); // keeping velocity
            //_InternalState.updateRequestPending = true;
        }

        /// <returns>true if there was a twin pass</returns>
        private bool ForceUpdateViewsHolder(TItemViewsHolder vh)
        {
            var twinPassScheduledBefore = this._InternalState.computeVisibilityTwinPassScheduled;
            if (twinPassScheduledBefore) throw new OSAException("You shouldn't call ForceUpdateViewsHolderIfVisible during a ComputeVisibilityForCurrentPosition, UpdateViewsHolder or CreateViewsHolder");

            this.UpdateViewsHolder(vh);

            // If a twin pass was scheduled during UpdateViewsHolder, act similarly to ComputeVisibilityPass where all visible items are iterated and have their sizes updated, 
            // but here ForceRebuildViewsHolderAndUpdateSize is used instead, because we only have 1 item
            var twinPassScheduledAfter = this._InternalState.ConsumeFlag_computeVisibilityTwinPassScheduled();
            if (twinPassScheduledAfter)
            {
                var preferEndStat = this._InternalState.ConsumeFlag_preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass();
                this.ForceRebuildViewsHolderAndUpdateSize(vh, preferEndStat, true);
            }

            return twinPassScheduledAfter;
        }

        private double ClampDouble(double t, double min, double max)
        {
            if (t < min) return min;
            if (t > max) return max;
            return t;
        }
    }
}