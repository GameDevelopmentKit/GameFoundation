//#define DEBUG_OSA_PROACTIVE_NAVIGATOR

using UnityEngine;
using Com.ForbiddenByte.OSA.Core;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using Com.ForbiddenByte.OSA.Core.SubComponents;
using System.Collections;
using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.AdditionalComponents
{
    /// <summary>
    /// <para>OSA can work with Unity's built-in navigation, at least in simple scenarios, but if you want full control over the navigation, this component provides it.</para>
    /// <para>Attach it to an OSA-containing GameObject.</para>
    /// <para>Set your Item's navigation to Explicit, and leave all its 4 directions as None. You can optionally set the Item's transversal directions (i.e. left/right in a vertical ScrollView) 
    /// to anything, but, unless you need to do it for very specific reasons, it's more convenient to assign them in the Selectables section of this script's Inspector</para>
    /// </summary>
    [RequireComponent(typeof(IOSA))]
    public class OSAProactiveNavigator : Selectable
    {
        [Tooltip("Note that if the OSA has looping active, you'll only be able to reach the transversal selectables (i.e. left/right for a vertical ScrollView)")]
        [SerializeField]
        private Selectables _Selectables = new();

        [Tooltip("What to do when the user navigates to a direction for which you didn't specify a selectable?")] [SerializeField] private OnNoSelectableSpecifiedEnum _OnNoSelectableSpecified = OnNoSelectableSpecifiedEnum.KEEP_CURRENTLY_SELECTED;

        [Tooltip(
            "This is a workaround to prevent the navigation from skipping the first item when entering OSA from outside, " + "in case your items also contain an inner navigation logic using Unity's built-in system. " + "Default is 0.4"
        )]
        [SerializeField]
        private float _JoystickInputMultiplier = .3f;

        [Tooltip("See description of 'JoystickInputMultiplier'. The same thing applies here, but for arrows")] [SerializeField] private float _ArrowsInputMultiplier = 1f;

        [Tooltip("If at extremity and moving futher, the item at the other extremity will be selected. This will supersede the selectables you set in your item or in the Selectables section of this script")]
        [SerializeField]
        private bool _LoopAtExtremity = false;

        private ViewsHolderFinder ViewsHolderFinder => this._NavManager.ViewsHolderFinder;

        private ScrollStateEnum ScrollState
        {
            get { return this._ScrollState; }
            set
            {
                #if DEBUG_OSA_PROACTIVE_NAVIGATOR
				Debug.Log("State from " + _ScrollState + " to " + value);
                #endif
                this._ScrollState = value;
            }
        }

        private IOSA                  _OSA;
        private SelectionWatcher      _SelectionWatcher;
        private BaseNavigationManager _NavManager;
        private bool                  _Initialized;
        private float                 _LastInputProcessingUnscaledTime;
        private Vector2               _ScrollStartInputVec;

        private float _ScrollStartInputVecUnscaledTime;

        // Contains data about the last command executed by this navigator. If a navigation event happened through Unity's own Navigation system, this won't reflect that.
        // This is useful to distinguish the cases where a new object is selected by this navigator vs by the Unity's system
        private NavEvents       _LastNav = new();
        private ScrollStateEnum _ScrollState;

        protected override void Awake()
        {
            base.Awake();

            this._OSA = this.GetComponent(typeof(IOSA)) as IOSA;
            if (this._OSA == null) throw new OSAException("OSAProactiveNavigatior: OSA component not found on this game object");

            this.SetNotInteractable();
        }

        private void Update()
        {
            // Only executing at runtime
            if (!Application.isPlaying) return;

            if (!this._Initialized)
            {
                if (this._OSA.IsInitialized) this.InitializeOnOSAReady();

                return;
            }

            var navEnabled = this._OSA.BaseParameters.Navigation.Enabled;

            this._SelectionWatcher.Enabled = navEnabled;
            this._SelectionWatcher.OnUpdate();

            if (navEnabled)
                this.CheckNav();
            else
                this.EnterState_NoneIfNeeded();
        }

        private void InitializeOnOSAReady()
        {
            this.SetNotInteractable();
            this._NavManager = this._OSA.GetBaseNavigationManager();

            if (this._Selectables.SyncAllProvidedSelectables) this._Selectables.SyncProvidedSelectables(this);

            this._SelectionWatcher                   =  new();
            this._SelectionWatcher.NewObjectSelected += this.SelectionWatcher_NewObjectSelected;

            this._Initialized = true;
        }

        private void SelectionWatcher_NewObjectSelected(GameObject lastGO, GameObject newGO)
        {
            if (newGO != this.gameObject)
            {
                // Ignore our own commands
                if (this._LastNav.New.Selectable && this._LastNav.New.Selectable.gameObject == newGO) return;

                //if (_LastNav.New.Type == NavMoveType.ENTER_OSA || _LastNav.New.Type == NavMoveType.EXIT_OSA)
                //{
                //	// This prevents Unity's own nav system from navigating through other selectables after we select the closest VH which also has its own inner navigation bindings
                //	if (!DidEnoughTimePassSinceBoundaryCrossinEventOrPreEvent(_LastNav.New))
                //	{
                //		UndoUnitySelectionSilently(lastGO, newGO);
                //		return;
                //	}
                //}

                this.HandleUnityBuiltinNavSelection(lastGO, newGO);
                return;
            }

            this.HandleOnOSASelected(lastGO);
        }

        private void HandleOnOSASelected(GameObject lastSelectedGO)
        {
            if (this._OSA.VisibleItemsCount == 0) return;

            Selectable          nextSelectable;
            AbstractViewsHolder nextSelectableContainingVH;
            if (this.GetClosestSelectableFromOSAsVHs(lastSelectedGO, out nextSelectable, out nextSelectableContainingVH))
            {
                var lastSelectedGOSelectable   = lastSelectedGO.GetComponent<Selectable>();
                var lastSelectedGOContainingVH = this.ViewsHolderFinder.GetViewsHolderFromSelectedGameObject(lastSelectedGO);
                this.SetOrFindNextSelectable(nextSelectable, Vector3.one, lastSelectedGOSelectable, lastSelectedGOContainingVH);
                this.EnterState_WaitingForInitial();
            }
        }

        private void HandleUnityBuiltinNavSelection(GameObject _, GameObject newGO)
        {
            var newGOSelectable = newGO.GetComponent<Selectable>();
            var newGOVH         = this.ViewsHolderFinder.GetViewsHolderFromSelectedGameObject(newGO);
            this._LastNav.OnNewEvent_UnityBuiltinNav(newGOSelectable, newGOVH, NavMoveType.UNKNOWN);
        }

        private void EnterState_NoneIfNeeded()
        {
            if (this.ScrollState == ScrollStateEnum.NONE) return;

            this._ScrollStartInputVec = Vector3.zero;
            this.ScrollState          = ScrollStateEnum.NONE;
        }

        private void EnterState_WaitingForInitial()
        {
            this._ScrollStartInputVecUnscaledTime = Time.unscaledTime;
            this._ScrollStartInputVec             = this.GetCurrentInputVector();
            this.ScrollState                      = ScrollStateEnum.WAITING_FOR_FIRST_DELAY;
        }

        private void CheckNav()
        {
            Selectable          curSelectable;
            AbstractViewsHolder curSelectableVH;
            if (!this.NavPreChecks(out curSelectable, out curSelectableVH)) return;

            var inputVec                     = this.GetCurrentInputVector();
            var curSelectableNavMode         = curSelectable.navigation.mode;
            var curSelectableUsesExplicitNav = curSelectableNavMode == Navigation.Mode.Explicit;
            var absVert                      = Math.Abs(inputVec.y);
            var absHor                       = Math.Abs(inputVec.x);
            var isVertDir                    = absVert >= absHor;
            var curSelectableIsVH            = curSelectableVH != null;

            var lastEv = this._LastNav.New;
            if (lastEv.Selectable)
            {
                if (lastEv.Type == NavMoveType.ENTER_OSA || lastEv.Type == NavMoveType.EXIT_OSA)
                {
                    if (lastEv.Selectable == curSelectable)
                        if (!this.DidEnoughTimePassSinceBoundaryCrossinEventOrPreEvent(lastEv))
                            return;
                }
                else
                {
                    if (lastEv.IsUnityBuiltinEvent)
                        if (!this.DidEnoughTimePassSinceBoundaryCrossinEventOrPreEvent(lastEv))
                            return;
                }
            }

            if (this._OSA.IsVertical)
            {
                float      input;
                int        inputSign;
                int        nextOSAItemDirSign;
                bool       isNextOSAItemDirNegative;
                Selectable nextSelectable;
                Vector3    findNextSelectableAuto_Vector;
                if (isVertDir)
                {
                    input                    = inputVec.y;
                    inputSign                = Math.Sign(input);
                    nextOSAItemDirSign       = -inputSign;
                    isNextOSAItemDirNegative = nextOSAItemDirSign < 0;

                    Selectable nextOuterSelectableFromParams;
                    Selectable curSelectableNextExplicitSelectable;
                    if (isNextOSAItemDirNegative)
                    {
                        curSelectableNextExplicitSelectable = curSelectable.navigation.selectOnUp;
                        nextOuterSelectableFromParams       = this._Selectables.Up;
                        findNextSelectableAuto_Vector       = Vector3.up;
                    }
                    else
                    {
                        curSelectableNextExplicitSelectable = curSelectable.navigation.selectOnDown;
                        nextOuterSelectableFromParams       = this._Selectables.Down;
                        findNextSelectableAuto_Vector       = Vector3.down;
                    }

                    if (curSelectableIsVH)
                    {
                        if (curSelectableUsesExplicitNav)
                            if (curSelectableNextExplicitSelectable)
                                // Let Unity system decide
                                return;

                        var itemIndexOfLastItemInList = this._OSA.GetItemsCount() - 1;
                        int extremityItemIndex;
                        if (isNextOSAItemDirNegative)
                            extremityItemIndex = 0;
                        else
                            extremityItemIndex = itemIndexOfLastItemInList;

                        var itemIndex = curSelectableVH.ItemIndex;
                        if (itemIndex == extremityItemIndex)
                        {
                            if (this._LoopAtExtremity)
                            {
                                var otherExtremityItemIndex = itemIndexOfLastItemInList - extremityItemIndex;
                                nextSelectable = this.FindVHSelectableInDirectionOrDefault(otherExtremityItemIndex - nextOSAItemDirSign, nextOSAItemDirSign, extremityItemIndex, this, nextOuterSelectableFromParams);
                            }
                            else
                            {
                                nextSelectable = nextOuterSelectableFromParams;
                            }

                            //SetOrFindNextSelectable(evSystem, nextSelectable, findNextSelectableAuto_Vector);
                        }
                        else
                        {
                            nextSelectable = this.FindVHSelectableInDirectionOrDefault(itemIndex, nextOSAItemDirSign, extremityItemIndex, this, nextOuterSelectableFromParams);
                        }
                    }
                    else
                    {
                        // Current selectable is an outter Selectable

                        if (curSelectableUsesExplicitNav && curSelectableNextExplicitSelectable)
                        {
                            if (curSelectableNextExplicitSelectable != this)
                                // Let Unity system decide
                                return;

                            // Currently selected is an outer selectable which has <this> pointed as the next selectable, explicitly => select the nearest item instead.
                            // Setting to null, and SetOrFindNextSelectable() will decide what to do next, based on params
                            nextSelectable = null;
                        }
                        else
                            // Either nav mode is None, or Explicit and no Selectable assigned in that direction.
                            // In this case, we don't do anything, as it's required to have <this> selected as the next selectable in order to 'enter' OSA
                        {
                            return;
                        }

                        //// Commented: setting to null, and SetOrFindNextSelectable() will decide what to do next, based on params
                        ////nextSelectable = curSelectable.FindSelectable(findNextSelectableAuto_Vector);
                        //nextSelectable = null;
                    }
                }
                else
                {
                    input                    = inputVec.x;
                    inputSign                = Math.Sign(input);
                    nextOSAItemDirSign       = inputSign;
                    isNextOSAItemDirNegative = nextOSAItemDirSign < 0;

                    Selectable nextOuterSelectableFromParams;
                    Selectable curSelectableNextExplicitSelectable;
                    if (isNextOSAItemDirNegative)
                    {
                        curSelectableNextExplicitSelectable = curSelectable.navigation.selectOnLeft;
                        nextOuterSelectableFromParams       = this._Selectables.Left;
                        findNextSelectableAuto_Vector       = Vector3.left;
                    }
                    else
                    {
                        curSelectableNextExplicitSelectable = curSelectable.navigation.selectOnRight;
                        nextOuterSelectableFromParams       = this._Selectables.Right;
                        findNextSelectableAuto_Vector       = Vector3.right;
                    }

                    if (curSelectableIsVH)
                    {
                        // Let Unity system decide
                        if (curSelectableUsesExplicitNav && curSelectableNextExplicitSelectable) return;

                        nextSelectable = nextOuterSelectableFromParams;
                    }
                    else
                    {
                        if (curSelectableUsesExplicitNav && curSelectableNextExplicitSelectable)
                        {
                            if (curSelectableNextExplicitSelectable != this)
                                // Let Unity system decide
                                return;

                            // Currently selected is an outer selectable which has <this> pointed as the next selectable, explicitly => select the nearest item instead.
                            // Setting to null, and SetOrFindNextSelectable() will decide what to do next, based on params
                            nextSelectable = null;
                        }
                        else
                            // Either nav mode is None, or Explicit and no Selectable assigned in that direction.
                            // In this case, we don't do anything, as it's required to have <this> selected as the next selectable in order to 'enter' OSA
                        {
                            return;
                        }
                    }
                    //SetOrFindNextSelectable(evSystem, nextSelectableFromParams, findNextSelectableAuto_Vector);
                }
                this.SetOrFindNextSelectable(nextSelectable, findNextSelectableAuto_Vector, curSelectable, curSelectableVH);
            }
            else
            {
            }
        }

        private Selectable FindVHSelectableInDirectionOrDefault(int itemIndex, int nextOSAItemDirSign, int extremityItemIndex, Selectable curSelectable, Selectable defaultSelectable)
        {
            Selectable nextSelectableCandidate = null;
            var        maxIterations           = 1000;
            var        curIterations           = 0;
            while (curIterations < maxIterations && !nextSelectableCandidate)
            {
                if (itemIndex == extremityItemIndex)
                {
                    // If after <maxIterations> items scrolled through OSA, none of them contains a Selectable, we just fallback to the selectable set in params. 
                    // SetOrFindNextSelectable() will further refine the decisions based on params
                    nextSelectableCandidate = defaultSelectable;
                    break;
                }

                itemIndex += nextOSAItemDirSign;

                this._OSA.BringToView(itemIndex);
                this._OSA.BringToView(itemIndex); // fix for variable-sized items cases; this needs to be called twice in that case
                var vh = this._OSA.GetBaseItemViewsHolderIfVisible(itemIndex);
                if (vh == null)
                {
                    nextSelectableCandidate = defaultSelectable;
                    break;
                }

                var nextSelectableCandidates = new List<Selectable>();
                vh.root.GetComponentsInChildren(nextSelectableCandidates);
                nextSelectableCandidate = this.GetClosestActiveSelectable(nextSelectableCandidates, curSelectable.gameObject);

                ++curIterations;
            }

            return nextSelectableCandidate;
        }

        private bool NavPreChecks(out Selectable curSelectable, out AbstractViewsHolder curSelectableVH)
        {
            if (this._OSA.BaseParameters.effects.LoopItems) throw new NotImplementedException("loop items nav");

            curSelectable   = null;
            curSelectableVH = null;
            if (this._OSA.VisibleItemsCount == 0)
            {
                this.EnterState_NoneIfNeeded();
                return false;
            }

            var evSystem = EventSystem.current;
            if (!evSystem)
            {
                this.EnterState_NoneIfNeeded();
                return false;
            }

            curSelectable = this.GetCurrentSelectable();
            if (!curSelectable)
            {
                this.EnterState_NoneIfNeeded();
                return false;
            }

            var inputVec = this.GetCurrentInputVector();
            if (!this.CheckInputStrength(inputVec)) return false;

            curSelectableVH = this.ViewsHolderFinder.GetViewsHolderFromSelectedGameObject(curSelectable.gameObject);
            if (!this.CheckScrollState(curSelectable, inputVec, curSelectableVH)) return false;

            return true;
        }

        private bool CheckInputStrength(Vector2 inputVec)
        {
            var absVert = Math.Abs(inputVec.y);
            var absHor  = Math.Abs(inputVec.x);

            if (absVert < .15f && absHor < .15f)
            {
                this.EnterState_NoneIfNeeded();
                return false;
            }

            return true;
        }

        private bool CheckScrollState(Selectable curSelectable, Vector2 inputVec, AbstractViewsHolder curSelectableVH)
        {
            //Debug.Log(GetCurrentInputVector().x + ", " + GetCurrentInputVector().y);

            var curSelectableNavMode = curSelectable.navigation.mode;
            if (curSelectableNavMode == Navigation.Mode.Automatic || curSelectableNavMode == Navigation.Mode.Horizontal || curSelectableNavMode == Navigation.Mode.Vertical)
            {
                this.EnterState_NoneIfNeeded();
                return false;
            }

            var vertSign = Math.Sign(inputVec.y);
            var horSign  = Math.Sign(inputVec.x);

            var prevIsVert = Math.Abs(this._ScrollStartInputVec.y) > Math.Abs(this._ScrollStartInputVec.x);
            var curIsVert  = Math.Abs(inputVec.y) > Math.Abs(inputVec.x);

            //bool sameDir = Math.Sign(_ScrollStartInputVec.x) == horSign && Math.Sign(_ScrollStartInputVec.y) == vertSign;
            var  sameOrientation = prevIsVert == curIsVert;
            bool sameSigns;
            if (curIsVert)
                sameSigns = Math.Sign(this._ScrollStartInputVec.y) == vertSign;
            else
                sameSigns = Math.Sign(this._ScrollStartInputVec.x) == horSign;

            var sameDir = sameOrientation && sameSigns;
            //bool curSelectableIsVH = curSelectableVH != null;
            //Debug.Log(ScrollState + ", sameDir " + sameDir + ", dt " + (Time.unscaledTime - _ScrollStartInputVecUnscaledTime));
            //bool inputStrengthIsSmaller = inputVec.magnitude < _ScrollStartInputVec.magnitude;
            switch (this.ScrollState)
            {
                case ScrollStateEnum.NONE:
                    this.EnterState_WaitingForInitial();
                    //if (curSelectable != _LastNavCommand.New.Selectable)
                    //{
                    //	if (curSelectableIsVH && _LastNavCommand.New.SelectableIsVH)
                    //	{
                    //		if (curSelectableVH.ItemIndex == _LastNavCommand.New.SelectableVHItemIndex)
                    //		{
                    //			// Don't select immediately, as curSelectable in this case was already selected by the Unity's nav system
                    //			return false;
                    //		}
                    //	}
                    //}

                    break;

                case ScrollStateEnum.WAITING_FOR_FIRST_DELAY:
                    //if (!sameDir || inputStrengthIsSmaller)
                    if (!sameDir)
                    {
                        //ScrollState = ScrollStateEnum.NONE;
                        //break;
                        this.EnterState_NoneIfNeeded();
                        return false;
                    }

                    if (Time.unscaledTime - this._ScrollStartInputVecUnscaledTime < .4f) return false;

                    this.ScrollState = ScrollStateEnum.SCROLLING;
                    break;

                case ScrollStateEnum.SCROLLING:
                    //if (!sameDir || inputStrengthIsSmaller)
                    if (!sameDir)
                    {
                        this.ScrollState = ScrollStateEnum.NONE;
                        break;
                    }

                    if (!this.CheckAndUpdateInputFrequency()) return false;

                    break;
            }
            #if DEBUG_OSA_PROACTIVE_NAVIGATOR
			Debug.Log("Aft " + ScrollState);
            #endif

            return true;
        }

        private bool CheckAndUpdateInputFrequency()
        {
            var actionsPerSec                     = this._NavManager.GetMaxInputModuleActionsPerSecondToExpect();
            if (actionsPerSec == 0) actionsPerSec = 10;
            var dtBetweenActions                  = 1f / actionsPerSec;
            if (Time.unscaledTime - this._LastInputProcessingUnscaledTime < dtBetweenActions) return false;
            this._LastInputProcessingUnscaledTime = Time.unscaledTime;

            return true;
        }

        private void SetOrFindNextSelectable(Selectable nextSelectable, Vector3 findNextSelectableAuto_Vector, Selectable curSelectable, AbstractViewsHolder curSelectableVH)
        {
            var                 evSystem          = EventSystem.current;
            var                 curSelectableIsVH = curSelectableVH != null;
            AbstractViewsHolder nextSelectableVH;
            if (nextSelectable)
                nextSelectableVH = this.ViewsHolderFinder.GetViewsHolderFromSelectedGameObject(nextSelectable.gameObject);
            else
                switch (this._OnNoSelectableSpecified)
                {
                    case OnNoSelectableSpecifiedEnum.KEEP_CURRENTLY_SELECTED: goto default;

                    case OnNoSelectableSpecifiedEnum.AUTO_FIND:
                        if (curSelectableIsVH)
                        {
                            nextSelectable = curSelectable.FindSelectable(findNextSelectableAuto_Vector);
                            if (nextSelectable)
                                nextSelectableVH = this.ViewsHolderFinder.GetViewsHolderFromSelectedGameObject(nextSelectable.gameObject);
                            else
                                nextSelectableVH = null;
                        }
                        else
                        {
                            this.GetClosestSelectableFromOSAsVHs(curSelectable.gameObject, out nextSelectable, out nextSelectableVH);
                        }

                        if (!nextSelectable) break;

                        break;

                    default:
                        nextSelectable   = null;
                        nextSelectableVH = null;
                        break;
                }

            if (curSelectable != this._LastNav.New.Selectable)
                throw new OSAException(
                    "[Please report this full error] Expecting curSelectable == _LastNavCommand.New.Selectable; " + "curSelectable = " + curSelectable + ", _LastNav.New.Selectable = " + this._LastNav.New.Selectable + ", _LastNav.New = " + this._LastNav.New.ToString()
                );

            if (nextSelectable)
            {
                var nextSelectableIsVH = nextSelectableVH != null;
                var type               = NavMoveType.UNKNOWN;
                if (curSelectableIsVH)
                {
                    if (!nextSelectableIsVH) type = NavMoveType.EXIT_OSA;
                }
                else
                {
                    if (nextSelectableIsVH) type = NavMoveType.ENTER_OSA;
                }

                var force = type == NavMoveType.ENTER_OSA;
                if (!force)
                {
                    var checkForTiming = false;
                    if (curSelectableIsVH)
                    {
                        if (this._LastNav.New.IsUnityBuiltinEvent)
                            if (!nextSelectableIsVH)
                                checkForTiming = true;
                    }
                    else
                    {
                        if (!this._LastNav.New.IsUnityBuiltinEvent)
                            if (nextSelectableIsVH)
                                checkForTiming = true;
                    }

                    if (checkForTiming)
                        // Going outside => Only allow if enough time has passed or if in the scrolling state
                        if (this.ScrollState != ScrollStateEnum.SCROLLING)
                            if (!this.DidEnoughTimePassSinceBoundaryCrossinEventOrPreEvent(this._LastNav.New))
                                return;
                }

                evSystem.SetSelectedGameObject(nextSelectable.gameObject);

                // TODO maybe call this from the same place where the builtin event is triggered
                this._LastNav.OnNewEvent_OSANav(nextSelectable, nextSelectableVH, type);
            }
        }

        private void SetNotInteractable()
        {
            // We don't want this to be selectable. It's just used for Unity's nav system so we can select the navigator as the target of outside selectables. 
            // When they're about to select this object, we select the nearest item instead

            //// Important to set these as null so that the interactable=false won't modify them
            this.targetGraphic = null;
            this.image         = null;

            //this.interactable = false;
            var nav = this.navigation;
            nav.mode        = Navigation.Mode.None;
            this.navigation = nav;
        }

        private bool GetClosestSelectableFromOSAsVHs(GameObject fromGO, out Selectable selectable, out AbstractViewsHolder containingVH)
        {
            var selectables       = new List<Selectable>();
            var mapSelectableToVh = new Dictionary<Selectable, AbstractViewsHolder>();
            for (var i = 0; i < this._OSA.VisibleItemsCount; i++)
            {
                var vh            = this._OSA.GetBaseItemViewsHolder(i);
                var vhSelectables = vh.root.GetComponentsInChildren<Selectable>();
                foreach (var vhSelectable in vhSelectables)
                {
                    selectables.Add(vhSelectable);
                    mapSelectableToVh[vhSelectable] = vh;
                }
            }

            var closestSelectable = this.GetClosestActiveSelectable(selectables, fromGO);
            if (closestSelectable)
            {
                selectable   = closestSelectable;
                containingVH = mapSelectableToVh[closestSelectable];
                return true;
            }

            selectable   = null;
            containingVH = null;
            return false;
        }

        private Selectable GetClosestActiveSelectable(List<Selectable> selectables, GameObject fromGO)
        {
            Selectable toSelect;
            if (selectables.Count == 0)
            {
                var inputVec = this.GetCurrentInputVector();
                toSelect = fromGO.GetComponent<Selectable>().FindSelectable(inputVec);
            }
            else
            {
                var fromPos = fromGO.transform.position;
                selectables.Sort((a, b) => Mathf.RoundToInt((Vector3.Distance(fromPos, a.transform.position) - Vector3.Distance(fromPos, b.transform.position)) * 50));
                toSelect = null;
                // Make sure it's interactable
                for (var i = 0; i < selectables.Count; i++)
                {
                    var s = selectables[i];
                    if (this.CanSelect(s))
                    {
                        toSelect = s;
                        break;
                    }
                }
            }

            return toSelect;
        }

        private bool CanSelect(Selectable s)
        {
            return s.interactable;
        }

        private Vector3 GetCurrentInputVector()
        {
            float vertInput;
            float horInput;
            var   down              = Input.GetKey(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.DownArrow);
            var   up                = Input.GetKey(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.UpArrow);
            var   vertArrowDown     = down || up;
            var   left              = Input.GetKey(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.LeftArrow);
            var   right             = Input.GetKey(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.RightArrow);
            var   horArrowDown      = left || right;
            var   j                 = Input.GetJoystickNames();
            var   isJoystickPresent = j != null && j.Length != 0;
            var   axisMultiplier    = isJoystickPresent ? this._JoystickInputMultiplier : this._ArrowsInputMultiplier;

            // Old approach: doesn't work well - arrows leave some velocity after they're released
            //if (down)
            //{
            //	if (!up)
            //		vertInput = -1;
            //}
            //else if (up)
            //	vertInput = 1;
            //else
            //{
            //	vertInput = Input.GetAxis("Vertical") * axisMultiplier;
            //}

            //if (left)
            //{
            //	if (!right)
            //		horInput = -1;
            //}
            //else if (down)
            //	horInput = 1;
            //else
            //	horInput = Input.GetAxis("Horizontal") * axisMultiplier;

            // Update: arrows leave some velocity even after they're released, so it's pointless to guess their initial velocities. Just using the axes instead, whether we have a joystick or not
            vertInput = Input.GetAxis("Vertical") * (vertArrowDown ? this._ArrowsInputMultiplier : axisMultiplier);
            horInput  = Input.GetAxis("Horizontal") * (horArrowDown ? this._ArrowsInputMultiplier : axisMultiplier);

            var vecInput = new Vector3(horInput, vertInput);
            return vecInput;
        }

        private Selectable GetCurrentSelectable()
        {
            var evSystem = EventSystem.current;
            if (!evSystem.currentSelectedGameObject) return null;

            var curSelectable = evSystem.currentSelectedGameObject.GetComponent<Selectable>();
            if (!curSelectable) return null;

            return curSelectable;
        }

        private bool DidEnoughTimePassSinceBoundaryCrossinEventOrPreEvent(NavEvent ev)
        {
            var minSecondsBase           = .05f;
            var minSecondsFromFiveFrames = Time.unscaledDeltaTime * 5;
            var minSeconds               = Math.Max(minSecondsBase, minSecondsFromFiveFrames);
            return ev.ElapsedUnscaledTime >= minSeconds;
        }
    }

    [Serializable]
    public class Selectables
    {
        [SerializeField] private Selectable _Up = null;
        public                   Selectable Up => this._Up;

        [SerializeField] private Selectable _Down = null;
        public                   Selectable Down => this._Down;

        [SerializeField] private Selectable _Left = null;
        public                   Selectable Left => this._Left;

        [SerializeField] private Selectable _Right = null;
        public                   Selectable Right => this._Right;

        [Tooltip(
            "Whether to set the corresponding Left/Right/Up/Down Selectables to point back this ScrollView (eg. the Down selectable's Up property will be <this> ScrollView). " + "This also overrides the Selectables' navigation mode to 'Explicit'"
        )]
        [SerializeField]
        private bool _SyncAllProvidedSelectables = true;

        public bool SyncAllProvidedSelectables => this._SyncAllProvidedSelectables;

        internal void SyncProvidedSelectables(Selectable target)
        {
            if (this.Up)
            {
                var nav = this.Up.navigation;
                nav.mode           = Navigation.Mode.Explicit;
                nav.selectOnDown   = target;
                this.Up.navigation = nav;
            }
            if (this.Down)
            {
                var nav = this.Down.navigation;
                nav.mode             = Navigation.Mode.Explicit;
                nav.selectOnUp       = target;
                this.Down.navigation = nav;
            }
            if (this.Left)
            {
                var nav = this.Left.navigation;
                nav.mode             = Navigation.Mode.Explicit;
                nav.selectOnRight    = target;
                this.Left.navigation = nav;
            }
            if (this.Right)
            {
                var nav = this.Right.navigation;
                nav.mode              = Navigation.Mode.Explicit;
                nav.selectOnLeft      = target;
                this.Right.navigation = nav;
            }
        }
    }

    //[Serializable]
    //public class OnNoSelectableSpecifiedInfo
    //{
    //	[SerializeField]
    //	OnNoSelectableSpecifiedEnum _FromItem = OnNoSelectableSpecifiedEnum.KEEP_CURRENTLY_SELECTED;
    //	public OnNoSelectableSpecifiedEnum Up { get { return _Up; } }

    //	[SerializeField]
    //	Selectable _Down = null;
    //	public Selectable Down { get { return _Down; } }

    //	[SerializeField]
    //	Selectable _Left = null;
    //	public Selectable Left { get { return _Left; } }

    //	[SerializeField]
    //	Selectable _Right = null;
    //	public Selectable Right { get { return _Right; } }
    //}

    public enum OnNoSelectableSpecifiedEnum
    {
        KEEP_CURRENTLY_SELECTED,
        AUTO_FIND,
    }

    internal enum ScrollStateEnum
    {
        NONE,
        WAITING_FOR_FIRST_DELAY,
        SCROLLING,
    }

    internal class NavEvents
    {
        public NavEvent Prev { get; private set; }
        public NavEvent New  { get; private set; }

        public NavEvents()
        {
            this.Prev = new();
            this.New  = new();
        }

        public void OnNewEvent_OSANav(Selectable newSelectable, AbstractViewsHolder nextSelectableVH, NavMoveType type)
        {
            this.UpdateInternal(newSelectable, nextSelectableVH, false, type);
        }

        public void OnNewEvent_UnityBuiltinNav(Selectable newSelectable, AbstractViewsHolder nextSelectableVH, NavMoveType type)
        {
            this.UpdateInternal(newSelectable, nextSelectableVH, true, type);
        }

        private void UpdateInternal(Selectable newSelectable, AbstractViewsHolder curSelectableVH, bool isUnityBuiltinEvent, NavMoveType type)
        {
            var p = this.Prev;
            this.Prev = this.New;
            this.New  = p;
            this.New.Update(newSelectable, curSelectableVH, isUnityBuiltinEvent, type);
        }
    }

    internal enum NavMoveType
    {
        EXIT_OSA,
        ENTER_OSA,
        UNKNOWN,
    }

    internal class NavEvent
    {
        public Selectable Selectable { get; private set; }

        //public AbstractViewsHolder SelectableVH { get; private set; }
        public int         SelectableVHItemIndex { get; private set; }
        public bool        IsUnityBuiltinEvent   { get; private set; }
        public bool        SelectableIsVH        => this.SelectableVHItemIndex != -1;
        public float       UnscaledTime          { get; private set; }
        public int         Frame                 { get; private set; }
        public float       ElapsedUnscaledTime   => Time.unscaledTime - this.UnscaledTime;
        public NavMoveType Type                  { get; private set; }

        public NavEvent()
        {
            this.SelectableVHItemIndex = -1;
        }

        public void Update(Selectable newSelectable, AbstractViewsHolder curSelectableVH, bool isUnityBuiltinEvent, NavMoveType type)
        {
            this.Selectable = newSelectable;
            //SelectableVH = curSelectableVH;
            this.SelectableVHItemIndex = curSelectableVH == null ? -1 : curSelectableVH.ItemIndex;
            this.IsUnityBuiltinEvent   = isUnityBuiltinEvent;
            this.Frame                 = Time.frameCount;
            this.UnscaledTime          = Time.unscaledTime;
            this.Type                  = type;
        }

        public override string ToString()
        {
            return
                "Selectable " + this.Selectable + ", SelectableVHItemIndex " + this.SelectableVHItemIndex + ", IsUnityBuiltinEvent " + this.IsUnityBuiltinEvent + ", UnscaledTime " + this.UnscaledTime + ", Frame " + this.Frame + ", ElapsedUnscaledTime " + this.ElapsedUnscaledTime + ", Type " + this.Type;
        }
    }
}