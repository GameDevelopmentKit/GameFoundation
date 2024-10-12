using System;
using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Visual.UI;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;

namespace Com.ForbiddenByte.OSA.Core
{
    /// <summary>
    /// Script that enables snapping on a <see cref="OSA{TParams, TItemViewsHolder}"/>. Attach it to the ScrollView's game object.
    /// </summary>
    public class Snapper8 : MonoBehaviour
    {
        public float snapWhenSpeedFallsBelow = 50f;
        public float viewportSnapPivot01     = .5f;

        public float itemSnapPivot01 = .5f;

        //public float snapOnlyIfSpeedIsAbove = 20f;
        public float snapDuration = .3f;

        public float snapAllowedError = 1f;

        //[Tooltip("This will be disabled during snapping animation")]
        public Scrollbar scrollbar;

        //public int maxNeighborsToSnapToRegardlessOfSpeed;
        [Tooltip("If the current drag distance is not enough to change the currently centered item, " + "snapping to the next item will still occur if the current speed is bigger than this. " + "Set to a negative value to disable (default). This was initially useful for things like page views")]
        public float minSpeedToAllowSnapToNext = -1;

        public bool skipIfReachedExtremity = true;

        public event Action SnappingStarted;
        public event Action SnappingEndedOrCancelled;

        /// <summary>This needs to be set externally</summary>
        public IOSA Adapter
        {
            set
            {
                if (this._Adapter == value) return;

                if (this._Adapter != null)
                {
                    this._Adapter.ScrollPositionChanged -= this.OnScrolled;
                    this._Adapter.ItemsRefreshed        -= this.OnItemsRefreshed;
                }
                this._Adapter = value;
                if (this._Adapter != null)
                {
                    this._Adapter.ScrollPositionChanged += this.OnScrolled;
                    this._Adapter.ItemsRefreshed        += this.OnItemsRefreshed;
                }
            }
        }

        public bool SnappingInProgress { get; private set; }

        //bool IsAdapterDragInProgress { get { return _ScrollRect != null && Utils.GetPointerEventDataWithPointerDragGO(_ScrollRect.gameObject, false) != null; } }
        private bool IsAdapterDragInProgress => this._Adapter != null && this._Adapter.IsDragging;

        //bool IsScrollbarDragInProgress { get { return scrollbar != null && Utils.GetPointerEventDataWithPointerDragGO(scrollbar.gameObject, false) != null; } }
        private bool IsScrollbarDragInProgress => this._ScrollbarFixer != null && this._ScrollbarFixer.IsDraggingOrPreDragging;

        private IOSA            _Adapter;
        private ScrollbarFixer8 _ScrollbarFixer;
        private bool            _SnappingDoneAndEndSnappingEventPending;
        private bool            _SnapNeeded; // a new snap will only start if after the las snap the scrollrect's scroll position has changed

        private bool _SnappingCancelled;

        //bool _PointerDown;
        private Func<float>   _GetSignedAbstractSpeed;
        private int           _LastSnappedItemIndex = -1;
        private bool          _SnapToNextOnlyEnabled;
        private bool          _StartCalled;
        private Canvas        _Canvas;
        private RectTransform _CanvasRT;
        private bool          _TriedToGetCanvasAtLeastOnce;
        private int           _LastItemIndexUnFinishedSnap = -1;

        private void Start()
        {
            //if (maxNeighborsToSnapToRegardlessOfSpeed < 0)
            //	maxNeighborsToSnapToRegardlessOfSpeed = 0;
            if (this.minSpeedToAllowSnapToNext < 0) this.minSpeedToAllowSnapToNext = float.MaxValue;
            this._SnapToNextOnlyEnabled = this.minSpeedToAllowSnapToNext != float.MaxValue;

            if (this.scrollbar)
            {
                this._ScrollbarFixer = this.scrollbar.GetComponent<ScrollbarFixer8>();
                if (!this._ScrollbarFixer) throw new OSAException("ScrollbarFixer8 should be attached to Scrollbar");
            }

            if (this.scrollbar) this.scrollbar.onValueChanged.AddListener(this.OnScrollbarValueChanged);
            this._StartCalled = true;
        }

        private void OnDisable()
        {
            this.CancelSnappingIfInProgress();
        }

        private void OnDestroy()
        {
            if (this.scrollbar) this.scrollbar.onValueChanged.RemoveListener(this.OnScrollbarValueChanged);
            //if (_ScrollRect)
            //	_ScrollRect.onValueChanged.RemoveListener(OnScrolled);

            this.Adapter = null; // will unregister listeners

            this.SnappingStarted          = null;
            this.SnappingEndedOrCancelled = null;
        }

        internal void CancelSnappingIfInProgress()
        {
            //Debug.Log(
            //	"CancelSnappingIfInProgress:\n" +
            //	"_SnappingDoneAndEndSnappingEventPending=" + _SnappingDoneAndEndSnappingEventPending +
            //	", _SnapNeeded=" + _SnapNeeded +
            //	", SnappingInProgress=" + SnappingInProgress);

            this._SnappingDoneAndEndSnappingEventPending = false;
            this._SnapNeeded                             = false;

            //Debug.Log("cancel: inProg=" + SnappingInProgress);
            if (!this.SnappingInProgress) return;

            this._SnappingCancelled = true;
            this.SnappingInProgress = false;
        }

        internal void StartSnappingIfNeeded()
        {
            if (!this._StartCalled) return;

            // Disabling the script should make it unoperable
            if (!this.enabled) return;

            if (this._SnappingDoneAndEndSnappingEventPending)
            {
                this.OnSnappingEndedOrCancelled();
                return;
            }

            if (this._Adapter == null || !this._Adapter.IsInitialized) return;

            // Commented: this now works
            //if (_Adapter.GetItemsCount() > OSAConst.MAX_ITEMS_TO_SUPPORT_SMOOTH_SCROLL_AND_ITEM_RESIZING)
            //	return;

            // Initializing it here, because in Start the adapter may not be initialized
            if (this._GetSignedAbstractSpeed == null)
            {
                // _ScrollRect.velocity doesn't reflect <curNormPos-prevNormPos>, as it would be expected, but the opposite of that (opposite sign)
                // Returning: negative, if towards end; positive, else.
                if (this._Adapter.BaseParameters.IsHorizontal)
                    this._GetSignedAbstractSpeed = () => this._Adapter.Velocity[0];
                else
                    this._GetSignedAbstractSpeed = () => -this._Adapter.Velocity[1];
            }
            var signedSpeed = this._GetSignedAbstractSpeed();
            var speed       = Mathf.Abs(signedSpeed);

            if (this.SnappingInProgress || !this._SnapNeeded || speed >= this.snapWhenSpeedFallsBelow || this.IsAdapterDragInProgress || this.IsScrollbarDragInProgress) return;

            if (this.skipIfReachedExtremity)
            {
                double maxAllowedDistFromExtremity = Mathf.Clamp(this.snapAllowedError, 1f, 20f);
                var    insetStartOrEnd             = Math.Max(this._Adapter.ContentVirtualInsetFromViewportStart, this._Adapter.ContentVirtualInsetFromViewportEnd);
                if (Math.Abs(insetStartOrEnd) <= maxAllowedDistFromExtremity) // Content is at start/end => don't force any snapping
                    return;
            }

            float distanceToTarget;
            var   middle = this.GetMiddleVH(out distanceToTarget);
            if (middle == null) return;

            this._SnapNeeded = false;
            if (distanceToTarget <= this.snapAllowedError) return;

            //Debug.Log(middle.ItemIndex);

            var indexToSnapTo = middle.ItemIndex;
            var snapToNextOnly =
                speed >= this.minSpeedToAllowSnapToNext
                // Not allowed to skip neighbors. Snapping to neigbors is only allowed if the current middle is the previous middle
                && (indexToSnapTo == this._LastSnappedItemIndex
                    // Update: Allowing skipping neighbors if no snapping occurred yet
                    || this._LastSnappedItemIndex == -1
                    // Update: Allowing skipping neighbors if the previous snap didn't finish naturally (most probably, the user swapped again fast, with the sole intent of skipping an item )
                    || this._LastItemIndexUnFinishedSnap == indexToSnapTo
                );

            if (snapToNextOnly)
            {
                var loopingEnabled = this._Adapter.BaseParameters.effects.LoopItems && this._Adapter.GetContentSizeToViewportRatio() > 1d;
                var count          = this._Adapter.GetItemsCount();
                if (signedSpeed < 0) // going towards end => snap to bigger indexInView
                {
                    if (indexToSnapTo == count - 1 && !loopingEnabled) return;
                    indexToSnapTo = (indexToSnapTo + 1) % count;
                }
                else // going towards start => snap to smaller indexInView
                {
                    if (indexToSnapTo == 0 && !loopingEnabled) return;
                    indexToSnapTo = (indexToSnapTo + count /*adding count to prevent a negative dividend*/ - 1) % count;
                }
            }
            else
                indexToSnapTo = middle.ItemIndex;

            //Debug.Log(
            //	"StartSnappingIfNeeded:\n" +
            //	"SnappingInProgress=" + SnappingInProgress +
            //	", _SnapNeeded=" + _SnapNeeded +
            //	", magnitude=" + _Adapter.Velocity.magnitude +
            //	", IsPointerDraggingOnScrollRect=" + IsAdapterDragInProgress +
            //	", IsPointerDraggingOnScrollbar=" + IsScrollbarDragInProgress +
            //	", signedSpeed " + signedSpeed +
            //	", snapWhenSpeedFallsBelow " + snapWhenSpeedFallsBelow +
            //	", indexToSnapTo Bef (middle.ItemIndex) " + middle.ItemIndex +
            //	", indexToSnapTo Aft (middle.ItemIndex) " + indexToSnapTo +
            //	", _LastSnappedItemIndex " + _LastSnappedItemIndex +
            //	", _LastItemIndexUnFinishedSnap " + _LastItemIndexUnFinishedSnap,
            //	middle.root.gameObject
            //);

            //Debug.Log("start: " + s);
            this._SnappingCancelled = false;
            bool continuteAnimation;
            var  cancelledOrEnded = false; // used to check if the scroll was cancelled immediately after calling SmoothScrollTo (i.e. without first setting SnappingInProgress = true)
            var  doneNaturally    = false;
            this._LastItemIndexUnFinishedSnap = indexToSnapTo;
            this._Adapter.SmoothScrollTo(
                indexToSnapTo,
                this.snapDuration,
                this.viewportSnapPivot01,
                this.itemSnapPivot01,
                progress =>
                {
                    continuteAnimation = true;
                    doneNaturally      = progress == 1f;
                    if (doneNaturally || this._SnappingCancelled || this.IsAdapterDragInProgress || this.IsScrollbarDragInProgress) // done. last iteration
                    {
                        cancelledOrEnded   = true;
                        continuteAnimation = false;

                        //Debug.Log(
                        //	"received end callback: SnappingInProgress=" + SnappingInProgress +
                        //	", doneNaturally=" + doneNaturally +
                        //	", _SnappingCancelled=" + _SnappingCancelled +
                        //	", IsPointerDraggingOnScrollRect=" + IsPointerDraggingOnScrollRect +
                        //	", IsPointerDraggingOnScrollbar=" + IsPointerDraggingOnScrollbar
                        //);
                        if (this.SnappingInProgress)
                        {
                            this._LastSnappedItemIndex                   = indexToSnapTo;
                            this.SnappingInProgress                      = false;
                            this._SnappingDoneAndEndSnappingEventPending = true;

                            if (doneNaturally) this._LastItemIndexUnFinishedSnap = -1;
                        }
                    }

                    // If the items were refreshed while the snap animation was playing or if the user touched the scrollview, don't continue;
                    return continuteAnimation;
                },
                null,
                true
            );

            // The scroll was cancelled immediately after calling SmoothScrollTo => cancel
            if (cancelledOrEnded)
            {
                if (doneNaturally) this._LastItemIndexUnFinishedSnap = -1;

                return;
            }

            this.SnappingInProgress = true; //always true, because we're overriding the previous scroll

            if (this.SnappingInProgress) this.OnSnappingStarted();
        }

        private Canvas FindOrGetCanvas()
        {
            if (this._TriedToGetCanvasAtLeastOnce) return this._Canvas;
            this._TriedToGetCanvasAtLeastOnce = true;

            return this._Canvas = this.GetComponentInParent<Canvas>();
        }

        private RectTransform FindOrGetCanvasRT()
        {
            if (this._TriedToGetCanvasAtLeastOnce) return this._CanvasRT;
            this._TriedToGetCanvasAtLeastOnce = true;

            return this._CanvasRT = this.FindOrGetCanvas().transform as RectTransform;
        }

        public AbstractViewsHolder GetMiddleVH(out float distanceToTarget)
        {
            return this._Adapter.GetViewsHolderClosestToViewportLongitudinalNormalizedAbstractPoint(this.FindOrGetCanvas(), this.FindOrGetCanvasRT(), this.viewportSnapPivot01, this.itemSnapPivot01, out distanceToTarget);
        }

        //void OnScrolled(Vector2 _) { if (!SnappingInProgress) _SnapNeeded = true; }
        private void OnScrolled(double _)
        {
            if (!this.SnappingInProgress)
            {
                this._SnapNeeded = true;

                if (this._SnapToNextOnlyEnabled && !this.IsScrollbarDragInProgress && !this.IsAdapterDragInProgress) this.UpdateLastSnappedIndexFromMiddleVH();
            }
        } // from adapter

        private void OnScrollbarValueChanged(float _)
        {
            if (this.IsScrollbarDragInProgress) this.CancelSnappingIfInProgress();
        } // from scrollbar

        private void OnItemsRefreshed(int newCount, int prevCount)
        {
            if (newCount == prevCount) return;

            if (this._SnapToNextOnlyEnabled) this.UpdateLastSnappedIndexFromMiddleVH();
        }

        private void UpdateLastSnappedIndexFromMiddleVH()
        {
            float _;
            var   middleVH = this.GetMiddleVH(out _);
            this._LastSnappedItemIndex        = middleVH == null ? -1 : middleVH.ItemIndex;
            this._LastItemIndexUnFinishedSnap = -1;
        }

        private void OnSnappingStarted()
        {
            //Debug.Log("start");
            //if (scrollbar)
            //	scrollbar.interactable = false;

            if (this.SnappingStarted != null) this.SnappingStarted();
        }

        private void OnSnappingEndedOrCancelled()
        {
            //Debug.Log("end");
            //if (scrollbar)
            //	scrollbar.interactable = true;

            this._SnappingDoneAndEndSnappingEventPending = false;

            if (this.SnappingEndedOrCancelled != null) this.SnappingEndedOrCancelled();
        }
    }
}