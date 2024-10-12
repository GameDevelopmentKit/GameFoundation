using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using frame8.Logic.Misc.Visual.UI;
using frame8.Logic.Misc.Other.Extensions;

namespace Com.ForbiddenByte.OSA.Util.PullToRefresh
{
    /// <summary>
    /// Attach it to your ScrollView where the pull to refresh functionality is needed.
    /// Browse the PullToRefreshExample scene to see how the gizmo should be set up. An image is better than 1k words.
    /// </summary>
    public class PullToRefreshBehaviour : MonoBehaviour, IScrollRectProxy, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Serialized fields

        /// <summary>The normalized distance relative to screen size. Always between 0 and 1</summary>
        [SerializeField]
        [Range(.1f, 1f)]
        [Tooltip("The normalized distance relative to screen size. Always between 0 and 1")]
        private float _PullAmountNormalized = .25f;

        /// <summary>The reference of the gizmo to use. If null, will try to GetComponentInChildren&lt;PullToRefreshGizmo&gt;()</summary>
        [SerializeField]
        [Tooltip("If null, will try to GetComponentInChildren()")]
        private PullToRefreshGizmo _RefreshGizmo = null;

        //[SerializeField]
        //RectTransform.Axis _Axis;

        /// <summary></summary>
        [SerializeField]
        private bool _AllowPullFromEnd = false;

        /// <summary>If false, you'll need to call HideGizmo() manually after pull. Subscribe to PullToRefreshBehaviour.OnRefresh event to know when a refresh event occurred. This method is used when the gizmo should do an animation while the refresh is executing (for ex., when some data is downloading)</summary>
        [SerializeField]
        [Tooltip("If false, you'll need to call HideGizmo() manually after pull. Subscribe to PullToRefreshBehaviour.OnRefresh event to know when a refresh event occurred")]
        private bool _AutoHideRefreshGizmo = true;
#pragma warning disable 0649
        /// <summary>Quick way of playing a sound effect when the pull power reaches 1f</summary>
        [SerializeField]
        private AudioClip _SoundOnPreRefresh = null;

        /// <summary>Quick way of playing a sound effect when the refresh occurred</summary>
        [SerializeField]
        private AudioClip _SoundOnRefresh = null;

        #endregion

#pragma warning restore 0649

        #region Unity events

        [Tooltip("Unity event fired when the pull was released")]
        /// <summary>Unity event (editable in inspector) fired when the refresh occurred</summary>
        public UnityEvent OnRefresh = null;

        [Tooltip("Same as OnRefresh, but also gives you the refresh sign.\n" + "1 = top, -1 = bottom")]
        /// <summary>Same as <see cref="OnRefresh"/>, but also gives you the refresh sign. 1 = top, -1 = bottom</summary>
        public UnityEventFloat OnRefreshWithSign = null;

        [Tooltip("Unity event (editable in inspector) fired when each frame the click/finger is dragged after it has touched the ScrollView.\n" + "Negative values indicate pulling from end")]
        /// <summary>
        /// Unity event (editable in inspector) fired when each frame the click/finger is dragged after it has touched the ScrollView.
        /// Negative values indicate pulling from end
        /// </summary>
        public UnityEventFloat OnPullProgress = null;

        #endregion

        /// <summary>
        /// Will be retrieved from the scrollrect. If not found, it can be assigned anytime before the first Update. 
        /// If not assigned, a default proxy will be used. The purpose of this is to allow custom implementations of ScrollRect to be used
        /// </summary>
        public IScrollRectProxy externalScrollRectProxy;

        #region IScrollRectProxy properties implementation

        public bool             IsInitialized                 => this._ScrollRect != null;
        public Vector2          Velocity                      { get; set; }
        public bool             IsHorizontal                  => this._ScrollRect.horizontal;
        public bool             IsVertical                    => this._ScrollRect.vertical;
        public RectTransform    Content                       => this._ScrollRect.content;
        public RectTransform    Viewport                      => this._ScrollRect.viewport;
        double IScrollRectProxy.ContentInsetFromViewportStart => this.Content.GetInsetFromParentEdge(this.Viewport, this.ScrollRectProxy.GetStartEdge());
        double IScrollRectProxy.ContentInsetFromViewportEnd   => this.Content.GetInsetFromParentEdge(this.Viewport, this.ScrollRectProxy.GetEndEdge());

        #endregion

        private IScrollRectProxy ScrollRectProxy => this.externalScrollRectProxy == null ? this : this.externalScrollRectProxy;

        private ScrollRect _ScrollRect;
        private float      _ResolvedAVGScreenSize;

        private bool _PlayedPreSoundForCurrentDrag;

        //bool _IgnoreCurrentDrag;
        private RectTransform _RT;
        private int           _CurrentDragSign;
        private StateEnum     _State;

        /// <summary>Not used in this default interface implementation</summary>
#pragma warning disable 0067
        public event Action<double> ScrollPositionChanged = delegate
        {
        };
#pragma warning restore 0067

        private void Awake()
        {
            this._RT                    = this.transform as RectTransform;
            this._ResolvedAVGScreenSize = (Screen.width + Screen.height) / 2f;
            this._ScrollRect            = this.GetComponent<ScrollRect>();
            this._RefreshGizmo          = this.GetComponentInChildren<PullToRefreshGizmo>(); // self or children
            if (this._ScrollRect)
                // May be null
            {
                this.externalScrollRectProxy = this._ScrollRect.GetComponent(typeof(IScrollRectProxy)) as IScrollRectProxy;
            }
            else
            {
                this.externalScrollRectProxy = this.GetComponentInParent(typeof(IScrollRectProxy)) as IScrollRectProxy;
                if (this.externalScrollRectProxy == null)
                {
                    if (this.enabled)
                    {
                        Debug.Log(this.GetType().Name + ": no scrollRect provided and found no " + typeof(IScrollRectProxy).Name + " component among ancestors. Disabling...");
                        this.enabled = false;
                    }
                    return;
                }
            }
        }

        #region IScrollRectProxy methods implementation (used if external proxy is not manually assigned)

        public void SetNormalizedPosition(double normalizedPosition)
        {
        }

        public double GetNormalizedPosition()
        {
            if (this._ScrollRect.horizontal) return this._ScrollRect.horizontalNormalizedPosition;
            return this._ScrollRect.verticalNormalizedPosition;
        }

        public double GetContentSize()
        {
            return this._RT.rect.size[this._ScrollRect.horizontal ? 0 : 1];
        }

        public double GetViewportSize()
        {
            return this.Viewport.rect.size[this._ScrollRect.horizontal ? 0 : 1];
        }

        public void StopMovement()
        {
            this._ScrollRect.StopMovement();
        }

        #endregion

        #region UI callbacks

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!this.isActiveAndEnabled) return;

            if (this._State != StateEnum.NONE) return;

            if (this._RefreshGizmo.IsShown) return;

            if (!this.ScrollRectProxy.IsInitialized) return;

            double dragAmountNorm, _;
            this.GetDragAmountNormalized(eventData, out dragAmountNorm, out _);
            if (!this._AllowPullFromEnd && dragAmountNorm < 0d) return;

            var curDragSign = Math.Sign(dragAmountNorm);
            this._CurrentDragSign              = curDragSign;
            this._PlayedPreSoundForCurrentDrag = false;

            this._State = StateEnum.DRAGGING_WAITING_FOR_PULL;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!this.isActiveAndEnabled) return;

            if (!this.ScrollRectProxy.IsInitialized) return;

            switch (this._State)
            {
                case StateEnum.DRAGGING_WAITING_FOR_PULL:
                    if (!this.IsContentBiggerThanViewport() || this.IsScrollRectAtTarget(this._CurrentDragSign))
                    {
                        this._State = StateEnum.PULLING_WAITING_FOR_RELEASE;
                        goto case StateEnum.PULLING_WAITING_FOR_RELEASE;
                    }

                    return;

                case StateEnum.PULLING_WAITING_FOR_RELEASE:
                    if (this.IsContentBiggerThanViewport() && !this.IsScrollRectAtTarget(this._CurrentDragSign))
                    {
                        this.HideGizmoInternal();
                        this._State = StateEnum.DRAGGING_WAITING_FOR_PULL;
                        return;
                    }

                    double dragAmountNorm, deltaNorm;
                    this.GetDragAmountNormalized(eventData, out dragAmountNorm, out deltaNorm);
                    if (Math.Sign(dragAmountNorm) != this._CurrentDragSign) return;

                    //if (!_AllowPullFromEnd && dragAmountNorm < 0d)
                    //{
                    //	HideGizmoInternal();
                    //	return;
                    //}

                    var pullPower = dragAmountNorm;

                    this.ShowGizmoIfNeeded();
                    if (this._RefreshGizmo) this._RefreshGizmo.OnPull(pullPower);

                    if (this.OnPullProgress != null) this.OnPullProgress.Invoke((float)pullPower);

                    if (Math.Abs(pullPower) >= 1d && !this._PlayedPreSoundForCurrentDrag)
                    {
                        this._PlayedPreSoundForCurrentDrag = true;

                        if (this._SoundOnPreRefresh) AudioSource.PlayClipAtPoint(this._SoundOnPreRefresh, Camera.main.transform.position);
                    }

                    return;
            }

            //Debug.Log("eventData.pressPosition=" + eventData.pressPosition + "\n eventData.position=" + eventData.position + "\neventData.scrollDelta="+ eventData.scrollDelta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!this.ScrollRectProxy.IsInitialized) return;

            if (this._State != StateEnum.PULLING_WAITING_FOR_RELEASE)
            {
                if (this._State == StateEnum.DRAGGING_WAITING_FOR_PULL)
                {
                    this.HideGizmoInternal();
                    this._State = StateEnum.NONE;
                }

                return;
            }

            var proceedRefresh = false;
            if (this.isActiveAndEnabled)
            {
                proceedRefresh = true;
                if (this.IsContentBiggerThanViewport()) proceedRefresh = this.IsScrollRectAtTarget(this._CurrentDragSign);

                if (proceedRefresh)
                {
                    double dragAmount, _;
                    this.GetDragAmountNormalized(eventData, out dragAmount, out _);
                    if (Math.Sign(dragAmount) != this._CurrentDragSign)
                        proceedRefresh                                 = false;
                    else if (Math.Abs(dragAmount) < 1d) proceedRefresh = false;
                }
            }

            if (proceedRefresh)
            {
                if (this.OnRefresh != null) this.OnRefresh.Invoke();

                if (this.OnRefreshWithSign != null) this.OnRefreshWithSign.Invoke(this._CurrentDragSign);

                if (this._RefreshGizmo) this._RefreshGizmo.OnRefreshed(this._AutoHideRefreshGizmo);

                if (this._SoundOnRefresh) AudioSource.PlayClipAtPoint(this._SoundOnRefresh, Camera.main.transform.position);
            }
            else
            {
                if (this._RefreshGizmo) this._RefreshGizmo.OnRefreshCancelled();
                this._State = StateEnum.NONE;
            }

            if (this._RefreshGizmo && this._RefreshGizmo.IsShown)
            {
                if (this._AutoHideRefreshGizmo)
                {
                    this.HideGizmoInternal();
                    this._State = StateEnum.NONE;
                }
                else
                {
                    this._State = StateEnum.AFTER_RELEASE_WAITING_FOR_GIZMO_TO_HIDE;
                }
            }
        }

        #endregion

        // sign: 1=start(top or left), -1=end(bottom or right); 
        private bool IsScrollRectAtTarget(int targetDragSign)
        {
            var normPos                                    = this.ScrollRectProxy.GetNormalizedPosition();
            if (this.ScrollRectProxy.IsHorizontal) normPos = 1d - normPos;

            if (targetDragSign == 1 && normPos >= 1d) return true;
            if (targetDragSign == -1 && normPos <= 0d) return true;

            return false;
        }

        private bool IsContentBiggerThanViewport()
        {
            return this.ScrollRectProxy.GetContentSize() > this._RT.rect.size[this.ScrollRectProxy.IsHorizontal ? 0 : 1];
        }

        public void ShowGizmoIfNeeded()
        {
            if (this._RefreshGizmo && !this._RefreshGizmo.IsShown) this._RefreshGizmo.IsShown = true;
        }

        public void HideGizmo()
        {
            this.HideGizmoInternal();
            if (this._State == StateEnum.AFTER_RELEASE_WAITING_FOR_GIZMO_TO_HIDE) this._State = StateEnum.NONE;
        }

        private void HideGizmoInternal()
        {
            if (this._RefreshGizmo) this._RefreshGizmo.IsShown = false;
        }

        private void GetDragAmountNormalized(PointerEventData eventData, out double total, out double delta)
        {
            total = 0d;
            delta = 0d;
            float pos;
            var   maxPullAmount = this._PullAmountNormalized * this._ResolvedAVGScreenSize;
            if (this.ScrollRectProxy.IsVertical)
            {
                pos = eventData.position.y;
                var worldDragVec = pos - eventData.pressPosition.y;
                total = -worldDragVec;
                delta = -eventData.delta.y;
            }
            else
            {
                pos = eventData.position.x;
                var worldDragVec = pos - eventData.pressPosition.x;
                total = worldDragVec;
                delta = eventData.delta.x;
            }
            total /= maxPullAmount;
            delta /= maxPullAmount;
        }

        private enum StateEnum
        {
            NONE,
            DRAGGING_WAITING_FOR_PULL,
            PULLING_WAITING_FOR_RELEASE,
            AFTER_RELEASE_WAITING_FOR_GIZMO_TO_HIDE,
        }

        [Serializable]
        public class UnityEventFloat : UnityEvent<float>
        {
        }
    }
}