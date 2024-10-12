using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using frame8.Logic.Misc.Other.Extensions;
using Com.ForbiddenByte.OSA.Core.SubComponents;
using Com.ForbiddenByte.OSA.Core.Data;
using Com.ForbiddenByte.OSA.Core.Data.Gallery;
using Com.ForbiddenByte.OSA.Core.Data.Animations;

namespace Com.ForbiddenByte.OSA.Core
{
    /// <summary>
    /// <para>Input params to be passed to <see cref="OSA{TParams, TItemViewsHolder}.Init()"/></para>
    /// <para>This can be used Monobehaviour's field and exposed via inspector (most common case)</para>
    /// <para>Or can be manually constructed, depending on what's easier in your context</para>
    /// </summary>
    [Serializable]
    public class BaseParams
    {
        #region Configuration

        #region Core params

        [SerializeField] private RectTransform _Content = null;
        public                   RectTransform Content { get => this._Content; set => this._Content = value; }

        [Tooltip("If null, the scrollRect is considered to be the viewport")] [SerializeField] [FormerlySerializedAs("viewport")] private RectTransform _Viewport = null;

        /// <summary>If null, <see cref="ScrollViewRT"/> is considered to be the viewport</summary>
        public RectTransform Viewport { get => this._Viewport; set => this._Viewport = value; }

        [SerializeField] private OrientationEnum _Orientation = OrientationEnum.VERTICAL;
        public                   OrientationEnum Orientation { get => this._Orientation; set => this._Orientation = value; }

        [SerializeField] private Scrollbar _Scrollbar = null;
        public                   Scrollbar Scrollbar { get => this._Scrollbar; set => this._Scrollbar = value; }

        [Tooltip("The sensivity to the Mouse's scrolling wheel or similar input methods. " + "Not related to dragging or scrolling via scrollbar")]
        [SerializeField]
        private float _ScrollSensivity = 100f;

        /// <summary>The sensivity to the Mouse's scrolling wheel or similar input methods. Not related to dragging or scrolling via scrollbar</summary>
        public float ScrollSensivity { get => this._ScrollSensivity; set => this._ScrollSensivity = value; }

        [Tooltip("The sensivity to the Mouse's horizontal scrolling wheel (where supported) or similar input methods that send scroll signals on the horizontal axis. " + "Not related to dragging or scrolling via scrollbar. \n" + "It's set to a positive value by default to comply with Unity's ScrollRect (which for some reason inverts the left and right directions). \n" + "If you'll nest OSA in a regular horizontal ScrollRect, set the ScrollRect's sensivity to a positive value and OSA's ScrollSensivityOnXAxis to a negative value to get an intuitive scroll")]
        [SerializeField]
        private float _ScrollSensivityOnXAxis = 100f;

        /// <summary>The sensivity to the Mouse's left/right scrolling wheel (where supported) or similar input methods that send scroll signals on the horizontal axis. Not related to dragging or scrolling via scrollbar</summary>
        public float ScrollSensivityOnXAxis { get => this._ScrollSensivityOnXAxis; set => this._ScrollSensivityOnXAxis = value; }

        [SerializeField]
        //[HideInInspector]
        [Tooltip("Padding for the 4 edges of the content panel.\n"
            + "Tip: if using a fixed, constant ItemTransversalSize, also set the paddings in the"
            + " transversal direction to -1 (left/right for vertical ScrollView and vice-versa)."
            + " This aligns the item in the center, transversally")]
        [FormerlySerializedAs("contentPadding")]
        private RectOffset _ContentPadding = new();

        /// <summary>
        /// Padding for the 4 edges of the content panel. 
        /// <para>Tip: if using a fixed, constant <see cref="ItemTransversalSize"/>, also set the paddings in the 
        /// transversal direction to -1 (left/right for vertical ScrollView and vice-versa). This aligns the item in the center, transversally</para>
        /// </summary>
        public RectOffset ContentPadding { get => this._ContentPadding; set => this._ContentPadding = value; }

        [SerializeField]
        //[HideInInspector]
        [FormerlySerializedAs("contentGravity")]
        private ContentGravity _Gravity = ContentGravity.START;

        /// <summary>
        /// The effect of this property can only be seen when the content size is smaller than the viewport, case in which there are 3 possibilities: 
        /// place the content at the start, middle or end. <see cref="ContentGravity.FROM_PIVOT"/> doesn't change the content's position (it'll be preserved from the way you aligned it in edit-mode)
        /// </summary>
        public ContentGravity Gravity { get => this._Gravity; set => this._Gravity = value; }

        [Tooltip("The space between items")]
        [SerializeField]
        //[HideInInspector]
        [FormerlySerializedAs("contentSpacing")]
        private float _ContentSpacing = 0f;

        /// <summary>Spacing between items (horizontal if the ScrollView is horizontal. else, vertical)</summary>
        public float ContentSpacing { get => this._ContentSpacing; set => this._ContentSpacing = value; }

        [Tooltip("The size of all items for which the size is not specified in CollectItemSizes()")]
        [SerializeField]
        //[HideInInspector]
        private float _DefaultItemSize = 60f;

        /// <summary>The size of all items for which the size is not specified</summary>
        public float DefaultItemSize { get => this._DefaultItemSize; protected set => this._DefaultItemSize = value; }

        [Tooltip("You'll probably need this if the scroll view is a child of another scroll view." + " If enabled, the first parent that implements all of IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler and IEndDragHandler," + " as well as the first IScrollHandler found in parents (but it should be on the same game object as other handlers, IF they're found), " + " will receive these events when they occur on this scroll view. This works both with Unity's ScrollRect and OSA")]
        [SerializeField]
        private bool _ForwardDragToParents = false;

        /// <summary>You'll probably need this if the scroll view is a child of another scroll view.
        /// If enabled, the first parent that implements all of IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler and IEndDragHandler, 
        /// as well as the first IScrollHandler found in parents (but it should be on the same game object as other handlers, IF they're found), 
        /// will receive these events when they occur on this scroll view. This works both with Unity's ScrollRect and OSA</summary>
        public bool ForwardDragToParents { get => this._ForwardDragToParents; protected set => this._ForwardDragToParents = value; }

        [Tooltip("It forwards the drag/scroll in the same direction to the parent when the current scrolled position is at boundary, " + "thus allowing scrolling through nested Scroll Views that have the same scroll direction. \n" + "ForwardDragToParents also needs to be enabled for this to work.")]
        [SerializeField]
        private bool _ForwardDragSameDirectionAtBoundary = false;

        /// <summary>It forwards the drag/scroll events in the same direction to the parent when the current scrolled position is at boundary,
        /// thus allowing scrolling through nested Scroll Views that have the same scroll direction.
        /// <para>ForwardDragToParents also needs to be enabled for this to work.</para>
        /// </summary>
        public bool ForwardDragSameDirectionAtBoundary { get => this._ForwardDragSameDirectionAtBoundary; protected set => this._ForwardDragSameDirectionAtBoundary = value; }

        [Tooltip("Allows you to click and drag the content directly (enabled by default). The property ForwardDragToParents is not affected by this")]
        [SerializeField]
        private bool _DragEnabled = true;

        /// <summary>
        /// Allows you to click and drag the content directly (enabled by default). 
        /// The <see cref="ForwardDragToParents"/> is not affected by this.
        /// </summary>
        public bool DragEnabled { get => this._DragEnabled; set => this._DragEnabled = value; }

        [Tooltip("Allows you to scroll by mouse wheel or other similar input devices (enabled by default). The property ForwardDragToParents is not affected by this")]
        [SerializeField]
        private bool _ScrollEnabled = true;

        /// <summary>
        /// Allows you to scroll by mouse wheel or other similar input devices (enabled by default). 
        /// The <see cref="ForwardDragToParents"/> is not affected by this.
        /// </summary>
        public bool ScrollEnabled { get => this._ScrollEnabled; set => this._ScrollEnabled = value; }

        [Tooltip("If enabled, will use Time.unscaledTime instead of Time.time, which means the animations, inertia etc. won't be affected by the Time.timeScale")]
        [SerializeField]
        private bool _UseUnscaledTime = true;

        /// <summary>If enabled, will use <see cref="Time.unscaledTime"/> and <see cref="Time.unscaledDeltaTime"/> for animations, inertia etc. instead of <see cref="Time.time"/> and <see cref="Time.deltaTime"/></summary>
        public bool UseUnscaledTime { get => this._UseUnscaledTime; set => this._UseUnscaledTime = value; }

        [Tooltip(
            "The item size in the direction perpendicular to scrolling direction. Width for vertical ScrollViews, and vice-versa.\n" + "-1 = items won't have their widths (heights for horizontal ScrollViews) changed at all - you'll be responsible for them\n" + "0 = fill to available space, taking ContentPadding into account\n" + ">0 = fixed size; in this case, it's better to also set transversal padding to -1 so the item will be centered\n")]
        [SerializeField]
        private float _ItemTransversalSize = 0f;

        /// <summary>
        /// The item size in the direction perpendicular to scrolling direction. Width for vertical ScrollViews, and vice-versa
        /// <para>-1 = items won't have their widths (heights for horizontal ScrollViews) changed at all - you'll be responsible for them</para>
        /// <para>0 = fill to available space, taking <see cref="ContentPadding"/> into account</para>
        /// <para>any positive value = fixed size; in this case, it's better to also set transversal padding to -1 so the item will be centered</para>
        /// </summary>
        public float ItemTransversalSize { get => this._ItemTransversalSize; set => this._ItemTransversalSize = value; }

        #endregion

        [SerializeField] [FormerlySerializedAs("effects")] private Effects _Effects = new();
        public                                                     Effects effects => this._Effects;

        [SerializeField] private NavigationParams _Navigation = new();
        public                   NavigationParams Navigation => this._Navigation;

        [SerializeField] private AnimationParams _Animation = new();
        public                   AnimationParams Animation => this._Animation;

        [SerializeField] [FormerlySerializedAs("optimization")] private Optimization _Optimization = new();
        public                                                          Optimization optimization => this._Optimization;

        #endregion

        public bool          IsHorizontal => this._Orientation == OrientationEnum.HORIZONTAL;
        public RectTransform ScrollViewRT => this._ScrollViewRT;
        public Snapper8      Snapper      => this._Snapper;

        private RectTransform _ScrollViewRT;
        private Snapper8      _Snapper;

        /// <summary>It's here just so the class can be serialized by Unity when used as a MonoBehaviour's field</summary>
        public BaseParams()
        {
        }

        public void PrepareForInit(bool firstTime)
        {
            if (!this.Content) throw new OSAException("Content cannot be null");
            this.Content.MatchParentSize(true);

            if (firstTime) Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Called internally in <see cref="OSA{TParams, TItemViewsHolder}.Init()"/> and every time the scrollview's size changes. 
        /// This makes sure the content and viewport have valid values. It can also be overridden to initialize custom data
        /// </summary>
        public virtual void InitIfNeeded(IOSA iAdapter)
        {
            this._ScrollViewRT = iAdapter.AsMonoBehaviour.transform as RectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.ScrollViewRT);

            this.AssertValidWidthHeight(this.ScrollViewRT);

            var sr = this.ScrollViewRT.GetComponent<ScrollRect>();
            if (sr && sr.enabled) throw new OSAException("The ScrollRect is not needed anymore starting with v4.0. Remove or disable it!");

            if (!this.Content) throw new OSAException("Content not set!");
            if (!this.Viewport)
            {
                this.Viewport = this.ScrollViewRT;
                if (this.Content.parent != this.ScrollViewRT) throw new OSAException("Content's parent should be the ScrollView itself if there's no viewport specified!");
            }
            if (!this._Snapper) this._Snapper = this.ScrollViewRT.GetComponent<Snapper8>();

            if (this.ForwardDragSameDirectionAtBoundary && !this.ForwardDragToParents) Debug.Log("OSA: ForwardDragSameDirectionAtBoundary is true, but ForwardDragToParents is false. This will have no effect");

            if (this.ContentPadding.left == -1 != (this.ContentPadding.right == -1))
            {
                Debug.Log("OSA: ContentPadding.right and .left should either be both zero or positive or both -1. Setting both to 0...");
                this.ContentPadding.left = this.ContentPadding.right = 0;
            }

            if (this.ContentPadding.top == -1 != (this.ContentPadding.bottom == -1))
            {
                Debug.Log("OSA: ContentPadding.top and .bottom should either be both zero or positive or both -1. Setting both to 0...");
                this.ContentPadding.top = this.ContentPadding.bottom = 0;
            }

            this.effects.InitIfNeeded();

            // There's no concept of content padding when looping. spacing should be used instead
            if (this.effects.LoopItems)
            {
                var showLog = false;
                var ctSp    = (int)this.ContentSpacing;
                if (this.IsHorizontal)
                {
                    if (this.ContentPadding.left != ctSp)
                    {
                        showLog                  = true;
                        this.ContentPadding.left = ctSp;
                    }

                    if (this.ContentPadding.right != ctSp)
                    {
                        showLog                   = true;
                        this.ContentPadding.right = ctSp;
                    }
                }
                else
                {
                    if (this.ContentPadding.top != ctSp)
                    {
                        showLog                 = true;
                        this.ContentPadding.top = ctSp;
                    }

                    if (this.ContentPadding.bottom != ctSp)
                    {
                        showLog                    = true;
                        this.ContentPadding.bottom = ctSp;
                    }
                }

                if (showLog) Debug.Log("OSA: setting conteng padding to be the same as content spacing (" + this.ContentSpacing.ToString("#############.##") + "), because looping is enabled");
            }

            this.Navigation.InitIfNeeded();

            this.UpdateContentPivotFromGravityType();
        }

        /// <summary>See <see cref="ContentGravity"/></summary>
        public void UpdateContentPivotFromGravityType()
        {
            if (this.Gravity != ContentGravity.FROM_PIVOT)
            {
                var v1_h0 = this.IsHorizontal ? 0 : 1;

                var piv = this.Content.pivot;

                // The transversal position is at the center
                piv[1 - v1_h0] = .5f;

                var   contentGravityAsInt = (int)this.Gravity;
                float pivotInScrollingDirection_IfVerticalScrollView;
                if (contentGravityAsInt < 3)
                    // 1 = TOP := 1f;
                    // 2 = CENTER := .5f;
                    pivotInScrollingDirection_IfVerticalScrollView = 1f / contentGravityAsInt;
                else
                    // 3 = BOTTOM := 0f;
                    pivotInScrollingDirection_IfVerticalScrollView = 0f;

                piv[v1_h0] = pivotInScrollingDirection_IfVerticalScrollView;
                if (v1_h0 == 0) // i.e. if horizontal
                    piv[v1_h0] = 1f - piv[v1_h0];

                this.Content.pivot = piv;
            }
        }

        public void ApplyScrollSensitivityTo(ref Vector2 vec)
        {
            vec.x *= OSAConst.SCROLL_DIR_X_MULTIPLIER * this.ScrollSensivityOnXAxis;
            vec.y *= OSAConst.SCROLL_DIR_Y_MULTIPLIER * this.ScrollSensivity;
        }

        public void AssertValidWidthHeight(RectTransform rt)
        {
            var    rectSize         = rt.rect.size;
            string widthOfHeightErr = null;
            float  sizErr;
            if ((sizErr = rectSize.x) < 1f)
                widthOfHeightErr                                  = "width";
            else if ((sizErr = rectSize.y) < 1f) widthOfHeightErr = "height";
            if (widthOfHeightErr != null)
                throw new OSAException("OSA: '" + rt.name + "' reports a zero or negative " + widthOfHeightErr + "(" + sizErr + "). " + "\nThis can happen if you don't have a Canvas component in the OSA's parents or if you accidentally set an invalid size in editor. " + "\nIf '" + rt.name + "' is instantiated at runtime, make sure you use the version of Object.Instantiate(..) that also takes the parent " + "so it can be directly instantiated in it. The parent should be a Canvas or a descendant of a Canvas"
                );
        }

        public enum OrientationEnum
        {
            VERTICAL,
            HORIZONTAL,
        }

        /// <summary> Represents how often or when the optimizer does his core loop: checking for any items that need to be created, destroyed, disabled, displayed, recycled</summary>
        public enum ContentGravity
        {
            /// <summary>you set it up manually</summary>
            FROM_PIVOT,

            /// <summary>top if vertical scrollview, else left</summary>
            START,

            /// <summary>top if vertical scrollview, else left</summary>
            CENTER,

            /// <summary>bottom if vertical scrollview, else right</summary>
            END,
        }

        [Serializable]
        public class Effects
        {
            public const float DEFAULT_MAX_SPEED    = 10 * 1000f;
            public const float MAX_SPEED            = DEFAULT_MAX_SPEED * 100;
            public const float MAX_SPEED_IF_LOOPING = DEFAULT_MAX_SPEED;

            [Tooltip("This RawImage will be scrolled together with the content. \n" + "The content is always stationary (this is the way the recycling process works), so the scrolling effect is faked by scrolling the texture's x/y.\n" + "Tip: use a seamless/looping background texture for best visual results")]
            [FormerlySerializedAs("contentVisual")]
            [SerializeField]
            private RawImage _ContentVisual = null;

            [Obsolete("Use ContentVisual instead", true)] public RawImage contentVisual { get => this.ContentVisual;  set => this.ContentVisual = value; }
            public                                               RawImage ContentVisual { get => this._ContentVisual; set => this._ContentVisual = value; }

            [FormerlySerializedAs("elasticMovement")] [SerializeField] private bool _ElasticMovement = true;
            [Obsolete("Use ElasticMovement instead", true)]            public  bool elasticMovement { get => this.ElasticMovement;  set => this.ElasticMovement = value; }
            public                                                             bool ElasticMovement { get => this._ElasticMovement; set => this._ElasticMovement = value; }

            [FormerlySerializedAs("pullElasticity")] [SerializeField] private float _PullElasticity = .3f;
            [Obsolete("Use PullElasticity instead", true)]            public  float pullElasticity { get => this.PullElasticity;  set => this.PullElasticity = value; }
            public                                                            float PullElasticity { get => this._PullElasticity; set => this._PullElasticity = value; }

            [FormerlySerializedAs("releaseTime")] [SerializeField] private float _ReleaseTime = .1f;
            [Obsolete("Use ReleaseTime instead", true)]            public  float releaseTime { get => this.ReleaseTime;  set => this.ReleaseTime = value; }
            public                                                         float ReleaseTime { get => this._ReleaseTime; set => this._ReleaseTime = value; }

            [FormerlySerializedAs("inertia")] [SerializeField] private bool _Inertia = true;
            [Obsolete("Use Inertia instead", true)]            public  bool inertia { get => this.Inertia;  set => this.Inertia = value; }
            public                                                     bool Inertia { get => this._Inertia; set => this._Inertia = value; }

            [Tooltip("What percent (0=0%, 1=100%) of the velociy will be lost per second after the drag ended. 1=all(immediate stop), 0=none(maitain constant scrolling speed indefinitely)")]
            [Range(0f, 1f)]
            [FormerlySerializedAs("inertiaDecelerationRate")]
            [SerializeField]
            private float _InertiaDecelerationRate = 1f - .135f;

            [Obsolete("Use InertiaDecelerationRate instead", true)] public float inertiaDecelerationRate { get => this.InertiaDecelerationRate; set => this.InertiaDecelerationRate = value; }

            /// <summary>What amount of the velociy will be lost per second after the drag ended</summary>
            // Fun fact: Unity's original ScrollRect mistakenly names "deceleration rate" the amount that should REMAIN, 
            // not the one that will be REMOVED from the velocity. And its deault value is 0.135. 
            // Here, we're setting the correct default value. A 0 deceleration rate should mean no deceleration
            public float InertiaDecelerationRate { get => this._InertiaDecelerationRate; set => this._InertiaDecelerationRate = value; }

            [SerializeField] [Tooltip("Stop any movement from inertia or scrolling animations when a mouse click/touch begins")] private bool _CutMovementOnPointerDown = true;

            /// <summary>
            /// Stop any movement from inertia or scrolling animations when a mouse click/touch begins
            /// </summary>
            public bool CutMovementOnPointerDown { get => this._CutMovementOnPointerDown; set => this._CutMovementOnPointerDown = value; }

            [FormerlySerializedAs("maxSpeed")] [SerializeField] private float _MaxSpeed = DEFAULT_MAX_SPEED;
            [Obsolete("Use MaxSpeed instead", true)]            public  float maxSpeed { get => this.MaxSpeed;  set => this.MaxSpeed = value; }
            public                                                      float MaxSpeed { get => this._MaxSpeed; set => this._MaxSpeed = value; }

            [Tooltip("If enabled, multiple drags in the same direction will lead to greater speeds")] [SerializeField] private bool _TransientSpeedBetweenDrags = true;

            /// <summary>If enabled, multiple drags in the same direction will lead to greater speeds</summary>
            public bool TransientSpeedBetweenDrags { get => this._TransientSpeedBetweenDrags; protected set => this._TransientSpeedBetweenDrags = value; }

            [Tooltip("If true: When the last item is reached, the first one appears after it, basically allowing you to scroll infinitely.\n" + " Initially intended for things like spinners, but it can be used for anything alike.\n" + " It may interfere with other functionalities in some very obscure/complex contexts/setups, so be sure to test the hell out of it.\n" + " Also please note that sometimes during dragging the content, the actual looping changes the Unity's internal PointerEventData for the current click/touch pointer id, so if you're also externally tracking the current click/touch, in this case only 'PointerEventData.pointerCurrentRaycast' and 'PointerEventData.position'(current position) are preserved, the other ones are reset to defaults to assure a smooth loop transition. Sorry for the long decription. Here's an ASCII potato: @")]
            [FormerlySerializedAs("loopItems")]
            [SerializeField]
            private bool _LoopItems = false;

            [Obsolete("Use LoopItems instead", true)] public bool loopItems { get => this.LoopItems; set => this.LoopItems = value; }

            /// <summary>
            /// <para>If true: When the last item is reached, the first one appears after it, basically allowing you to scroll infinitely.</para>
            /// <para>Initially intended for things like spinners, but it can be used for anything alike. It may interfere with other functionalities in some very obscure/complex contexts/setups, so be sure to test the hell out of it.</para>
            /// <para>Also please note that sometimes during dragging the content, the actual looping changes the Unity's internal PointerEventData for the current click/touch pointer id, </para>
            /// <para>so if you're also externally tracking the current click/touch, in this case only 'PointerEventData.pointerCurrentRaycast' and 'PointerEventData.position'(current position) are </para>
            /// <para>preserved, the other ones are reset to defaults to assure a smooth loop transition</para>
            /// </summary>
            public bool LoopItems { get => this._LoopItems; set => this._LoopItems = value; }

            [Tooltip("The contentVisual's additional drag factor. Examples:\n" + "-2: the contentVisual will move exactly by the same amount as the items, but in the opposite direction\n" + "-1: no movement\n" + " 0: same speed (together with the items)\n" + " 1: 2x faster in the same direction\n" + " 2: 3x faster etc.")]
            [Range(-5f, 5f)]
            [SerializeField]
            private float _ContentVisualParallaxEffect = -.85f;

            public float ContentVisualParallaxEffect { get => this._ContentVisualParallaxEffect; set => this._ContentVisualParallaxEffect = value; }

            [SerializeField] private GalleryEffectParams _Gallery = new();
            public                   GalleryEffectParams Gallery { get => this._Gallery; set => this._Gallery = value; }

            [Range(0f, 1f)] [FormerlySerializedAs("galleryEffectAmount")] [SerializeField] [HideInInspector] private float _GalleryEffectAmount = 0f;
            [Obsolete("Use GalleryEffectAmount instead", true)]                                              public  float galleryEffectAmount { get => this.Gallery.OverallAmount; set => this.Gallery.OverallAmount = value; }
            [Obsolete("Use Gallery.OverallAmount instead")]                                                  public  float GalleryEffectAmount { get => this.Gallery.OverallAmount; set => this.Gallery.OverallAmount = value; }

            [Range(0f, 1f)] [FormerlySerializedAs("galleryEffectViewportPivot")] [SerializeField] [HideInInspector] private float _GalleryEffectViewportPivot = .5f;
            [Obsolete("Use GalleryEffectViewportPivot instead", true)]                                              public  float galleryEffectViewportPivot { get => this.Gallery.Scale.ViewportPivot; set => this.Gallery.Scale.ViewportPivot = value; }
            [Obsolete("Use Gallery.Scale.ViewportPivot instead")]                                                   public  float GalleryEffectViewportPivot { get => this.Gallery.Scale.ViewportPivot; set => this.Gallery.Scale.ViewportPivot = value; }

            public bool HasContentVisual => this._HasContentVisual;

            private bool _HasContentVisual;

            public void InitIfNeeded()
            {
                this._HasContentVisual = this.ContentVisual != null;

                float  maxAllowed;
                string asString;
                if (this.LoopItems)
                {
                    maxAllowed = MAX_SPEED_IF_LOOPING;
                    asString   = "MAX_SPEED_IF_LOOPING";
                }
                else
                {
                    maxAllowed = MAX_SPEED;
                    asString   = "MAX_SPEED";
                }
                var maxSpeedClamped = Mathf.Clamp(this.MaxSpeed, 0f, maxAllowed);
                if (Math.Abs(maxSpeedClamped - this.MaxSpeed) > 1f)
                {
                    Debug.Log("OSA: maxSpeed(" + this.MaxSpeed.ToString("#########.00") + ") value is negative or exceeded " + asString + "(" + maxAllowed.ToString("#########.00") + "). Clamped it to " + maxSpeedClamped.ToString("#########.00")
                    );
                    this.MaxSpeed = maxSpeedClamped;
                }

                if (this.ElasticMovement && this.LoopItems)
                {
                    this.ElasticMovement = false;
                    Debug.Log("OSA: 'elasticMovement' was set to false, because 'loopItems' is true. Elasticity only makes sense when there is an end");
                }

                if (this.HasContentVisual) this.ContentVisual.rectTransform.MatchParentSize(true);

                this.InitGalleryEffectMigrations();
            }

            private void InitGalleryEffectMigrations()
            {
                var defVal = 0f;
                if (this._GalleryEffectAmount != defVal)
                {
                    var warn                                       = "OSA: Please go to BaseParams.cs, comment the [HideInInspector] attribute on _GalleryEffectAmount, then set this property to " + defVal + " (the default) in inspector, as this property will be removed in next versions." + " Use Gallery.OverallAmount instead to set the gallery effect amount. It's available through inspector." + " Will use _GalleryEffectAmount as the active one.";
                    if (this.Gallery.OverallAmount != defVal) warn += ". Additional migration warning: Both _GalleryEffectAmount and Gallery.OverallAmount are non-default (non " + defVal + ").";
                    this.Gallery.OverallAmount = this._GalleryEffectAmount;
                    Debug.Log(warn);
                }
                defVal = .5f;
                if (this._GalleryEffectViewportPivot != defVal)
                {
                    var warn                                             = "OSA: Please go to BaseParams.cs, comment the [HideInInspector] attribute on _GalleryEffectViewportPivot, then set this property to " + defVal + " (the default) in inspector, as this property will be removed in next versions." + " Use Gallery.Scale.ViewportPivot instead to set the gallery effect pivot. It's available through inspector." + " Preserving the value of _GalleryEffectViewportPivot as the active one.";
                    if (this.Gallery.Scale.ViewportPivot != defVal) warn += ". Additional migration warning: Both _GalleryEffectViewportPivot and Gallery.Scale.ViewportPivot are non-default (non " + defVal + ").";
                    this.Gallery.Scale.ViewportPivot = this._GalleryEffectViewportPivot;
                    Debug.Log(warn);
                }
            }
        }

        [Serializable]
        public class NavigationParams
        {
            [Tooltip("Uses EventSystem.current (by default) to scroll to the currently selected item, if it's a ViewsHolder. Disabled by default for backward-compatibility.")]
            [SerializeField]
            private bool _Enabled = false;

            /// <summary>
            /// Uses EventSystem.current (by default) to scroll to the currently selected item, if it's a ViewsHolder. Disabled by default for backward-compatibility.
            /// </summary>
            public bool Enabled { get => this._Enabled; set => this._Enabled = value; }

            [Tooltip(
                "Maximum depth up to which to search for a ViewsHolder in the parents of the currently selected GameObject (By default, EventSystem.current.currentSelectedGameObject). \n" + "A value of 0 means the selected GameObject itself should be the same as ViewsHolder.root. \n" + "If you have a Button that's selectable and it's a direct child of the ViewsHolder.root, the depth is 1.  \n" + "Don't use a high value unnecessarily, as it affects performance. Default is 2.")]
            [SerializeField]
            private int _MaxSearchDepthForViewsHolder = 2;

            /// <summary>
            /// <para>Maximum depth up to which to search for a ViewsHolder in the parents of the currently selected GameObject (By default, EventSystem.current.currentSelectedGameObject).</para>
            /// <para>A value of 0 means the selected GameObject itself should be the same as ViewsHolder.root.</para>
            /// <para>If you have a Button that's selectable and it's a direct child of the ViewsHolder.root, the depth is 1.</para>
            /// <para>Don't use a high value unnecessarily, as it affects performance. Default is 2.</para>
            /// </summary>
            public int MaxSearchDepthForViewsHolder { get => this._MaxSearchDepthForViewsHolder; set => this._MaxSearchDepthForViewsHolder = value; }

            [Tooltip(
                "Duration of scrolling to outside items when they're selected. Use 0 for no animation (immediate jump).\n" + "Clamped to (0, 1) and then again based on the input module's 'max actions per second'. Default is 0.1")]
            [SerializeField]
            private float _ScrollDuration = .1f;

            /// <summary>
            /// <para>Duration of scrolling to outside items when they're selected. Use 0 for no animation (immediate jump).</para>
            /// <para>Clamped to (0, 1) and then again based on the input module's 'max actions per second'. Default is 0.1</para>
            /// </summary>
            public float ScrollDuration { get => this._ScrollDuration; set => this._ScrollDuration = value; }

            [Tooltip("If on, will center the selected item in the Viewport. Default is false")] [SerializeField] private bool _Centered = false;

            /// <summary>
            /// <para>If on, will center the selected item in the Viewport. Default is false</para>
            /// </summary>
            public bool Centered { get => this._Centered; set => this._Centered = value; }

            [Tooltip(
                "Increase this to add more space between the focused (selected) item and the edge towards which you navigate. " + "This is useful for scenarios like when you have some items that aren't selectable, but still want to see the ones after them (and thus, select them).\n" + "Has no effect if Centered=true")]
            [SerializeField]
            private float _AdditionalSpacingTowardsEdge = 0f;

            /// <summary>
            /// <para>Increase this to add more space between the focused (selected) item and the edge towards which you navigate.</para>
            /// <para>This is useful for scenarios like when you have some items that aren't selectable, but still want to see the ones after them (and thus, select them).</para>
            /// <para>Has no effect if <see cref="Centered"/> is true<.</para>
            /// </summary>
            public float AdditionalSpacingTowardsEdge { get => this._AdditionalSpacingTowardsEdge; set => this._AdditionalSpacingTowardsEdge = value; }

            public void InitIfNeeded()
            {
            }
        }

        [Serializable]
        public class Optimization
        {
            [Tooltip("How many objects besides the visible ones to keep in memory at max. \n" + "By default, no more than the heuristically found \"ideal\" number of items will be held in memory.\n" + "Set to a positive integer to limit it. In this case, you'll either use more RAM than needed or more CPU than needed. " + "One advantage is that you can get a predictable usage of the resources (for example, by specifying a constant bin size, no item will be destroyed if the actual visible items count won't exceed that).\n" + "Note that this doesn't include the 'buffered' items, which by design can't be directly destroyed")]
            [FormerlySerializedAs("recycleBinCapacity")]
            [SerializeField]
            private int _RecycleBinCapacity = -1;

            [Obsolete("Use RecycleBinCapacity instead", true)] public int recycleBinCapacity { get => this.RecycleBinCapacity; set => this.RecycleBinCapacity = value; }

            /// <summary>
            /// <para>How many objects besides the visible ones to keep in memory at max, besides the visible ones</para>
            /// <para>By default, no more than the heuristically found "ideal" number of items will be held in memory</para>
            /// <para>Set to a positive integer to limit it - Not recommended, unless you're OK with more GC calls (i.e. occasional FPS hiccups) in favor of using less RAM</para>
            /// </summary>
            public int RecycleBinCapacity { get => this._RecycleBinCapacity; set => this._RecycleBinCapacity = value; }

            [FormerlySerializedAs("scaleToZeroInsteadOfDisable")] [SerializeField] private bool _ScaleToZeroInsteadOfDisable = false;
            [Obsolete("Use ScaleToZeroInsteadOfDisable instead", true)]            public  bool scaleToZeroInsteadOfDisable { get => this.ScaleToZeroInsteadOfDisable; set => this.ScaleToZeroInsteadOfDisable = value; }

            /// <summary>
            /// Enables ability to scale out-of-view objects to zero instead of de-activating them, 
            /// since GameObject.SetActive is slightly more expensive to call each frame (especially when scrolling via the scrollbar). 
            /// This is not a major speed improvement, but rather a slight memory improvement. 
            /// It's recommended to use this option if your game/business logic doesn't require the game objects to be de-activated.
            /// </summary>
            public bool ScaleToZeroInsteadOfDisable { get => this._ScaleToZeroInsteadOfDisable; set => this._ScaleToZeroInsteadOfDisable = value; }

            // WIP
            ///// <summary>
            ///// <para>The bigger, the more items will be active past the minimum needed to fill the viewport - with a performance cost, of course</para>
            ///// <para>1f = generally, the number of visible items + 1 will always be active</para>
            ///// <para>2f = twice the number of visible items + 1 will be always active</para>
            ///// <para>2.5f = 2.5 * (the number of visible items) + 1 will be always active</para>
            ///// </summary>
            //[Range(1f, 5f)]
            //public float recyclingToleranceFactor = 1f;

            [FormerlySerializedAs("forceLayoutRebuildOnBeginSmoothScroll")] [SerializeField] private bool _ForceLayoutRebuildOnBeginSmoothScroll = true;
            [Obsolete("Use ForceLayoutRebuildOnBeginSmoothScroll instead", true)]            public  bool forceLayoutRebuildOnBeginSmoothScroll { get => this.ForceLayoutRebuildOnBeginSmoothScroll; set => this.ForceLayoutRebuildOnBeginSmoothScroll = value; }

            /// <summary>Disable only if you see FPS drops when calling <see cref="OSA{TParams, TItemViewsHolder}.SmoothScrollTo(int, float, float, float, Func{float, bool}, Action, bool)"/></summary>
            public bool ForceLayoutRebuildOnBeginSmoothScroll { get => this._ForceLayoutRebuildOnBeginSmoothScroll; set => this._ForceLayoutRebuildOnBeginSmoothScroll = value; }

            [FormerlySerializedAs("forceLayoutRebuildOnDrag")] [SerializeField] private bool _ForceLayoutRebuildOnDrag = false;
            [Obsolete("Use ForceLayoutRebuildOnDrag instead", true)]            public  bool forceLayoutRebuildOnDrag { get => this.ForceLayoutRebuildOnDrag; set => this.ForceLayoutRebuildOnDrag = value; }

            /// <summary>
            /// Enable only if you experience issues with misaligned items. If OSA is correctly implemented, this shouldn't happen (please report if you find otherwise). 
            /// However, we still provide this property if you want a quick fix
            /// </summary>
            public bool ForceLayoutRebuildOnDrag { get => this._ForceLayoutRebuildOnDrag; set => this._ForceLayoutRebuildOnDrag = value; }

            [SerializeField]
            [Tooltip("Whether to sort the actual GameObjects representing the items under the Content. Only use if needed, as it slightly affects performance. Off by default")]
            private bool _KeepItemsSortedInHierarchy = false;

            /// <summary>
            /// Whether to sort the actual GameObjects representing the items under the Content. Only use if needed, as it slightly affects performance. Off by default
            /// </summary>
            public bool KeepItemsSortedInHierarchy { get => this._KeepItemsSortedInHierarchy; set => this._KeepItemsSortedInHierarchy = value; }

            [SerializeField]
            [Tooltip("When the list becomes empty, should the currently cached item views be kept (True) or destroyed in order to start with a fresh state (False)? False by default")]
            private bool _KeepItemsPoolOnEmptyList = false;

            /// <summary>When the list becomes empty, should the currently cached item views be kept (True) or destroyed in order to start with a fresh state (False)? False by default</summary>
            public bool KeepItemsPoolOnEmptyList { get => this._KeepItemsPoolOnEmptyList; set => this._KeepItemsPoolOnEmptyList = value; }

            [SerializeField]
            [Tooltip("When the ScrollView is rebuilt (as when its size changes), should the currently cached item views be kept (True) or destroyed in order to start with a fresh state (False)? False by default")]
            private bool _KeepItemsPoolOnLayoutRebuild = false;

            /// <summary>When the ScrollView is rebuilt (as when its size changes), should the currently cached item views be kept (True) or destroyed in order to start with a fresh state (False)? False by default</summary>
            public bool KeepItemsPoolOnLayoutRebuild { get => this._KeepItemsPoolOnLayoutRebuild; set => this._KeepItemsPoolOnLayoutRebuild = value; }

            [SerializeField]
            [Tooltip("When the ScrollView is rebuilt (as when its size changes), should the currently cached item sizes views be kept (True)? False by default")]
            private bool _KeepItemsSizesOnLayoutRebuild = false;

            /// <summary>When the ScrollView is rebuilt (as when its size changes), should the currently cached item sizes views be kept (True)? False by default</summary>
            public bool KeepItemsSizesOnLayoutRebuild { get => this._KeepItemsSizesOnLayoutRebuild; set => this._KeepItemsSizesOnLayoutRebuild = value; }
        }
    }
}