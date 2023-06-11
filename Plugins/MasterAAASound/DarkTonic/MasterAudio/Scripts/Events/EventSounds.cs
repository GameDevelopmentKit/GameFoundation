/*! \cond PRIVATE */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;
#if MULTIPLAYER_ENABLED
    using DarkTonic.MasterAudio.Multiplayer;
#endif

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [AudioScriptOrder(-30)]
    [AddComponentMenu("Dark Tonic/Master Audio/Event Sounds")]
    // ReSharper disable once CheckNamespace
    public class EventSounds : MonoBehaviour, ICustomEventReceiver {
        // ReSharper disable InconsistentNaming
        public MasterAudio.SoundSpawnLocationMode soundSpawnMode = MasterAudio.SoundSpawnLocationMode.AttachToCaller;
        public bool disableSounds = false;
        public bool showPoolManager = false;
        public bool showNGUI = false;
#if MULTIPLAYER_ENABLED
        public bool multiplayerBroadcast = false;
#endif        
        public AudioEvent eventToGizmo = null;
        public UnityUIVersion unityUIMode = UnityUIVersion.uGUI;

        public bool logMissingEvents = true;
        // ReSharper restore InconsistentNaming

        public enum UnityUIVersion {
            Legacy,
            uGUI
        }

#if MULTIPLAYER_ENABLED
        /*! \cond PRIVATE */
        public static readonly List<EventType> DisallowedMultBroadcastEventType = new List<EventType> {
            EventType.OnSpawned,
            EventType.OnDespawned
        };
        /*! \endcond */
#endif

        public enum EventType {
            OnStart,
            OnVisible,
            OnInvisible,
            OnCollision,
            OnTriggerEnter,
            OnTriggerExit,
            OnMouseEnter,
            OnMouseClick,
            OnSpawned,
            OnDespawned,
            OnEnable,
            OnDisable,
            OnCollision2D,
            OnTriggerEnter2D,
            OnTriggerExit2D,
            OnParticleCollision,
            UserDefinedEvent,
            OnCollisionExit,
            OnCollisionExit2D,
            OnMouseUp,
            OnMouseExit,
            OnMouseDrag,
            NGUIOnClick,
            NGUIMouseDown,
            NGUIMouseUp,
            NGUIMouseEnter,
            NGUIMouseExit,
            MechanimStateChanged,
            UnitySliderChanged,
            UnityButtonClicked,
            UnityPointerDown,
            UnityPointerUp,
            UnityPointerEnter,
            UnityPointerExit,
            UnityDrag,
            UnityDrop,
            UnityScroll,
            UnityUpdateSelected,
            UnitySelect,
            UnityDeselect,
            UnityMove,
            UnityInitializePotentialDrag,
            UnityBeginDrag,
            UnityEndDrag,
            UnitySubmit,
            UnityCancel,
            UnityToggle,
            OnTriggerStay,
            OnTriggerStay2D,
            CodeTriggeredEvent1,
            CodeTriggeredEvent2
        }

        public enum GlidePitchType {
            None,
            RaisePitch,
            LowerPitch
        }

        public enum VariationType {
            PlaySpecific,
            PlayRandom
        }

        public enum PreviousSoundStopMode {
            None,
            Stop,
            FadeOut
        }

        public enum RetriggerLimMode {
            None,
            FrameBased,
            TimeBased
        }

#if MULTIPLAYER_ENABLED
        public static List<MasterAudio.EventSoundFunctionType> CommandTypesExcludedFromMultiplayerBroadcast = new List<MasterAudio.EventSoundFunctionType> {
            MasterAudio.EventSoundFunctionType.PersistentSettingsControl
        };
#endif

        public static List<string> LayerTagFilterEvents = new List<string>()
        {
            EventType.OnCollision.ToString(),
            EventType.OnTriggerEnter.ToString(),
            EventType.OnTriggerExit.ToString(),
            EventType.OnCollision2D.ToString(),
            EventType.OnTriggerEnter2D.ToString(),
            EventType.OnTriggerExit2D.ToString(),
            EventType.OnParticleCollision.ToString(),
            EventType.OnCollisionExit.ToString(),
            EventType.OnCollisionExit2D.ToString()
        };

        public static List<MasterAudio.PlaylistCommand> PlaylistCommandsWithAll = new List<MasterAudio.PlaylistCommand>()
        {
            MasterAudio.PlaylistCommand.FadeToVolume,
            MasterAudio.PlaylistCommand.Pause,
            MasterAudio.PlaylistCommand.PlayNextSong,
            MasterAudio.PlaylistCommand.PlayRandomSong,
            MasterAudio.PlaylistCommand.Resume,
            MasterAudio.PlaylistCommand.Stop,
            MasterAudio.PlaylistCommand.Mute,
            MasterAudio.PlaylistCommand.Unmute,
            MasterAudio.PlaylistCommand.ToggleMute,
            MasterAudio.PlaylistCommand.Restart,
            MasterAudio.PlaylistCommand.StopLoopingCurrentSong,
            MasterAudio.PlaylistCommand.StopPlaylistAfterCurrentSong
        };

        // ReSharper disable InconsistentNaming
        public AudioEventGroup startSound;
        public AudioEventGroup visibleSound;
        public AudioEventGroup invisibleSound;
        public AudioEventGroup collisionSound;
        public AudioEventGroup collisionExitSound;
        public AudioEventGroup triggerSound;
        public AudioEventGroup triggerExitSound;
        public AudioEventGroup triggerStaySound;
        public AudioEventGroup mouseEnterSound;
        public AudioEventGroup mouseExitSound;
        public AudioEventGroup mouseClickSound;
        public AudioEventGroup mouseUpSound;
        public AudioEventGroup mouseDragSound;
        public AudioEventGroup spawnedSound;
        public AudioEventGroup despawnedSound;
        public AudioEventGroup enableSound;
        public AudioEventGroup disableSound;
        public AudioEventGroup collision2dSound;
        public AudioEventGroup collisionExit2dSound;
        public AudioEventGroup triggerEnter2dSound;
        public AudioEventGroup triggerStay2dSound;
        public AudioEventGroup triggerExit2dSound;
        public AudioEventGroup particleCollisionSound;
        public AudioEventGroup nguiOnClickSound;
        public AudioEventGroup nguiMouseDownSound;
        public AudioEventGroup nguiMouseUpSound;
        public AudioEventGroup nguiMouseEnterSound;
        public AudioEventGroup nguiMouseExitSound;
        public AudioEventGroup codeTriggeredEvent1Sound;
        public AudioEventGroup codeTriggeredEvent2Sound;

        public AudioEventGroup unitySliderChangedSound;
        public AudioEventGroup unityButtonClickedSound;
        public AudioEventGroup unityPointerDownSound;
        public AudioEventGroup unityDragSound;
        public AudioEventGroup unityPointerUpSound;
        public AudioEventGroup unityPointerEnterSound;
        public AudioEventGroup unityPointerExitSound;
        public AudioEventGroup unityDropSound;
        public AudioEventGroup unityScrollSound;
        public AudioEventGroup unityUpdateSelectedSound;
        public AudioEventGroup unitySelectSound;
        public AudioEventGroup unityDeselectSound;
        public AudioEventGroup unityMoveSound;
        public AudioEventGroup unityInitializePotentialDragSound;
        public AudioEventGroup unityBeginDragSound;
        public AudioEventGroup unityEndDragSound;
        public AudioEventGroup unitySubmitSound;
        public AudioEventGroup unityCancelSound;
        public AudioEventGroup unityToggleSound;

        public List<AudioEventGroup> userDefinedSounds = new List<AudioEventGroup>();
        public List<AudioEventGroup> mechanimStateChangedSounds = new List<AudioEventGroup>();

        public bool useStartSound = false;
        public bool useVisibleSound = false;
        public bool useInvisibleSound = false;
        public bool useCollisionSound = false;
        public bool useCollisionExitSound = false;
        public bool useTriggerEnterSound = false;
        public bool useTriggerExitSound = false;
        public bool useTriggerStaySound = false;
        public bool useMouseEnterSound = false;
        public bool useMouseExitSound = false;
        public bool useMouseClickSound = false;
        public bool useMouseUpSound = false;
        public bool useMouseDragSound = false;
        public bool useSpawnedSound = false;
        public bool useDespawnedSound = false;
        public bool useEnableSound = false;
        public bool useDisableSound = false;
        public bool useCollision2dSound = false;
        public bool useCollisionExit2dSound = false;
        public bool useTriggerEnter2dSound = false;
        public bool useTriggerStay2dSound = false;
        public bool useTriggerExit2dSound = false;
        public bool useParticleCollisionSound = false;

        public bool useNguiOnClickSound = false;
        public bool useNguiMouseDownSound = false;
        public bool useNguiMouseUpSound = false;
        public bool useNguiMouseEnterSound = false;
        public bool useNguiMouseExitSound = false;

        public bool useCodeTriggeredEvent1Sound = false;
        public bool useCodeTriggeredEvent2Sound = false;

        public bool useUnitySliderChangedSound = false;
        public bool useUnityButtonClickedSound = false;
        public bool useUnityPointerDownSound = false;
        public bool useUnityDragSound = false;
        public bool useUnityPointerUpSound = false;
        public bool useUnityPointerEnterSound = false;
        public bool useUnityPointerExitSound = false;
        public bool useUnityDropSound = false;
        public bool useUnityScrollSound = false;
        public bool useUnityUpdateSelectedSound = false;
        public bool useUnitySelectSound = false;
        public bool useUnityDeselectSound = false;
        public bool useUnityMoveSound = false;
        public bool useUnityInitializePotentialDragSound = false;
        public bool useUnityBeginDragSound = false;
        public bool useUnityEndDragSound = false;
        public bool useUnitySubmitSound = false;
        public bool useUnityCancelSound = false;
        public bool useUnityToggleSound = false;
        // ReSharper restore InconsistentNaming

        // ReSharper disable RedundantNameQualifier
        private UnityEngine.UI.Slider _slider;
        private UnityEngine.UI.Toggle _toggle;
        private UnityEngine.UI.Button _button;
        // ReSharper restore RedundantNameQualifier

        private bool _isVisible;
        private bool _needsCoroutine;
        private float? _triggerEnterTime;
        private float? _triggerEnter2dTime;

#if UNITY_IPHONE || UNITY_ANDROID
    // no mouse events!
#else
        private bool _mouseDragSoundPlayed;
        private PlaySoundResult _mouseDragResult;
#endif

        private Transform _trans;
        private readonly List<AudioEventGroup> _validMechanimStateChangedSounds = new List<AudioEventGroup>();
        private Animator _anim;
        private AudioEventGroup eventsToPlayDuringStart = null;
        private bool startHappened = false;

        // ReSharper disable once UnusedMember.Local
        private void Awake() {
            _trans = transform;
            _anim = GetComponent<Animator>();

            // ReSharper disable RedundantNameQualifier
            _slider = GetComponent<UnityEngine.UI.Slider>();
            _button = GetComponent<UnityEngine.UI.Button>();
            _toggle = GetComponent<UnityEngine.UI.Toggle>();
            // ReSharper restore RedundantNameQualifier

            if (IsSetToUGUI) {
                AddUGUIComponents();
            }

            SpawnedOrAwake();
        }

        protected virtual void SpawnedOrAwake() {
            _isVisible = false;

            // check if we need a coroutine for Mechanim stuff
            _validMechanimStateChangedSounds.Clear();
            _needsCoroutine = false;

            if (disableSounds || _anim == null) {
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < mechanimStateChangedSounds.Count; i++) {
                var state = mechanimStateChangedSounds[i];
                if (!state.mechanimEventActive || string.IsNullOrEmpty(state.mechanimStateName)) {
                    continue;
                }
                _needsCoroutine = true;
                _validMechanimStateChangedSounds.Add(state);
            }
        }

        private IEnumerator CoUpdate() {
            while (true) {
                yield return MasterAudio.EndOfFrameDelay;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _validMechanimStateChangedSounds.Count; i++) {
                    var chg = _validMechanimStateChangedSounds[i];

                    var matchState = _anim.GetCurrentAnimatorStateInfo(0).IsName(chg.mechanimStateName);
                    if (!matchState) {
                        chg.mechEventPlayedForState = false;
                        continue;
                    }

                    if (chg.mechEventPlayedForState) {
                        continue;
                    }

                    chg.mechEventPlayedForState = true;
                    PlaySounds(chg, EventType.MechanimStateChanged);
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        #region Core Monobehavior events

        // ReSharper disable once UnusedMember.Local
        private void Start() {
            CheckForIllegalCustomEvents();

            if (useStartSound) {
                PlaySounds(startSound, EventType.OnStart);
            }

            if (eventsToPlayDuringStart != null && !startHappened)
            {
                PlaySounds(eventsToPlayDuringStart, EventType.OnStart);
            }

            eventsToPlayDuringStart = null;
            startHappened = true;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnBecameVisible() {
            if (!useVisibleSound || _isVisible) {
                return;
            }
            _isVisible = true;
            PlaySounds(visibleSound, EventType.OnVisible);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnBecameInvisible() {
            if (!useInvisibleSound) {
                return;
            }
            _isVisible = false;
            PlaySounds(invisibleSound, EventType.OnInvisible);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEnable() {
            if (_slider != null) {
                _slider.onValueChanged.AddListener(SliderChanged);
                RestorePersistentSliders();
            }
            if (_button != null) {
                _button.onClick.AddListener(ButtonClicked);
            }
            if (_toggle != null) {
                _toggle.onValueChanged.AddListener(ToggleChanged);
            }

#if UNITY_IPHONE || UNITY_ANDROID
    // no mouse events!
#else
            _mouseDragResult = null;
#endif

            RegisterReceiver();


            // start coroutine if we're doing Mechanim monitoring.
            // ReSharper disable once InvertIf
            if (_needsCoroutine) {
                StopAllCoroutines();
                StartCoroutine(CoUpdate());
            }

            if (useEnableSound) {
                if (!startHappened) {
                    var hasPlaylistEvent = false;
                    for (var i = 0; i < enableSound.SoundEvents.Count; i++) {
                        if (enableSound.SoundEvents[i].currentSoundFunctionType == MasterAudio.EventSoundFunctionType.PlaylistControl) {
                            hasPlaylistEvent = true;
                            break;
                        }
                    }

                    if (hasPlaylistEvent) {
                        eventsToPlayDuringStart = enableSound;
                        return;
                    }
                }

                PlaySounds(enableSound, EventType.OnEnable);
            }
        }

        private void RestorePersistentSliders() {
            // restore sliders if they are used for "Persistent" settings.
            if (!useUnitySliderChangedSound) {
                return;
            }

            foreach (var action in unitySliderChangedSound.SoundEvents) {
                if (action.currentSoundFunctionType != MasterAudio.EventSoundFunctionType.PersistentSettingsControl) {
                    continue;
                }

                if (action.targetVolMode != AudioEvent.TargetVolumeMode.UseSliderValue) {
                    continue;
                }

                switch (action.currentPersistentSettingsCommand) {
                    case MasterAudio.PersistentSettingsCommand.SetMusicVolume:
                        var musicVol = PersistentAudioSettings.MusicVolume;
                        if (musicVol.HasValue) {
                            _slider.value = musicVol.Value;
                        }

                        break;
                    case MasterAudio.PersistentSettingsCommand.SetBusVolume:
                        if (action.allSoundTypesForBusCmd) {
                            continue;
                        }

                        var busVol = PersistentAudioSettings.GetBusVolume(action.busName);
                        if (busVol.HasValue) {
                            _slider.value = busVol.Value;
                        }

                        break;
                    case MasterAudio.PersistentSettingsCommand.SetMixerVolume:
                        var mixerVol = PersistentAudioSettings.MixerVolume;
                        if (mixerVol.HasValue) {
                            _slider.value = mixerVol.Value;
                        }

                        break;
                    case MasterAudio.PersistentSettingsCommand.SetGroupVolume:
                        if (action.allSoundTypesForGroupCmd) {
                            continue;
                        }

                        var grpVol = PersistentAudioSettings.GetGroupVolume(action.soundType);
                        if (grpVol.HasValue) {
                            _slider.value = grpVol.Value;
                        }

                        break;
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDisable() {
            if (MasterAudio.SafeInstance == null || MasterAudio.AppIsShuttingDown) {
                return;
            }

            if (_slider != null) {
                _slider.onValueChanged.RemoveListener(SliderChanged);
            }
            if (_button != null) {
                _button.onClick.RemoveListener(ButtonClicked);
            }
            if (_toggle != null) {
                _toggle.onValueChanged.RemoveListener(ToggleChanged);
            }

            UnregisterReceiver();

            if (!useDisableSound || MasterAudio.AppIsShuttingDown) {
                return;
            }

            PlaySounds(disableSound, EventType.OnDisable);
        }

        #endregion

        #region Collision and Trigger Events


#if PHY2D_ENABLED
        // ReSharper disable once UnusedMember.Local
        private void OnTriggerEnter2D(Collider2D other) {
            _triggerEnter2dTime = Time.realtimeSinceStartup;

            if (!useTriggerEnter2dSound) {
                return;
            }

            // check filters for matches if turned on
            if (triggerEnter2dSound.useLayerFilter &&
                !triggerEnter2dSound.matchingLayers.Contains(other.gameObject.layer)) {
                return;
            }

            if (triggerEnter2dSound.useTagFilter && !triggerEnter2dSound.matchingTags.Contains(other.gameObject.tag)) {
                return;
            }

            PlaySounds(triggerEnter2dSound, EventType.OnTriggerEnter2D);
        }

        private void OnTriggerStay2D(Collider2D other) {
            if (!useTriggerStay2dSound) {
                return;
            }

            float stayTime = 0f;

            if (!_triggerEnter2dTime.HasValue) {
                stayTime = 0f;
            } else {
                stayTime = Time.realtimeSinceStartup - _triggerEnter2dTime.Value;
            }

            if (triggerStay2dSound.triggerStayForTime > stayTime) {
                return;
            }

            var wasPlayed = PlaySounds(triggerStay2dSound, EventType.OnTriggerStay2D);
            if (wasPlayed && triggerStay2dSound.doesTriggerStayRepeat) {
                _triggerEnter2dTime = Time.realtimeSinceStartup; // restart the stayed timer.
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnTriggerExit2D(Collider2D other) {
            _triggerEnter2dTime = null;

            if (!useTriggerExit2dSound) {
                return;
            }

            // check filters for matches if turned on
            if (triggerExit2dSound.useLayerFilter && !triggerExit2dSound.matchingLayers.Contains(other.gameObject.layer)) {
                return;
            }

            if (triggerExit2dSound.useTagFilter && !triggerExit2dSound.matchingTags.Contains(other.gameObject.tag)) {
                return;
            }

            PlaySounds(triggerExit2dSound, EventType.OnTriggerExit2D);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnCollisionEnter2D(Collision2D collision) {
            if (!useCollision2dSound) {
                return;
            }

            // check filters for matches if turned on
            if (collision2dSound.useLayerFilter && !collision2dSound.matchingLayers.Contains(collision.gameObject.layer)) {
                return;
            }

            if (collision2dSound.useTagFilter && !collision2dSound.matchingTags.Contains(collision.gameObject.tag)) {
                return;
            }

            PlaySounds(collision2dSound, EventType.OnCollision2D);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnCollisionExit2D(Collision2D collision) {
            if (!useCollisionExit2dSound) {
                return;
            }

            // check filters for matches if turned on
            if (collisionExit2dSound.useLayerFilter &&
                !collisionExit2dSound.matchingLayers.Contains(collision.gameObject.layer)) {
                return;
            }

            if (collisionExit2dSound.useTagFilter &&
                !collisionExit2dSound.matchingTags.Contains(collision.gameObject.tag)) {
                return;
            }

            PlaySounds(collisionExit2dSound, EventType.OnCollisionExit2D);
        }
#endif

#if PHY3D_ENABLED
        // ReSharper disable once UnusedMember.Local
        private void OnCollisionEnter(Collision collision) {
            if (!useCollisionSound) {
                return;
            }

            // check filters for matches if turned on
            if (collisionSound.useLayerFilter && !collisionSound.matchingLayers.Contains(collision.gameObject.layer)) {
                return;
            }

            if (collisionSound.useTagFilter && !collisionSound.matchingTags.Contains(collision.gameObject.tag)) {
                return;
            }

            PlaySounds(collisionSound, EventType.OnCollision);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnCollisionExit(Collision collision) {
            if (!useCollisionExitSound) {
                return;
            }

            // check filters for matches if turned on
            if (collisionExitSound.useLayerFilter &&
                !collisionExitSound.matchingLayers.Contains(collision.gameObject.layer)) {
                return;
            }

            if (collisionExitSound.useTagFilter && !collisionExitSound.matchingTags.Contains(collision.gameObject.tag)) {
                return;
            }

            PlaySounds(collisionExitSound, EventType.OnCollisionExit);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnTriggerEnter(Collider other) {
            _triggerEnterTime = Time.realtimeSinceStartup;

            if (!useTriggerEnterSound) {
                return;
            }

            // check filters for matches if turned on
            if (triggerSound.useLayerFilter && !triggerSound.matchingLayers.Contains(other.gameObject.layer)) {
                return;
            }

            if (triggerSound.useTagFilter && !triggerSound.matchingTags.Contains(other.gameObject.tag)) {
                return;
            }

            PlaySounds(triggerSound, EventType.OnTriggerEnter);
        }

        private void OnTriggerStay(Collider other) { 
            if (!useTriggerStaySound) {
                return;
            }

            float stayTime = 0f;

            if (!_triggerEnterTime.HasValue) {
                stayTime = 0f;
            } else {
                stayTime = Time.realtimeSinceStartup - _triggerEnterTime.Value;
            }

            if (triggerStaySound.triggerStayForTime > stayTime) {
                return;
            }

            var wasPlayed = PlaySounds(triggerStaySound, EventType.OnTriggerStay);
            if (wasPlayed && triggerStaySound.doesTriggerStayRepeat) {
                _triggerEnterTime = Time.realtimeSinceStartup; // restart the stayed timer.
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnTriggerExit(Collider other) {
            _triggerEnterTime = null;

            if (!useTriggerExitSound) {
                return;
            }

            // check filters for matches if turned on
            if (triggerExitSound.useLayerFilter && !triggerExitSound.matchingLayers.Contains(other.gameObject.layer)) {
                return;
            }

            if (triggerExitSound.useTagFilter && !triggerExitSound.matchingTags.Contains(other.gameObject.tag)) {
                return;
            }

            PlaySounds(triggerExitSound, EventType.OnTriggerExit);
        }
#endif

        // ReSharper disable once UnusedMember.Local
        private void OnParticleCollision(GameObject other) {
            if (!useParticleCollisionSound) {
                return;
            }

            // check filters for matches if turned on
            if (particleCollisionSound.useLayerFilter &&
                !particleCollisionSound.matchingLayers.Contains(other.gameObject.layer)) {
                return;
            }

            if (particleCollisionSound.useTagFilter &&
                !particleCollisionSound.matchingTags.Contains(other.gameObject.tag)) {
                return;
            }

            PlaySounds(particleCollisionSound, EventType.OnParticleCollision);
        }

#endregion

#region UI Events
        public void OnPointerEnter(PointerEventData data) {
            if (IsSetToUGUI && useUnityPointerEnterSound) {
                PlaySounds(unityPointerEnterSound, EventType.UnityPointerEnter);
            }
        }

        public void OnPointerExit(PointerEventData data) {
            if (IsSetToUGUI && useUnityPointerExitSound) {
                PlaySounds(unityPointerExitSound, EventType.UnityPointerExit);
            }
        }

        public void OnPointerDown(PointerEventData data) {
            if (IsSetToUGUI && useUnityPointerDownSound) {
                PlaySounds(unityPointerDownSound, EventType.UnityPointerDown);
            }
        }

        public void OnPointerUp(PointerEventData data) {
            if (IsSetToUGUI && useUnityPointerUpSound) {
                PlaySounds(unityPointerUpSound, EventType.UnityPointerUp);
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnDrag(Vector2 delta) {
            // No Function. Here to prevent error spam.
        }

        public void OnDrag(PointerEventData data) {
            if (IsSetToUGUI && useUnityDragSound) {
                PlaySounds(unityDragSound, EventType.UnityDrag);
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnDrop(GameObject go) {
            // No Function. Here to prevent error spam.
        }

        public void OnDrop(PointerEventData data) {
            if (IsSetToUGUI && useUnityDropSound) {
                PlaySounds(unityDropSound, EventType.UnityDrop);
            }
        }

        public void OnScroll(PointerEventData data) {
            if (IsSetToUGUI && useUnityScrollSound) {
                PlaySounds(unityScrollSound, EventType.UnityScroll);
            }
        }

        public void OnUpdateSelected(BaseEventData data) {
            if (IsSetToUGUI && useUnityUpdateSelectedSound) {
                PlaySounds(unityUpdateSelectedSound, EventType.UnityUpdateSelected);
            }
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnSelect(bool isSelected) {
            // No Function. Here to prevent error spam from NGUI
        }

        public void OnSelect(BaseEventData data) {
            if (IsSetToUGUI && useUnitySelectSound) {
                PlaySounds(unitySelectSound, EventType.UnitySelect);
            }
        }

        public void OnDeselect(BaseEventData data) {
            if (IsSetToUGUI && useUnityDeselectSound) {
                PlaySounds(unityDeselectSound, EventType.UnityDeselect);
            }
        }

        public void OnMove(AxisEventData data) {
            if (IsSetToUGUI && useUnityMoveSound) {
                PlaySounds(unityMoveSound, EventType.UnityMove);
            }
        }

        public void OnInitializePotentialDrag(PointerEventData data) {
            if (IsSetToUGUI && useUnityInitializePotentialDragSound) {
                PlaySounds(unityInitializePotentialDragSound, EventType.UnityInitializePotentialDrag);
            }
        }

        public void OnBeginDrag(PointerEventData data) {
            if (IsSetToUGUI && useUnityBeginDragSound) {
                PlaySounds(unityBeginDragSound, EventType.UnityBeginDrag);
            }
        }

        public void OnEndDrag(PointerEventData data) {
            if (IsSetToUGUI && useUnityEndDragSound) {
                PlaySounds(unityEndDragSound, EventType.UnityEndDrag);
            }
        }

        public void OnSubmit(BaseEventData data) {
            if (IsSetToUGUI && useUnitySubmitSound) {
                PlaySounds(unitySubmitSound, EventType.UnitySubmit);
            }
        }

        public void OnCancel(BaseEventData data) {
            if (IsSetToUGUI && useUnityCancelSound) {
                PlaySounds(unityCancelSound, EventType.UnityCancel);
            }
        }

#endregion

#region Unity UI Events (4.6)
        private void SliderChanged(float newValue) {
            if (!useUnitySliderChangedSound) {
                return;
            }

            unitySliderChangedSound.sliderValue = newValue;
            PlaySounds(unitySliderChangedSound, EventType.UnitySliderChanged);
        }

        private void ToggleChanged(bool newValue) {
            if (!useUnityToggleSound) {
                return;
            }

            PlaySounds(unityToggleSound, EventType.UnityToggle);
        }

        private void ButtonClicked() {
            if (useUnityButtonClickedSound) {
                PlaySounds(unityButtonClickedSound, EventType.UnityButtonClicked);
            }
        }
#endregion

#region Unity GUI Mouse Events

        // ReSharper disable once UnusedMember.Local
        private bool IsSetToUGUI {
            get { return unityUIMode != UnityUIVersion.Legacy; }
        }

        private bool IsSetToLegacyUI {
            get { return unityUIMode == UnityUIVersion.Legacy; }
        }

#if UNITY_IPHONE || UNITY_ANDROID
    // no mouse events!
#else
        // ReSharper disable once UnusedMember.Local
        private void OnMouseEnter() {
            if (IsSetToLegacyUI && useMouseEnterSound) {
                PlaySounds(mouseEnterSound, EventType.OnMouseEnter);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnMouseExit() {
            if (IsSetToLegacyUI && useMouseExitSound) {
                PlaySounds(mouseExitSound, EventType.OnMouseExit);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnMouseDown() {
            if (IsSetToLegacyUI && useMouseClickSound) {
                PlaySounds(mouseClickSound, EventType.OnMouseClick);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnMouseUp() {
            if (IsSetToLegacyUI && useMouseUpSound) {
                PlaySounds(mouseUpSound, EventType.OnMouseUp);
            }

            if (useMouseDragSound) {
                switch (mouseUpSound.mouseDragStopMode) {
                    case PreviousSoundStopMode.Stop:
                        // stop the drag sound
                        if (_mouseDragResult != null &&
                            (_mouseDragResult.SoundPlayed || _mouseDragResult.SoundScheduled)) {
                            _mouseDragResult.ActingVariation.Stop(true);
                        }
                        break;
                    case PreviousSoundStopMode.FadeOut:
                        // stop the drag sound
                        if (_mouseDragResult != null &&
                            (_mouseDragResult.SoundPlayed || _mouseDragResult.SoundScheduled)) {
                            _mouseDragResult.ActingVariation.FadeToVolume(0f, mouseUpSound.mouseDragFadeOutTime);
                        }
                        break;
                }

                _mouseDragResult = null;
            }

            _mouseDragSoundPlayed = false; // can play drag sound again next time
        }

        // ReSharper disable once UnusedMember.Local
        private void OnMouseDrag() {
            if (!IsSetToLegacyUI || !useMouseDragSound) {
                return;
            }
            if (_mouseDragSoundPlayed) {
                return;
            }
            PlaySounds(mouseDragSound, EventType.OnMouseDrag);
            _mouseDragSoundPlayed = true;
        }
#endif

#endregion

#region NGUI Events

        // ReSharper disable once UnusedMember.Local
        private void OnPress(bool isDown) {
            if (!showNGUI) {
                return;
            }

            if (isDown) {
                if (useNguiMouseDownSound) {
                    PlaySounds(nguiMouseDownSound, EventType.NGUIMouseDown);
                }
            } else {
                if (useNguiMouseUpSound) {
                    PlaySounds(nguiMouseUpSound, EventType.NGUIMouseUp);
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnClick() {
            if (showNGUI && useNguiOnClickSound) {
                PlaySounds(nguiOnClickSound, EventType.NGUIOnClick);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnHover(bool isOver) {
            if (!showNGUI) {
                return;
            }

            if (isOver) {
                if (useNguiMouseEnterSound) {
                    PlaySounds(nguiMouseEnterSound, EventType.NGUIMouseEnter);
                }
            } else {
                if (useNguiMouseExitSound) {
                    PlaySounds(nguiMouseExitSound, EventType.NGUIMouseExit);
                }
            }
        }

#endregion

#region Pooling Events

        // ReSharper disable once UnusedMember.Local
        private void OnSpawned() {
            SpawnedOrAwake();

            if (showPoolManager && useSpawnedSound) {
                PlaySounds(spawnedSound, EventType.OnSpawned);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDespawned() {
            if (showPoolManager && useDespawnedSound) {
                PlaySounds(despawnedSound, EventType.OnDespawned);
            }
        }

        #endregion

        #region Code-Triggered Events 
        public void ActivateCodeTriggeredEvent1()
        {
            if (useCodeTriggeredEvent1Sound)
            {
                PlaySounds(codeTriggeredEvent1Sound, EventType.CodeTriggeredEvent1);
            }
        }

        public void ActivateCodeTriggeredEvent2()
        {
            if (useCodeTriggeredEvent2Sound)
            {
                PlaySounds(codeTriggeredEvent2Sound, EventType.CodeTriggeredEvent2);
            }
        }
        #endregion

        #region public methods
        /*! \cond PRIVATE */
        public void CalculateRadius(AudioEvent anEvent) {
            var aud = GetNamedOrFirstAudioSource(anEvent);

            if (aud == null) {
                anEvent.colliderMaxDistance = 0f;
                return;
            }

            anEvent.colliderMaxDistance = aud.maxDistance;
        }

        public AudioSource GetNamedOrFirstAudioSource(AudioEvent anEvent) {
            if (string.IsNullOrEmpty(anEvent.soundType)) {
                anEvent.colliderMaxDistance = 0;
                return null;
            }

            if (MasterAudio.SafeInstance == null) {
                anEvent.colliderMaxDistance = 0;
                return null;
            }

            var grp = MasterAudio.Instance.transform.Find(anEvent.soundType);
            if (grp == null) {
                anEvent.colliderMaxDistance = 0;
                return null;
            }

            Transform transVar = null;

            switch (anEvent.variationType) {
                case VariationType.PlayRandom:
                    transVar = grp.GetChild(0);
                    break;
                case VariationType.PlaySpecific:
                    transVar = grp.transform.Find(anEvent.variationName);
                    break;
            }

            if (transVar == null) {
                anEvent.colliderMaxDistance = 0;
                return null;
            }

            return transVar.GetComponent<AudioSource>();
        }

        public List<AudioSource> GetAllVariationAudioSources(AudioEvent anEvent) {
            if (string.IsNullOrEmpty(anEvent.soundType)) {
                anEvent.colliderMaxDistance = 0;
                return null;
            }

            if (MasterAudio.SafeInstance == null) {
                anEvent.colliderMaxDistance = 0;
                return null;
            }

            var grp = MasterAudio.Instance.transform.Find(anEvent.soundType);
            if (grp == null) {
                anEvent.colliderMaxDistance = 0;
                return null;
            }

            var audioSources = new List<AudioSource>(grp.childCount);

            for (var i = 0; i < grp.childCount; i++) {
                var a = grp.GetChild(i).GetComponent<AudioSource>();
                audioSources.Add(a);
            }

            return audioSources;
        }

        /*! \endcond */

        /*! \cond PRIVATE */
        public AudioEventGroup GetMechanimAudioEventGroup(string stateName) {
            for (var i = 0; i < _validMechanimStateChangedSounds.Count; i++)
            {
                var aSound = _validMechanimStateChangedSounds[i];
                if (aSound.mechanimStateName == stateName)
                {
                    return aSound;
                }
            }

            return null;
        }
        /*! \endcond */

        public bool PlaySounds(AudioEventGroup eventGrp, EventType eType) {
            if (!CheckForRetriggerLimit(eventGrp)) {
                return false;
            }

            if (MasterAudio.SafeInstance == null) {
                return false;
            }

            // set the last triggered time or frame
            switch (eventGrp.retriggerLimitMode) {
                case RetriggerLimMode.FrameBased:
                    eventGrp.triggeredLastFrame = AudioUtil.FrameCount;
                    break;
                case RetriggerLimMode.TimeBased:
                    eventGrp.triggeredLastTime = AudioUtil.Time;
                    break;
            }

            // Pre-warm event sounds!
            if (!MasterAudio.AppIsShuttingDown && MasterAudio.IsWarming) {
                var evt = new AudioEvent();
                PerformSingleAction(eventGrp, evt, eType);
                return true;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < eventGrp.SoundEvents.Count; i++) {
                PerformSingleAction(eventGrp, eventGrp.SoundEvents[i], eType);
            }

            return true;
        }
#endregion

#region Helpers
        // ReSharper disable once UnusedMember.Local
        private void OnDrawGizmos() {
            if (MasterAudio.SafeInstance == null || !MasterAudio.Instance.showRangeSoundGizmos || eventToGizmo == null) {
                return;
            }

            if (eventToGizmo.colliderMaxDistance == 0f) {
                return;
            }

            var gizmoColor = Color.green;
            if (MasterAudio.SafeInstance != null) {
                gizmoColor = MasterAudio.Instance.rangeGizmoColor;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, eventToGizmo.colliderMaxDistance);
        }

        void OnDrawGizmosSelected() {
            if (MasterAudio.SafeInstance == null || !MasterAudio.Instance.showSelectedRangeSoundGizmos || eventToGizmo == null) {
                return;
            }

            if (eventToGizmo.colliderMaxDistance == 0f) {
                return;
            }

            var gizmoColor = Color.green;
            if (MasterAudio.SafeInstance != null) {
                gizmoColor = MasterAudio.Instance.selectedRangeGizmoColor;
            }

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, eventToGizmo.colliderMaxDistance);
        }

        private static bool CheckForRetriggerLimit(AudioEventGroup grp) {
            // check for limiting restraints
            switch (grp.retriggerLimitMode) {
                case RetriggerLimMode.FrameBased:
                    if (grp.triggeredLastFrame > 0 && AudioUtil.FrameCount - grp.triggeredLastFrame < grp.limitPerXFrm) {
                        return false;
                    }
                    break;
                case RetriggerLimMode.TimeBased:
                    if (grp.triggeredLastTime > 0 && AudioUtil.Time - grp.triggeredLastTime < grp.limitPerXSec) {
                        return false;
                    }
                    break;
            }

            return true;
        }


#if MULTIPLAYER_ENABLED
        private bool AllPlayersShouldHearAction(AudioEventGroup grp, EventType eType) {
            if (DisallowedMultBroadcastEventType.Contains(eType)) {
                return false;
            }

            return (multiplayerBroadcast || grp.multiplayerBroadcast) && MasterAudioMultiplayerAdapter.CanSendRPCs;
        }
#endif

        // ReSharper disable once FunctionComplexityOverflow
        private void PerformSingleAction(AudioEventGroup grp, AudioEvent aEvent, EventType eType) {
            if (disableSounds || MasterAudio.AppIsShuttingDown || MasterAudio.SafeInstance == null) {
                return;
            }

            var useSliderValue = (eType == EventType.UnitySliderChanged && aEvent.targetVolMode == AudioEvent.TargetVolumeMode.UseSliderValue);
            var volume = aEvent.volume;
            var sType = aEvent.soundType;
            float? pitch = aEvent.pitch;
            if (!aEvent.useFixedPitch) {
                pitch = null;
            }

            PlaySoundResult soundPlayed = null;
#if MULTIPLAYER_ENABLED
            var willSendToAllPlayers = AllPlayersShouldHearAction(grp, eType);
#endif
            var soundSpawnModeToUse = soundSpawnMode;

            if (eType == EventType.OnDisable || eType == EventType.OnDespawned) {
                soundSpawnModeToUse = MasterAudio.SoundSpawnLocationMode.CallerLocation;
            }

            // these events need a PlaySoundResult, the rest do not. Save on allocation!
            var needsResult = eType == EventType.OnMouseDrag || aEvent.glidePitchType != GlidePitchType.None;

            switch (aEvent.currentSoundFunctionType) {
                case MasterAudio.EventSoundFunctionType.PlaySound:
                    string variationName = null;
                    if (aEvent.variationType == VariationType.PlaySpecific) {
                        variationName = aEvent.variationName;
                    }

                    if (useSliderValue) {
                        volume = grp.sliderValue;
                    }

                    switch (soundSpawnModeToUse) {
                        case MasterAudio.SoundSpawnLocationMode.CallerLocation:
                            if (needsResult) {
#if MULTIPLAYER_ENABLED                                
                                if (willSendToAllPlayers) {
                                    soundPlayed = MasterAudioMultiplayerAdapter.PlaySound3DAtTransform(sType, Trans, volume, pitch, aEvent.delaySound, variationName);
                                } else {
                                    soundPlayed = MasterAudio.PlaySound3DAtTransform(sType, Trans, volume, pitch,
                                        aEvent.delaySound, variationName);
                                }
#else
                                soundPlayed = MasterAudio.PlaySound3DAtTransform(sType, _trans, volume, pitch,
                                    aEvent.delaySound, variationName);
#endif
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(sType, Trans, volume, pitch, aEvent.delaySound, variationName);
                                } else {
                                    MasterAudio.PlaySound3DAtTransformAndForget(sType, Trans, volume, pitch, aEvent.delaySound, variationName);
                                }
#else
                                MasterAudio.PlaySound3DAtTransformAndForget(sType, _trans, volume, pitch,
                                    aEvent.delaySound, variationName);
#endif
                            }
                            break;
                        case MasterAudio.SoundSpawnLocationMode.AttachToCaller:
                            if (needsResult) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    soundPlayed = MasterAudioMultiplayerAdapter.PlaySound3DFollowTransform(sType, Trans, volume, pitch,
                                        aEvent.delaySound, variationName);
                                } else {
                                    soundPlayed = MasterAudio.PlaySound3DFollowTransform(sType, Trans, volume, pitch,
                                        aEvent.delaySound, variationName);
                                }
#else
                                soundPlayed = MasterAudio.PlaySound3DFollowTransform(sType, _trans, volume, pitch,
                                    aEvent.delaySound, variationName);
#endif
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(sType, Trans, volume, pitch,
                                        aEvent.delaySound, variationName);
                                } else {
                                    MasterAudio.PlaySound3DFollowTransformAndForget(sType, Trans, volume, pitch,
                                        aEvent.delaySound, variationName);
                                }
#else
                                MasterAudio.PlaySound3DFollowTransformAndForget(sType, _trans, volume, pitch,
                                    aEvent.delaySound, variationName);
#endif
                            }
                            break;
                        case MasterAudio.SoundSpawnLocationMode.MasterAudioLocation:
                            if (needsResult) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    soundPlayed = MasterAudioMultiplayerAdapter.PlaySound(Trans, sType, volume, pitch, aEvent.delaySound,
                                        variationName);
                                } else {
                                    soundPlayed = MasterAudio.PlaySound(sType, volume, pitch, aEvent.delaySound,
                                        variationName);
                                }
#else
                                soundPlayed = MasterAudio.PlaySound(sType, volume, pitch, aEvent.delaySound,
                                    variationName);
#endif
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PlaySoundAndForget(Trans, sType, volume, pitch, aEvent.delaySound, variationName);
                                } else {
                                    MasterAudio.PlaySoundAndForget(sType, volume, pitch, aEvent.delaySound, variationName);
                                }
#else
                                MasterAudio.PlaySoundAndForget(sType, volume, pitch, aEvent.delaySound,
                                    variationName);
#endif
                            }
                            break;
                    }

                    if (soundPlayed != null && soundPlayed.ActingVariation != null && aEvent.glidePitchType != GlidePitchType.None) {
                        switch (aEvent.glidePitchType) {
                            case GlidePitchType.RaisePitch:
                                if (!string.IsNullOrEmpty(aEvent.theCustomEventName)) {
                                    soundPlayed.ActingVariation.GlideByPitch(aEvent.targetGlidePitch, aEvent.pitchGlideTime, 
                                        delegate
                                        {
                                            MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                        });
                                } else {
                                    soundPlayed.ActingVariation.GlideByPitch(aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                }
                                break;
                            case GlidePitchType.LowerPitch:
                                if (!string.IsNullOrEmpty(aEvent.theCustomEventName)) {
                                    soundPlayed.ActingVariation.GlideByPitch(aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                        delegate
                                        {
                                            MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                        });
                                } else {
                                    soundPlayed.ActingVariation.GlideByPitch(-aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                }
                                break;
                        }
                    }

#if UNITY_IPHONE || UNITY_ANDROID
    // no mouse events!
#else
                    if (eType == EventType.OnMouseDrag) {
                        _mouseDragResult = soundPlayed;
                    }
#endif
                    break;
                case MasterAudio.EventSoundFunctionType.PlaylistControl:
                    soundPlayed = new PlaySoundResult() {
                        ActingVariation = null,
                        SoundPlayed = true,
                        SoundScheduled = false
                    };

                    if (string.IsNullOrEmpty(aEvent.playlistControllerName)) {
                        aEvent.playlistControllerName = MasterAudio.OnlyPlaylistControllerName;
                    }

                    switch (aEvent.currentPlaylistCommand) {
                        case MasterAudio.PlaylistCommand.None:
                            soundPlayed.SoundPlayed = false;
                            break;
                        case MasterAudio.PlaylistCommand.Restart:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.RestartAllPlaylists(Trans);
                                } else {
                                    MasterAudio.RestartAllPlaylists();
                                }
#else
                                MasterAudio.RestartAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.RestartPlaylist(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.RestartPlaylist(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.RestartPlaylist(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.Start:
                            if (aEvent.playlistControllerName == MasterAudio.NoGroupName ||
                                aEvent.playlistName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StartPlaylist(Trans, aEvent.playlistControllerName, aEvent.playlistName);
                                } else {
                                    MasterAudio.StartPlaylist(aEvent.playlistControllerName, aEvent.playlistName);
                                }
#else
                                MasterAudio.StartPlaylist(aEvent.playlistControllerName, aEvent.playlistName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.ChangePlaylist:
                            if (string.IsNullOrEmpty(aEvent.playlistName)) {
                                Debug.Log("You have not specified a Playlist name for Event Sounds on '" +
                                            _trans.name + "'.");
                                soundPlayed.SoundPlayed = false;
                            } else {
                                if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                    // don't play	
                                } else {
#if MULTIPLAYER_ENABLED
                                    if (willSendToAllPlayers) {
                                        MasterAudioMultiplayerAdapter.ChangePlaylistByName(Trans, aEvent.playlistControllerName, aEvent.playlistName, aEvent.startPlaylist);
                                    } else {
                                        MasterAudio.ChangePlaylistByName(aEvent.playlistControllerName,
                                            aEvent.playlistName, aEvent.startPlaylist);
                                    }
#else
                                    MasterAudio.ChangePlaylistByName(aEvent.playlistControllerName,
                                        aEvent.playlistName, aEvent.startPlaylist);
#endif
                                }
                            }

                            break;
                        case MasterAudio.PlaylistCommand.StopLoopingCurrentSong:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopLoopingAllCurrentSongs(Trans);
                                } else {
                                    MasterAudio.StopLoopingAllCurrentSongs();
                                }
#else
                                MasterAudio.StopLoopingAllCurrentSongs();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopLoopingCurrentSong(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.StopLoopingCurrentSong(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.StopLoopingCurrentSong(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.StopPlaylistAfterCurrentSong:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopAllPlaylistsAfterCurrentSongs(Trans);
                                } else {
                                    MasterAudio.StopAllPlaylistsAfterCurrentSongs();
                                }
#else
                                MasterAudio.StopAllPlaylistsAfterCurrentSongs();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopPlaylistAfterCurrentSong(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.StopPlaylistAfterCurrentSong(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.StopPlaylistAfterCurrentSong(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.FadeToVolume:
                            var targetVol = useSliderValue ? grp.sliderValue : aEvent.fadeVolume;

                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadeAllPlaylistsToVolume(Trans, targetVol, aEvent.fadeTime);
                                } else {
                                    MasterAudio.FadeAllPlaylistsToVolume(targetVol, aEvent.fadeTime);
                                }
#else
                                MasterAudio.FadeAllPlaylistsToVolume(targetVol, aEvent.fadeTime);
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadePlaylistToVolume(Trans, aEvent.playlistControllerName, targetVol,
                                        aEvent.fadeTime);
                                } else {
                                    MasterAudio.FadePlaylistToVolume(aEvent.playlistControllerName, targetVol,
                                        aEvent.fadeTime);
                                }
#else
                                MasterAudio.FadePlaylistToVolume(aEvent.playlistControllerName, targetVol,
                                    aEvent.fadeTime);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.Mute:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.MuteAllPlaylists(Trans);
                                } else {
                                    MasterAudio.MuteAllPlaylists();
                                }
#else
                                MasterAudio.MuteAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.MutePlaylist(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.MutePlaylist(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.MutePlaylist(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.Unmute:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnmuteAllPlaylists(Trans);
                                } else {
                                    MasterAudio.UnmuteAllPlaylists();
                                }
#else
                                MasterAudio.UnmuteAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnmutePlaylist(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.UnmutePlaylist(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.UnmutePlaylist(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.ToggleMute:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.ToggleMuteAllPlaylists(Trans);
                                } else {
                                    MasterAudio.ToggleMuteAllPlaylists();
                                }
#else
                                MasterAudio.ToggleMuteAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.ToggleMutePlaylist(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.ToggleMutePlaylist(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.ToggleMutePlaylist(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.PlaySong:
                            if (string.IsNullOrEmpty(aEvent.clipName)) {
                                Debug.Log("You have not specified a song name for Event Sounds on '" + _trans.name +
                                            "'.");
                                soundPlayed.SoundPlayed = false;
                            } else {
                                if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                    // don't play	
                                } else {
#if MULTIPLAYER_ENABLED
                                    if (willSendToAllPlayers) {
                                        if (!MasterAudioMultiplayerAdapter.TriggerPlaylistClip(Trans, aEvent.playlistControllerName, aEvent.clipName)) {
                                            soundPlayed.SoundPlayed = false;
                                        }
                                    } else {
                                        if (!MasterAudio.TriggerPlaylistClip(aEvent.playlistControllerName, aEvent.clipName)) {
                                            soundPlayed.SoundPlayed = false;
                                        }
                                    }
#else
                                    if (!MasterAudio.TriggerPlaylistClip(aEvent.playlistControllerName, aEvent.clipName)) {
                                        soundPlayed.SoundPlayed = false;
                                    }
#endif
                                }
                            }

                            break;
                        case MasterAudio.PlaylistCommand.AddSongToQueue:
                            soundPlayed.SoundPlayed = false;

                            if (string.IsNullOrEmpty(aEvent.clipName)) {
                                Debug.Log("You have not specified a song name for Event Sounds on '" + _trans.name + "'.");
                            } else {
                                if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                    // don't play	
                                } else {
#if MULTIPLAYER_ENABLED
                                    if (willSendToAllPlayers) {
                                        MasterAudioMultiplayerAdapter.QueuePlaylistClip(Trans, aEvent.playlistControllerName, aEvent.clipName);
                                    } else {
                                        MasterAudio.QueuePlaylistClip(aEvent.playlistControllerName, aEvent.clipName);
                                    }
#else
                                    MasterAudio.QueuePlaylistClip(aEvent.playlistControllerName, aEvent.clipName);
#endif
                                    soundPlayed.SoundPlayed = true;
                                }
                            }

                            break;
                        case MasterAudio.PlaylistCommand.PlayRandomSong:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.TriggerRandomClipAllPlaylists(Trans);
                                } else {
                                    MasterAudio.TriggerRandomClipAllPlaylists();
                                }
#else
                                MasterAudio.TriggerRandomClipAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.TriggerRandomPlaylistClip(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.TriggerRandomPlaylistClip(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.TriggerRandomPlaylistClip(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.PlayNextSong:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.TriggerNextClipAllPlaylists(Trans);
                                } else {
                                    MasterAudio.TriggerNextClipAllPlaylists();
                                }
#else
                                MasterAudio.TriggerNextClipAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.TriggerNextPlaylistClip(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.TriggerNextPlaylistClip(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.TriggerNextPlaylistClip(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.Pause:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PauseAllPlaylists(Trans);
                                } else {
                                    MasterAudio.PauseAllPlaylists();
                                }
#else
                                MasterAudio.PauseAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PausePlaylist(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.PausePlaylist(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.PausePlaylist(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.Stop:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopAllPlaylists(Trans);
                                } else {
                                    MasterAudio.StopAllPlaylists();
                                }
#else
                                MasterAudio.StopAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopPlaylist(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.StopPlaylist(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.StopPlaylist(aEvent.playlistControllerName);
#endif
                            }
                            break;
                        case MasterAudio.PlaylistCommand.Resume:
                            if (aEvent.allPlaylistControllersForGroupCmd) {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnpauseAllPlaylists(Trans);
                                } else {
                                    MasterAudio.UnpauseAllPlaylists();
                                }
#else
                                MasterAudio.UnpauseAllPlaylists();
#endif
                            } else if (aEvent.playlistControllerName == MasterAudio.NoGroupName) {
                                // don't play	
                            } else {
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnpausePlaylist(Trans, aEvent.playlistControllerName);
                                } else {
                                    MasterAudio.UnpausePlaylist(aEvent.playlistControllerName);
                                }
#else
                                MasterAudio.UnpausePlaylist(aEvent.playlistControllerName);
#endif
                            }
                            break;
                    }
                    break;
                case MasterAudio.EventSoundFunctionType.GroupControl:
                    soundPlayed = new PlaySoundResult() {
                        ActingVariation = null,
                        SoundPlayed = true,
                        SoundScheduled = false
                    };

                    var soundTypeOverride = string.Empty;

                    var soundTypesForCmd = new List<string>();
                    if (!aEvent.allSoundTypesForGroupCmd || MasterAudio.GroupCommandsWithNoAllGroupSelector.Contains(aEvent.currentSoundGroupCommand)) {
                        soundTypesForCmd.Add(aEvent.soundType);
                    } else {
                        soundTypesForCmd.AddRange(MasterAudio.RuntimeSoundGroupNames);
#if MULTIPLAYER_ENABLED
                        if (willSendToAllPlayers) {
                            soundTypeOverride = MasterAudio.AllBusesName;
                        }
#endif
                    }

                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < soundTypesForCmd.Count; i++) {
                        var soundType = soundTypesForCmd[i];
                        if (!string.IsNullOrEmpty(soundTypeOverride)) { // for multiplayer "do all"
                            soundType = soundTypeOverride;
                        }

                        switch (aEvent.currentSoundGroupCommand) {
                            case MasterAudio.SoundGroupCommand.None:
                                soundPlayed.SoundPlayed = false;
                                break;
                            case MasterAudio.SoundGroupCommand.ToggleSoundGroup:
                                if (MasterAudio.IsSoundGroupPlaying(soundType)) {
#if MULTIPLAYER_ENABLED
                                    if (willSendToAllPlayers) {
                                        MasterAudioMultiplayerAdapter.FadeOutAllOfSound(Trans, soundType, aEvent.fadeTime);
                                    } else {
                                        MasterAudio.FadeOutAllOfSound(soundType, aEvent.fadeTime);
                                    }
#else
                                    MasterAudio.FadeOutAllOfSound(soundType, aEvent.fadeTime);
#endif
                                } else {
                                    switch (soundSpawnModeToUse) {
                                        case MasterAudio.SoundSpawnLocationMode.CallerLocation:
#if MULTIPLAYER_ENABLED
                                            if (willSendToAllPlayers) {
                                                MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            } else {
                                                MasterAudio.PlaySound3DAtTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            }
#else
                                            MasterAudio.PlaySound3DAtTransformAndForget(soundType, _trans, volume, pitch, aEvent.delaySound);
#endif
                                            break;
                                        case MasterAudio.SoundSpawnLocationMode.AttachToCaller:
#if MULTIPLAYER_ENABLED
                                            if (willSendToAllPlayers) {
                                                MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            } else {
                                                MasterAudio.PlaySound3DFollowTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            }
#else
                                            MasterAudio.PlaySound3DFollowTransformAndForget(soundType, _trans, volume, pitch, aEvent.delaySound);
#endif
                                            break;
                                        case MasterAudio.SoundSpawnLocationMode.MasterAudioLocation:
#if MULTIPLAYER_ENABLED
                                            if (willSendToAllPlayers) {
                                                MasterAudioMultiplayerAdapter.PlaySoundAndForget(Trans, soundType, volume, pitch, aEvent.delaySound);
                                            } else {
                                                MasterAudio.PlaySoundAndForget(soundType, volume, pitch, aEvent.delaySound);
                                            }
#else
                                            MasterAudio.PlaySoundAndForget(soundType, volume, pitch, aEvent.delaySound);
#endif
                                            break;
                                    }
                                }
                                break;
                            case MasterAudio.SoundGroupCommand.ToggleSoundGroupOfTransform:
                                if (MasterAudio.IsTransformPlayingSoundGroup(soundType, _trans))                                 {
#if MULTIPLAYER_ENABLED
                                    if (willSendToAllPlayers) {
                                        MasterAudioMultiplayerAdapter.FadeOutSoundGroupOfTransform(Trans, soundType, aEvent.fadeTime);
                                    } else {
                                        MasterAudio.FadeOutSoundGroupOfTransform(Trans, soundType, aEvent.fadeTime);
                                    }
#else
                                    MasterAudio.FadeOutSoundGroupOfTransform(_trans, soundType, aEvent.fadeTime);
#endif
                                } else {
                                    switch (soundSpawnModeToUse) {
                                        case MasterAudio.SoundSpawnLocationMode.CallerLocation:
#if MULTIPLAYER_ENABLED
                                            if (willSendToAllPlayers) {
                                                MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            } else {
                                                MasterAudio.PlaySound3DAtTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            }
#else
                                            MasterAudio.PlaySound3DAtTransformAndForget(soundType, _trans, volume, pitch, aEvent.delaySound);
#endif
                                            break;
                                        case MasterAudio.SoundSpawnLocationMode.AttachToCaller:
#if MULTIPLAYER_ENABLED
                                            if (willSendToAllPlayers) {
                                                MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            } else {
                                                MasterAudio.PlaySound3DFollowTransformAndForget(soundType, Trans, volume, pitch, aEvent.delaySound);
                                            }
#else
                                            MasterAudio.PlaySound3DFollowTransformAndForget(soundType, _trans, volume, pitch, aEvent.delaySound);
#endif
                                            break;
                                        case MasterAudio.SoundSpawnLocationMode.MasterAudioLocation:
#if MULTIPLAYER_ENABLED
                                            if (willSendToAllPlayers) {
                                                MasterAudioMultiplayerAdapter.PlaySoundAndForget(Trans, soundType, volume, pitch, aEvent.delaySound);
                                            } else {
                                                MasterAudio.PlaySoundAndForget(soundType, volume, pitch, aEvent.delaySound);
                                            }
#else
                                            MasterAudio.PlaySoundAndForget(soundType, volume, pitch, aEvent.delaySound);
#endif
                                            break;
                                    }
                                }
                                break;
                            case MasterAudio.SoundGroupCommand.RefillSoundGroupPool:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.RefillSoundGroupPool(Trans, soundType);
                                } else {
                                    MasterAudio.RefillSoundGroupPool(soundType);
                                }
#else
                                MasterAudio.RefillSoundGroupPool(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.FadeToVolume:
                                var targetVol = useSliderValue ? grp.sliderValue : aEvent.fadeVolume;
                                var hasDelegate = aEvent.fireCustomEventAfterFade && !string.IsNullOrEmpty(aEvent.theCustomEventName);
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    if (hasDelegate) {
                                        MasterAudioMultiplayerAdapter.FadeSoundGroupToVolume(Trans, soundType, targetVol, aEvent.fadeTime,
                                            delegate
                                            {
                                                MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                            },
                                            aEvent.stopAfterFade,
                                            aEvent.restoreVolumeAfterFade);
                                    } else {
                                        MasterAudioMultiplayerAdapter.FadeSoundGroupToVolume(Trans, soundType, targetVol, aEvent.fadeTime, null, aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                    }
                                } else {
                                    if (hasDelegate) {
                                        MasterAudio.FadeSoundGroupToVolume(soundType, targetVol, aEvent.fadeTime,
                                            delegate
                                            {
                                                MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                            },
                                            aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                    } else {
                                        MasterAudio.FadeSoundGroupToVolume(soundType, targetVol, aEvent.fadeTime, null, aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                    }
                                }
#else
                                if (hasDelegate) {
                                    MasterAudio.FadeSoundGroupToVolume(soundType, targetVol, aEvent.fadeTime,
                                        delegate
                                        {
                                            MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                        },
                                        aEvent.stopAfterFade, 
                                        aEvent.restoreVolumeAfterFade);
                                } else {
                                    MasterAudio.FadeSoundGroupToVolume(soundType, targetVol, aEvent.fadeTime, null, aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                }
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.FadeOutAllOfSound:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadeOutAllOfSound(Trans, soundType, aEvent.fadeTime);
                                } else {
                                    MasterAudio.FadeOutAllOfSound(soundType, aEvent.fadeTime);
                                }
#else
                                MasterAudio.FadeOutAllOfSound(soundType, aEvent.fadeTime);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.FadeSoundGroupOfTransformToVolume:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadeSoundGroupOfTransformToVolume(Trans, soundType, aEvent.fadeTime, aEvent.fadeVolume);
                                } else {
                                    MasterAudio.FadeSoundGroupOfTransformToVolume(Trans, soundType, aEvent.fadeTime, aEvent.fadeVolume);
                                }
#else
                                MasterAudio.FadeSoundGroupOfTransformToVolume(Trans, soundType, aEvent.fadeTime, aEvent.fadeVolume);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.Mute:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.MuteGroup(Trans, soundType);
                                } else {
                                    MasterAudio.MuteGroup(soundType);
                                }
#else
                                MasterAudio.MuteGroup(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.Pause:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PauseSoundGroup(Trans, soundType);
                                } else {
                                    MasterAudio.PauseSoundGroup(soundType);
                                }
#else
                                MasterAudio.PauseSoundGroup(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.Solo:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.SoloGroup(Trans, soundType);
                                } else {
                                    MasterAudio.SoloGroup(soundType);
                                }
#else
                                MasterAudio.SoloGroup(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.StopAllOfSound:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopAllOfSound(Trans, soundType);
                                } else {
                                    MasterAudio.StopAllOfSound(soundType);
                                }
#else
                                MasterAudio.StopAllOfSound(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.Unmute:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnmuteGroup(Trans, soundType);
                                } else {
                                    MasterAudio.UnmuteGroup(soundType);
                                }
#else
                                MasterAudio.UnmuteGroup(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.Unpause:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnpauseSoundGroup(Trans, soundType);
                                } else {
                                    MasterAudio.UnpauseSoundGroup(soundType);
                                }
#else
                                MasterAudio.UnpauseSoundGroup(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.Unsolo:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnsoloGroup(Trans, soundType);
                                } else {
                                    MasterAudio.UnsoloGroup(soundType);
                                }
#else
                                MasterAudio.UnsoloGroup(soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.StopAllSoundsOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopAllSoundsOfTransform(Trans);
                                } else {
                                    MasterAudio.StopAllSoundsOfTransform(Trans);
                                }
#else
                                MasterAudio.StopAllSoundsOfTransform(_trans);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.StopSoundGroupOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopSoundGroupOfTransform(Trans, soundType);
                                } else {
                                    MasterAudio.StopSoundGroupOfTransform(Trans, soundType);
                                }
#else
                                MasterAudio.StopSoundGroupOfTransform(_trans, soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.PauseAllSoundsOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PauseAllSoundsOfTransform(Trans);
                                } else {
                                    MasterAudio.PauseAllSoundsOfTransform(Trans);
                                }
#else
                                MasterAudio.PauseAllSoundsOfTransform(_trans);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.PauseSoundGroupOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PauseSoundGroupOfTransform(Trans, soundType);
                                } else {
                                    MasterAudio.PauseSoundGroupOfTransform(Trans, soundType);
                                }
#else
                                MasterAudio.PauseSoundGroupOfTransform(_trans, soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.UnpauseAllSoundsOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnpauseAllSoundsOfTransform(Trans);
                                } else {
                                    MasterAudio.UnpauseAllSoundsOfTransform(Trans);
                                }
#else
                                MasterAudio.UnpauseAllSoundsOfTransform(_trans);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.UnpauseSoundGroupOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnpauseSoundGroupOfTransform(Trans, soundType);
                                } else {
                                    MasterAudio.UnpauseSoundGroupOfTransform(Trans, soundType);
                                }
#else
                                MasterAudio.UnpauseSoundGroupOfTransform(_trans, soundType);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.FadeOutSoundGroupOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadeOutSoundGroupOfTransform(Trans, soundType, aEvent.fadeTime);
                                } else {
                                    MasterAudio.FadeOutSoundGroupOfTransform(Trans, soundType, aEvent.fadeTime);
                                }
#else
                                MasterAudio.FadeOutSoundGroupOfTransform(_trans, soundType, aEvent.fadeTime);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.FadeOutAllSoundsOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadeOutAllSoundsOfTransform(Trans, aEvent.fadeTime);
                                } else {
                                    MasterAudio.FadeOutAllSoundsOfTransform(Trans, aEvent.fadeTime);
                                }
#else
                                MasterAudio.FadeOutAllSoundsOfTransform(_trans, aEvent.fadeTime);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.RouteToBus:
                                var busName = aEvent.busName;
                                if (busName == MasterAudio.NoGroupName) {
                                    busName = null;
                                }

#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.RouteGroupToBus(Trans, soundType, busName);
                                } else {
                                    MasterAudio.RouteGroupToBus(soundType, busName);
                                }
#else
                                MasterAudio.RouteGroupToBus(soundType, busName);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.GlideByPitch:
                                var hasActionDelegate = !string.IsNullOrEmpty(aEvent.theCustomEventName);

                                switch (aEvent.glidePitchType) {
                                    case GlidePitchType.RaisePitch:
#if MULTIPLAYER_ENABLED
                                        if (willSendToAllPlayers) {
                                            if (hasActionDelegate) {
                                                MasterAudioMultiplayerAdapter.GlideSoundGroupByPitch(Trans, soundType, aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                    delegate
                                                    {
                                                        MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                    });
                                            } else {
                                                MasterAudioMultiplayerAdapter.GlideSoundGroupByPitch(Trans, soundType, aEvent.targetGlidePitch, aEvent.pitchGlideTime, null);
                                            }
                                        } else {
                                            if (hasActionDelegate) {
                                                MasterAudio.GlideSoundGroupByPitch(soundType, aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                    delegate
                                                    {
                                                        MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                    });
                                            } else {
                                                MasterAudio.GlideSoundGroupByPitch(soundType, aEvent.targetGlidePitch, aEvent.pitchGlideTime, null);
                                            }
                                        }
#else
                                        if (hasActionDelegate) {
                                            MasterAudio.GlideSoundGroupByPitch(soundType, aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                delegate
                                                {
                                                    MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                                });
                                        } else {
                                            MasterAudio.GlideSoundGroupByPitch(soundType, aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                        }
#endif
                                        break;
                                    case GlidePitchType.LowerPitch:
#if MULTIPLAYER_ENABLED
                                        if (willSendToAllPlayers) {
                                            if (hasActionDelegate) {
                                                MasterAudioMultiplayerAdapter.GlideSoundGroupByPitch(Trans, soundType, -aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                    delegate
                                                    {
                                                        MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                    });
                                            } else {
                                                MasterAudioMultiplayerAdapter.GlideSoundGroupByPitch(Trans, soundType, -aEvent.targetGlidePitch, aEvent.pitchGlideTime, null);
                                            }
                                        } else {
                                            if (hasActionDelegate) {
                                                MasterAudio.GlideSoundGroupByPitch(soundType, -aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                    delegate
                                                    {
                                                        MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                    });
                                            } else {
                                                MasterAudio.GlideSoundGroupByPitch(soundType, -aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                            }
                                        }
#else
                                        if (hasActionDelegate) {
                                            MasterAudio.GlideSoundGroupByPitch(soundType, -aEvent.targetGlidePitch, aEvent.pitchGlideTime, 
                                                delegate
                                                {
                                                    MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                                });
                                        } else {
                                            MasterAudio.GlideSoundGroupByPitch(soundType, -aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                        }
#endif
                                        break;
                                }

                                break;
                            case MasterAudio.SoundGroupCommand.StopOldSoundGroupVoices:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopOldSoundGroupVoices(Trans, soundType, aEvent.minAge);
                                } else {
                                    MasterAudio.StopOldSoundGroupVoices(soundType, aEvent.minAge);
                                }
#else
                                MasterAudio.StopOldSoundGroupVoices(soundType, aEvent.minAge);
#endif
                                break;
                            case MasterAudio.SoundGroupCommand.FadeOutOldSoundGroupVoices:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadeOutOldSoundGroupVoices(Trans, soundType, aEvent.minAge, aEvent.fadeTime);
                                } else {
                                    MasterAudio.FadeOutOldSoundGroupVoices(soundType, aEvent.minAge, aEvent.fadeTime);
                                }
#else
                                MasterAudio.FadeOutOldSoundGroupVoices(soundType, aEvent.minAge, aEvent.fadeTime);
#endif
                                break;
                        }
                         
#if MULTIPLAYER_ENABLED
                        if (willSendToAllPlayers) {
                            // don't continue loop, we've done everything already.
                            break;
                        }
#endif
                    }

                    break;
                case MasterAudio.EventSoundFunctionType.BusControl:
                    soundPlayed = new PlaySoundResult() {
                        ActingVariation = null,
                        SoundPlayed = true,
                        SoundScheduled = false
                    };

                    var busNameOverride = string.Empty;

                    var busesForCmd = new List<string>();
                    if (!aEvent.allSoundTypesForBusCmd) {
                        busesForCmd.Add(aEvent.busName);
                    } else {
                        busesForCmd.AddRange(MasterAudio.RuntimeBusNames);
#if MULTIPLAYER_ENABLED
                        if (willSendToAllPlayers) {
                            busNameOverride = MasterAudio.AllBusesName;
                        }
#endif
                    }

                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < busesForCmd.Count; i++) {
                        var busName = busesForCmd[i];
                        if (!string.IsNullOrEmpty(busNameOverride)) { // for multiplayer "do all"
                            busName = busNameOverride;
                        }

                        switch (aEvent.currentBusCommand) {
                            case MasterAudio.BusCommand.None:
                                soundPlayed.SoundPlayed = false;
                                break;
                            case MasterAudio.BusCommand.FadeToVolume:
                                var targetVol = useSliderValue ? grp.sliderValue : aEvent.fadeVolume;
                                var hasCustomEventAfter = aEvent.fireCustomEventAfterFade && !string.IsNullOrEmpty(aEvent.theCustomEventName);

#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    if (hasCustomEventAfter) {
                                        MasterAudioMultiplayerAdapter.FadeBusToVolume(Trans, busName, targetVol, aEvent.fadeTime,
                                             delegate
                                             {
                                                 MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                             },
                                             aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                    } else {
                                        MasterAudioMultiplayerAdapter.FadeBusToVolume(Trans, busName, targetVol, aEvent.fadeTime, null, aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                    }
                                } else {
                                    if (hasCustomEventAfter) {
                                        MasterAudio.FadeBusToVolume(busName, targetVol, aEvent.fadeTime,
                                             delegate
                                             {
                                                 MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                             },
                                             aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                    } else {
                                        MasterAudio.FadeBusToVolume(busName, targetVol, aEvent.fadeTime, null, aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                    }
                                }
#else
                                if (hasCustomEventAfter) {
                                    MasterAudio.FadeBusToVolume(busName, targetVol, aEvent.fadeTime, 
                                        delegate
                                        {
                                            MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                        }, aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                } else {
                                    MasterAudio.FadeBusToVolume(busName, targetVol, aEvent.fadeTime, null, aEvent.stopAfterFade, aEvent.restoreVolumeAfterFade);
                                }
#endif
                                break;
                            case MasterAudio.BusCommand.GlideByPitch:
                                var willFireCustomEventAfter = !string.IsNullOrEmpty(aEvent.theCustomEventName);

                                switch (aEvent.glidePitchType) {
                                    case GlidePitchType.RaisePitch:
#if MULTIPLAYER_ENABLED
                                        if (willSendToAllPlayers) {
                                            if (willFireCustomEventAfter) {
                                                MasterAudioMultiplayerAdapter.GlideBusByPitch(Trans, busName, aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                     delegate
                                                     {
                                                         MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                     });
                                            } else {
                                                MasterAudioMultiplayerAdapter.GlideBusByPitch(Trans, busName, aEvent.targetGlidePitch, aEvent.pitchGlideTime, null);
                                            }
                                        } else {
                                            if (willFireCustomEventAfter) {
                                                MasterAudio.GlideBusByPitch(busName, aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                     delegate
                                                     {
                                                         MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                     });
                                            } else {
                                                MasterAudio.GlideBusByPitch(busName, aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                            }
                                        }
#else
                                        if (willFireCustomEventAfter) {
                                            MasterAudio.GlideBusByPitch(busName, aEvent.targetGlidePitch, aEvent.pitchGlideTime, 
                                                delegate
                                                {
                                                    MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                                });
                                        } else {
                                            MasterAudio.GlideBusByPitch(busName, aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                        }
#endif
                                        break;
                                    case GlidePitchType.LowerPitch:
#if MULTIPLAYER_ENABLED
                                        if (willSendToAllPlayers) {
                                            if (willFireCustomEventAfter) {
                                                MasterAudioMultiplayerAdapter.GlideBusByPitch(Trans, busName, -aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                     delegate
                                                     {
                                                         MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                     });
                                            } else {
                                                MasterAudioMultiplayerAdapter.GlideBusByPitch(Trans, busName, -aEvent.targetGlidePitch, aEvent.pitchGlideTime, null);
                                            }
                                        } else {
                                            if (willFireCustomEventAfter) {
                                                MasterAudio.GlideBusByPitch(busName, -aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                     delegate
                                                     {
                                                         MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans);
                                                     });
                                            } else {
                                                MasterAudio.GlideBusByPitch(busName, -aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                            }
                                        }
#else
                                        if (willFireCustomEventAfter) {
                                            MasterAudio.GlideBusByPitch(busName, -aEvent.targetGlidePitch, aEvent.pitchGlideTime,
                                                delegate
                                                {
                                                    MasterAudio.FireCustomEvent(aEvent.theCustomEventName, _trans);
                                                });
                                        } else {
                                            MasterAudio.GlideBusByPitch(busName, -aEvent.targetGlidePitch, aEvent.pitchGlideTime);
                                        }
#endif
                                        break;
                                }
                                break;
                            case MasterAudio.BusCommand.Pause:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PauseBus(Trans, busName);
                                } else {
                                    MasterAudio.PauseBus(busName);
                                }
#else
                                MasterAudio.PauseBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.Stop:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopBus(Trans, busName);
                                } else {
                                    MasterAudio.StopBus(busName);
                                }
#else
                                MasterAudio.StopBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.Unpause:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnpauseBus(Trans, busName);
                                } else {
                                    MasterAudio.UnpauseBus(busName);
                                }
#else
                                MasterAudio.UnpauseBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.Mute:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.MuteBus(Trans, busName);
                                } else {
                                    MasterAudio.MuteBus(busName);
                                }
#else
                                MasterAudio.MuteBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.Unmute:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnmuteBus(Trans, busName);
                                } else {
                                    MasterAudio.UnmuteBus(busName);
                                }
#else
                                MasterAudio.UnmuteBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.ToggleMute:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.ToggleMuteBus(Trans, busName);
                                } else {
                                    MasterAudio.ToggleMuteBus(busName);
                                }
#else
                                MasterAudio.ToggleMuteBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.Solo:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.SoloBus(Trans, busName);
                                } else {
                                    MasterAudio.SoloBus(busName);
                                }
#else
                                MasterAudio.SoloBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.Unsolo:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnsoloBus(Trans, busName);
                                } else {
                                    MasterAudio.UnsoloBus(busName);
                                }
#else
                                MasterAudio.UnsoloBus(busName);
#endif
                                break;
                            case MasterAudio.BusCommand.ChangePitch:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.ChangeBusPitch(Trans, busName, aEvent.pitch);
                                } else {
                                    MasterAudio.ChangeBusPitch(busName, aEvent.pitch);
                                }
#else
                                MasterAudio.ChangeBusPitch(busName, aEvent.pitch);
#endif
                                break;
							case MasterAudio.BusCommand.PauseBusOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.PauseBusOfTransform(Trans, busName);
                                } else {
                                    MasterAudio.PauseBusOfTransform(Trans, aEvent.busName);
                                }
#else
                                MasterAudio.PauseBusOfTransform(_trans, aEvent.busName);
#endif
								break;
							case MasterAudio.BusCommand.UnpauseBusOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.UnpauseBusOfTransform(Trans, busName);
                                } else {
                                    MasterAudio.UnpauseBusOfTransform(Trans, aEvent.busName);
                                }
#else
                                MasterAudio.UnpauseBusOfTransform(_trans, aEvent.busName);
#endif
								break;
							case MasterAudio.BusCommand.StopBusOfTransform:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopBusOfTransform(Trans, busName);
                                } else {
                                    MasterAudio.StopBusOfTransform(Trans, aEvent.busName);
                                }
#else
                                MasterAudio.StopBusOfTransform(_trans, aEvent.busName);
#endif
								break;
                            case MasterAudio.BusCommand.StopOldBusVoices:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.StopOldBusVoices(Trans, busName, aEvent.minAge);
                                } else {
                                    MasterAudio.StopOldBusVoices(busName, aEvent.minAge);
                                }
#else
                                MasterAudio.StopOldBusVoices(busName, aEvent.minAge);
#endif
                                break;
                            case MasterAudio.BusCommand.FadeOutOldBusVoices:
#if MULTIPLAYER_ENABLED
                                if (willSendToAllPlayers) {
                                    MasterAudioMultiplayerAdapter.FadeOutOldBusVoices(Trans, busName, aEvent.minAge, aEvent.fadeTime);
                                } else {
                                    MasterAudio.FadeOutOldBusVoices(busName, aEvent.minAge, aEvent.fadeTime);
                                }
#else
                                MasterAudio.FadeOutOldBusVoices(busName, aEvent.minAge, aEvent.fadeTime);
#endif
                                break;
                            }

#if MULTIPLAYER_ENABLED
                        if (willSendToAllPlayers) {
                            // don't continue loop, we've done everything already.
                            break;
                        }
#endif
                    }

                    break;
                case MasterAudio.EventSoundFunctionType.CustomEventControl:
                    if (eType == EventType.UserDefinedEvent) {
                        Debug.LogError("Custom Event Receivers cannot fire events. Occured in Transform '" + name + "'.");
                        break;
                    }
                    switch (aEvent.currentCustomEventCommand) {
                        case MasterAudio.CustomEventCommand.FireEvent:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.FireCustomEvent(aEvent.theCustomEventName, Trans, aEvent.logDupeEventFiring);
                            } else {
                                MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans, aEvent.logDupeEventFiring);
                            }
#else
                            MasterAudio.FireCustomEvent(aEvent.theCustomEventName, Trans, aEvent.logDupeEventFiring);
#endif
                            break;
                    }
                    break;
                case MasterAudio.EventSoundFunctionType.GlobalControl:
                    switch (aEvent.currentGlobalCommand) {
                        case MasterAudio.GlobalCommand.PauseAudioListener:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.AudioListenerPause(Trans);
                            } else {
                                AudioListener.pause = true;
                            }
#else
                            AudioListener.pause = true;
#endif
                            break;
                        case MasterAudio.GlobalCommand.UnpauseAudioListener:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.AudioListenerUnpause(Trans);
                            } else {
                                AudioListener.pause = false;
                            }
#else
                            AudioListener.pause = false;
#endif
                            break;
                        case MasterAudio.GlobalCommand.SetMasterMixerVolume:
                            var targetVol = useSliderValue ? grp.sliderValue : aEvent.volume;
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.SetMasterMixerVolume(Trans, targetVol);
                            } else {
                                MasterAudio.MasterVolumeLevel = targetVol;
                            }
#else
                            MasterAudio.MasterVolumeLevel = targetVol;
#endif
                            break;
                        case MasterAudio.GlobalCommand.SetMasterPlaylistVolume:
                            var tgtVol = useSliderValue ? grp.sliderValue : aEvent.volume;
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.SetPlaylistMasterVolume(Trans, tgtVol);
                            } else {
                                MasterAudio.PlaylistMasterVolume = tgtVol;
                            }
#else
                            MasterAudio.PlaylistMasterVolume = tgtVol;
#endif
                            break;
                        case MasterAudio.GlobalCommand.PauseMixer:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.PauseMixer(Trans);
                            } else
                            {
                                MasterAudio.PauseMixer();
                            }
#else
                            MasterAudio.PauseMixer();
#endif
                            break;
                        case MasterAudio.GlobalCommand.UnpauseMixer:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.UnpauseMixer(Trans);
                            } else {
                                MasterAudio.UnpauseMixer();
                            }
#else
                            MasterAudio.UnpauseMixer();
#endif
                            break;
                        case MasterAudio.GlobalCommand.StopMixer:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.StopMixer(Trans);
                            } else {
                                MasterAudio.StopMixer();
                            }
#else
                            MasterAudio.StopMixer();
#endif
                            break;
                        case MasterAudio.GlobalCommand.MuteEverything:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.MuteEverything(Trans);
                            } else {
                                MasterAudio.MuteEverything();
                            }
#else
                            MasterAudio.MuteEverything();
#endif
                            break;
                        case MasterAudio.GlobalCommand.UnmuteEverything:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.UnmuteEverything(Trans);
                            } else {
                                MasterAudio.UnmuteEverything();
                            }
#else
                            MasterAudio.UnmuteEverything();
#endif
                            break;
                        case MasterAudio.GlobalCommand.PauseEverything:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.PauseEverything(Trans);
                            } else {
                                MasterAudio.PauseEverything();
                            }
#else
                            MasterAudio.PauseEverything();
#endif
                            break;
                        case MasterAudio.GlobalCommand.UnpauseEverything:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.UnpauseEverything(Trans);
                            } else {
                                MasterAudio.UnpauseEverything();
                            }
#else
                            MasterAudio.UnpauseEverything();
#endif
                            break;
                        case MasterAudio.GlobalCommand.StopEverything:
#if MULTIPLAYER_ENABLED
                            if (willSendToAllPlayers) {
                                MasterAudioMultiplayerAdapter.StopEverything(Trans);
                            } else {
                                MasterAudio.StopEverything();
                            }
#else
                            MasterAudio.StopEverything();
#endif
                            break;
                    }
                    break;
                case MasterAudio.EventSoundFunctionType.UnityMixerControl:
                    switch (aEvent.currentMixerCommand) {
                        case MasterAudio.UnityMixerCommand.TransitionToSnapshot:
                            var snapshot = aEvent.snapshotToTransitionTo;
                            if (snapshot != null)
                            {
                                // if we add more mixer functionality, move this next line somewhere DRY.
                                snapshot.audioMixer.updateMode = MasterAudio.Instance.mixerUpdateMode;
                                snapshot.audioMixer.TransitionToSnapshots(
                                    new[] { snapshot },
                                    new[] { 1f },
                                    aEvent.snapshotTransitionTime);
                            }
                            break;
                        case MasterAudio.UnityMixerCommand.TransitionToSnapshotBlend:
                            var snapshots = new List<AudioMixerSnapshot>();
                            var weights = new List<float>();
                            AudioMixer theMixer = null;

                            // ReSharper disable once ForCanBeConvertedToForeach
                            for (var i = 0; i < aEvent.snapshotsToBlend.Count; i++) {
                                var aSnap = aEvent.snapshotsToBlend[i];
                                if (aSnap.snapshot == null) {
                                    continue;
                                }

                                if (theMixer == null) {
                                    theMixer = aSnap.snapshot.audioMixer;
                                } else if (theMixer != aSnap.snapshot.audioMixer) {
                                    Debug.LogError("Snapshot '" + aSnap.snapshot.name + "' isn't in the same Audio Mixer as the previous snapshot in EventSounds on GameObject '" + name + "'. Please make sure all the Snapshots to blend are on the same mixer.");
                                    break;
                                }

                                snapshots.Add(aSnap.snapshot);
                                weights.Add(aSnap.weight);
                            }

                            if (snapshots.Count > 0) {
                                theMixer.updateMode = MasterAudio.Instance.mixerUpdateMode;
                                // ReSharper disable once PossibleNullReferenceException
                                theMixer.TransitionToSnapshots(snapshots.ToArray(), weights.ToArray(), aEvent.snapshotTransitionTime);
                            }

                            break;
                    }
                    break;
                case MasterAudio.EventSoundFunctionType.PersistentSettingsControl:
                    switch (aEvent.currentPersistentSettingsCommand) {
                        case MasterAudio.PersistentSettingsCommand.SetBusVolume:
                            var busesForCommand = new List<string>();
                            if (!aEvent.allSoundTypesForBusCmd) {
                                busesForCommand.Add(aEvent.busName);
                            } else {
                                busesForCommand.AddRange(MasterAudio.RuntimeBusNames);
                            }

                            // ReSharper disable once ForCanBeConvertedToForeach
                            for (var i = 0; i < busesForCommand.Count; i++) {
                                var aBusName = busesForCommand[i];
                                var tgtVol = useSliderValue ? grp.sliderValue : aEvent.volume;
                                PersistentAudioSettings.SetBusVolume(aBusName, tgtVol);
                            }
                            break;
                        case MasterAudio.PersistentSettingsCommand.SetGroupVolume:
                            var groupsForCommand = new List<string>();
                            if (!aEvent.allSoundTypesForGroupCmd) {
                                groupsForCommand.Add(aEvent.soundType);
                            } else {
                                groupsForCommand.AddRange(MasterAudio.RuntimeSoundGroupNames);
                            }

                            // ReSharper disable once ForCanBeConvertedToForeach
                            for (var i = 0; i < groupsForCommand.Count; i++) {
                                var aGroupName = groupsForCommand[i];
                                var tgtVol = useSliderValue ? grp.sliderValue : aEvent.volume;
                                PersistentAudioSettings.SetGroupVolume(aGroupName, tgtVol);
                            }
                            break;
                        case MasterAudio.PersistentSettingsCommand.SetMixerVolume:
                            var targetVol = useSliderValue ? grp.sliderValue : aEvent.volume;
                            PersistentAudioSettings.MixerVolume = targetVol;
                            break;
                        case MasterAudio.PersistentSettingsCommand.SetMusicVolume:
                            var targVol = useSliderValue ? grp.sliderValue : aEvent.volume;
                            PersistentAudioSettings.MusicVolume = targVol;
                            break;
                        case MasterAudio.PersistentSettingsCommand.MixerMuteToggle:
                            if (PersistentAudioSettings.MixerMuted.HasValue) {
                                PersistentAudioSettings.MixerMuted = !PersistentAudioSettings.MixerMuted.Value;
                            } else {
                                PersistentAudioSettings.MixerMuted = true;
                            }
                            break;
                        case MasterAudio.PersistentSettingsCommand.MusicMuteToggle:
                            if (PersistentAudioSettings.MusicMuted.HasValue) {
                                PersistentAudioSettings.MusicMuted = !PersistentAudioSettings.MusicMuted.Value;
                            } else {
                                PersistentAudioSettings.MusicMuted = true;
                            }
                            break;
                    }
                    break;
            }
        }

        private void LogIfCustomEventMissing(AudioEventGroup eventGroup) {
            if (!logMissingEvents) {
                return;
            }

            if (eventGroup.isCustomEvent) {
                if (!eventGroup.customSoundActive || string.IsNullOrEmpty(eventGroup.customEventName)) {
                    return;
                }
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < eventGroup.SoundEvents.Count; i++) {
                var aEvent = eventGroup.SoundEvents[i];

                if (aEvent.currentSoundFunctionType != MasterAudio.EventSoundFunctionType.CustomEventControl) {
                    continue;
                }

                var customEventName = aEvent.theCustomEventName;
                if (!MasterAudio.CustomEventExists(customEventName)) {
                    MasterAudio.LogWarning("Transform '" + name + "' is set up to receive or fire Custom Event '" +
                                           customEventName + "', which does not exist in Master Audio.");
                }
            }
        }

#endregion

#region ICustomEventReceiver methods

        public void CheckForIllegalCustomEvents() {
            if (useStartSound) {
                LogIfCustomEventMissing(startSound);
            }
            if (useVisibleSound) {
                LogIfCustomEventMissing(visibleSound);
            }
            if (useInvisibleSound) {
                LogIfCustomEventMissing(invisibleSound);
            }
            if (useCollisionSound) {
                LogIfCustomEventMissing(collisionSound);
            }
            if (useCollisionExitSound) {
                LogIfCustomEventMissing(collisionExitSound);
            }
            if (useTriggerEnterSound) {
                LogIfCustomEventMissing(triggerSound);
            }
            if (useTriggerExitSound) {
                LogIfCustomEventMissing(triggerExitSound);
            }
            if (useMouseEnterSound) {
                LogIfCustomEventMissing(mouseEnterSound);
            }
            if (useMouseExitSound) {
                LogIfCustomEventMissing(mouseExitSound);
            }
            if (useMouseClickSound) {
                LogIfCustomEventMissing(mouseClickSound);
            }
            if (useMouseDragSound) {
                LogIfCustomEventMissing(mouseDragSound);
            }
            if (useMouseUpSound) {
                LogIfCustomEventMissing(mouseUpSound);
            }
            if (useNguiMouseDownSound) {
                LogIfCustomEventMissing(nguiMouseDownSound);
            }
            if (useNguiMouseUpSound) {
                LogIfCustomEventMissing(nguiMouseUpSound);
            }
            if (useNguiOnClickSound) {
                LogIfCustomEventMissing(nguiOnClickSound);
            }
            if (useNguiMouseEnterSound) {
                LogIfCustomEventMissing(nguiMouseEnterSound);
            }
            if (useNguiMouseExitSound) {
                LogIfCustomEventMissing(nguiMouseExitSound);
            }
            if (useSpawnedSound) {
                LogIfCustomEventMissing(spawnedSound);
            }
            if (useDespawnedSound) {
                LogIfCustomEventMissing(despawnedSound);
            }
            if (useEnableSound) {
                LogIfCustomEventMissing(enableSound);
            }
            if (useDisableSound) {
                LogIfCustomEventMissing(disableSound);
            }
            if (useCollision2dSound) {
                LogIfCustomEventMissing(collision2dSound);
            }
            if (useCollisionExit2dSound) {
                LogIfCustomEventMissing(collisionExit2dSound);
            }
            if (useTriggerEnter2dSound) {
                LogIfCustomEventMissing(triggerEnter2dSound);
            }
            if (useTriggerExit2dSound) {
                LogIfCustomEventMissing(triggerExit2dSound);
            }
            if (useParticleCollisionSound) {
                LogIfCustomEventMissing(particleCollisionSound);
            }

            if (useUnitySliderChangedSound) {
                LogIfCustomEventMissing(unitySliderChangedSound);
            }
            if (useUnityButtonClickedSound) {
                LogIfCustomEventMissing(unityButtonClickedSound);
            }
            if (useUnityPointerDownSound) {
                LogIfCustomEventMissing(unityPointerDownSound);
            }
            if (useUnityDragSound) {
                LogIfCustomEventMissing(unityDragSound);
            }
            if (useUnityDropSound) {
                LogIfCustomEventMissing(unityDropSound);
            }
            if (useUnityPointerUpSound) {
                LogIfCustomEventMissing(unityPointerUpSound);
            }
            if (useUnityPointerEnterSound) {
                LogIfCustomEventMissing(unityPointerEnterSound);
            }
            if (useUnityPointerExitSound) {
                LogIfCustomEventMissing(unityPointerExitSound);
            }
            if (useUnityScrollSound) {
                LogIfCustomEventMissing(unityScrollSound);
            }
            if (useUnityUpdateSelectedSound) {
                LogIfCustomEventMissing(unityUpdateSelectedSound);
            }
            if (useUnitySelectSound) {
                LogIfCustomEventMissing(unitySelectSound);
            }
            if (useUnityDeselectSound) {
                LogIfCustomEventMissing(unityDeselectSound);
            }
            if (useUnityMoveSound) {
                LogIfCustomEventMissing(unityMoveSound);
            }
            if (useUnityInitializePotentialDragSound) {
                LogIfCustomEventMissing(unityInitializePotentialDragSound);
            }
            if (useUnityBeginDragSound) {
                LogIfCustomEventMissing(unityBeginDragSound);
            }
            if (useUnityEndDragSound) {
                LogIfCustomEventMissing(unityEndDragSound);
            }
            if (useUnitySubmitSound) {
                LogIfCustomEventMissing(unitySubmitSound);
            }
            if (useUnityCancelSound) {
                LogIfCustomEventMissing(unityCancelSound);
            }
            if (useCodeTriggeredEvent1Sound)
            {
                LogIfCustomEventMissing(codeTriggeredEvent1Sound);
            }
            if (useCodeTriggeredEvent2Sound)
            {
                LogIfCustomEventMissing(codeTriggeredEvent2Sound);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < userDefinedSounds.Count; i++) {
                var custEvent = userDefinedSounds[i];

                LogIfCustomEventMissing(custEvent);
            }
        }

        public void ReceiveEvent(string customEventName, Vector3 originPoint) {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < userDefinedSounds.Count; i++) {
                var userDefGroup = userDefinedSounds[i];

                if (!userDefGroup.customSoundActive || string.IsNullOrEmpty(userDefGroup.customEventName)) {
                    continue;
                }

                if (!userDefGroup.customEventName.Equals(customEventName)) {
                    continue;
                }

                PlaySounds(userDefGroup, EventType.UserDefinedEvent);
            }
        }

        public bool SubscribesToEvent(string customEventName) {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < userDefinedSounds.Count; i++) {
                var customGrp = userDefinedSounds[i];

                if (customGrp.customSoundActive && !string.IsNullOrEmpty(customGrp.customEventName) &&
                    customGrp.customEventName.Equals(customEventName)) {
                    return true;
                }
            }

            return false;
        }

        public void RegisterReceiver() {
            if (userDefinedSounds.Count > 0) {
                MasterAudio.AddCustomEventReceiver(this, _trans);
            }
        }

        public void UnregisterReceiver() {
            if (userDefinedSounds.Count > 0) {
                MasterAudio.RemoveCustomEventReceiver(this);
            }
        }

        public IList<AudioEventGroup> GetAllEvents() {
#if UNITY_WSA
				return userDefinedSounds; // can't compile the one below.
#else
            return userDefinedSounds.AsReadOnly();
#endif
        }

#endregion


        // UGUI events are handled by separate components so that only
        // the events we care about are actually trapped and handled
        private void AddUGUIComponents() {
            AddUGUIHandler<EventSoundsPointerEnterHandler>(useUnityPointerEnterSound);
            AddUGUIHandler<EventSoundsPointerExitHandler>(useUnityPointerExitSound);
            AddUGUIHandler<EventSoundsPointerDownHandler>(useUnityPointerDownSound);
            AddUGUIHandler<EventSoundsPointerUpHandler>(useUnityPointerUpSound);
            AddUGUIHandler<EventSoundsDragHandler>(useUnityDragSound);
            AddUGUIHandler<EventSoundsDropHandler>(useUnityDropSound);
            AddUGUIHandler<EventSoundsScrollHandler>(useUnityScrollSound);
            AddUGUIHandler<EventSoundsUpdateSelectedHandler>(useUnityUpdateSelectedSound);
            AddUGUIHandler<EventSoundsSelectHandler>(useUnitySelectSound);
            AddUGUIHandler<EventSoundsDeselectHandler>(useUnityDeselectSound);
            AddUGUIHandler<EventSoundsMoveHandler>(useUnityMoveSound);
            AddUGUIHandler<EventSoundsInitializePotentialDragHandler>(useUnityInitializePotentialDragSound);
            AddUGUIHandler<EventSoundsBeginDragHandler>(useUnityBeginDragSound);
            AddUGUIHandler<EventSoundsEndDragHandler>(useUnityEndDragSound);
            AddUGUIHandler<EventSoundsSubmitHandler>(useUnitySubmitSound);
            AddUGUIHandler<EventSoundsCancelHandler>(useUnityCancelSound);
        }

        private void AddUGUIHandler<T>(bool useSound) where T : EventSoundsUGUIHandler {
            if (!useSound) {
                return;
            }

            var handler = gameObject.AddComponent<T>();
            handler.eventSounds = this;
        }

        private Transform Trans {
            get {
                if (_trans != null) {
                    return _trans;
                }
                _trans = transform;

                return _trans;
            }
        }
    }

    /*! \cond PRIVATE */
#region UGUI methods
    // UGUI event handler components
    public class EventSoundsUGUIHandler : MonoBehaviour {
        // ReSharper disable once InconsistentNaming
        public EventSounds eventSounds { get; set; }
    }

    public class EventSoundsPointerEnterHandler : EventSoundsUGUIHandler, IPointerEnterHandler {
        public void OnPointerEnter(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnPointerEnter(data);
            }
        }
    }

    public class EventSoundsPointerExitHandler : EventSoundsUGUIHandler, IPointerExitHandler {
        public void OnPointerExit(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnPointerExit(data);
            }
        }
    }

    public class EventSoundsPointerDownHandler : EventSoundsUGUIHandler, IPointerDownHandler {
        public void OnPointerDown(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnPointerDown(data);
            }
        }
    }

    public class EventSoundsPointerUpHandler : EventSoundsUGUIHandler, IPointerUpHandler {
        public void OnPointerUp(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnPointerUp(data);
            }
        }
    }

    public class EventSoundsDragHandler : EventSoundsUGUIHandler, IDragHandler {
        public void OnDrag(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnDrag(data);
            }
        }
    }

    public class EventSoundsDropHandler : EventSoundsUGUIHandler, IDropHandler {
        public void OnDrop(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnDrop(data);
            }
        }
    }

    public class EventSoundsScrollHandler : EventSoundsUGUIHandler, IScrollHandler {
        public void OnScroll(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnScroll(data);
            }
        }
    }

    public class EventSoundsUpdateSelectedHandler : EventSoundsUGUIHandler, IUpdateSelectedHandler {
        public void OnUpdateSelected(BaseEventData data) {
            if (eventSounds != null) {
                eventSounds.OnUpdateSelected(data);
            }
        }
    }

    public class EventSoundsSelectHandler : EventSoundsUGUIHandler, ISelectHandler {
        public void OnSelect(BaseEventData data) {
            if (eventSounds != null) {
                eventSounds.OnSelect(data);
            }
        }
    }

    public class EventSoundsDeselectHandler : EventSoundsUGUIHandler, IDeselectHandler {
        public void OnDeselect(BaseEventData data) {
            if (eventSounds != null) {
                eventSounds.OnDeselect(data);
            }
        }
    }

    public class EventSoundsMoveHandler : EventSoundsUGUIHandler, IMoveHandler {
        public void OnMove(AxisEventData data) {
            if (eventSounds != null) {
                eventSounds.OnMove(data);
            }
        }
    }

    public class EventSoundsInitializePotentialDragHandler : EventSoundsUGUIHandler, IInitializePotentialDragHandler {
        public void OnInitializePotentialDrag(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnInitializePotentialDrag(data);
            }
        }
    }

    public class EventSoundsBeginDragHandler : EventSoundsUGUIHandler, IBeginDragHandler {
        public void OnBeginDrag(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnBeginDrag(data);
            }
        }
    }

    public class EventSoundsEndDragHandler : EventSoundsUGUIHandler, IEndDragHandler {
        public void OnEndDrag(PointerEventData data) {
            if (eventSounds != null) {
                eventSounds.OnEndDrag(data);
            }
        }
    }

    public class EventSoundsSubmitHandler : EventSoundsUGUIHandler, ISubmitHandler {
        public void OnSubmit(BaseEventData data) {
            if (eventSounds != null) {
                eventSounds.OnSubmit(data);
            }
        }
    }

    public class EventSoundsCancelHandler : EventSoundsUGUIHandler, ICancelHandler {
        public void OnCancel(BaseEventData data) {
            if (eventSounds != null) {
                eventSounds.OnCancel(data);
            }
        }
    }
#endregion
    /*! \endcond */
}
/*! \endcond */
