//#define DEBUG_EVENTS

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.Assertions;

namespace frame8.Logic.Misc.Visual.UI.MonoBehaviours
{
    /// <summary>
    /// <para>Fixes ScrollView inertia when the content grows too big. The default method cuts off the inertia in most cases.</para>
    /// <para>Attach it to the Scrollbar and make sure no scrollbars are assigned to the ScrollRect</para>
    /// <para>It also contains a lot of other silky-smooth features</para>
    /// </summary>
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollbarFixer8 : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler, IEndDragHandler, IScrollRectProxy
    {
        public bool hideWhenNotNeeded = true;
        public bool autoHide          = true;

        [Tooltip("A CanvasGroup will be added to the Scrollbar, if not already present, and the fade effect will be achieved by changing its alpha property")] public bool autoHideFadeEffect = true;

        [Tooltip("The collapsing effect will change the localScale of the Scrollbar, so the pivot's position decides in what direction it'll grow/shrink.\n " + "Note that sometimes a really nice effect is achieved by placing the pivot slightly outside the rect (the minimized scrollbar will move outside while collapsing)")]
        public bool autoHideCollapseEffect = false;

        [Tooltip("Used if autoHide is on. Duration in seconds")] public float autoHideTime = 1f;

        public float autoHideFadeEffectMinAlpha     = .8f;
        public float autoHideCollapseEffectMinScale = .2f;

        [Range(0.01f, 1f)] public float minSize = .1f;

        [Range(0.01f, 1f)]
        [Tooltip("When using elasticity in the ScrollView and pulling the content outside bounds, this value determines how small can the scrollbar get")]
        public float minCompressedSize = .01f;

        [Range(0.01f, 1f)] public float maxSize = 1f;

        [Range(0.005f, 2f)] public float sizeUpdateInterval = .01f;

        [Tooltip("Used to prevent updates to be processed too often, in case this is a concern")] public int skippedFramesBetweenPositionChanges;

        [Tooltip(
            "Enable this if the Scrollbar has any interractable children objects (Button, Image etc.), \n" + "because they'll 'consume' some of the events that this script needs to function properly. \n" + "\n" + "However, sometimes you might want to keep this disabled even in the above case, but be \n" + "warned that we'll be in a slightly invalid state: 'pre-dragging' will start, but will \n" + "not end on 'pointer up' because the Scrollbar won't ever receive 'on pointer up' in the \n" + "first place, it'll be 'consumed' by the respective child. Do your testing as this will \n" + "can be a problem in some cases")]
        public bool ignoreDragWithoutPointerDown = false;

        [Tooltip("If not assigned, will try to find a ScrollRect or an IScrollRectProxy in the parent. If viewport is assigned, the search starts from there")]
        public ScrollRect scrollRect;

        [Tooltip("If not assigned, will use the resolved scrollRect")] public RectTransform viewport;

        public UnityEvent OnScrollbarSizeChanged;

        /// <summary>
        /// Will be retrieved from the scrollrect. If not found, it can be assigned anytime before the first Update. 
        /// If not assigned, a default proxy will be used. The purpose of this is to allow custom implementations of ScrollRect to be used
        /// </summary>
        public IScrollRectProxy externalScrollRectProxy
        {
            get => this._ExternalScrollRectProxy;
            set
            {
                this._ExternalScrollRectProxy = value;

                if (this._ExternalScrollRectProxy != null)
                    if (this.scrollRect)
                    {
                        this.scrollRect.onValueChanged.RemoveListener(this.ScrollRect_OnValueChangedCalled);
                        this.scrollRect = null;
                    }
            }
        }

        private IScrollRectProxy _ExternalScrollRectProxy;

        #region IScrollRectProxy properties implementation

        public bool             IsInitialized                 => this.scrollRect != null;
        public Vector2          Velocity                      { get; set; }
        public bool             IsHorizontal                  => this.scrollRect.horizontal;
        public bool             IsVertical                    => this.scrollRect.vertical;
        public RectTransform    Content                       => this.scrollRect.content;
        public RectTransform    Viewport                      => this.scrollRect.viewport != null ? this.scrollRect.viewport : this.scrollRect.transform as RectTransform;
        double IScrollRectProxy.ContentInsetFromViewportStart => this.Content.GetInsetFromParentEdge(this.Viewport, this.ScrollRectProxy.GetStartEdge());
        double IScrollRectProxy.ContentInsetFromViewportEnd   => this.Content.GetInsetFromParentEdge(this.Viewport, this.ScrollRectProxy.GetEndEdge());

        #endregion

        public bool IsPreDragging           => this.State == StateEnum.PRE_DRAGGING;
        public bool IsDragging              => this.State == StateEnum.DRAGGING;
        public bool IsDraggingOrPreDragging => this.IsDragging || this.IsPreDragging;

        /// <summary> Using Scaled time for a scrollbar's animation doesn't make too much sense, so we're always using unscaledTime</summary>
        private float Time => UnityEngine.Time.unscaledTime;

        private IScrollRectProxy ScrollRectProxy => this.externalScrollRectProxy == null ? this : this.externalScrollRectProxy;

        private StateEnum State
        {
            get { return this._State; }
            set
            {
                #if DEBUG_EVENTS
				Debug.Log("State: " + State + " -> " + value);
                #endif
                this._State = value;
            }
        }

        private const float HIDE_EFFECT_START_DELAY_01 = .4f; // relative to this.autoHideTime

        private RectTransform _ScrollViewRT;
        private Scrollbar     _Scrollbar;
        private CanvasGroup   _CanvasGroupForFadeEffect;
        private bool          _HorizontalScrollBar;
        private Vector3       _InitialScale = Vector3.one;
        private bool          _Hidden, _AutoHidden, _HiddenNotNeeded;
        private double        _LastValue;
        private float         _TimeOnLastValueChange;
        private StateEnum     _State = StateEnum.NONE;
        private IEnumerator   _SlowUpdateCoroutine;
        private float         _TransversalScaleOnLastDrag, _AlphaOnLastDrag;
        private bool          _FullyInitialized;
        private int           _FrameCountOnLastPositionUpdate;
        private bool          _TriedToCallOnScrollbarSizeChangedAtLeastOnce;

        private int? _PrimaryEventID;

        // We might only receive OnInitializePotentialDrag() directly if a child 'consumed' our OnPointerDown (and thus will also comsume the OnPointerUp)
        private bool? _PointerDownReceivedForCurrentDrag;

        private void Awake()
        {
            if (this.autoHideTime == 0f) this.autoHideTime = 1f;

            this._Scrollbar             = this.GetComponent<Scrollbar>();
            this._InitialScale          = this._Scrollbar.transform.localScale;
            this._LastValue             = this._Scrollbar.value;
            this._TimeOnLastValueChange = this.Time;
            this._HorizontalScrollBar   = this._Scrollbar.direction == Scrollbar.Direction.LeftToRight || this._Scrollbar.direction == Scrollbar.Direction.RightToLeft;

            // Fix/Improvement 24.08.2022: If viewport is specified, then there's a 99% chance the target ScrollRect/OSA is on the nearest GameObject up the hierarchy starting from it
            var parentSearchOrigin = this.viewport == null ? this.transform.parent : this.viewport;
            if (this.externalScrollRectProxy == null && this.scrollRect == null)
            {
                var curTr = parentSearchOrigin;
                while (curTr != null)
                {
                    var esrp = curTr.GetComponent(typeof(IScrollRectProxy)) as IScrollRectProxy;
                    if (esrp != null)
                    {
                        this.externalScrollRectProxy = esrp;
                        break;
                    }
                    var sr = curTr.GetComponent<ScrollRect>();
                    if (sr != null)
                    {
                        this.scrollRect = sr;
                        break;
                    }

                    curTr = curTr.parent;
                }
            }

            if (this.scrollRect)
            {
                this._ScrollViewRT = this.scrollRect.transform as RectTransform;
                if (this._HorizontalScrollBar)
                {
                    if (!this.scrollRect.horizontal) throw new UnityException("ScrollbarFixer8: Can't use horizontal scrollbar with non-horizontal scrollRect");

                    if (this.scrollRect.horizontalScrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: setting scrollRect.horizontalScrollbar to null (the whole point of using ScrollbarFixer8 is to NOT have any scrollbars assigned)");
                        this.scrollRect.horizontalScrollbar = null;
                    }
                    if (this.scrollRect.verticalScrollbar == this._Scrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: Can't use the same scrollbar for both vert and hor");
                        this.scrollRect.verticalScrollbar = null;
                    }
                }
                else
                {
                    if (!this.scrollRect.vertical) throw new UnityException("Can't use vertical scrollbar with non-vertical scrollRect");

                    if (this.scrollRect.verticalScrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: setting scrollRect.verticalScrollbar to null (the whole point of using ScrollbarFixer8 is to NOT have any scrollbars assigned)");
                        this.scrollRect.verticalScrollbar = null;
                    }
                    if (this.scrollRect.horizontalScrollbar == this._Scrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: Can't use the same scrollbar for both vert and hor");
                        this.scrollRect.horizontalScrollbar = null;
                    }
                }
            }
            else
            {
            }

            if (this.scrollRect)
            {
                this.scrollRect.onValueChanged.AddListener(this.ScrollRect_OnValueChangedCalled);

                // May be null
                this.externalScrollRectProxy = this.scrollRect.GetComponent(typeof(IScrollRectProxy)) as IScrollRectProxy;
            }
            else
            {
                if (this.externalScrollRectProxy == null)
                {
                    // Start with directly with the parent when searching for IScrollRectProxy, as the scrollbar itself is a IScrollRectProxy and needs to be avoided;
                    this.externalScrollRectProxy = parentSearchOrigin.GetComponentInParent(typeof(IScrollRectProxy)) as IScrollRectProxy;
                    if (this.externalScrollRectProxy == null)
                        // Try starting from the viewport, as the scrollbar might not be a child of the target ScrollView
                        if (this.viewport)
                        {
                            var alreadySearchedViewport                                = parentSearchOrigin == this.viewport;
                            if (!alreadySearchedViewport) this.externalScrollRectProxy = this.viewport.GetComponentInParent(typeof(IScrollRectProxy)) as IScrollRectProxy;
                        }
                }

                if (this.externalScrollRectProxy == null)
                {
                    if (this.enabled)
                    {
                        Debug.Log(this.GetType().Name + ": no scrollRect provided and found no " + typeof(IScrollRectProxy).Name + " component among ancestors. Disabling...");
                        this.enabled = false;
                    }
                    return;
                }

                this._ScrollViewRT = this.externalScrollRectProxy.gameObject.transform as RectTransform;
            }

            if (!this.viewport) this.viewport = this._ScrollViewRT;

            if (this.autoHide) this.UpdateStartingValuesForAutoHideEffect();
        }

        private void OnEnable()
        {
            this.ResetPointerEvent(); // in case State was stuck in non-NONE and the object was disabled
            this._SlowUpdateCoroutine = this.SlowUpdate();

            this.StartCoroutine(this._SlowUpdateCoroutine);
        }

        private void Update()
        {
            if (!this._FullyInitialized) this.InitializeInFirstUpdate();

            if (this.scrollRect || (this.externalScrollRectProxy != null && this.externalScrollRectProxy.IsInitialized))
            {
                // Don't override between pointer down event and OnPointerUp or OnBeginDrag
                // Don't override when dragging
                if (this.State == StateEnum.NONE)
                    this.UpdateOnNoDragging();
                else if (this.State == StateEnum.PRE_DRAGGING) this.UpdateOnPreDragging();
            }
        }

        private void UpdateOnNoDragging()
        {
            var value = this.ScrollRectProxy.GetNormalizedPosition();
            #if DEBUG_EVENTS
			string didReceivePointerDown = _PointerDownReceivedForCurrentDrag == null ? "(null)" : _PointerDownReceivedForCurrentDrag.Value.ToString();
			Debug.Log(
				gameObject.name + gameObject.GetInstanceID() + ", didReceivePointerDown=" + didReceivePointerDown + 
				", UpdateOnNoDragging _Scrollbar.value = " + (float)value, gameObject
			);
            #endif
            this._Scrollbar.value = (float)value;

            if (this.autoHide)
            {
                if (value == this._LastValue)
                {
                    if (!this._Hidden)
                    {
                        var timePassedForHide01 = Mathf.Clamp01((this.Time - this._TimeOnLastValueChange) / this.autoHideTime);
                        if (timePassedForHide01 >= HIDE_EFFECT_START_DELAY_01)
                        {
                            var hideEffectAmount01 = (timePassedForHide01 - HIDE_EFFECT_START_DELAY_01) / (1f - HIDE_EFFECT_START_DELAY_01);
                            hideEffectAmount01 = hideEffectAmount01 * hideEffectAmount01 * hideEffectAmount01; // slow in, fast-out effect
                            if (this.CheckForAudoHideFadeEffectAndInitIfNeeded()) this._CanvasGroupForFadeEffect.alpha = Mathf.Lerp(this._AlphaOnLastDrag, this.autoHideFadeEffectMinAlpha, hideEffectAmount01);

                            if (this.autoHideCollapseEffect)
                            {
                                var localScale = this.transform.localScale;
                                localScale[this.ScrollRectProxy.IsHorizontal ? 1 : 0] = Mathf.Lerp(this._TransversalScaleOnLastDrag, this.autoHideCollapseEffectMinScale, hideEffectAmount01);
                                this.transform.localScale                             = localScale;
                            }
                        }

                        if (timePassedForHide01 == 1f)
                        {
                            this._AutoHidden = true;
                            this.Hide();
                        }
                    }
                }
                else
                {
                    this._TimeOnLastValueChange = this.Time;
                    this._LastValue             = value;

                    if (this._Hidden && !this._HiddenNotNeeded) this.Show();
                }
            }
            // Handling the case when the scrollbar was hidden but its autoHide property was set to false afterwards 
            // and hideWhenNotNeeded is also false, meaning the scrollbar won't ever be shown
            else if (!this.hideWhenNotNeeded)
                if (this._Hidden)
                    this.Show();
        }

        private void UpdateOnPreDragging()
        {
            var didReceivePointerDown = this._PointerDownReceivedForCurrentDrag.Value;
            #if DEBUG_EVENTS
			Debug.Log(
				gameObject.name + gameObject.GetInstanceID() + ", didReceivePointerDown=" + didReceivePointerDown +
				", UpdateOnPreDragging _Scrollbar.value = " + (float)_Scrollbar.value, gameObject
			);
            #endif
            // During keeping the pointer down (and not moving), we're constantly updating ScrollRect's position,
            // as in most cases the Scrollbar will move towards the pointer across multiple frames
            this.OnScrollRectValueChanged(false);
        }

        private void OnDisable()
        {
            if (this._SlowUpdateCoroutine != null) this.StopCoroutine(this._SlowUpdateCoroutine);
        }

        private void OnDestroy()
        {
            if (this.scrollRect) this.scrollRect.onValueChanged.RemoveListener(this.ScrollRect_OnValueChangedCalled);

            if (this.externalScrollRectProxy != null) this.externalScrollRectProxy.ScrollPositionChanged -= this.ExternalScrollRectProxy_OnScrollPositionChanged;
        }

        private double GetOutsideBoundsAmountRaw()
        {
            if (this.ScrollRectProxy == null) return 0d;

            double inset = 0;

            var ctInsetFromEdge            = this.ScrollRectProxy.ContentInsetFromViewportStart;
            if (ctInsetFromEdge > 0) inset += ctInsetFromEdge;

            ctInsetFromEdge = this.ScrollRectProxy.ContentInsetFromViewportEnd;
            if (ctInsetFromEdge > 0) inset += ctInsetFromEdge;

            return inset;
        }

        #region IScrollRectProxy methods implementation (used if external proxy is not manually assigned)

        /// <summary>Not used in this default interface implementation</summary>
#pragma warning disable 0067
        event System.Action<double> IScrollRectProxy.ScrollPositionChanged { add { } remove { } }
#pragma warning restore 0067
        public void SetNormalizedPosition(double normalizedPosition)
        {
            Debug.Log("SetNormalizedPosition: " + normalizedPosition);
            if (this._HorizontalScrollBar)
                this.scrollRect.horizontalNormalizedPosition = (float)normalizedPosition;
            else
                this.scrollRect.verticalNormalizedPosition = (float)normalizedPosition;
        }

        public double GetNormalizedPosition()
        {
            return this._HorizontalScrollBar ? this.scrollRect.horizontalNormalizedPosition : this.scrollRect.verticalNormalizedPosition;
        }

        public double GetContentSize()
        {
            return this.IsHorizontal ? this.Content.rect.width : this.Content.rect.height;
        }

        public double GetViewportSize()
        {
            return this.IsHorizontal ? this.Viewport.rect.width : this.Viewport.rect.height;
        }

        public void StopMovement()
        {
            this.scrollRect.StopMovement();
        }

        #endregion

        #region Unity UI event callbacks

        public void OnPointerDown(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnPointerDown: " + eventData.pointerId);
            #endif
            this._PointerDownReceivedForCurrentDrag = true;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnInitializePotentialDrag: " + eventData.pointerId + ", State=" + State);
            #endif
            if (this.State == StateEnum.PRE_DRAGGING)
            {
                if (this._PointerDownReceivedForCurrentDrag.Value)
                {
                    #if DEBUG_EVENTS
					Debug.Log("ScrollbarF: OnInitializePotentialDrag: not accepted, already in a valid event");
                    #endif
                    return;
                }

                if (this.ignoreDragWithoutPointerDown)
                {
                    #if DEBUG_EVENTS
					Debug.Log("ScrollbarF: OnInitializePotentialDrag: not accepted, ignoreDragWithoutPointerDown is set. Force resetting pointer event");
                    #endif
                    this.ResetPointerEvent();
                    return;
                }

                #if DEBUG_EVENTS
				Debug.Log("ScrollbarF: OnInitializePotentialDrag: state was " + State + " and ignoreDragWithoutPointerDown=false. " +
					"Force resetting pointer event");
                #endif
                this.ResetPointerEvent();
            }

            if (this._PrimaryEventID != null)
            {
                #if DEBUG_EVENTS
				Debug.Log("ScrollbarF: OnInitializePotentialDrag: not accepted, _PrimaryEventID != null (shouldn't really happen)");
                #endif
                return;
            }

            if (this._PointerDownReceivedForCurrentDrag == null) this._PointerDownReceivedForCurrentDrag = false;

            if (!this._PointerDownReceivedForCurrentDrag.Value && this.ignoreDragWithoutPointerDown) return;

            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnInitializePotentialDrag: about to be accepted");
            #endif

            Assert.AreEqual(StateEnum.NONE, this.State);
            this._PrimaryEventID = eventData.pointerId;
            if (this.externalScrollRectProxy != null && this.externalScrollRectProxy.IsInitialized) this.externalScrollRectProxy.StopMovement();

            this.State = StateEnum.PRE_DRAGGING;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnBeginDrag: " + eventData.pointerId);
            #endif
            if (eventData.pointerId != this._PrimaryEventID) return;

            Assert.AreEqual(StateEnum.PRE_DRAGGING, this.State);
            this.State = StateEnum.DRAGGING;
        }

        public void OnDrag(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnDrag: " + eventData.pointerId);
            #endif
            if (eventData.pointerId != this._PrimaryEventID) return;

            Assert.AreEqual(StateEnum.DRAGGING, this.State);
            this.OnScrollRectValueChanged(false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnPointerUp: " + eventData.pointerId);
            #endif
            if (eventData.pointerId != this._PrimaryEventID) return;

            // OnEndDrag won't get called if the pointer itself didn't move (and thus didn't trigger OnBeginDrag), so we need do FinishDrag here as well
            this.FinishDrag();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnEndDrag: " + eventData.pointerId);
            #endif
            if (eventData.pointerId != this._PrimaryEventID) return;

            // OnPointerUp won't get called if the scrollbar has a child that captured that event, so we need do FinishDrag here as well
            this.FinishDrag();
        }

        #endregion

        private void FinishDrag()
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: FinishDrag: State=" + State);
            #endif
            Assert.IsTrue(this.State == StateEnum.PRE_DRAGGING || this.State == StateEnum.DRAGGING);

            if (this.externalScrollRectProxy != null && this.externalScrollRectProxy.IsInitialized) this.externalScrollRectProxy.StopMovement();

            this.ResetPointerEvent();
        }

        private void ResetPointerEvent()
        {
            #if DEBUG_EVENTS
			Debug.Log("ScrollbarF: ResetPointerEvent: State=" + State);
            #endif
            this._PointerDownReceivedForCurrentDrag = null;
            this._PrimaryEventID                    = null;
            this.State                              = StateEnum.NONE;
        }

        private void InitializeInFirstUpdate()
        {
            if (this.externalScrollRectProxy != null) this.externalScrollRectProxy.ScrollPositionChanged += this.ExternalScrollRectProxy_OnScrollPositionChanged;
            this._FullyInitialized = true;
        }

        private IEnumerator SlowUpdate()
        {
            var waitAmount = new WaitForSecondsRealtime(this.sizeUpdateInterval);

            while (true)
            {
                yield return waitAmount;

                if (!this.enabled) break;

                if (this._ScrollViewRT && ((this.scrollRect && this.scrollRect.content) || (this.externalScrollRectProxy != null && this.externalScrollRectProxy.IsInitialized)))
                {
                    var proxy = this.ScrollRectProxy;

                    double size, viewportSize, contentSize = proxy.GetContentSize();
                    if (this._HorizontalScrollBar)
                        viewportSize = this.viewport.rect.width;
                    else
                        viewportSize = this.viewport.rect.height;

                    double sizeUnclamped;
                    if (contentSize <= 0d || contentSize == double.NaN || contentSize == double.Epsilon || contentSize == double.NegativeInfinity || contentSize == double.PositiveInfinity)
                        sizeUnclamped = size = 1d;
                    else
                    {
                        sizeUnclamped = viewportSize / contentSize;
                        var sizeClamped = this.ClampDouble(sizeUnclamped, this.minSize, this.maxSize);

                        // 17.07.2020: Scrollbar "Pulling" effect as in the classic ScrollRect. Thanks to "GladFox" (Unity forums)
                        var outsideAmtRaw                     = this.GetOutsideBoundsAmountRaw();
                        if (outsideAmtRaw < 0d) outsideAmtRaw = 0d;

                        // Fix 12.06.2022 OSA-62: Scrollbar disregards minSize/maxSize when compressing
                        var outside01 = outsideAmtRaw / viewportSize;
                        if (this.minCompressedSize >= this.minSize)
                            size = sizeClamped;
                        else
                            size = this.LerpDouble(sizeClamped, this.minCompressedSize, outside01);
                    }

                    var oldSizeFloat = this._Scrollbar.size;
                    this._Scrollbar.size = (float)size;
                    var currentSizeFloat = this._Scrollbar.size;

                    if (this.hideWhenNotNeeded)
                    {
                        if (sizeUnclamped > .99d)
                        {
                            if (!this._Hidden)
                            {
                                this._HiddenNotNeeded = true;
                                this.Hide();
                            }
                        }
                        else
                        {
                            // If autohidden, we don't interfere with the process
                            if (this._Hidden && !this._AutoHidden) this.Show();
                        }
                    }
                    // Handling the case when the scrollbar was hidden but its hideWhenNotNeeded property was set to false afterwards
                    // and autoHide is also false, meaning the scrollbar won't ever be shown
                    else if (!this.autoHide)
                        if (this._Hidden)
                            this.Show();

                    if (!this._TriedToCallOnScrollbarSizeChangedAtLeastOnce || oldSizeFloat != currentSizeFloat)
                    {
                        this._TriedToCallOnScrollbarSizeChangedAtLeastOnce = true;
                        if (this.OnScrollbarSizeChanged != null) this.OnScrollbarSizeChanged.Invoke();
                    }
                }
            }
        }

        private void Hide()
        {
            this._Hidden = true;
            if (!this.autoHide || this._HiddenNotNeeded) this.gameObject.transform.localScale = Vector3.zero;
        }

        private void Show()
        {
            this.gameObject.transform.localScale = this._InitialScale;
            this._HiddenNotNeeded                = this._AutoHidden                                    = this._Hidden = false;
            if (this.CheckForAudoHideFadeEffectAndInitIfNeeded()) this._CanvasGroupForFadeEffect.alpha = 1f;

            this.UpdateStartingValuesForAutoHideEffect();
        }

        private void UpdateStartingValuesForAutoHideEffect()
        {
            if (this.CheckForAudoHideFadeEffectAndInitIfNeeded()) this._AlphaOnLastDrag = this._CanvasGroupForFadeEffect.alpha;

            if (this.autoHideCollapseEffect) this._TransversalScaleOnLastDrag = this.transform.localScale[this.ScrollRectProxy.IsHorizontal ? 1 : 0];
        }

        private bool CheckForAudoHideFadeEffectAndInitIfNeeded()
        {
            if (this.autoHideFadeEffect && !this._CanvasGroupForFadeEffect)
            {
                this._CanvasGroupForFadeEffect = this.GetComponent<CanvasGroup>();
                if (!this._CanvasGroupForFadeEffect) this._CanvasGroupForFadeEffect = this.gameObject.AddComponent<CanvasGroup>();
            }

            return this.autoHideFadeEffect;
        }

        private void ScrollRect_OnValueChangedCalled(Vector2 _)
        {
            // Only consider this callback if there's no external proxy provided, which is supposed to call ExternalScrollRectProxy_OnScrollPositionChanged()
            if (this.externalScrollRectProxy == null) this.OnScrollRectValueChanged(true);
        }

        private void ExternalScrollRectProxy_OnScrollPositionChanged(double _)
        {
            this.OnScrollRectValueChanged(true);
        }

        private void OnScrollRectValueChanged(bool fromScrollRect)
        {
            if (!fromScrollRect)
            {
                this.ScrollRectProxy.StopMovement();

                if (this._FrameCountOnLastPositionUpdate + this.skippedFramesBetweenPositionChanges < UnityEngine.Time.frameCount)
                {
                    this.ScrollRectProxy.SetNormalizedPosition(this._Scrollbar.value);
                    this._FrameCountOnLastPositionUpdate = UnityEngine.Time.frameCount;
                }
            }

            this._TimeOnLastValueChange = this.Time;
            if (this.autoHide) this.UpdateStartingValuesForAutoHideEffect();

            if (!this._HiddenNotNeeded
                && this._Scrollbar.size < 1f) // is needed
                this.Show();
        }

        private double ClampDouble(double t, double min, double max)
        {
            if (t < min) return min;
            if (t > max) return max;
            return t;
        }

        private double LerpDouble(double a, double b, double t)
        {
            return a * (1.0 - t) + b * t;
        }

        private enum StateEnum
        {
            NONE,
            PRE_DRAGGING,
            DRAGGING,
        }
    }
}