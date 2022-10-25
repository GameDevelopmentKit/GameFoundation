﻿//#define DEBUG_EVENTS

//using System;
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
        public bool autoHide = true;

		[Tooltip("A CanvasGroup will be added to the Scrollbar, if not already present, and the fade effect will be achieved by changing its alpha property")]
		public bool autoHideFadeEffect = true;

		[Tooltip("The collapsing effect will change the localScale of the Scrollbar, so the pivot's position decides in what direction it'll grow/shrink.\n " +
				 "Note that sometimes a really nice effect is achieved by placing the pivot slightly outside the rect (the minimized scrollbar will move outside while collapsing)")]
        public bool autoHideCollapseEffect = false;

		[Tooltip("Used if autoHide is on. Duration in seconds")]
        public float autoHideTime = 1f;

		public float autoHideFadeEffectMinAlpha = .8f;
		public float autoHideCollapseEffectMinScale = .2f;

		[Range(0.01f, 1f)]
		public float minSize = .1f;

		[Range(0.01f, 1f)]
		[Tooltip("When using elasticity in the ScrollView and pulling the content outside bounds, this value determines how small can the scrollbar get")]
		public float minCompressedSize = .1f;

		[Range(0.01f, 1f)]
		public float maxSize = 1f;

		[Range(0.005f, 2f)]
		public float sizeUpdateInterval = .01f;

		[Tooltip("Used to prevent updates to be processed too often, in case this is a concern")]
		public int skippedFramesBetweenPositionChanges;

        [Tooltip("If not assigned, will try to find a ScrollRect or an IScrollRectProxy in the parent")]
        public ScrollRect scrollRect;

        [Tooltip("If not assigned, will use the resolved scrollRect")]
        public RectTransform viewport;

		public UnityEvent OnScrollbarSizeChanged;

		/// <summary>
		/// Will be retrieved from the scrollrect. If not found, it can be assigned anytime before the first Update. 
		/// If not assigned, a default proxy will be used. The purpose of this is to allow custom implementations of ScrollRect to be used
		/// </summary>
		public IScrollRectProxy externalScrollRectProxy
		{
			get { return _ExternalScrollRectProxy; }
			set
			{
				_ExternalScrollRectProxy = value;

				if (_ExternalScrollRectProxy != null)
				{
					if (scrollRect)
					{
						scrollRect.onValueChanged.RemoveListener(ScrollRect_OnValueChangedCalled);
						scrollRect = null;
					}
				}
			}
		}
		IScrollRectProxy _ExternalScrollRectProxy;

		#region IScrollRectProxy properties implementation
		public bool IsInitialized { get { return scrollRect != null; } }
		public Vector2 Velocity { get; set; }
		public bool IsHorizontal { get { return scrollRect.horizontal; } }
		public bool IsVertical { get { return scrollRect.vertical; } }
		public RectTransform Content { get { return scrollRect.content; } }
		public RectTransform Viewport { get { return scrollRect.viewport != null ? scrollRect.viewport : (scrollRect.transform as RectTransform); } }
		double IScrollRectProxy.ContentInsetFromViewportStart { get { return Content.GetInsetFromParentEdge(Viewport, ScrollRectProxy.GetStartEdge()); } }
		double IScrollRectProxy.ContentInsetFromViewportEnd { get { return Content.GetInsetFromParentEdge(Viewport, ScrollRectProxy.GetEndEdge()); } }
		#endregion

		public bool IsPreDragging { get { return _State == StateEnum.PRE_DRAGGING; } }
		public bool IsDragging { get { return _State == StateEnum.DRAGGING; } }
		public bool IsDraggingOrPreDragging { get { return IsDragging || IsPreDragging; } }

		/// <summary> Using Scaled time for a scrollbar's animation doesn't make too much sense, so we're always using unscaledTime</summary>
		float Time { get { return UnityEngine.Time.unscaledTime; } }

		IScrollRectProxy ScrollRectProxy { get { return externalScrollRectProxy == null ? this : externalScrollRectProxy; } }

		const float HIDE_EFFECT_START_DELAY_01 = .4f; // relative to this.autoHideTime

		RectTransform _ScrollViewRT;
        Scrollbar _Scrollbar;
		CanvasGroup _CanvasGroupForFadeEffect;
        bool _HorizontalScrollBar;
        Vector3 _InitialScale = Vector3.one;
        bool _Hidden, _AutoHidden, _HiddenNotNeeded;
		double _LastValue;
        float _TimeOnLastValueChange;
		StateEnum _State = StateEnum.NONE;
        IEnumerator _SlowUpdateCoroutine;
		float _TransversalScaleOnLastDrag, _AlphaOnLastDrag;
		bool _FullyInitialized;
		int _FrameCountOnLastPositionUpdate;
		bool _TriedToCallOnScrollbarSizeChangedAtLeastOnce;
		int? _PrimaryEventID;


		void Awake()
        {
			if (autoHideTime == 0f)
				autoHideTime = 1f;

			_Scrollbar = GetComponent<Scrollbar>();
			_InitialScale = _Scrollbar.transform.localScale;
            _LastValue = _Scrollbar.value;
            _TimeOnLastValueChange = Time;
            _HorizontalScrollBar = _Scrollbar.direction == Scrollbar.Direction.LeftToRight || _Scrollbar.direction == Scrollbar.Direction.RightToLeft;

			if (externalScrollRectProxy == null)
			{
				if (!scrollRect)
				{
					scrollRect = GetComponentInParent<ScrollRect>();
					//if (!scrollRect)
					//    throw new UnityException("Please provide a ScrollRect for ScrollbarFixer8 to work");
				}
			}

            if (scrollRect)
            {
                _ScrollViewRT = scrollRect.transform as RectTransform;
				if (_HorizontalScrollBar)
                {
                    if (!scrollRect.horizontal)
                        throw new UnityException("ScrollbarFixer8: Can't use horizontal scrollbar with non-horizontal scrollRect");

                    if (scrollRect.horizontalScrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: setting scrollRect.horizontalScrollbar to null (the whole point of using ScrollbarFixer8 is to NOT have any scrollbars assigned)");
                        scrollRect.horizontalScrollbar = null;
                    }
                    if (scrollRect.verticalScrollbar == _Scrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: Can't use the same scrollbar for both vert and hor");
                        scrollRect.verticalScrollbar = null;
                    }
                }
                else
                {
                    if (!scrollRect.vertical)
                        throw new UnityException("Can't use vertical scrollbar with non-vertical scrollRect");

                    if (scrollRect.verticalScrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: setting scrollRect.verticalScrollbar to null (the whole point of using ScrollbarFixer8 is to NOT have any scrollbars assigned)");
                        scrollRect.verticalScrollbar = null;
                    }
                    if (scrollRect.horizontalScrollbar == _Scrollbar)
                    {
                        Debug.Log("ScrollbarFixer8: Can't use the same scrollbar for both vert and hor");
                        scrollRect.horizontalScrollbar = null;
                    }
                }

            }
			else
			{

			}

			if (scrollRect)
			{
				scrollRect.onValueChanged.AddListener(ScrollRect_OnValueChangedCalled);

				// May be null
				externalScrollRectProxy = scrollRect.GetComponent(typeof(IScrollRectProxy)) as IScrollRectProxy;
			}
			else
			{
				if (externalScrollRectProxy == null)
				{
					// Start with directly with the parent when searching for IScrollRectProxy, as the scrollbar itself is a IScrollRectProxy and needs to be avoided;
					externalScrollRectProxy = transform.parent.GetComponentInParent(typeof(IScrollRectProxy)) as IScrollRectProxy;
					if (externalScrollRectProxy == null)
					{
						// Try starting from the viewport, as the scrollbar might not be a child of the target ScrollView
						if (viewport)
							externalScrollRectProxy = viewport.GetComponentInParent(typeof(IScrollRectProxy)) as IScrollRectProxy;

						if (externalScrollRectProxy == null)
						{
							if (enabled)
							{
								Debug.Log(GetType().Name + ": no scrollRect provided and found no " + typeof(IScrollRectProxy).Name + " component among ancestors. Disabling...");
								enabled = false;
							}
							return;
						}
					}
				}

				_ScrollViewRT = externalScrollRectProxy.gameObject.transform as RectTransform;
			}

			if (!viewport)
				viewport = _ScrollViewRT;

			if (autoHide)
				UpdateStartingValuesForAutoHideEffect();
		}

		//void Start()
		//{
		//	// In case useUnscaledTime has changed between Awake and Start, this needs to be updated
		//	_TimeOnLastValueChange = Time;
		//}

		void OnEnable()
        {
			ResetPointerEvent(); // in case _State was stuck in non-NONE and the object was disabled
			_SlowUpdateCoroutine = SlowUpdate();

			StartCoroutine(_SlowUpdateCoroutine);
		}

		void Update()
		{
			//Application.targetFrameRate = 6;
			if (!_FullyInitialized)
				InitializeInFirstUpdate();

			//Debug.Log("_Scrollbar.value " + _Scrollbar.value);
			//Debug.Log("_Scrollbar.value " + _Scrollbar.value + ", _Scrollbar.size " + _Scrollbar.size);
			//return;

			if (scrollRect || externalScrollRectProxy != null && externalScrollRectProxy.IsInitialized)
			{
				// Don't override between pointer down event and OnPointerUp or OnBeginDrag
				// Don't override when dragging
				if (_State == StateEnum.NONE)
					UpdateOnNoDragging();
				else if (_State == StateEnum.PRE_DRAGGING)
					UpdateOnPreDragging();
			}
		}

        void UpdateOnNoDragging()
		{
			var value = ScrollRectProxy.GetNormalizedPosition();
			//Debug.Log("_Scrollbar.value = " + (float)value);
			_Scrollbar.value = (float)value;
			if (autoHide)
			{
				if (value == _LastValue)
				{
					if (!_Hidden)
					{
						float timePassedForHide01 = Mathf.Clamp01((Time - _TimeOnLastValueChange) / autoHideTime);
						if (timePassedForHide01 >= HIDE_EFFECT_START_DELAY_01)
						{
							float hideEffectAmount01 = (timePassedForHide01 - HIDE_EFFECT_START_DELAY_01) / (1f - HIDE_EFFECT_START_DELAY_01);
							hideEffectAmount01 = hideEffectAmount01 * hideEffectAmount01 * hideEffectAmount01; // slow in, fast-out effect
							if (CheckForAudoHideFadeEffectAndInitIfNeeded())
								_CanvasGroupForFadeEffect.alpha = Mathf.Lerp(_AlphaOnLastDrag, autoHideFadeEffectMinAlpha, hideEffectAmount01);

							if (autoHideCollapseEffect)
							{
								Vector3 localScale = transform.localScale;
								localScale[ScrollRectProxy.IsHorizontal ? 1 : 0] = Mathf.Lerp(_TransversalScaleOnLastDrag, autoHideCollapseEffectMinScale, hideEffectAmount01);
								transform.localScale = localScale;
							}
						}

						if (timePassedForHide01 == 1f)
						{
							_AutoHidden = true;
							Hide();
						}
					}
				}
				else
				{
					_TimeOnLastValueChange = Time;
					_LastValue = value;

					if (_Hidden && !_HiddenNotNeeded)
						Show();
				}
			}
			// Handling the case when the scrollbar was hidden but its autoHide property was set to false afterwards 
			// and hideWhenNotNeeded is also false, meaning the scrollbar won't ever be shown
			else if (!hideWhenNotNeeded)
			{
				if (_Hidden)
					Show();
			}
		}

		void UpdateOnPreDragging()
        {
			// During keeping the pointer down (and not moving), we're constantly updating ScrollRect's position,
			// as in most cases the Scrollbar will move towards the pointer across multiple frames
			OnScrollRectValueChanged(false);
		}

		void OnDisable()
        {
			if (_SlowUpdateCoroutine != null)
				StopCoroutine(_SlowUpdateCoroutine);
		}

		void OnDestroy()
		{
			if (scrollRect)
				scrollRect.onValueChanged.RemoveListener(ScrollRect_OnValueChangedCalled);

			if (externalScrollRectProxy != null)
				externalScrollRectProxy.ScrollPositionChanged -= ExternalScrollRectProxy_OnScrollPositionChanged;
		}

		double GetOutsideBoundsAmountRaw()
		{
			if (ScrollRectProxy == null)
				return 0d;

			double inset = 0;

			var ctInsetFromEdge = ScrollRectProxy.ContentInsetFromViewportStart;
			if (ctInsetFromEdge > 0) 
				inset += ctInsetFromEdge;

			ctInsetFromEdge = ScrollRectProxy.ContentInsetFromViewportEnd;
			if (ctInsetFromEdge > 0) 
				inset += ctInsetFromEdge;

			return inset;

			//size -= size * inset / viewportSize; 

			//if (inset > 0) 
			//{ 
			//	size -= size * inset / viewportSize; 
			//	size = System.Math.Max(minCompressedSize, System.Math.Min(maxSize, size)); 
			//} 
			//else 
			//{ 
			//	size = System.Math.Max(minSize, System.Math.Min(maxSize, size)); 
			//}
		}

		#region IScrollRectProxy methods implementation (used if external proxy is not manually assigned)
		/// <summary>Not used in this default interface implementation</summary>
#pragma warning disable 0067
		event System.Action<double> IScrollRectProxy.ScrollPositionChanged { add { } remove { } }
#pragma warning restore 0067
		public void SetNormalizedPosition(double normalizedPosition) 
		{ 
			if (_HorizontalScrollBar) 
				scrollRect.horizontalNormalizedPosition = (float)normalizedPosition; 
			else 
				scrollRect.verticalNormalizedPosition = (float)normalizedPosition; 
		}
		public double GetNormalizedPosition() { return _HorizontalScrollBar ? scrollRect.horizontalNormalizedPosition : scrollRect.verticalNormalizedPosition; }
		public double GetContentSize() { return IsHorizontal ? Content.rect.width : Content.rect.height; }
		public double GetViewportSize() { return IsHorizontal ? Viewport.rect.width : Viewport.rect.height; }
		public void StopMovement() { scrollRect.StopMovement(); }
		#endregion

		#region Unity UI event callbacks
		//        public override void OnMove(AxisEventData eventData)
		//		{
		//#if DEBUG_EVENTS
		//			Debug.Log("ScrollbarF: OnMove: " + eventData.moveDir);
		//#endif
		//			base.OnMove(eventData);

		//        }

		public void OnPointerDown(PointerEventData eventData)
		{
#if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnPointerDown: " + eventData.pointerId);
#endif
		}
		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
#if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnInitializePotentialDrag: " + eventData.pointerId);
#endif
			if (_PrimaryEventID != null)
				return;

			Assert.AreEqual(StateEnum.NONE, _State);
			_PrimaryEventID = eventData.pointerId;
			if (externalScrollRectProxy != null && externalScrollRectProxy.IsInitialized)
				externalScrollRectProxy.StopMovement();
			//_Scrollbar.OnPointerDown(eventData);

			//OnScrollRectValueChanged(false);
			_State = StateEnum.PRE_DRAGGING;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
#if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnBeginDrag: " + eventData.pointerId);
#endif
			if (eventData.pointerId != _PrimaryEventID)
				return;

			Assert.AreEqual(StateEnum.PRE_DRAGGING, _State);
			//_Scrollbar.OnBeginDrag(eventData);
			_State = StateEnum.DRAGGING;
		}
		public void OnDrag(PointerEventData eventData)
		{
#if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnDrag: " + eventData.pointerId);
#endif
			if (eventData.pointerId != _PrimaryEventID)
				return;

			Assert.AreEqual(StateEnum.DRAGGING, _State);
			//_Scrollbar.OnDrag(eventData);
			OnScrollRectValueChanged(false);
		}
		public void OnPointerUp(PointerEventData eventData)
		{
#if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnPointerUp: " + eventData.pointerId);
#endif
			if (eventData.pointerId != _PrimaryEventID)
				return;

			// OnEndDrag won't get called if the pointer itself didn't move (and thus didn't trigger OnBeginDrag), so we need do FinishDrag here as well
			FinishDrag();
		}

		public void OnEndDrag(PointerEventData eventData)
		{
#if DEBUG_EVENTS
			Debug.Log("ScrollbarF: OnEndDrag: " + eventData.pointerId);
#endif
			if (eventData.pointerId != _PrimaryEventID)
				return;

			// OnPointerUp won't get called if the scrollbar has a child that captured that event, so we need do FinishDrag here as well
			FinishDrag();
		}
		#endregion

		void FinishDrag()
		{
			Assert.IsTrue(_State == StateEnum.PRE_DRAGGING || _State == StateEnum.DRAGGING);

			if (externalScrollRectProxy != null && externalScrollRectProxy.IsInitialized)
				externalScrollRectProxy.StopMovement();
			//var bef = _Scrollbar.value;
			//_Scrollbar.OnPointerUp(eventData);
			//var aft = _Scrollbar.value;
			//Debug.Log("ScrollbarF: OnPointerUp: bef " + bef + ", aft " + aft);
			//OnScrollRectValueChanged(false);
			ResetPointerEvent();
		}

		void ResetPointerEvent()
		{
			_PrimaryEventID = null;
			_State = StateEnum.NONE;
		}

		void InitializeInFirstUpdate()
		{
			if (externalScrollRectProxy != null)
				externalScrollRectProxy.ScrollPositionChanged += ExternalScrollRectProxy_OnScrollPositionChanged;
			_FullyInitialized = true;
		}

		IEnumerator SlowUpdate()
		{
			var waitAmount = new WaitForSecondsRealtime(sizeUpdateInterval);

            while (true)
            {
                yield return waitAmount;

                if (!enabled)
                    break;

                if (_ScrollViewRT && (scrollRect && scrollRect.content || externalScrollRectProxy != null && externalScrollRectProxy.IsInitialized))
                {
					var proxy = ScrollRectProxy;

					double size, viewportSize, contentSize = proxy.GetContentSize();
                    if (_HorizontalScrollBar)
                        viewportSize = viewport.rect.width;
                    else
                        viewportSize = viewport.rect.height;

					double sizeUnclamped;
					if (contentSize <= 0d || contentSize == double.NaN || contentSize == double.Epsilon || contentSize == double.NegativeInfinity || contentSize == double.PositiveInfinity)
						sizeUnclamped = size = 1d;
					else
					{
						sizeUnclamped = viewportSize / contentSize;
						var sizeClamped = ClampDouble(sizeUnclamped, minSize, maxSize);

						// 17.07.2020: Scrollbar "Pulling" effect as in the classic ScrollRect. Thanks to "GladFox" (Unity forums)
						double outsideAmtRaw = GetOutsideBoundsAmountRaw();
						if (outsideAmtRaw < 0d)
							outsideAmtRaw = 0d;

						// Fix 12.06.2022 OSA-62: Scrollbar disregards minSize/maxSize when compressing
						double outside01 = outsideAmtRaw / viewportSize;
						if (minCompressedSize >= minSize)
							size = sizeClamped;
						else
						{
							size = LerpDouble(sizeClamped, minCompressedSize, outside01);
						}
					}

					//Debug.Log(viewportSize + ", ct=" + contentSize);

					float oldSizeFloat = _Scrollbar.size;
                    _Scrollbar.size = (float)size;
					float currentSizeFloat = _Scrollbar.size;
     //               Debug.Log(
					//	"oldSize " + oldSizeFloat + ", curSize " + currentSizeFloat + " (requested " + 
					//	size + ", unclamped "+ sizeUnclamped + ", contentSize "+ contentSize + "), from " + proxy.Content.parent.parent.gameObject.name
					//);

                    if (hideWhenNotNeeded)
                    {
                        if (sizeUnclamped > .99d)
                        {
                            if (!_Hidden)
                            {
                                _HiddenNotNeeded = true;
                                Hide();
                            }
                        }
                        else
                        {
                            if (_Hidden && !_AutoHidden) // if autohidden, we don't interfere with the process
                            {

                                Show();
                            }
                        }
                    }
                    // Handling the case when the scrollbar was hidden but its hideWhenNotNeeded property was set to false afterwards
                    // and autoHide is also false, meaning the scrollbar won't ever be shown
                    else if (!autoHide)
                    {
                        if (_Hidden)
                            Show();
                    }

					if (!_TriedToCallOnScrollbarSizeChangedAtLeastOnce || oldSizeFloat != currentSizeFloat)
					{
						_TriedToCallOnScrollbarSizeChangedAtLeastOnce = true;
						if (OnScrollbarSizeChanged != null)
							OnScrollbarSizeChanged.Invoke();
					}
                }
            }
        }

        void Hide()
        {
            _Hidden = true;
			if (!autoHide || _HiddenNotNeeded)
				gameObject.transform.localScale = Vector3.zero;
        }

        void Show()
        {
            gameObject.transform.localScale = _InitialScale;
            _HiddenNotNeeded = _AutoHidden = _Hidden = false;
			if (CheckForAudoHideFadeEffectAndInitIfNeeded())
				_CanvasGroupForFadeEffect.alpha = 1f;

			UpdateStartingValuesForAutoHideEffect();
		}

		void UpdateStartingValuesForAutoHideEffect()
		{
			if (CheckForAudoHideFadeEffectAndInitIfNeeded())
				_AlphaOnLastDrag = _CanvasGroupForFadeEffect.alpha;

			if (autoHideCollapseEffect)
				_TransversalScaleOnLastDrag = transform.localScale[ScrollRectProxy.IsHorizontal ? 1 : 0];
		}

		bool CheckForAudoHideFadeEffectAndInitIfNeeded()
		{
			if (autoHideFadeEffect && !_CanvasGroupForFadeEffect)
			{
				_CanvasGroupForFadeEffect = GetComponent<CanvasGroup>();
				if (!_CanvasGroupForFadeEffect)
					_CanvasGroupForFadeEffect = gameObject.AddComponent<CanvasGroup>();
			}

			return autoHideFadeEffect;
		}

		void ScrollRect_OnValueChangedCalled(Vector2 _)
		{
			// Only consider this callback if there's no external proxy provided, which is supposed to call ExternalScrollRectProxy_OnScrollPositionChanged()
			if (externalScrollRectProxy == null)
				OnScrollRectValueChanged(true);
		}

		void ExternalScrollRectProxy_OnScrollPositionChanged(double _) { OnScrollRectValueChanged(true); }

		void OnScrollRectValueChanged(bool fromScrollRect)
		{
			if (!fromScrollRect)
			{
				//Debug.Log("OnScrollRectValueChanged " + fromScrollRect + ", val " + _Scrollbar.value);
				ScrollRectProxy.StopMovement();

				if (_FrameCountOnLastPositionUpdate + skippedFramesBetweenPositionChanges < UnityEngine.Time.frameCount)
				{
					ScrollRectProxy.SetNormalizedPosition(_Scrollbar.value);
					//var newNormPos = (float)ScrollRectProxy.GetNormalizedPosition();
					//Debug.Log("  OnScrollRectValueChanged newNormPos " + newNormPos);
					//_Scrollbar.value = newNormPos;
					_FrameCountOnLastPositionUpdate = UnityEngine.Time.frameCount;
				}
			}

			//var normPos = ScrollRectProxy.GetNormalizedPosition();
			//if (_HorizontalScrollBar)
			//	normPos.x = _Scrollbar.value;
			//else
			//	normPos.y = _Scrollbar.value;

			//if (!fromScrollRect)
			//	ScrollRectProxy.SetNormalizedPosition(_Scrollbar.value);

			_TimeOnLastValueChange = Time;
			if (autoHide)
				UpdateStartingValuesForAutoHideEffect();

			if (!_HiddenNotNeeded
				&& _Scrollbar.size < 1f) // is needed
				Show();
		}

		double ClampDouble(double t, double min, double max)
		{
			if (t < min)
				return min;
			if (t > max)
				return max;
			return t;
		}

		double LerpDouble(double a, double b, double t)
		{
			return a * (1.0 - t) + b * t;
		}


		enum StateEnum
        {
			NONE,
			PRE_DRAGGING,
			DRAGGING
        }
	}
}