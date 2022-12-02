using System.Text;
using UnityEngine;
using RelationsInspector.Backend;
using RelationsInspector;
using System.Collections.Generic;
using DarkTonic.MasterAudio;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

// ReSharper disable once CheckNamespace
public class MasterAudioEventBackend : MinimalBackend<object, string> {
    public const string AllBusesOrGroupsWord = "All";

    public class AudioEventGroupNode {
        public EventSounds.EventType type;
        public string name;
        public bool useGroup;
        public AudioEventGroup group;
    }

    public class AmbientSoundNode {
        public string _groupName;

        public AmbientSoundNode(string groupName) {
            _groupName = groupName;
        }
    }

    public class FootstepSoundsNode
    {
        public string _groupName;

        public FootstepSoundsNode(string groupName)
        {
            _groupName = groupName;
        }
    }

    public class LinkedGroupNode {
        public List<string> parentLinkedGroups { get; set; }
        public string groupName;
        public bool hasLinkedGroupLoop;
        public string linkedGroupLoopName;

        public LinkedGroupNode(string groupName, List<string> parentLinkedGroups)
        {
            this.groupName = groupName;
            this.parentLinkedGroups = new List<string>(parentLinkedGroups);
        }
    }

    public class EndLinkedGroupNode {
        public List<string> parentLinkedGroups { get; set; }
        public string groupName;
        public bool hasLinkedGroupLoop;
        public string linkedGroupLoopName;

        public EndLinkedGroupNode(string groupName, List<string> parentLinkedGroups) {
            this.groupName = groupName;
            this.parentLinkedGroups = new List<string>(parentLinkedGroups);
        }
    }

    public class EventGroupData {
        public EventSounds.EventType type;
        public Func<EventSounds, AudioEventGroup> getGroup;
        public Func<EventSounds, bool> useGroup;
        public string name;
    }

    public class MechanimEventSound {
        public string group;
        public string type;
        public MechanimEventSound(string group, string type) { this.group = group; this.type = type; }
    }

    public class MechanimEventCustomEvent {
        public string customEvent;
        public string type;
        public MechanimEventCustomEvent(string customEvent, string type) { this.customEvent = customEvent; this.type = type; }
    }

    public static List<EventGroupData> namedEventGroups = new List<EventGroupData>()
    {
        new EventGroupData() {type = EventSounds.EventType.OnStart, getGroup = es=> es.startSound, useGroup = es => es.useStartSound, name = "Start" },
        new EventGroupData() {type = EventSounds.EventType.OnVisible, getGroup = es=> es.visibleSound, useGroup = es => es.useVisibleSound, name = "Visible" },
        new EventGroupData() {type = EventSounds.EventType.OnInvisible, getGroup = es=> es.invisibleSound, useGroup = es => es.useInvisibleSound, name = "Invisible" },
        new EventGroupData() {type = EventSounds.EventType.OnCollision, getGroup = es=> es.collisionSound, useGroup = es => es.useCollisionSound, name = "Collision Enter" },
        new EventGroupData() {type = EventSounds.EventType.OnCollisionExit, getGroup = es=> es.collisionExitSound, useGroup = es => es.useCollisionExitSound, name = "Collision Exit" },
        new EventGroupData() {type = EventSounds.EventType.OnTriggerEnter, getGroup = es=> es.triggerSound, useGroup = es => es.useTriggerEnterSound, name = "Trigger Enter" },
        new EventGroupData() {type = EventSounds.EventType.OnTriggerExit, getGroup = es=> es.triggerExitSound, useGroup = es => es.useTriggerExitSound, name = "Trigger Exit" },
        new EventGroupData() {type = EventSounds.EventType.OnMouseEnter, getGroup = es=> es.mouseEnterSound, useGroup = es => es.useMouseEnterSound, name = "Mouse Enter (Legacy)" },
        new EventGroupData() {type = EventSounds.EventType.OnMouseExit, getGroup = es=> es.mouseExitSound, useGroup = es => es.useMouseExitSound, name = "Mouse Exit (Legacy)" },
        new EventGroupData() {type = EventSounds.EventType.OnMouseClick, getGroup = es=> es.mouseClickSound, useGroup = es => es.useMouseClickSound, name = "Mouse Down (Legacy)" },
        new EventGroupData() {type = EventSounds.EventType.OnMouseUp, getGroup = es=> es.mouseUpSound, useGroup = es => es.useMouseUpSound, name = "Mouse Up (Legacy)" },
        new EventGroupData() {type = EventSounds.EventType.OnMouseDrag, getGroup = es=> es.mouseDragSound, useGroup = es => es.useMouseDragSound, name = "Mouse Drag (Legacy)" },
        new EventGroupData() {type = EventSounds.EventType.OnSpawned, getGroup = es=> es.spawnedSound, useGroup = es => es.useSpawnedSound, name = "Spawned (Pooling)" },
        new EventGroupData() {type = EventSounds.EventType.OnDespawned, getGroup = es=> es.despawnedSound, useGroup = es => es.useDespawnedSound, name = "Despawned (Pooling)" },
        new EventGroupData() {type = EventSounds.EventType.OnEnable, getGroup = es=> es.enableSound, useGroup = es => es.useEnableSound, name = "Enable" },
        new EventGroupData() {type = EventSounds.EventType.OnDisable, getGroup = es=> es.disableSound, useGroup = es => es.useDisableSound, name = "Disable" },
        new EventGroupData() {type = EventSounds.EventType.OnCollision2D, getGroup = es=> es.collision2dSound, useGroup = es => es.useCollision2dSound, name = "2D Collision Enter" },
        new EventGroupData() {type = EventSounds.EventType.OnCollisionExit2D, getGroup = es=> es.collisionExit2dSound, useGroup = es => es.useCollisionExit2dSound, name = "2D Collision Exit" },
        new EventGroupData() {type = EventSounds.EventType.OnTriggerEnter2D, getGroup = es=> es.triggerEnter2dSound, useGroup = es => es.useTriggerEnter2dSound, name = "2D Trigger Enter" },
        new EventGroupData() {type = EventSounds.EventType.OnTriggerExit2D, getGroup = es=> es.triggerExit2dSound, useGroup = es => es.useTriggerExit2dSound, name = "2D Trigger Exit" },
        new EventGroupData() {type = EventSounds.EventType.OnParticleCollision, getGroup = es=> es.particleCollisionSound, useGroup = es => es.useParticleCollisionSound, name = "Particle Collision" },
        new EventGroupData() {type = EventSounds.EventType.NGUIOnClick, getGroup = es=> es.nguiOnClickSound, useGroup = es => es.useNguiOnClickSound, name = "NGUI Mouse Click" },
        new EventGroupData() {type = EventSounds.EventType.NGUIMouseDown, getGroup = es=> es.nguiMouseDownSound, useGroup = es => es.useNguiMouseDownSound, name = "NGUI Mouse Down" },
        new EventGroupData() {type = EventSounds.EventType.NGUIMouseUp, getGroup = es=> es.nguiMouseUpSound, useGroup = es => es.useNguiMouseUpSound, name = "NGUI Mouse Up" },
        new EventGroupData() {type = EventSounds.EventType.NGUIMouseEnter, getGroup = es=> es.nguiMouseEnterSound, useGroup = es => es.useNguiMouseEnterSound, name = "NGUI Mouse Enter" },
        new EventGroupData() {type = EventSounds.EventType.NGUIMouseExit, getGroup = es=> es.nguiMouseExitSound, useGroup = es => es.useNguiMouseExitSound, name = "NGUI Mouse Exit" },
        new EventGroupData() {type = EventSounds.EventType.UnitySliderChanged, getGroup = es=> es.unitySliderChangedSound, useGroup = es => es.useUnitySliderChangedSound, name = "Slider Changed (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityButtonClicked, getGroup = es=> es.unityButtonClickedSound, useGroup = es => es.useUnityButtonClickedSound, name = "Button Click (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityPointerDown, getGroup = es=> es.unityPointerDownSound, useGroup = es => es.useUnityPointerDownSound, name = "Pointer Down (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityDrag, getGroup = es=> es.unityDragSound, useGroup = es => es.useUnityDragSound, name = "Drag (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityPointerUp, getGroup = es=> es.unityPointerUpSound, useGroup = es => es.useUnityPointerUpSound, name = "Pointer Up (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityPointerEnter, getGroup = es=> es.unityPointerEnterSound, useGroup = es => es.useUnityPointerEnterSound, name = "Pointer Enter (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityPointerExit, getGroup = es=> es.unityPointerExitSound, useGroup = es => es.useUnityPointerExitSound, name = "Pointer Exit (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityDrop, getGroup = es=> es.unityDropSound, useGroup = es => es.useUnityDropSound, name = "Drop (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityScroll, getGroup = es=> es.unityScrollSound, useGroup = es => es.useUnityScrollSound, name = "Scroll (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityUpdateSelected, getGroup = es=> es.unityUpdateSelectedSound, useGroup = es => es.useUnityUpdateSelectedSound, name = "Update Slected (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnitySelect, getGroup = es=> es.unitySelectSound, useGroup = es => es.useUnitySelectSound, name = "Select (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityDeselect, getGroup = es=> es.unityDeselectSound, useGroup = es => es.useUnityDeselectSound, name = "Deselect (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityMove, getGroup = es=> es.unityMoveSound, useGroup = es => es.useUnityMoveSound, name = "Move (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityInitializePotentialDrag, getGroup = es=> es.unityInitializePotentialDragSound, useGroup = es => es.useUnityInitializePotentialDragSound, name = "Initialize Potential Drag (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityBeginDrag, getGroup = es=> es.unityBeginDragSound, useGroup = es => es.useUnityBeginDragSound, name = "Begin Drag (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityEndDrag, getGroup = es=> es.unityEndDragSound, useGroup = es => es.useUnityEndDragSound, name = "End Drag (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnitySubmit, getGroup = es=> es.unitySubmitSound, useGroup = es => es.useUnitySubmitSound, name = "Submit (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityCancel, getGroup = es=> es.unityCancelSound, useGroup = es => es.useUnityCancelSound, name = "Cancel (uGUI)" },
        new EventGroupData() {type = EventSounds.EventType.UnityToggle, getGroup = es=> es.unityToggleSound, useGroup = es => es.useUnityToggleSound, name = "Toggle (uGUI)" }
    };

    const string busFilterPrefsKey = "MasterAudioEventBackend" + "busFilter";
    static string busFilter;
    public static string BusFilter {
        get {
            busFilter = EditorPrefs.GetString(busFilterPrefsKey, string.Empty);
            return busFilter;
        }
        set {
            busFilter = value;
            EditorPrefs.SetString(busFilterPrefsKey, busFilter);
        }
    }

    const string groupFilterPrefsKey = "MasterAudioEventBackend" + "groupFilter";
    static string groupFilter;
    public static string GroupFilter {
        get {
            groupFilter = EditorPrefs.GetString(groupFilterPrefsKey, string.Empty);
            return groupFilter;
        }
        set {
            groupFilter = value;
            EditorPrefs.SetString(groupFilterPrefsKey, groupFilter);
        }
    }


    public override void Awake(GetAPI getAPI) {
        base.Awake(getAPI);

        var targets = api.GetTargets();
        if (targets.Length == 1 && (targets[0] as string == "currentScene"))
            ResetGraph();

        busFilter = EditorPrefs.GetString(busFilterPrefsKey, string.Empty);
        groupFilter = EditorPrefs.GetString(groupFilterPrefsKey, string.Empty);

    }

    public override IEnumerable<Relation<object, string>> GetRelations(object entity) {
        var asAmbientSound = entity as DarkTonic.MasterAudio.AmbientSound;
        if (asAmbientSound != null) {
            yield return new Relation<object, string>(asAmbientSound, new AmbientSoundNode(asAmbientSound.AmbientSoundGroup), "Ambient Sound");
        }

        var asFootstepSound = entity as DarkTonic.MasterAudio.FootstepSounds;
        if (asFootstepSound != null)
        {
            foreach (var stepGroup in asFootstepSound.footstepGroups)
            {
                yield return new Relation<object, string>(asFootstepSound, new FootstepSoundsNode(stepGroup.soundType), "Footstep Sound");
            }
        }

        var asEndLinkedGroupNode = entity as EndLinkedGroupNode;
        if (asEndLinkedGroupNode != null) {
            foreach (var linkedGroupName in GetStartLinkedGroups(asEndLinkedGroupNode.groupName)) {
                if (asEndLinkedGroupNode.parentLinkedGroups.Contains(linkedGroupName)) {
                    asEndLinkedGroupNode.hasLinkedGroupLoop = true;
                    asEndLinkedGroupNode.linkedGroupLoopName = linkedGroupName;
                    continue;
                }

                yield return
                    new Relation<object, string>(asEndLinkedGroupNode, new LinkedGroupNode(linkedGroupName, asEndLinkedGroupNode.parentLinkedGroups),
                        GetGroupLinkLabel(asEndLinkedGroupNode.groupName, false));
            }

            foreach (var endLinkedGroupName in GetStopLinkedGroups(asEndLinkedGroupNode.groupName)) {
                if (asEndLinkedGroupNode.parentLinkedGroups.Contains(endLinkedGroupName)) {
                    asEndLinkedGroupNode.hasLinkedGroupLoop = true;
                    asEndLinkedGroupNode.linkedGroupLoopName = endLinkedGroupName;
                    continue;
                }

                yield return
                    new Relation<object, string>(asEndLinkedGroupNode, new EndLinkedGroupNode(endLinkedGroupName, asEndLinkedGroupNode.parentLinkedGroups),
                        GetGroupLinkLabel(asEndLinkedGroupNode.groupName, true));
            }
            yield break;
        }

        var asLinkedGroupNode = entity as LinkedGroupNode;
        if (asLinkedGroupNode != null) {
            foreach (var linkedGroupName in GetStartLinkedGroups(asLinkedGroupNode.groupName)) {
                if (asLinkedGroupNode.parentLinkedGroups.Contains(linkedGroupName)) {
                    asLinkedGroupNode.hasLinkedGroupLoop = true;
                    asLinkedGroupNode.linkedGroupLoopName = linkedGroupName;
                    continue;
                }

                yield return
                    new Relation<object, string>(asLinkedGroupNode, new LinkedGroupNode(linkedGroupName, asLinkedGroupNode.parentLinkedGroups),
                        GetGroupLinkLabel(asLinkedGroupNode.groupName, false));
            }
            foreach (var startLinkedGroupName in GetStopLinkedGroups(asLinkedGroupNode.groupName)) {
                if (asLinkedGroupNode.parentLinkedGroups.Contains(startLinkedGroupName)) {
                    asLinkedGroupNode.hasLinkedGroupLoop = true;
                    asLinkedGroupNode.linkedGroupLoopName = startLinkedGroupName;
                    continue;
                }

                yield return
                    new Relation<object, string>(asLinkedGroupNode, new EndLinkedGroupNode(startLinkedGroupName, asLinkedGroupNode.parentLinkedGroups),
                        GetGroupLinkLabel(asLinkedGroupNode.groupName, true));
            }
            yield break;
        }

        var asAmbientSoundNode = entity as AmbientSoundNode;
        if (asAmbientSoundNode != null) {
            foreach (var linkedGroupName in GetStartLinkedGroups(asAmbientSoundNode._groupName)) {
                yield return
                    new Relation<object, string>(asAmbientSoundNode, new LinkedGroupNode(linkedGroupName, new List<string> { asAmbientSoundNode._groupName }),
                        GetGroupLinkLabel(asAmbientSoundNode._groupName, false));
            }
            foreach (var endLinkedGroupName in GetStopLinkedGroups(asAmbientSoundNode._groupName)) {
                yield return
                    new Relation<object, string>(asAmbientSoundNode, new EndLinkedGroupNode(endLinkedGroupName, new List<string> { asAmbientSoundNode._groupName }),
                        GetGroupLinkLabel(asAmbientSoundNode._groupName, true));
            }
            yield break;
        }

        // connect EventSounds components to their AudioEventGroups that contain actions
        var asEventSounds = entity as EventSounds;
        if (asEventSounds != null) {
            foreach (var groupNode in GetGroupNodes(asEventSounds))
                yield return new Relation<object, string>(asEventSounds, groupNode, "handles event");

            yield break;
        }

        // connect AudioEventGroups to their actions
        var asGroupNode = entity as AudioEventGroupNode;
        if (asGroupNode != null) {
            foreach (var audioEvent in GetAudioEvents(asGroupNode.group))
                yield return new Relation<object, string>(asGroupNode, audioEvent, "invokes action");

            yield break;
        }

        // connect PlaySound actions to their linked groups
        var asAction = entity as AudioEvent;
        if (asAction != null) {
            if (asAction.currentSoundFunctionType == MasterAudio.EventSoundFunctionType.PlaySound) {
                foreach (var linkedGroupName in GetStartLinkedGroups(asAction.soundType)) {
                    yield return
                        new Relation<object, string>(asAction, new LinkedGroupNode(linkedGroupName, new List<string> { asAction.soundType }),
                            GetGroupLinkLabel(asAction.soundType, true));
                }
                foreach (var endLinkedGroupName in GetStopLinkedGroups(asAction.soundType)) {
                    yield return
                        new Relation<object, string>(asAction, new EndLinkedGroupNode(endLinkedGroupName, new List<string> { asAction.soundType }),
                            GetGroupLinkLabel(asAction.soundType, false));
                }
            }
            yield break;
        }

        var asAnimator = entity as Animator;
        if (asAnimator != null) {
            var controller = asAnimator.runtimeAnimatorController as AnimatorController;
            if (controller != null) {
                foreach (var layer in controller.layers) {
                    if (GetSoundGroups(layer).Any())
                        yield return new Relation<object, string>(asAnimator, layer, "contains layer");
                }
            }
            yield break;
        }

        var asLayer = entity as AnimatorControllerLayer;
        if (asLayer != null) {
            foreach (var state in asLayer.stateMachine.states) {
                if (GetSoundGroups(state.state).Any())
                    yield return new Relation<object, string>(asLayer, state.state, "contains state");
            }
            yield break;
        }

        var asAnimState = entity as AnimatorState;
        if (asAnimState != null) {
            foreach (var behaviour in asAnimState.behaviours) {
                var asStateSounds = behaviour as MechanimStateSounds;
                if (asStateSounds != null) {
                    if (!string.IsNullOrEmpty(asStateSounds.enterSoundGroup) && !FilterSoundGroup(asStateSounds.enterSoundGroup)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventSound(asStateSounds.enterSoundGroup, "On Enter"),
                            "plays on state entry");
                    }

                    if (!string.IsNullOrEmpty(asStateSounds.exitSoundGroup) && !FilterSoundGroup(asStateSounds.exitSoundGroup)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventSound(asStateSounds.exitSoundGroup, "On Exit"),
                            "plays on state exit");
                    }

                    if (!string.IsNullOrEmpty(asStateSounds.TimedSoundGroup) && !FilterSoundGroup(asStateSounds.TimedSoundGroup)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventSound(asStateSounds.TimedSoundGroup, "Anim-Timed"),
                            "plays on animation timeline");
                    }

                    if (!string.IsNullOrEmpty(asStateSounds.MultiSoundsTimedGroup) && !FilterSoundGroup(asStateSounds.MultiSoundsTimedGroup)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventSound(asStateSounds.MultiSoundsTimedGroup, "Multi Anim-Timed"),
                            "plays on animation timeline 0-4 times");
                    }
                }

                var asStateEvents = behaviour as MechanimStateCustomEvents;
                if (asStateEvents != null) {
                    if (!string.IsNullOrEmpty(asStateEvents.enterCustomEvent) && !FilterCustomEvent(asStateEvents.enterCustomEvent)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventCustomEvent(asStateEvents.enterCustomEvent, "On Enter"),
                            "plays on state entry");
                    }

                    if (!string.IsNullOrEmpty(asStateEvents.exitCustomEvent) && !FilterCustomEvent(asStateEvents.exitCustomEvent)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventCustomEvent(asStateEvents.exitCustomEvent, "On Exit"),
                            "plays on state exit");
                    }

                    if (!string.IsNullOrEmpty(asStateEvents.timedCustomEvent) && !FilterCustomEvent(asStateEvents.timedCustomEvent)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventCustomEvent(asStateEvents.timedCustomEvent, "Anim-Timed"),
                            "plays on animation timeline");
                    }

                    if (!string.IsNullOrEmpty(asStateEvents.MultiTimedEvent) && !FilterCustomEvent(asStateEvents.MultiTimedEvent)) {
                        yield return new Relation<object, string>(
                            asAnimState,
                            new MechanimEventCustomEvent(asStateEvents.MultiTimedEvent, "Multi Anim-Timed"),
                            "plays on animation timeline");
                    }
                }
            }
            yield break;
        }

        var asMechanimEventSound = entity as MechanimEventSound;
        if (asMechanimEventSound != null) {
            foreach (var linkedGroupName in GetStartLinkedGroups(asMechanimEventSound.group)) {
                yield return
                    new Relation<object, string>(asMechanimEventSound, new LinkedGroupNode(linkedGroupName, new List<string> { asMechanimEventSound.group }),
                        GetGroupLinkLabel(asMechanimEventSound.group, true));
            }
            foreach (var linkedGroupName in GetStopLinkedGroups(asMechanimEventSound.group)) {
                yield return
                    new Relation<object, string>(asMechanimEventSound, new EndLinkedGroupNode(linkedGroupName, new List<string> { asMechanimEventSound.group }),
                        GetGroupLinkLabel(asMechanimEventSound.group, false));
            }

            yield break;
        }
    }

    public override Rect OnGUI() {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            // regenerate the graph
            if (GUILayout.Button("Show active scene", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                ResetGraph();

            // filter actions by group
            GUILayout.Space(20);
            string[] groupNames;
            if (MasterAudio.SafeInstance != null) {
                groupNames = new string[] { AllBusesOrGroupsWord }.Concat(MasterAudio.Instance.GroupNames.Skip(2)).ToArray();
            } else {
                groupNames = new string[] { AllBusesOrGroupsWord };
            }

            int groupFilterIndex = Mathf.Max(0, Array.IndexOf(groupNames, groupFilter));    // short for: if(id==-1) id = 0;
            GUILayout.Label("Filter by group", EditorStyles.miniLabel);
            int newGroupFilterIndex = EditorGUILayout.Popup(groupFilterIndex, groupNames, EditorStyles.toolbarPopup, GUILayout.Width(70));
            string newGroupFilter = newGroupFilterIndex <= 0 ? string.Empty : groupNames[newGroupFilterIndex];
            if (newGroupFilter != groupFilter) {
                groupFilter = newGroupFilter;
                EditorPrefs.SetString(groupFilterPrefsKey, groupFilter);
                ResetGraph();
            }

            // filter actions by bus
            GUILayout.Space(20);
            string[] busNames;
            if (MasterAudio.SafeInstance != null) {
                busNames = new string[] { AllBusesOrGroupsWord }.Concat(MasterAudio.Instance.BusNames.Skip(2)).ToArray();
            } else {
                busNames = new string[] { AllBusesOrGroupsWord };
            }
            int busFilterIndex = Mathf.Max(0, Array.IndexOf(busNames, busFilter));
            GUILayout.Label("Filter by bus", EditorStyles.miniLabel);
            int newBusFilterIndex = EditorGUILayout.Popup(busFilterIndex, busNames, EditorStyles.toolbarPopup, GUILayout.Width(70));
            string newBusFilter = newBusFilterIndex <= 0 ? string.Empty : busNames[newBusFilterIndex];
            if (newBusFilter != busFilter) {
                busFilter = newBusFilter;
                EditorPrefs.SetString(busFilterPrefsKey, busFilter);
                ResetGraph();
            }

            // fill up the toolbar
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
        return base.OnGUI();
    }

    void ResetGraph() {
        var eventSoundsComponents = UnityEngine.Object
            .FindObjectsOfType<EventSounds>()
            .Where(comp => GetGroupNodes(comp).Any());

        var newTargets = eventSoundsComponents
            .Cast<object>();

        var mechanimStateSoundsObjects = UnityEngine.Object
            .FindObjectsOfType<Animator>()
            .Where(animator => GetSoundGroups(animator).Any());

        newTargets = newTargets.Concat(mechanimStateSoundsObjects.Cast<object>());

        var mechanimStateCustomEventObjects = UnityEngine.Object
            .FindObjectsOfType<Animator>()
            .Where(animator => GetCustomEvents(animator).Any());

        newTargets = newTargets.Concat(mechanimStateCustomEventObjects.Cast<object>());

        var ambientSoundsComponents = UnityEngine.Object
            .FindObjectsOfType<DarkTonic.MasterAudio.AmbientSound>()
            .Where(f => IncludeAmbientSound(f))
            .ToArray();

        newTargets = newTargets.Concat(ambientSoundsComponents);
        // put all components in an object array

        var footstepSoundsComponents = UnityEngine.Object
            .FindObjectsOfType<DarkTonic.MasterAudio.FootstepSounds>()
            .Where(f => IncludeFootstepSounds(f))
            .ToArray();

        newTargets = newTargets.Concat(footstepSoundsComponents);

        var newTargetList = newTargets.ToArray();

        api.ResetTargets(newTargetList);
    }

    public override GUIContent GetContent(object entity) {
        var asEventSounds = entity as EventSounds;
        if (asEventSounds != null) {
            return base.GetContent(asEventSounds.gameObject);
        }

        var asFootstepSounds = entity as FootstepSounds;
        if (asFootstepSounds != null)
        {
            return base.GetContent(asFootstepSounds.gameObject);
        }

        var asFootstepNode = entity as FootstepSoundsNode;
        if (asFootstepNode != null)
        {
            return new GUIContent(GetFoostepSoundNodeLabel(asFootstepNode));
        }

        var asGroupNode = entity as AudioEventGroupNode;
        if (asGroupNode != null) {
            return new GUIContent(asGroupNode.name);
        }

        var asAmbientSound = entity as AmbientSound;
        if (asAmbientSound != null) {
            return base.GetContent(asAmbientSound.gameObject);
        }

        var asAmbientSoundNode = entity as AmbientSoundNode;
        if (asAmbientSoundNode != null) {
            return new GUIContent(GetAmbientSoundNodeLabel(asAmbientSoundNode));
        }

        var asAudioEvent = entity as AudioEvent;
        if (asAudioEvent != null) {
            return new GUIContent(GetAudioEventLabel(asAudioEvent));
        }

        var asLinkedGroupNode = entity as LinkedGroupNode;
        if (asLinkedGroupNode != null) {
            var label = "Play 'Start' Linked\nGroup: " + asLinkedGroupNode.groupName;
            if (asLinkedGroupNode.hasLinkedGroupLoop) {
                label += "\n-Linked Group Loop to '" + asLinkedGroupNode.linkedGroupLoopName + "'-";
            }
            return new GUIContent(label);
        }

        var asStopLinkedGroupNode = entity as EndLinkedGroupNode;
        if (asStopLinkedGroupNode != null) {
            var label = "Play 'Stop' Linked\nGroup: " + asStopLinkedGroupNode.groupName;
            if (asStopLinkedGroupNode.hasLinkedGroupLoop) {
                label += "\n-Linked Group Loop to '" + asStopLinkedGroupNode.linkedGroupLoopName + "'-";
            }
            return new GUIContent(label);
        }

        var asAnimator = entity as Animator;
        if (asAnimator != null) {
            return base.GetContent(asAnimator.gameObject);
        }

        var asLayer = entity as AnimatorControllerLayer;
        if (asLayer != null) {
            return new GUIContent("Mechanim Layer\n" + asLayer.name);
        }

        var asState = entity as AnimatorState;
        if (asState != null) {
            return new GUIContent("State\n" + asState.name);
        }

        var asMechanimEventSound = entity as MechanimEventSound;
        if (asMechanimEventSound != null) {
            return new GUIContent(asMechanimEventSound.type + "\n" + "Play Sound Group\n" + asMechanimEventSound.group);
        }

        var asMechanimCustomEvent = entity as MechanimEventCustomEvent;
        if (asMechanimCustomEvent != null) {
            return new GUIContent(asMechanimCustomEvent.type + "\n" + "Fire Custom Event\n" + asMechanimCustomEvent.customEvent);
        }

        return base.GetContent(entity);
    }

    public override string GetEntityTooltip(object entity) {
        var asAudioEvent = entity as AudioEvent;
        if (asAudioEvent != null)
            return GetAudioEventTooltip(asAudioEvent);

        var asAnimator = entity as Animator;
        if (asAnimator != null)
            return asAnimator.name + " Animator";

        return base.GetEntityTooltip(entity);
    }

    // handles node selection changes:
    // when a EventSounds node is selected, its GameObject should be be selected other Unity windows

    public override void OnEntitySelectionChange(object[] selection) {
        if (selection.Count() == 1) {
            var single = selection.First();

            var asAmbientSoundNode = single as AmbientSoundNode;
            if (asAmbientSoundNode != null) {
                var groupTransform = MasterAudio.FindGroupTransform(asAmbientSoundNode._groupName);
                if (groupTransform != null) {
                    Selection.activeObject = groupTransform.gameObject;
                }
                return;
            }

            var asFootstepSoundNode = single as FootstepSoundsNode;
            if (asFootstepSoundNode != null)
            {
                var groupTransform = MasterAudio.FindGroupTransform(asFootstepSoundNode._groupName);
                if (groupTransform != null)
                {
                    Selection.activeObject = groupTransform.gameObject;
                }
                return;
            }

            // action nodes are handled depending on their action type
            var asAudioEvent = single as AudioEvent;
            if (asAudioEvent != null) {
                // PlaySound: the played group should be selected in other Unity windows
                // GroupControl: if only a single group is affected, it should be selected in other unity windows
                switch (asAudioEvent.currentSoundFunctionType) {
                    case MasterAudio.EventSoundFunctionType.PlaySound: {
                            var groupTransform = MasterAudio.FindGroupTransform(asAudioEvent.soundType);
                            if (groupTransform != null)
                                Selection.activeObject = groupTransform;
                        }
                        break;

                    case MasterAudio.EventSoundFunctionType.GroupControl: {
                            if (!asAudioEvent.allSoundTypesForGroupCmd) {
                                var groupTransform = MasterAudio.FindGroupTransform(asAudioEvent.soundType);
                                if (groupTransform != null)
                                    Selection.activeObject = groupTransform;
                            }
                        }
                        break;

                    case MasterAudio.EventSoundFunctionType.PlaylistControl: {
                            if (!asAudioEvent.allPlaylistControllersForGroupCmd) {
                                var controller = PlaylistController.InstanceByName(asAudioEvent.playlistControllerName);
                                if (controller != null)
                                    Selection.activeObject = controller;
                            }
                        }
                        break;
                    case MasterAudio.EventSoundFunctionType.UnityMixerControl: {
                            if (asAudioEvent.currentMixerCommand == MasterAudio.UnityMixerCommand.TransitionToSnapshot) {
                                if (asAudioEvent.snapshotToTransitionTo != null) {
                                    // run AudioMixerWindow.Create
                                    // Selection.activeObject = asAudioEvent.snapshotToTransitionTo.audioMixer;
                                }
                            }
                        }
                        break;
                    case MasterAudio.EventSoundFunctionType.PersistentSettingsControl: {
                            if (asAudioEvent.currentPersistentSettingsCommand == MasterAudio.PersistentSettingsCommand.SetGroupVolume) {
                                if (!asAudioEvent.allSoundTypesForGroupCmd) {
                                    var groupTransform = MasterAudio.FindGroupTransform(asAudioEvent.soundType);
                                    if (groupTransform != null)
                                        Selection.activeObject = groupTransform;
                                }
                            }
                        }
                        break;
                }
                return;
            }

            // AudioEventGroup: don't do anything
            var asGroupNode = single as AudioEventGroupNode;
            if (asGroupNode != null)
                return;

            var asLinkedGroup = single as LinkedGroupNode;
            if (asLinkedGroup != null) {
                var groupTransform = MasterAudio.FindGroupTransform(asLinkedGroup.groupName);
                if (groupTransform != null) {
                    Selection.activeObject = groupTransform;
                }
                return;
            }

            var asStopLinkedGroup = single as EndLinkedGroupNode;
            if (asStopLinkedGroup != null) {
                var groupTransform = MasterAudio.FindGroupTransform(asStopLinkedGroup.groupName);
                if (groupTransform != null) {
                    Selection.activeObject = groupTransform;
                }
                return;
            }

            var asAnimatorState = single as AnimatorState;
            if (asAnimatorState != null) {
                Selection.activeObject = asAnimatorState;
                return;
            }

            var asMechanimEventSound = single as MechanimEventSound;
            if (asMechanimEventSound != null) {
                var groupTransform = MasterAudio.FindGroupTransform(asMechanimEventSound.group);
                if (groupTransform != null) {
                    Selection.activeObject = groupTransform;
                }
                return;
            }

            var asMechanimCustomEvent = single as MechanimEventCustomEvent;
            if (asMechanimCustomEvent != null) {
                var mago = MasterAudio.SafeInstance;
                if (mago != null) {
                    Selection.activeObject = mago;
                }
                return;
            }
        }
        base.OnEntitySelectionChange(selection);
    }

    // returns all actions of the given AudioEventGroup that pass the group- and bus filters
    static IEnumerable<AudioEvent> GetAudioEvents(AudioEventGroup group) {
        return group.SoundEvents.Where(IncludeAudioEvent);
    }

    static bool IncludeAmbientSound(DarkTonic.MasterAudio.AmbientSound ambient) {
        if (MasterAudio.SoundGroupHardCodedNames.Contains(ambient.AmbientSoundGroup)) {
            return false;
        }

        if (FilterSoundGroup(ambient.AmbientSoundGroup)) {
            return false;
        }

        return true;
    }

    static bool IncludeFootstepSounds(DarkTonic.MasterAudio.FootstepSounds footstepSounds)
    {
        if (footstepSounds.footstepGroups.Count == 0)
        {
            return false;
        }

        foreach (var footstep in footstepSounds.footstepGroups)
        {
            if (!FilterSoundGroup(footstep.soundType))
            {
                return true;
            }
        }


        return false;
    }

    static bool IncludeAudioEvent(AudioEvent ev) {
        switch (ev.currentSoundFunctionType) {
            case MasterAudio.EventSoundFunctionType.PlaySound:
            case MasterAudio.EventSoundFunctionType.GroupControl:
                return !FilterSoundGroup(ev.soundType);

            case MasterAudio.EventSoundFunctionType.BusControl:
                if (!string.IsNullOrEmpty(groupFilter)) {
                    return false;
                }

                return ev.allSoundTypesForBusCmd || string.IsNullOrEmpty(busFilter) || ev.busName == busFilter;

            case MasterAudio.EventSoundFunctionType.PlaylistControl:
            case MasterAudio.EventSoundFunctionType.UnityMixerControl:
            case MasterAudio.EventSoundFunctionType.CustomEventControl:
            case MasterAudio.EventSoundFunctionType.PersistentSettingsControl:
                return string.IsNullOrEmpty(busFilter) && string.IsNullOrEmpty(groupFilter);
        }
        return true;
    }

    static IEnumerable<string> GetSoundGroups(Animator animator) {
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
            return Enumerable.Empty<string>();

        return controller.layers.SelectMany(layer => GetSoundGroups(layer));
    }

    static IEnumerable<string> GetSoundGroups(AnimatorControllerLayer layer) {
        return layer.stateMachine.states.SelectMany(state => GetSoundGroups(state.state));
    }

    static IEnumerable<string> GetSoundGroups(AnimatorState animatorState) {
        return animatorState.behaviours
            .OfType<MechanimStateSounds>()
            .SelectMany(stateSound => GetSoundGroups(stateSound));
    }

    static IEnumerable<string> GetSoundGroups(MechanimStateSounds behaviour) {
        var groups = new[] { behaviour.enterSoundGroup, behaviour.exitSoundGroup, behaviour.TimedSoundGroup, behaviour.MultiSoundsTimedGroup };
        return groups.Where(name => !string.IsNullOrEmpty(name) && !FilterSoundGroup(name));
    }



    static IEnumerable<string> GetCustomEvents(Animator animator) {
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
            return Enumerable.Empty<string>();

        return controller.layers.SelectMany(layer => GetCustomEvents(layer));
    }

    static IEnumerable<string> GetCustomEvents(AnimatorControllerLayer layer) {
        return layer.stateMachine.states.SelectMany(state => GetCustomEvents(state.state));
    }

    static IEnumerable<string> GetCustomEvents(AnimatorState animatorState) {
        return animatorState.behaviours
            .OfType<MechanimStateCustomEvents>()
            .SelectMany(stateEvents => GetCustomEvents(stateEvents));
    }

    static IEnumerable<string> GetCustomEvents(MechanimStateCustomEvents behaviour) {
        var customEvents = new[] { behaviour.enterCustomEvent, behaviour.exitCustomEvent, behaviour.timedCustomEvent, behaviour.MultiTimedEvent };
        return customEvents.Where(name => !string.IsNullOrEmpty(name) && !FilterCustomEvent(name));
    }

    static bool FilterSoundGroup(string name) {
        if (!string.IsNullOrEmpty(groupFilter) && groupFilter != name) {
            return true;
        }

        if (MasterAudio.SoundGroupHardCodedNames.Contains(name)) {
            return true;
        }

        if (!string.IsNullOrEmpty(busFilter)) {
            var bus = GetGroupBus(name);
            if (bus == null || bus.busName != busFilter) {
                return true;
            }
        }

        return false;
    }

    static bool FilterCustomEvent(string customEventName) {
        if (MasterAudio.SoundGroupHardCodedNames.Contains(customEventName)) {
            return true;
        }

        return !string.IsNullOrEmpty(groupFilter) || !string.IsNullOrEmpty(busFilter);
    }

    // returns nodes for all AudioEventGroups of the given component which are in use and contain actions
    static IEnumerable<AudioEventGroupNode> GetGroupNodes(EventSounds esComponent) {
        var customGroupNodes = esComponent.userDefinedSounds.Select(
            element => new AudioEventGroupNode() {
                type = 0,
                group = element,
                useGroup = true,
                name = "Custom event\n" + element.customEventName
            }
            );

        var componentGroups = GetNamedEventGroupNodes(esComponent).Concat(customGroupNodes);

        foreach (var node in componentGroups) {
            if (!node.useGroup)
                continue;
            if (!GetAudioEvents(node.group).Any())
                continue;

            yield return node;
        }
    }

    static string GetAudioEventTooltip(AudioEvent ev) {
        switch (ev.currentSoundFunctionType) {
            case MasterAudio.EventSoundFunctionType.PlaySound: {
                    var bus = GetGroupBus(ev.soundType);
                    if (bus != null)
                        return "Bus: " + bus.busName;
                }
                break;

            case MasterAudio.EventSoundFunctionType.GroupControl: {
                    if (ev.allSoundTypesForGroupCmd)
                        return string.Empty;
                    var bus = GetGroupBus(ev.soundType);
                    if (bus != null)
                        return "Bus: " + bus.busName;
                }
                break;

            default:
                return string.Empty;
        }
        return string.Empty;
    }

    static GroupBus GetGroupBus(string groupName) {
        var groupComponent = GetGroupComponent(groupName);
        if (groupComponent == null)
            return null;

        var index = groupComponent.busIndex - MasterAudio.HardCodedBusOptions;
        if (index < 0 || index >= MasterAudio.GroupBuses.Count)
            return null;

        return MasterAudio.GroupBuses[index];
    }

    static string GetFoostepSoundNodeLabel(FootstepSoundsNode node)
    {
        var sb = new StringBuilder("Footstep Sound");
        sb.Append("\nGroup: " + node._groupName);
        return sb.ToString();
    }

    static string GetAmbientSoundNodeLabel(AmbientSoundNode node) {
        var sb = new StringBuilder("Ambient Sound");
        sb.Append("\nGroup: " + node._groupName);
        return sb.ToString();
    }

    // returns a node label text for the given action
    static string GetAudioEventLabel(AudioEvent ev) {
        var sb = new System.Text.StringBuilder(ev.currentSoundFunctionType.ToString());

        switch (ev.currentSoundFunctionType) {
            case MasterAudio.EventSoundFunctionType.PlaySound:
                sb.Append("\nGroup: " + ev.soundType);
                break;

            case MasterAudio.EventSoundFunctionType.GroupControl:
                sb.Append("\nCmd: " + ev.currentSoundGroupCommand);
                sb.Append("\nGroup: " + (ev.allSoundTypesForGroupCmd ? "all" : ev.soundType));
                break;

            case MasterAudio.EventSoundFunctionType.BusControl:
                sb.Append("\nCmd: " + ev.currentBusCommand);
                sb.Append("\nBus: " + (ev.allSoundTypesForBusCmd ? "all" : ev.busName));
                break;

            case MasterAudio.EventSoundFunctionType.PlaylistControl:
                sb.Append("\nCmd: " + ev.currentPlaylistCommand);
                sb.Append("\nController: " + (ev.allPlaylistControllersForGroupCmd ? "all" : ev.playlistControllerName));
                break;

            case MasterAudio.EventSoundFunctionType.CustomEventControl:
                sb.Append("\nCmd: " + ev.currentCustomEventCommand);
                sb.Append("\nName: " + ev.theCustomEventName);
                break;

            case MasterAudio.EventSoundFunctionType.GlobalControl:
                sb.Append("\nCmd: " + ev.currentGlobalCommand);
                break;
            case MasterAudio.EventSoundFunctionType.UnityMixerControl:
                sb.Append("\nCmd: " + ev.currentMixerCommand);
                switch (ev.currentMixerCommand) {
                    case MasterAudio.UnityMixerCommand.TransitionToSnapshot:
                        sb.Append("\nSnapshot: " + (ev.snapshotToTransitionTo == null ? "" : ev.snapshotToTransitionTo.name));
                        break;
                }
                break;
            case MasterAudio.EventSoundFunctionType.PersistentSettingsControl:
                sb.Append("\nCmd: " + ev.currentPersistentSettingsCommand);
                switch (ev.currentPersistentSettingsCommand) {
                    case MasterAudio.PersistentSettingsCommand.SetBusVolume:
                        sb.Append("\nBus: " + (ev.allSoundTypesForBusCmd ? "all" : ev.busName));
                        break;
                    case MasterAudio.PersistentSettingsCommand.SetGroupVolume:
                        sb.Append("\nGroup: " + (ev.allSoundTypesForGroupCmd ? "all" : ev.soundType));
                        break;
                }
                break;

            default:
                break;
        }
        return sb.ToString();
    }

    // returns nodes for all AudioEventGroups of the given component which are in use
    static IEnumerable<AudioEventGroupNode> GetNamedEventGroupNodes(EventSounds es) {
        return namedEventGroups
            .Select(
                item => new AudioEventGroupNode() {
                    type = item.type,
                    group = item.getGroup(es),
                    useGroup = item.useGroup(es),
                    name = item.name
                }
            );
    }

    static MasterAudioGroup GetGroupComponent(string groupName) {
        var groupTransform = MasterAudio.FindGroupTransform(groupName);
        if (groupTransform == null)
            return null;

        return groupTransform.GetComponent<MasterAudioGroup>();
    }

    // returns a label for a group link relation comming from the given group
    private string GetGroupLinkLabel(string groupName, bool isStartLinkedGroup) {
        var component = MasterAudio.Instance.Trans.GetChildTransform(groupName);
        if (component == null) {
            return string.Empty;
        }

        var group = component.GetComponent<MasterAudioGroup>();

        if (group == null) {
            return string.Empty;
        }

        if (isStartLinkedGroup) {
            if (group.childSoundGroups.Count <= 1 || group.linkedStartGroupSelectionType == MasterAudio.LinkedGroupSelectionType.All) {
                return "plays when linker group\nstarts playing";
            }

            return "may play when linker group\nstarts playing (random selection)";
        }

        if (group.endLinkedGroups.Count <= 1 || group.linkedStopGroupSelectionType == MasterAudio.LinkedGroupSelectionType.All) {
            return "plays when linker group\nfinishes playing";
        }

        return "may play when linker group\nfinishes playing (random selection)";
    }

    // returns the names of all groups that the given group links to
    static IEnumerable<string> GetStartLinkedGroups(string groupName) {
        var grpTrans = MasterAudio.Instance.Trans.GetChildTransform(groupName);
        if (grpTrans == null) {
            return Enumerable.Empty<string>();
        }

        var component = grpTrans.GetComponent<MasterAudioGroup>();
        if (component == null) {
            return Enumerable.Empty<string>();
        }

        return component.childSoundGroups;
    }

    static IEnumerable<string> GetStopLinkedGroups(string groupName) {
        var grpTrans = MasterAudio.Instance.Trans.GetChildTransform(groupName);
        if (grpTrans == null) {
            return Enumerable.Empty<string>();
        }

        var component = grpTrans.GetComponent<MasterAudioGroup>();
        if (component == null) {
            return Enumerable.Empty<string>();
        }

        return component.endLinkedGroups;
    }
}
