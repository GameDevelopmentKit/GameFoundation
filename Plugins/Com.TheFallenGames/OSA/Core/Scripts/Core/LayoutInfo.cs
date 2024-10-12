using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using frame8.Logic.Misc.Other;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI;

namespace Com.ForbiddenByte.OSA.Core
{
    /// <summary>Holds basic info, like padding, content size etc, abstractized from the scrolling direction</summary>
    public class LayoutInfo
    {
        public readonly Vector2 constantAnchorPosForAllItems = new(0f, 1f); // top-left

        public Vector2 scrollViewSize { get; private set; }

        /// <summary>Assuming vertical scrolling direction, this would be the viewport's Height. Else, the Width</summary>
        public double vpSize { get; private set; }

        public double transversalContentSize { get; private set; }

        /// <summary>Assuming vertical scrolling direction, this would be the Top padding. Else, the Left padding</summary>
        public double paddingContentStart { get; private set; }

        /// <summary>Assuming vertical scrolling direction, this would be the Left padding. Else, the Top padding</summary>
        public double transversalPaddingContentStart { get; private set; }

        public double transversalPaddingContentEnd   { get; private set; }
        public double paddingContentEnd              { get; private set; } // bottom/right
        public double paddingStartPlusEnd            { get; private set; }
        public double transversalPaddingStartPlusEnd { get; private set; }
        public double spacing                        { get; private set; }

        /// <summary>Assuming vertical scrolling direction, this would be the Width of all items. Else, the Height</summary>
        public double itemsConstantTransversalSize { get; private set; }

        /// <summary>Assuming vertical scrolling direction, this would be the Top edge. Else, the Left edge</summary>
        public RectTransform.Edge startEdge { get; private set; }

        /// <summary>Assuming vertical scrolling direction, this would be the Bottom edge. Else, the Right edge</summary>
        public RectTransform.Edge endEdge { get; private set; }

        /// <summary>Transversal starting edge. Assuming vertical scrolling direction, this would be the Left edge. Else, the Top edge</summary>
        public RectTransform.Edge transvStartEdge { get; private set; }

        /// <summary>0, if it's a horizontal ScrollView. 1, else</summary>
        public int hor0_vert1 { get; private set; }

        /// <summary>1, if it's a horizontal ScrollView. -1, else</summary>
        public int hor1_vertMinus1 { get; private set; }

        internal void CacheScrollViewInfo(BaseParams parameters)
        {
            this.scrollViewSize = parameters.ScrollViewRT.rect.size;

            var vpRT   = parameters.Viewport;
            var vpRect = vpRT.rect;
            var ctRect = parameters.Content.rect;

            if (parameters.IsHorizontal)
            {
                this.startEdge       = RectTransform.Edge.Left;
                this.endEdge         = RectTransform.Edge.Right;
                this.transvStartEdge = RectTransform.Edge.Top;

                this.hor0_vert1                     = 0;
                this.hor1_vertMinus1                = 1;
                this.vpSize                         = vpRect.width;
                this.paddingContentStart            = parameters.ContentPadding.left;
                this.paddingContentEnd              = parameters.ContentPadding.right;
                this.transversalPaddingContentStart = parameters.ContentPadding.top;
                this.transversalPaddingContentEnd   = parameters.ContentPadding.bottom;
            }
            else
            {
                this.startEdge       = RectTransform.Edge.Top;
                this.endEdge         = RectTransform.Edge.Bottom;
                this.transvStartEdge = RectTransform.Edge.Left;

                this.hor0_vert1                     = 1;
                this.hor1_vertMinus1                = -1;
                this.vpSize                         = vpRect.height;
                this.paddingContentStart            = parameters.ContentPadding.top;
                this.paddingContentEnd              = parameters.ContentPadding.bottom;
                this.transversalPaddingContentStart = parameters.ContentPadding.left;
                this.transversalPaddingContentEnd   = parameters.ContentPadding.right;
            }

            this.transversalContentSize = ctRect.size[1 - this.hor0_vert1];

            if (this.transversalPaddingContentStart == -1d || this.transversalPaddingContentEnd == -1d)
            {
                if (parameters.ItemTransversalSize == 0f)
                    throw new OSAException(
                        "ItemTransversalSize is 0, meaning the item should fill the available space transversally, " + "but the transversal padding is not specified (it's -1, which is a reserved value)"
                    );

                this.itemsConstantTransversalSize = this.transversalPaddingStartPlusEnd = this.transversalPaddingContentStart = this.transversalPaddingContentEnd = -1d;
            }
            else
            {
                this.transversalPaddingStartPlusEnd = this.transversalPaddingContentStart + this.transversalPaddingContentEnd;
                this.itemsConstantTransversalSize   = this.transversalContentSize - this.transversalPaddingStartPlusEnd;
            }

            this.spacing = parameters.ContentSpacing;

            // There's no concept of content start/end padding when looping. instead, the spacing amount is appended before+after the first+last item
            if (parameters.effects.LoopItems && (this.paddingContentStart != this.spacing || this.paddingContentEnd != this.spacing))
                throw new OSAException(
                    "When looping is active, the content padding should be the same as content spacing. " + "This is handled automatically in Params.InitIfNeeded(). " + "If you overrode method, please call base's implementation first"
                );

            this.paddingStartPlusEnd = this.paddingContentStart + this.paddingContentEnd;
        }
    }
}