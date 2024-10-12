using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using frame8.Logic.Misc.Other;
using frame8.Logic.Misc.Other.Extensions;
using System;

namespace Com.ForbiddenByte.OSA.Core.SubComponents
{
    using Object = UnityEngine.Object;

    /// <summary>
    /// Contains cached variables, helper methods and generally things that are not exposed to inheritors. Note: the LayoutGroup component on content, if any, will be disabled.
    /// <para>Comments format: value if vertical scrolling/value if horizontal scrolling</para>
    /// </summary>
    public class InternalState<TItemViewsHolder> where TItemViewsHolder : BaseItemViewsHolder
    {
        #region Fields & Props

        public LayoutInfo layoutInfo = new();

        // Constant params (until the scrollview size changes)
        //public readonly double proximityToLimitNeeded01ToResetPos = .95d;
        public double             ProximityToLimitNeeded01ToResetPos => this._SourceParams.effects.ElasticMovement ? 1d : .9999995d;
        public double             vpSize                             => this.layoutInfo.vpSize;
        public double             paddingContentStart                => this.layoutInfo.paddingContentStart;
        public double             transversalPaddingContentStart     => this.layoutInfo.transversalPaddingContentStart;
        public double             paddingContentEnd                  => this.layoutInfo.paddingContentEnd;
        public double             paddingStartPlusEnd                => this.layoutInfo.paddingStartPlusEnd;
        public double             spacing                            => this.layoutInfo.spacing;
        public RectTransform.Edge startEdge                          => this.layoutInfo.startEdge;
        public RectTransform.Edge endEdge                            => this.layoutInfo.endEdge;
        public RectTransform.Edge transvStartEdge                    => this.layoutInfo.transvStartEdge;
        public int                hor0_vert1                         => this.layoutInfo.hor0_vert1;
        public int                hor1_vertMinus1                    => this.layoutInfo.hor1_vertMinus1;

        // Cache params
        public double lastProcessedCTVirtualInsetFromVPS;

        public double ctVirtualInsetFromVPS_Cached { get; private set; } // todo set back to field

        //internal double ctVirtualInsetFromVPS_Cached_NotConsideringNegativeVSA { get { return VirtualScrollableArea > 0 ? ctVirtualInsetFromVPS_Cached : 0d; } }
        public Vector2 scrollViewSize => this.layoutInfo.scrollViewSize;

        //internal float ctRealSize; // height/width // same as vpSize for now
        public double ctVirtualSize; // height/width

        //internal bool updateRequestPending;
        public bool computeVisibilityTwinPassScheduled;

        public bool preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass;

        //internal bool lastComputeVisibilityHadATwinPass;
        public int totalNumberOfSizeChanges;
        public int totalNumberOfSizeChangesLastFrame;
        //internal Func<RectTransform, float> getRTCurrentSizeFn;

        public bool HasScrollViewSizeChanged
        {
            get
            {
                // Update: although not recommended, the Viewport's size can change directly, without having the ScrollView itself change,
                // so we're also checking for the viewport size changes. 
                // This comment will/should be removed in further versions

                var svRect = this._SourceParams.ScrollViewRT.rect;

                if (this._SourceParams.ItemTransversalSize == -1f // items' transversal size is not managed by OSA
                    && this.transversalPaddingContentStart != -1f // neither their transversal position
                )
                {
                    // Only checking for the size in the scrolling direction, if OSA doesn't change item's transversal pos/size

                    if (this.scrollViewSize[this.hor0_vert1] != svRect.size[this.hor0_vert1]) return true;

                    var vpRect = this._SourceParams.Viewport.rect;
                    if ((float)this.vpSize != vpRect.size[this.hor0_vert1]) return true;

                    return false;
                }

                return this.scrollViewSize != svRect.size
                    || (float)this.vpSize != this._SourceParams.Viewport.rect.size[this.hor0_vert1];
            }
        }

        public double CTVirtualInsetFromVPE_Cached => -this.ctVirtualSize + this.vpSize - this.ctVirtualInsetFromVPS_Cached;
        public double VirtualScrollableArea        => this.ctVirtualSize - this.vpSize; // negative/zero when all the content is inside vp, positive else
        public double AbstractPivot01              => this.hor0_vert1 + this.hor1_vertMinus1 * this._SourceParams.Content.pivot[this.hor0_vert1];

        private ItemsDescriptor _ItemsDesc;
        private BaseParams      _SourceParams;

        #endregion

        public static InternalState<TItemViewsHolder> CreateFromSourceParamsOrThrow(BaseParams sourceParams, ItemsDescriptor itemsDescriptor)
        {
            return new(sourceParams, itemsDescriptor);
        }

        protected InternalState(BaseParams sourceParams, ItemsDescriptor itemsDescriptor)
        {
            this._SourceParams = sourceParams;
            this._ItemsDesc    = itemsDescriptor;

            var lg = sourceParams.Content.GetComponent<LayoutGroup>();
            if (lg && lg.enabled)
            {
                lg.enabled = false;
                Debug.Log("LayoutGroup on GameObject " + lg.name + " has beed disabled in order to use OSA");
            }

            var contentSizeFitter = sourceParams.Content.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter && contentSizeFitter.enabled)
            {
                contentSizeFitter.enabled = false;
                Debug.Log("ContentSizeFitter on GameObject " + contentSizeFitter.name + " has beed disabled in order to use OSA");
            }

            var layoutElement = sourceParams.Content.GetComponent<LayoutElement>();
            if (layoutElement)
            {
                Object.Destroy(layoutElement);
                Debug.Log("LayoutElement on GameObject " + contentSizeFitter.name + " has beed DESTROYED in order to use OSA");
            }

            //if (sourceParams.IsHorizontal)
            //{
            //	layoutInfo.startEdge = RectTransform.Edge.Left;
            //	layoutInfo.endEdge = RectTransform.Edge.Right;
            //	layoutInfo.transvStartEdge = RectTransform.Edge.Top;
            //	//getRTCurrentSizeFn = root => root.rect.width;
            //}
            //else
            //{
            //	layoutInfo.startEdge = RectTransform.Edge.Top;
            //	layoutInfo.endEdge = RectTransform.Edge.Bottom;
            //	layoutInfo.transvStartEdge = RectTransform.Edge.Left;
            //	//getRTCurrentSizeFn = root => root.rect.height;
            //}
        }

        public void CacheScrollViewInfo()
        {
            this.layoutInfo.CacheScrollViewInfo(this._SourceParams);
        }

        //public void CorrectPositionsBasedOnCachedCTInsetFromVPS(List<TItemViewsHolder> vhs, bool alsoCorrectTransversalPositioning)//, bool itemEndEdgeStationary)
        //{
        //	// Update the positions of the provided vhs so they'll retain their position relative to the viewport
        //	TItemViewsHolder vh;
        //	int count = vhs.Count;

        //	double insetStartOfCurItem = GetItemVirtualInsetFromParentStartUsingItemIndexInView(vhs[0].itemIndexInView);
        //	float curSize;
        //	float realInset;
        //	for (int i = 0; i < count; ++i)
        //	{
        //		vh = vhs[i];
        //		curSize = _ItemsDesc[vh.itemIndexInView];
        //		realInset = ConvertItemInsetFromParentStart_FromVirtualToInferredReal(insetStartOfCurItem);
        //		vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(
        //			_SourceParams.Content,
        //			startEdge,
        //			realInset,
        //			curSize
        //		);
        //		insetStartOfCurItem += curSize + spacing;

        //		if (alsoCorrectTransversalPositioning && realInset >= 0f && realInset < viewportSize)
        //			vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(transvStartEdge, transversalPaddingContentStart, _ItemsDesc.itemsConstantTransversalSize);
        //	}
        //}

        // Gives a consistent value regardless if horizontal or vertical scrollview (1 = start, 0 = end)
        public Vector2 GetPointerPositionInCTSpace(PointerEventData currentPointerEventData)
        {
            return UIUtils8.Instance.ScreenPointToLocalPointInRectangle(this._SourceParams.Content, currentPointerEventData);
        }

        //public Vector2 GetVectorInCTSpaceFrom(Vector2 startPosInCTSpace, PointerEventData currentPointerEventData)
        //{
        //	Vector2 curLocalPos = GetPointInCTSpaceFrom(currentPointerEventData);
        //	return curLocalPos - startPosInCTSpace;
        //}

        public double GetCTAbstractSpaceVectorLongitudinalComponentFromCTSpaceVector(Vector2 vectorCTSpace)
        {
            var abstrDeltaInCTSpace = (double)vectorCTSpace[this.hor0_vert1] * this.hor1_vertMinus1;

            return abstrDeltaInCTSpace;
        }

        public double CalculateContentVirtualSize()
        {
            return this._ItemsDesc.CumulatedSizeOfAllItems + this.spacing * Math.Max(0, this._ItemsDesc.itemsCount - 1) + this.paddingStartPlusEnd;
        }

        // Don't abuse this! It's only used when the items' sizes have externally changed and thus we don't know if their 
        // positions remained the same or not (most probably, not)
        public void CorrectPositions(List<TItemViewsHolder> vhs, bool alsoCorrectTransversalPositioning) //, bool itemEndEdgeStationary)
        {
            // Update the positions of the provided vhs so they'll retain their position relative to the viewport
            TItemViewsHolder vh;
            var              count = vhs.Count;
            //var edge = itemEndEdgeStationary ? endEdge : startEdge;
            //Func<int, float> getInferredRealOffsetFromParentStartOrEndFn;
            //if (itemEndEdgeStationary)
            //	getInferredRealOffsetFromParentStartOrEndFn = GetItemInferredRealOffsetFromParentEnd;
            //else
            //	getInferredRealOffsetFromParentStartOrEndFn = GetItemInferredRealOffsetFromParentStart;

            //double insetStartOfCurItem = GetItemVirtualInsetFromParentStartUsingItemIndexInView(vhs[0].itemIndexInView);
            var    insetStartOfCurItem = this.GetItemInferredRealInsetFromParentStart(vhs[0].itemIndexInView);
            double curSize;

            //Debug.Log("CorrectPositions:" + vhs[0].ItemIndex + " to " + vhs[vhs.Count-1].ItemIndex);
            for (var i = 0; i < count; ++i)
            {
                vh      = vhs[i];
                curSize = this._ItemsDesc[vh.itemIndexInView];
                vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(this.startEdge,
                    //ConvertItemInsetFromParentStart_FromVirtualToInferredReal(insetStartOfCurItem),
                    (float)insetStartOfCurItem,
                    (float)curSize
                );
                insetStartOfCurItem += curSize + this.spacing;

                if (alsoCorrectTransversalPositioning)
                {
                    float tInsetStartToUse;
                    float tSizeToUse;
                    this.GetTransversalInsetStartAndSizeToUse(vh, out tInsetStartToUse, out tSizeToUse);

                    // Transversal float precision doesn't matter
                    vh.root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(this.transvStartEdge, tInsetStartToUse, tSizeToUse);
                }
            }
        }

        public void UpdateLastProcessedCTVirtualInsetFromVPStart()
        {
            this.lastProcessedCTVirtualInsetFromVPS = this.ctVirtualInsetFromVPS_Cached;
        }

        /// <summary> See the <see cref="OSA{TParams, TItemViewsHolder}.GetVirtualAbstractNormalizedScrollPosition"/> for documentation</summary>
        public double GetVirtualAbstractNormalizedScrollPosition()
        {
            var    vsa = this.VirtualScrollableArea;
            double val;
            if (vsa <= 0) // vp bigger than- or equal (avoiding div by zero below) to ct
            {
                val = 1d;
            }
            else
            {
                var insetClamped = Math.Min(0d, this.ctVirtualInsetFromVPS_Cached);
                val = 1d + insetClamped / vsa;
            }
            //Debug.Log("GetVirtAbstr " + val + ", VSA " + VirtualScrollableArea + ", insetClamped " + (Math.Min(0d, ctVirtualInsetFromVPS_Cached) / VirtualScrollableArea));
            return val;
        }

        public void UpdateCachedCTVirtInsetFromVPS(double virtualInset, bool allowOutsideBounds)
        {
            if (!allowOutsideBounds)
            {
                double maxInsetStart, minInsetStart;
                var    emptyArea = -this.VirtualScrollableArea;
                if (emptyArea > 0d)
                    //maxInsetStart = GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP(emptyArea);
                    //double maxInsetEnd = vpSize - (ctVirtualSize + maxInsetStart);
                    //minInsetStart = vpSize - (ctVirtualSize + maxInsetEnd);
                {
                    minInsetStart = maxInsetStart = this.GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP(emptyArea);
                }
                else
                {
                    var vsa = this.VirtualScrollableArea;
                    maxInsetStart = 0d;
                    minInsetStart = -vsa;
                }

                if (minInsetStart > maxInsetStart) throw new OSAException(string.Format("[Internal] Clamping content offset failed: minInsetStart(={0}) > maxInsetStart(-{1})", minInsetStart, maxInsetStart));

                virtualInset = Math.Max(minInsetStart, Math.Min(maxInsetStart, virtualInset));
            }
            //double prev = ctVirtualInsetFromVPS_Cached;

            // This is the only place the ct inset should be changed. 
            this.ctVirtualInsetFromVPS_Cached = virtualInset;

            // TODO see if needed
            //Canvas.ForceUpdateCanvases();
            if (this._SourceParams.optimization.ForceLayoutRebuildOnDrag) this.RebuildLayoutImmediateCompat(this._SourceParams.ScrollViewRT);

            //return ctVirtualInsetFromVPS_Cached - prev;
        }

        public double GetItemVirtualInsetFromParentStartUsingItemIndexInView(int itemIndexInView)
        {
            var cumulativeSizeOfAllItemsBeforePlusSpacing                      = 0d;
            if (itemIndexInView > 0) cumulativeSizeOfAllItemsBeforePlusSpacing = this._ItemsDesc.GetItemSizeCumulative(itemIndexInView - 1) + itemIndexInView * this.spacing;

            var inset = this.paddingContentStart + cumulativeSizeOfAllItemsBeforePlusSpacing;

            //double emptyAreaWhenCTSmallerThanVP = -VirtualScrollableArea;
            //if (emptyAreaWhenCTSmallerThanVP > 0)
            //	inset += GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP(emptyAreaWhenCTSmallerThanVP);

            return inset;
        }

        public double GetItemVirtualInsetFromParentEndUsingItemIndexInView(int itemIndexInView)
        {
            return this.ctVirtualSize - this.GetItemVirtualInsetFromParentStartUsingItemIndexInView(itemIndexInView) - this._ItemsDesc[itemIndexInView];
        }

        public double GetItemInferredRealInsetFromParentStart(int itemIndexInView)
        {
            return this.ConvertItemInsetFromParentStart_FromVirtualToInferredReal(this.GetItemVirtualInsetFromParentStartUsingItemIndexInView(itemIndexInView));
        }

        public double GetItemInferredRealInsetFromParentEnd(int itemIndexInView)
        {
            return this.vpSize - this.GetItemInferredRealInsetFromParentStart(itemIndexInView) - this._ItemsDesc[itemIndexInView];
        }

        public double GetContentInferredRealInsetFromVPS(TItemViewsHolder firstVH)
        {
            return firstVH.root.GetInsetFromParentEdge(this._SourceParams.Content, this.startEdge) - this.paddingContentStart;
        }

        public double GetContentInferredRealInsetFromVPE(TItemViewsHolder lastVH)
        {
            return lastVH.root.GetInsetFromParentEdge(this._SourceParams.Content, this.endEdge) - this.paddingContentEnd;
        }

        //public double ConvertItemOffsetFromParentStart_FromRealToVirtual(float realOffsetFromParrentStart)
        //{ return -contentPanelSkippedInsetDueToVirtualization + realOffsetFromParrentStart; }
        public double ConvertItemInsetFromParentStart_FromVirtualToInferredReal(double virtualInsetFromParrentStart)
        {
            return this.ctVirtualInsetFromVPS_Cached + virtualInsetFromParrentStart;
        }

        // This assumes vsa is negative
        public double GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP()
        {
            var emptyAreaWhenCTSmallerThanVP = -this.VirtualScrollableArea;
            return this.GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP(emptyAreaWhenCTSmallerThanVP);
        }

        public double GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP(double emptyAreaWhenCTSmallerThanVP)
        {
            var target = this.AbstractPivot01 * emptyAreaWhenCTSmallerThanVP;

            return target;
        }

        public void GetTransversalInsetStartAndSizeToUse(TItemViewsHolder vh, out float insetStart, out float size)
        {
            var transvSizeInParams = this._SourceParams.ItemTransversalSize;
            if (transvSizeInParams == 0f)
                // Default behavior: expand item to fill available space
            {
                size = (float)this.layoutInfo.itemsConstantTransversalSize;
            }
            else
            {
                if (transvSizeInParams == -1f)
                    // Don't touch its size
                    size = vh.root.rect.size[1 - this.hor0_vert1];
                else
                    // Fixed size, regardless of available space
                    size = transvSizeInParams;

                if (this.layoutInfo.transversalPaddingStartPlusEnd == -1d)
                {
                    // Center it
                    insetStart = (float)((this.layoutInfo.transversalContentSize - size) / 2);
                    return;
                }
            }

            insetStart = (float)this.transversalPaddingContentStart;
        }

        public void CorrectParametersOnCTSizeChange(bool contentPanelEndEdgeStationary, out double? ctInsetFromVPSOverride, ref double additionalCTDragAbstrDelta, double newCTSize, double deltaSize)
        {
            if (deltaSize < 0) // shrinking
            {
                var newVirtualizedAmount = newCTSize - this.vpSize;
                var emptyAreaInViewport  = -newVirtualizedAmount;
                // In case the ct is smaller than vp, we set the inset from start manually, as it's done when correcting the position according to pivot, in late update
                if (emptyAreaInViewport > 0)
                {
                    ctInsetFromVPSOverride = this.GetTargetCTVirtualInsetFromVPSWhenCTSmallerThanVP(emptyAreaInViewport);
                    return;
                }

                var cut = -deltaSize;
                if (contentPanelEndEdgeStationary)
                {
                    var contentAmountBeforeVP = -this.ctVirtualInsetFromVPS_Cached;
                    if (contentAmountBeforeVP < 0d)
                    {
                        ctInsetFromVPSOverride     = 0d;
                        additionalCTDragAbstrDelta = contentAmountBeforeVP - cut;
                    }
                    else
                    {
                        var cutAmountInsideVP = cut - contentAmountBeforeVP;
                        if (cutAmountInsideVP >= 0d)
                        {
                            // Commented: the non-virtualized ct case is handled before
                            //if (vpSize > newCTSize)
                            //{
                            //	ctInsetFromVPSOverride = vpSize - newCTSize;
                            //	double uncutAmountInsideVP = vpSize - cutAmountInsideVP;
                            //	double contentAmountAfterVP = newCTSize - uncutAmountInsideVP;
                            //	additionalCTDragAbstrDelta = -contentAmountAfterVP;
                            //}
                            //else
                            //{
                            //	ctInsetFromVPSOverride = 0d;
                            //	additionalCTDragAbstrDelta = -cutAmountInsideVP;
                            //}
                            ctInsetFromVPSOverride     = 0d;
                            additionalCTDragAbstrDelta = -cutAmountInsideVP;
                        }
                        else
                        {
                            ctInsetFromVPSOverride = null;
                        }
                    }
                    //Debug.Log("contentAmountBeforeVP:" + contentAmountBeforeVP + ", additionalCTDragAbstrDelta=" + additionalCTDragAbstrDelta);
                }
                else
                {
                    var contentAmountAfterVP = -this.CTVirtualInsetFromVPE_Cached;

                    if (contentAmountAfterVP < 0d)
                    {
                        ctInsetFromVPSOverride     = this.vpSize - newCTSize;
                        additionalCTDragAbstrDelta = -contentAmountAfterVP + cut;
                    }
                    else
                    {
                        var cutAmountInsideVP = cut - contentAmountAfterVP;
                        if (cutAmountInsideVP >= 0d)
                        {
                            // Commented: the non-virtualized ct case is handled before
                            //if (vpSize > newCTSize)
                            //{
                            //	ctInsetFromVPSOverride = 0d;
                            //	additionalCTDragAbstrDelta = -ctVirtualInsetFromVPS_Cached;
                            //}
                            //else
                            //{
                            //	ctInsetFromVPSOverride = vpSize - newCTSize;
                            //	additionalCTDragAbstrDelta = cutAmountInsideVP;
                            //}
                            ctInsetFromVPSOverride     = this.vpSize - newCTSize;
                            additionalCTDragAbstrDelta = cutAmountInsideVP;
                        }
                        else
                        {
                            ctInsetFromVPSOverride = null;
                        }
                    }
                    //Debug.Log("contentAmountAfterVP:" + contentAmountAfterVP + ", additionalCTDragAbstrDelta=" + additionalCTDragAbstrDelta);
                }
            }
            else
            {
                ctInsetFromVPSOverride = null;
            }
        }

        public void RebuildLayoutImmediateCompat(RectTransform rectTransform)
        {
            //Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        public bool ConsumeFlag_computeVisibilityTwinPassScheduled()
        {
            var val = this.computeVisibilityTwinPassScheduled;
            this.computeVisibilityTwinPassScheduled = false;

            return val;
        }

        public bool ConsumeFlag_preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass()
        {
            var val = this.preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass;
            this.preferKeepingCTEndEdgeStationaryInNextComputeVisibilityTwinPass = false;

            return val;
        }
    }
}