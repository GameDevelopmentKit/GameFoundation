using UnityEngine;
using Com.ForbiddenByte.OSA.Core;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI;

namespace Com.ForbiddenByte.OSA.AdditionalComponents
{
    /// <summary>
    /// Very useful script when you want to attach arbitrary content anywhere in an OSA and have it scrollable as any other item.
    /// Needs to be attached to a child of OSA's Viewport.
    /// <para>Note: If you use Unity 2019.3.5f1 (probably there are other buggy versions as well), this won't work properly if you don't bring the anchors together in the scrolling direction. 
    /// It's a bug in Unity where a RectTransform's size isn't correctly reported in Awake(), and it affects all UI components, not only this one</para>
    /// </summary>
    public class OSAContentDecorator : MonoBehaviour
    {
        [SerializeField] private InsetEdgeEnum _InsetEdge = InsetEdgeEnum.START;

        [SerializeField]
        [Tooltip("How far from the InsetEdge should this be positioned. Can be normalized ([0, 1]), if InsetIsNormalized=true, or raw. Default is 0")]
        private float _Inset = 0f;

        [SerializeField]
        [Tooltip("If false, will interpret the Inset property as the raw distance from InsetEdge, rather than the normalized inset relative to the content's full size")]
        private bool _InsetIsNormalized = true;

        [SerializeField] private bool _DisableWhenNotVisible = true;

        [SerializeField] [Tooltip("If false, won't be dragged together with the OSA's content when it's pulled when already at the scrolling limit")] private bool _AffectedByElasticity = false;

        [SerializeField]
        [Tooltip(
            "Sets when the OSA's padding from InsetEdge will be controlled to be the same as this object's size.\n" + "Once at initialization, or adapting continuously, or none (i.e. you'll probably set OSA's padding manually, in case the decorator shouldn't overlap with items).\n" + "Only works if Inset is 0")]
        private ControlOSAPaddingMode _ControlOSAPaddingAtInsetEdge = ControlOSAPaddingMode.ONCE_AT_INIT;

        [SerializeField] [Tooltip("If null, will use the first an implementation of IOSA found in parents")] private RectTransform _OSATransform = null;

        private RectTransform _ParentRT;
        private RectTransform _RT;
        private IOSA          _OSA;
        private bool          _Initialized;
        private double        _LastKnownInset;
        private double        _MyLastKnownSize;
        private RectOffset    _OSALastKnownPadding = new();

        /// <summary>
        /// Only to be called if OSA is initialized manually via <see cref="OSA{TParams, TItemViewsHolder}.Init"/>. Call it before that.
        /// With the default setup, where OSA initializes itself in its Start(), you don't need to call this, as it's called from this.Awake()
        /// </summary>
        public void Init()
        {
            this._RT       = this.transform as RectTransform;
            this._ParentRT = this._RT.parent as RectTransform;
            if (this._OSA == null)
            {
                this._OSA = this._ParentRT.GetComponentInParent(typeof(IOSA)) as IOSA;
                if (this._OSA == null) throw new OSAException("Component implementing IOSA not found in parents");
            }
            else
            {
                this._OSA = this._OSATransform.GetComponent(typeof(IOSA)) as IOSA;
                if (this._OSA == null) throw new OSAException("Component implementing IOSA not found on the specified object '" + this._OSATransform.name + "'");
            }

            if (this._OSA.BaseParameters.Viewport != this._ParentRT) throw new OSAException(typeof(OSAContentDecorator).Name + " can only work when attached to a direct child of OSA's Viewport.");

            if (this._ControlOSAPaddingAtInsetEdge != ControlOSAPaddingMode.DONT_CONTROL)
            {
                if (this._OSA.IsInitialized)
                    Debug.Log(
                        "OSA's content padding can't be set after OSA was initialized. " + "You're most probably calling OSA.Init manually(), in which case make sure to also manually call Init() on this decorator, before OSA.Init()"
                    );
                else
                    this.SetOSAPadding();
            }

            this._OSA.ScrollPositionChanged += this.OSAScrollPositionChanged;

            // Improvement 14.09.2020: this was limiting - each user should be able to set of their own anchors for maximum flexibility. OSA should only control the decorator's position
            //var aPos = _RT.localPosition;
            //_RT.anchorMin = _RT.anchorMax = new Vector2(0f, 1f); // top-right
            //_RT.localPosition = aPos;

            this._Initialized = true;
        }

        private void Awake()
        {
            if (!this._Initialized) this.Init();

            this.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (this._ControlOSAPaddingAtInsetEdge == ControlOSAPaddingMode.ADAPTIVE) this.AdaptToPadding();
        }

        public void SetInset(float newInset)
        {
            this._Inset = newInset;

            if (this._OSA != null && this._Initialized) this.OSAScrollPositionChanged(0d);
        }

        private void OnRectTransformDimensionsChange()
        {
        }

        public void AdaptToPadding()
        {
            if (this._ControlOSAPaddingAtInsetEdge != ControlOSAPaddingMode.ADAPTIVE) return;

            if (this._OSA == null || !this._OSA.IsInitialized) // make sure adapter wasn't disposed
                return;

            var    rect   = this._RT.rect;
            var    li     = this._OSA.GetLayoutInfoReadonly();
            double mySize = rect.size[li.hor0_vert1];

            // Update OSA's padding when either the decorator's size changes or when OSA's own padding is externally changed
            if (this._LastKnownInset != this._Inset || this._MyLastKnownSize != mySize || !this.IsSamePadding(this._OSALastKnownPadding, this._OSA.BaseParameters.ContentPadding))
            {
                this.SetPadding(mySize);
                // Commented: updating sooner is better than later
                //_OSA.ScheduleForceRebuildLayout();
                this._OSA.ForceRebuildLayoutNow();
            }
        }

        private void SetOSAPadding()
        {
            var rect = this._RT.rect;
            // Commented: layout info may not be available
            //var li = _OSA.GetLayoutInfoReadonly();

            double mySize = rect.size[this._OSA.IsHorizontal ? 0 : 1];
            this.SetPadding(mySize);
        }

        private void SetPadding(double myNewSize)
        {
            var p                   = this._OSA.BaseParameters;
            var paddingToSet        = myNewSize + this._Inset;
            var paddingToSetCeiling = (int)(paddingToSet + .6f);
            var pad                 = p.ContentPadding;
            if (this._InsetEdge == InsetEdgeEnum.START)
            {
                if (p.IsHorizontal)
                    pad.left = paddingToSetCeiling;
                else
                    pad.top = paddingToSetCeiling;
            }
            else
            {
                if (p.IsHorizontal)
                    pad.right = paddingToSetCeiling;
                else
                    pad.bottom = paddingToSetCeiling;
            }
            this._LastKnownInset      = this._Inset;
            this._MyLastKnownSize     = myNewSize;
            this._OSALastKnownPadding = new(pad.left, pad.right, pad.top, pad.bottom);
        }

        private bool IsSamePadding(RectOffset a, RectOffset b)
        {
            return
                a.left == b.left && a.right == b.right && a.top == b.top && a.bottom == b.bottom;
        }

        private void OSAScrollPositionChanged(double scrollPos)
        {
            // The terms 'before' and 'after' mean what they should, if _InsetEdge is START,
            // but their meaning is swapped when _InsetEdge is END.

            var                li = this._OSA.GetLayoutInfoReadonly();
            double             osaInsetFromEdge;
            RectTransform.Edge edgeToInsetFrom;
            if (this._InsetEdge == InsetEdgeEnum.START)
            {
                osaInsetFromEdge = this._OSA.ContentVirtualInsetFromViewportStart;
                edgeToInsetFrom  = li.startEdge;
            }
            else
            {
                osaInsetFromEdge = this._OSA.ContentVirtualInsetFromViewportEnd;
                edgeToInsetFrom  = li.endEdge;
            }

            double myExpectedInsetFromVirtualContent                       = this._Inset;
            var    rect                                                    = this._RT.rect;
            double mySize                                                  = rect.size[li.hor0_vert1];
            var    osaViewportSize                                         = li.vpSize;
            if (this._InsetIsNormalized) myExpectedInsetFromVirtualContent *= this._OSA.GetContentSize() - mySize;

            var myExpectedInsetFromViewport = osaInsetFromEdge + myExpectedInsetFromVirtualContent;
            var visible                     = true;
            if (myExpectedInsetFromViewport < 0d)
            {
                if (myExpectedInsetFromViewport <= -mySize) // completely 'before' the viewport
                {
                    myExpectedInsetFromViewport = -mySize; // don't position it too far away
                    visible                     = false;
                }
            }
            else
            {
                if (myExpectedInsetFromViewport >= osaViewportSize) // completely 'after' the viewport
                {
                    myExpectedInsetFromViewport = osaViewportSize; // don't position it too far away
                    visible                     = false;
                }
            }

            var disable           = false;
            if (!visible) disable = this._DisableWhenNotVisible;

            if (this.gameObject.activeSelf == disable) this.gameObject.SetActive(!disable);

            if (disable)
                // No need to position it, since it's disabled now
                return;

            if (!this._AffectedByElasticity)
                // If OSA's Content is pulled outside bounds (elasticity)
                if (osaInsetFromEdge > .1d)
                    if (myExpectedInsetFromViewport > 0d)
                        // Update: actually, it looks better to just keep it at the edge, no matter what
                        // // only if the content is bigger than viewport, otherwise the decorator is forced to stay with the content
                        //if (_OSA.GetContentSizeToViewportRatio() > 1d) 
                        //{
                        //}
                        // Bugfix 30.09.2020: Actually, the decorator should take _Inset into account, no matter what
                        //myExpectedInsetFromViewport = 0d;
                        myExpectedInsetFromViewport = this._Inset;

            this._RT.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(edgeToInsetFrom, (float)myExpectedInsetFromViewport, (float)mySize);
        }

        public enum InsetEdgeEnum
        {
            START,
            END,
        }

        public enum ControlOSAPaddingMode
        {
            DONT_CONTROL,
            ONCE_AT_INIT,
            ADAPTIVE,
        }
    }
}