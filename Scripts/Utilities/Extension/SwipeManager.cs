namespace GameFoundation.Scripts.Utilities.Extension
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    internal class CardinalDirection
    {
        public static readonly Vector2 Up        = new(0, 1);
        public static readonly Vector2 Down      = new(0, -1);
        public static readonly Vector2 Right     = new(1, 0);
        public static readonly Vector2 Left      = new(-1, 0);
        public static readonly Vector2 UpRight   = new(1, 1);
        public static readonly Vector2 UpLeft    = new(-1, 1);
        public static readonly Vector2 DownRight = new(1, -1);
        public static readonly Vector2 DownLeft  = new(-1, -1);
    }

    public enum Swipe
    {
        None,
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
    };

    public class SwipeManager : MonoBehaviour
    {
        #region Inspector Variables

        [Tooltip("Min swipe distance (inches) to register as swipe")] [SerializeField] private float minSwipeLength = 0.5f;

        [Tooltip("If true, a swipe is counted when the min swipe length is reached. If false, a swipe is counted when the touch/click ends.")]
        [SerializeField]
        private bool triggerSwipeAtMinLength = false;

        [Tooltip("Whether to detect eight or four cardinal directions")] [SerializeField] private bool useEightDirections = false;

        #endregion

        private const float eightDirAngle = 0.906f;
        private const float fourDirAngle  = 0.5f;
        private const float defaultDPI    = 72f;
        private const float dpcmFactor    = 2.54f;

        private static Dictionary<Swipe, Vector2> cardinalDirections = new()
        {
            { Swipe.Up, CardinalDirection.Up },
            { Swipe.Down, CardinalDirection.Down },
            { Swipe.Right, CardinalDirection.Right },
            { Swipe.Left, CardinalDirection.Left },
            { Swipe.UpRight, CardinalDirection.UpRight },
            { Swipe.UpLeft, CardinalDirection.UpLeft },
            { Swipe.DownRight, CardinalDirection.DownRight },
            { Swipe.DownLeft, CardinalDirection.DownLeft },
        };

        public delegate void OnSwipeDetectedHandler(Swipe swipeDirection, Vector2 swipeVelocity);

        private static OnSwipeDetectedHandler _OnSwipeDetected;

        public static event OnSwipeDetectedHandler OnSwipeDetected
        {
            add
            {
                _OnSwipeDetected += value;
                autoDetectSwipes =  true;
            }
            remove => _OnSwipeDetected -= value;
        }

        public static Vector2 swipeVelocity;

        private static float        dpcm;
        private static float        swipeStartTime;
        private static float        swipeEndTime;
        private static bool         autoDetectSwipes;
        private static bool         swipeEnded;
        private static Swipe        swipeDirection;
        private static Vector2      firstPressPos;
        private static Vector2      secondPressPos;
        private static SwipeManager instance;

        private void Awake()
        {
            instance = this;
            var dpi = Screen.dpi == 0 ? defaultDPI : Screen.dpi;
            dpcm = dpi / dpcmFactor;
        }

        private void Update()
        {
            if (autoDetectSwipes) DetectSwipe();
        }

        /// <summary>
        /// Attempts to detect the current swipe direction.
        /// Should be called over multiple frames in an Update-like loop.
        /// </summary>
        private static void DetectSwipe()
        {
            if (GetTouchInput() || GetMouseInput())
            {
                // Swipe already ended, don't detect until a new swipe has begun
                if (swipeEnded) return;

                var currentSwipe = secondPressPos - firstPressPos;
                var swipeCm      = currentSwipe.magnitude / dpcm;

                // Check the swipe is long enough to count as a swipe (not a touch, etc)
                if (swipeCm < instance.minSwipeLength)
                {
                    // Swipe was not long enough, abort
                    if (instance.triggerSwipeAtMinLength) return;

                    // if (Application.isEditor)
                    // {
                    //     Debug.Log("[SwipeManager] Swipe was not long enough.");
                    // }

                    swipeDirection = Swipe.None;

                    return;
                }

                swipeEndTime   = Time.time;
                swipeVelocity  = currentSwipe * (swipeEndTime - swipeStartTime);
                swipeDirection = GetSwipeDirByTouch(currentSwipe);
                swipeEnded     = true;

                if (_OnSwipeDetected != null) _OnSwipeDetected(swipeDirection, swipeVelocity);
            }
            else
            {
                swipeDirection = Swipe.None;
            }
        }

        public static bool IsSwiping()
        {
            return swipeDirection != Swipe.None;
        }

        public static bool IsSwipingRight()
        {
            return IsSwipingDirection(Swipe.Right);
        }

        public static bool IsSwipingLeft()
        {
            return IsSwipingDirection(Swipe.Left);
        }

        public static bool IsSwipingUp()
        {
            return IsSwipingDirection(Swipe.Up);
        }

        public static bool IsSwipingDown()
        {
            return IsSwipingDirection(Swipe.Down);
        }

        public static bool IsSwipingDownLeft()
        {
            return IsSwipingDirection(Swipe.DownLeft);
        }

        public static bool IsSwipingDownRight()
        {
            return IsSwipingDirection(Swipe.DownRight);
        }

        public static bool IsSwipingUpLeft()
        {
            return IsSwipingDirection(Swipe.UpLeft);
        }

        public static bool IsSwipingUpRight()
        {
            return IsSwipingDirection(Swipe.UpRight);
        }

        #region Helper Functions

        private static bool GetTouchInput()
        {
            if (Input.touches.Length > 0)
            {
                var t = Input.GetTouch(0);

                // Swipe/Touch started
                if (t.phase == TouchPhase.Began)
                {
                    firstPressPos  = t.position;
                    swipeStartTime = Time.time;
                    swipeEnded     = false;
                    // Swipe/Touch ended
                }
                else if (t.phase == TouchPhase.Ended)
                {
                    secondPressPos = t.position;
                    return true;
                    // Still swiping/touching
                }
                else
                {
                    // Could count as a swipe if length is long enough
                    if (instance.triggerSwipeAtMinLength) return true;
                }
            }

            return false;
        }

        private static bool GetMouseInput()
        {
            // Swipe/Click started
            if (Input.GetMouseButtonDown(0))
            {
                firstPressPos  = (Vector2)Input.mousePosition;
                swipeStartTime = Time.time;
                swipeEnded     = false;
                // Swipe/Click ended
            }
            else if (Input.GetMouseButtonUp(0))
            {
                secondPressPos = (Vector2)Input.mousePosition;
                return true;
                // Still swiping/clicking
            }
            else
            {
                // Could count as a swipe if length is long enough
                if (instance.triggerSwipeAtMinLength) return true;
            }

            return false;
        }

        private static bool IsDirection(Vector2 direction, Vector2 cardinalDirection)
        {
            var angle = instance.useEightDirections ? eightDirAngle : fourDirAngle;
            return Vector2.Dot(direction, cardinalDirection) > angle;
        }

        private static Swipe GetSwipeDirByTouch(Vector2 currentSwipe)
        {
            currentSwipe.Normalize();
            var swipeDir = cardinalDirections.FirstOrDefault(dir => IsDirection(currentSwipe, dir.Value));
            return swipeDir.Key;
        }

        private static bool IsSwipingDirection(Swipe swipeDir)
        {
            DetectSwipe();
            return swipeDirection == swipeDir;
        }

        #endregion
    }
}