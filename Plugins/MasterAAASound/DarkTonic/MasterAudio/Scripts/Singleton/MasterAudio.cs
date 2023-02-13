using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.Video;
#endif

#if UNITY_XBOXONE
    using PlayerPrefs = DarkTonic.MasterAudio.FilePlayerPrefs;
#endif

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class contains the heart of the Master Audio API. There are also convenience methods here for Playlist Controllers, even though you can call those methods on the Playlist Controller itself as well.
    /// </summary>
    // ReSharper disable once CheckNamespace
    [AudioScriptOrder(-50)]
    public class MasterAudio : MonoBehaviour {
        /*! \cond PRIVATE */
        #region Constants

#pragma warning disable 1591
        public const string VideoPlayersSoundGroupSelectedError = "Can't use specially named Sound Group for Video Players. Please select another.";
        public const string VideoPlayerSoundGroupName = "_VideoPlayers";
        public const string VideoPlayerBusName = "_VideoPlayers";
        public const string MasterAudioDefaultFolder = "Assets/Plugins/DarkTonic/MasterAudio";
        public const string PreviewText = "Random delay, custom fading & start/end position settings are ignored by preview in edit mode.";
        public const string LoopDisabledLoopedChain = "Loop Clip is always OFF for Looped Chain Groups";
        public const string LoopDisabledCustomEnd = "Loop Clip is always OFF when using Custom End Position";
        public const string DragAudioTip = "Drag Audio clips or a folder containing some here";
        public const string NoCategory = "[Uncategorized]";
        public const float SemiTonePitchFactor = 1.05946f;
        public const float SpatialBlend_2DValue = 0f;
        public const float SpatialBlend_3DValue = 1f;
        public const float MaxCrossFadeTimeSeconds = 120;
        public const float DefaultDuckVolCut = -6f;

        // error numbers
        public const int ERROR_MA_LAYER_COLLISIONS_DISABLED = 1;
        public const int PHYSICS_DISABLED = 2;

        public const string StoredLanguageNameKey = "~MA_Language_Key~";

        public static readonly YieldInstruction EndOfFrameDelay = new WaitForEndOfFrame();
        public static readonly List<string> ExemptChildNames = new List<string> { AmbientUtil.FollowerHolderName };
        public static readonly HashSet<int> ErrorNumbersLogged = new HashSet<int>();
        public static List<string> ImportanceChoices = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
#if ADDRESSABLES_ENABLED
        public static List<string> AddressableDeadIds = new List<string>();
#endif

        /// <summary>
        /// Subscribe to this event to be notified when the number of Audio Sources being used by Master Audio changes.
        /// </summary>
        // ReSharper disable once RedundantNameQualifier
        public static System.Action NumberOfAudioSourcesChanged;

        public const int HardCodedBusOptions = 2;
        public const string AllBusesName = "[All]";
        public const string NoGroupName = "[None]";
        public const string DynamicGroupName = "[Type In]";
        public const string NoPlaylistName = "[No Playlist]";
        public const string NoVoiceLimitName = "[NO LMT]";
        public const string OnlyPlaylistControllerName = "~only~";
        public const float InnerLoopCheckInterval = .1f;

        private const int MaxComponents = 20;
#endregion

#region Public Variables

        // ReSharper disable InconsistentNaming
        public AudioLocation bulkLocationMode = AudioLocation.Clip;
        public string groupTemplateName = "Default Single";
        public string audioSourceTemplateName = "Max Distance 500";
        public bool showGroupCreation = true;
        public bool useGroupTemplates = false;
        public DragGroupMode curDragGroupMode = DragGroupMode.OneGroupPerClip;
        public List<GameObject> groupTemplates = new List<GameObject>(10);
        public List<GameObject> audioSourceTemplates = new List<GameObject>(10);

        public bool mixerMuted;
        public bool playlistsMuted;

        public LanguageMode langMode = LanguageMode.UseDeviceSetting;
        public SystemLanguage testLanguage = SystemLanguage.English;
        public SystemLanguage defaultLanguage = SystemLanguage.English;

        public List<SystemLanguage> supportedLanguages = new List<SystemLanguage>()
        {
            SystemLanguage.English
        };

        public string busFilter = string.Empty;
        public bool useTextGroupFilter = false;
        public string textGroupFilter = string.Empty;
        public bool resourceClipsPauseDoNotUnload = false;
        public Transform playlistControllerPrefab;
        public bool persistBetweenScenes = false;
        public bool shouldLogDestroys = false;
        public bool showBusColors = false;
        public bool showGroupImportance = false;
        public bool areGroupsExpanded = true;
        public Transform soundGroupTemplate;
        public Transform soundGroupVariationTemplate;
        public List<GroupBus> groupBuses = new List<GroupBus>();
        public bool groupByBus = true;
        public bool sortAlpha = true;
        public bool showRangeSoundGizmos = true;
        public bool showSelectedRangeSoundGizmos = true;
        public Color rangeGizmoColor = Color.green;
        public Color selectedRangeGizmoColor = Color.cyan;
        public bool showAdvancedSettings = true;
        public bool showLocalization = true;

        public bool showVideoPlayerSettings = false;
#if UNITY_2019_3_OR_NEWER && VIDEO_ENABLED
        public List<VideoPlayer> videoPlayers = new List<VideoPlayer>();
#endif

        public bool useTextPlaylistFilter = false;
        public string textPlaylistFilter = string.Empty;
        public bool playListExpanded = true;
        public bool playlistsExpanded = true;

        public AllMusicSpatialBlendType musicSpatialBlendType = AllMusicSpatialBlendType.ForceAllTo2D;
        public float musicSpatialBlend = 0f;

#if DISABLE_3D_SOUND
        public AllMixerSpatialBlendType mixerSpatialBlendType = AllMixerSpatialBlendType.ForceAllTo2D;
#else
        public AllMixerSpatialBlendType mixerSpatialBlendType = AllMixerSpatialBlendType.ForceAllTo3D;
#endif
        public float mixerSpatialBlend = 1f;

        public GroupPlayType groupPlayType = GroupPlayType.Always;
        public DefaultGroupPlayType defaultGroupPlayType = DefaultGroupPlayType.Always;

#if DISABLE_3D_SOUND
        public ItemSpatialBlendType newGroupSpatialType = ItemSpatialBlendType.ForceTo2D;
#else
        public ItemSpatialBlendType newGroupSpatialType = ItemSpatialBlendType.ForceTo3D;
#endif

        public float newGroupSpatialBlend = 1f;

        public List<Playlist> musicPlaylists = new List<Playlist>()
        {
            new Playlist()
        };

        public float _masterAudioVolume = 1.0f;
        public bool vrSettingsExpanded = false;
        public bool useSpatializer = false;
        public bool useSpatializerPostFX = false;
        public bool addOculusAudioSources = false;
        public bool addResonanceAudioSources = false;
        public bool ignoreTimeScale = false;
        public bool useGaplessPlaylists = false;
        public bool useGaplessAutoReschedule = false;
        public bool saveRuntimeChanges = false;
        public bool prioritizeOnDistance = false;
        public int rePrioritizeEverySecIndex = 1;

        public bool useOcclusion = false;
        public float occlusionMaxCutoffFreq = AudioUtil.DefaultMaxOcclusionCutoffFrequency;
        public float occlusionMinCutoffFreq = AudioUtil.DefaultMinOcclusionCutoffFrequency;
        public float occlusionFreqChangeSeconds = 0f;
        public OcclusionSelectionType occlusionSelectType = OcclusionSelectionType.AllGroups;
        public int occlusionMaxRayCastsPerFrame = 4;
        public float occlusionRayCastOffset = 0f;
        public bool occlusionUseLayerMask;
        public LayerMask occlusionLayerMask;
        public bool occlusionShowRaycasts = true;
        public bool occlusionShowCategories = false;
        public RaycastMode occlusionRaycastMode = RaycastMode.Physics3D;
        public bool occlusionIncludeStartRaycast2DCollider = true;
        public bool occlusionRaycastsHitTriggers = true;

        public bool ambientAdvancedExpanded = false;
        public int ambientMaxRecalcsPerFrame = 4;

        public bool visualAdvancedExpanded = true;
        public bool logAdvancedExpanded = true;
        public bool listenerAdvancedExpanded = false;
        public bool listenerFollowerHasRigidBody = true;
        public bool deletePreviewerAudioSourceWhenPlaying = true;
        public VariationFollowerType variationFollowerType = VariationFollowerType.LateUpdate;

        public bool showFadingSettings = false;
        public bool stopZeroVolumeGroups = false;
        public bool stopZeroVolumeBuses = false;
        public bool stopZeroVolumePlaylists = false;
        public float stopOldestBusFadeTime = 0.3f;

        public bool resourceAdvancedExpanded = true;
        public bool useClipAgePriority = false;
        public bool logOutOfVoices = true;
        public bool LogSounds;
        public bool logCustomEvents = false;
        public bool disableLogging = false;
        public bool showMusicDucking = false;
        public bool enableMusicDucking = true;
        public List<DuckGroupInfo> musicDuckingSounds = new List<DuckGroupInfo>();
        public float defaultRiseVolStart = .5f;
        public float defaultUnduckTime = 1f;
        public float defaultDuckedVolumeCut = DefaultDuckVolCut;
        public float crossFadeTime = 1f;
        public float _masterPlaylistVolume = 1f;
        public bool showGroupSelect = false;
        public bool hideGroupsWithNoActiveVars = false;

		public JukeBoxDisplayMode jukeBoxDisplayMode = JukeBoxDisplayMode.DisplayAll;

        public bool logPerfExpanded = true;
        public bool willWarm = true;

        public bool mixerSettingsExpanded;
        public AudioMixerUpdateMode mixerUpdateMode = AudioMixerUpdateMode.UnscaledTime;

        public string newEventName = "my event";
        public bool showCustomEvents = true;
        public string newCustomEventCategoryName = "New Category";
        public string addToCustomEventCategoryName = "New Category";
        public List<CustomEvent> customEvents = new List<CustomEvent>();
        public List<CustomEventCategory> customEventCategories = new List<CustomEventCategory> {
                new CustomEventCategory()
            };

        public Dictionary<string, DuckGroupInfo> duckingBySoundType = new Dictionary<string, DuckGroupInfo>(StringComparer.OrdinalIgnoreCase);
        // populated at runtime

#if ADDRESSABLES_ENABLED
        //public SortedCache<int> addressablesToReleaseAfterSecondsByAddressableId
#endif

        public int frames;

        public bool showUnityMixerGroupAssignment = true;

        public static readonly PlaySoundResult AndForgetSuccessResult = new PlaySoundResult {
            SoundPlayed = true
        };

        private static readonly PlaySoundResult failedResultDuringInit = new PlaySoundResult {
            SoundPlayed = false
        };

#endregion

#region Private Variables

        private readonly Dictionary<string, AudioGroupInfo> AudioSourcesBySoundType = 
            new Dictionary<string, AudioGroupInfo>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, List<int>> _randomizer = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<int>> _randomizerOrigin = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<int>> _randomizerLeftovers = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<int>> _nonRandomChoices = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, List<int>> _clipsPlayedBySoundTypeOldestFirst = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        private readonly List<SoundGroupVariationUpdater> ActiveVariationUpdaters = new List<SoundGroupVariationUpdater>(32);
        private readonly List<SoundGroupVariationUpdater> ActiveUpdatersToRemove = new List<SoundGroupVariationUpdater>();
        private readonly List<ICustomEventReceiver> ValidReceivers = new List<ICustomEventReceiver>();
        private readonly List<CustomEventCandidate> ValidReceiverCandidates = new List<CustomEventCandidate>(10);
        private readonly List<MasterAudioGroup> SoloedGroups = new List<MasterAudioGroup>(); 
		private readonly List<AmbientSoundToTriggerInfo> AmbientsToDelayedTrigger = new List<AmbientSoundToTriggerInfo>();
		private readonly Queue<CustomEventToFireInfo> CustomEventsToFire = new Queue<CustomEventToFireInfo>(32);
        private readonly Queue<TransformFollower> TransFollowerColliderPositionRecalcs = new Queue<TransformFollower>(32);
        private readonly List<TransformFollower> ProcessedColliderPositionRecalcs = new List<TransformFollower>(32);
        private readonly List<BusFadeInfo> BusFades = new List<BusFadeInfo>(2);
        private readonly List<GroupFadeInfo> GroupFades = new List<GroupFadeInfo>();
        private readonly List<GroupPitchGlideInfo> GroupPitchGlides = new List<GroupPitchGlideInfo>();
        private readonly List<BusPitchGlideInfo> BusPitchGlides = new List<BusPitchGlideInfo>();
        private readonly List<OcclusionFreqChangeInfo> VariationOcclusionFreqChanges = new List<OcclusionFreqChangeInfo>();
        private readonly List<AudioSource> AllAudioSources = new List<AudioSource>();
        private readonly List<BusDuckInfo> BusDucks = new List<BusDuckInfo>();

        private readonly Dictionary<string, Dictionary<ICustomEventReceiver, Transform>> ReceiversByEventName =
            new Dictionary<string, Dictionary<ICustomEventReceiver, Transform>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PlaylistController> PlaylistControllersByName =
            new Dictionary<string, PlaylistController>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, SoundGroupRefillInfo> LastTimeSoundGroupPlayed =
            new Dictionary<string, SoundGroupRefillInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly List<GameObject> OcclusionSourcesInRange = new List<GameObject>(32);
        private readonly List<GameObject> OcclusionSourcesOutOfRange = new List<GameObject>(32);
        private readonly List<GameObject> OcclusionSourcesBlocked = new List<GameObject>(32);
        private readonly Queue<SoundGroupVariationUpdater> QueuedOcclusionRays = new Queue<SoundGroupVariationUpdater>(32);
#if UNITY_2019_3_OR_NEWER && VIDEO_ENABLED
        private readonly List<VideoPlayerTracker> VideoPlayerTrackers = new List<VideoPlayerTracker>();
#endif
#if ADDRESSABLES_ENABLED
        private readonly List<AddressableDelayedRelease> AddressablesToReleaseLater = new List<AddressableDelayedRelease>();
#endif

        private readonly List<string> AllSoundGroupNames = new List<string>(32); // use this to loop through the list. Looping on .Keys of a Dictionary allocates.
        private readonly List<string> AllBusNames = new List<string>(32); // use this to loop through the list. Looping on .Keys of a Dictionary allocates.


        private readonly List<AudioInfo> GroupsToDelete = new List<AudioInfo>();
        
        private readonly List<SoundGroupVariation> VariationsStartedDuringMultiStop = new List<SoundGroupVariation>(16);
        private readonly List<PlaylistController> ControllersToPause = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToUnpause = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToMute = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToUnmute = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToToggleMute = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToStop = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToFade = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToTrigNext = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToTrigRandom = new List<PlaylistController>();
        private readonly List<PlaylistController> ControllersToStart = new List<PlaylistController>();
        private readonly List<AmbientSoundToTriggerInfo> AmbientsToTriggerNow = new List<AmbientSoundToTriggerInfo>();

        private bool _isStoppingMultiple;
        private float _repriTime = -1f;
        private List<string> _groupsToRemove;
        private bool _mustRescanGroups;

        private Transform _trans;
        private bool _soundsLoaded;
        private bool _warming;
        // ReSharper restore InconsistentNaming

        private static MasterAudio _instance;
        private static string _prospectiveMAFolder = string.Empty;
        private static Transform _listenerTrans;
	
#endregion

#region Master Audio enums
		public enum JukeBoxDisplayMode
		{
			DisplayAll,
			DisplayActive
		}

		public enum BusVoiceLimitExceededMode
        {
            DoNotPlayNewSound,
            StopOldestSound,
            StopFarthestSound,
            StopLeastImportantSound
        }
        
        public enum AmbientSoundExitMode {
            StopSound,
            FadeSound
        }

        public enum AmbientSoundReEnterMode {
            StopExistingSound,
            FadeInSameSound
        }

        public enum VariationFollowerType {
            LateUpdate,
            FixedUpdate
        }

        public enum LinkedGroupSelectionType {
            All,
            OneAtRandom
        }

        public enum OcclusionSelectionType {
            AllGroups,
            TurnOnPerBusOrGroup
        }

        public enum RaycastMode {
            Physics3D,
            Physics2D
        }

        public enum AllMusicSpatialBlendType {
            ForceAllTo2D
#if DISABLE_3D_SOUND
#else
            ,
            ForceAllTo3D,
            ForceAllToCustom,
            AllowDifferentPerController
#endif
        }

        public enum AllMixerSpatialBlendType {
            ForceAllTo2D
#if DISABLE_3D_SOUND
#else
            ,
            ForceAllTo3D,
            ForceAllToCustom,
            AllowDifferentPerGroup
#endif
        }

        public enum ItemSpatialBlendType {
            ForceTo2D
#if DISABLE_3D_SOUND
#else
            ,
            ForceTo3D,
            ForceToCustom,
            UseCurveFromAudioSource
#endif
        }

        public enum GroupPlayType
        {
            Always,
            WhenActorInAudibleRange,
            AllowDifferentPerGroup
        }

        public enum DefaultGroupPlayType {
            Always,
            WhenActorInAudibleRange
        }

        public enum MixerWidthMode {
            Narrow,
            Normal,
            Wide
        }

        public enum CustomEventReceiveMode {
            Always,
            WhenDistanceLessThan,
            WhenDistanceMoreThan,
            Never,
            OnSameGameObject,
            OnChildGameObject,
            OnParentGameObject,
            OnSameOrChildGameObject,
            OnSameOrParentGameObject
        }

        public enum EventReceiveFilter {
            All,
            Closest,
            Random
        }

        public enum VariationLoadStatus {
            None,
            Loading,
            Loaded,
            LoadFailed
        }

        /*! \endcond */
        /// <summary>
        /// This setting lets you choose where the Audio Clip lives: Clip, Resource File or Addressable
        /// </summary>
        public enum AudioLocation {
            Clip,
            ResourceFile
#if ADDRESSABLES_ENABLED
            ,Addressable
#endif
        }

        /// <summary>
        /// This controls where the song starts, Beginning, Specific Time or Random Time.
        /// </summary>
        public enum CustomSongStartTimeMode
        {
            Beginning,
            SpecificTime,
            RandomTime,
            Section
        }

        /*! \cond PRIVATE */


        public enum BusCommand {
            None,
            FadeToVolume,
            Mute,
            Pause,
            Solo,
            Unmute,
            Unpause,
            Unsolo,
            Stop,
            ChangePitch,
            ToggleMute,
            StopBusOfTransform,
            PauseBusOfTransform,
            UnpauseBusOfTransform,
            GlideByPitch,
            StopOldBusVoices,
            FadeOutOldBusVoices
        }

        public enum DragGroupMode {
            OneGroupPerClip,
            OneGroupWithVariations
        }

        public enum EventSoundFunctionType {
            PlaySound,
            GroupControl,
            BusControl,
            PlaylistControl,
            CustomEventControl,
            GlobalControl,
            UnityMixerControl,
            PersistentSettingsControl
        }

        public enum LanguageMode {
            UseDeviceSetting,
            SpecificLanguage,
            DynamicallySet
        }

        public enum UnityMixerCommand {
            None,
            TransitionToSnapshot,
            TransitionToSnapshotBlend
        }

        public enum PlaylistCommand {
            None,
            ChangePlaylist, // by name
            FadeToVolume,
            PlaySong, // by name
            PlayRandomSong,
            PlayNextSong,
            Pause,
            Resume,
            Stop,
            Mute,
            Unmute,
            ToggleMute,
            Restart,
            Start,
            StopLoopingCurrentSong,
            StopPlaylistAfterCurrentSong,
            AddSongToQueue
        }

        public enum CustomEventCommand {
            None,
            FireEvent
        }

        public enum GlobalCommand {
            None,
            PauseMixer,
            UnpauseMixer,
            StopMixer,
            StopEverything,
            PauseEverything,
            UnpauseEverything,
            MuteEverything,
            UnmuteEverything,
            SetMasterMixerVolume,
            SetMasterPlaylistVolume,
            PauseAudioListener,
            UnpauseAudioListener
        }

        public enum SoundGroupCommand {
            None,
            FadeToVolume,
            FadeOutAllOfSound,
            Mute,
            Pause,
            Solo,
            StopAllOfSound,
            Unmute,
            Unpause,
            Unsolo,
            StopAllSoundsOfTransform,
            PauseAllSoundsOfTransform,
            UnpauseAllSoundsOfTransform,
            StopSoundGroupOfTransform,
            PauseSoundGroupOfTransform,
            UnpauseSoundGroupOfTransform,
            FadeOutSoundGroupOfTransform,
            RefillSoundGroupPool,
            RouteToBus,
            GlideByPitch,
            ToggleSoundGroup,
            ToggleSoundGroupOfTransform,
            FadeOutAllSoundsOfTransform,
            StopOldSoundGroupVoices,
            FadeOutOldSoundGroupVoices,
            FadeSoundGroupOfTransformToVolume
        }

        public enum PersistentSettingsCommand {
            None,
            SetBusVolume,
            SetGroupVolume,
            SetMixerVolume,
            SetMusicVolume,
            MixerMuteToggle,
            MusicMuteToggle
        }

        public enum SongFadeInPosition {
            NewClipFromBeginning = 1,
            NewClipFromLastKnownPosition = 3,
            SynchronizeClips = 5,
        }

        public enum SoundSpawnLocationMode {
            MasterAudioLocation,
            CallerLocation,
            AttachToCaller
        }

        public enum VariationCommand {
            None = 0,
            Stop = 1,
            Pause = 2,
            Unpause = 3
        }

        public static readonly List<SoundGroupCommand> GroupCommandsWithNoGroupSelector = new List<SoundGroupCommand> {
            SoundGroupCommand.None,
            SoundGroupCommand.PauseAllSoundsOfTransform,
            SoundGroupCommand.StopAllSoundsOfTransform,
            SoundGroupCommand.UnpauseAllSoundsOfTransform,
            SoundGroupCommand.FadeOutAllSoundsOfTransform
        };

        public static readonly List<SoundGroupCommand> GroupCommandsWithNoAllGroupSelector = new List<SoundGroupCommand> {
            SoundGroupCommand.None,
            SoundGroupCommand.FadeOutAllSoundsOfTransform 
        };

#endregion

#region Inner classes & Structs
        [Serializable]
        public struct CustomEventCandidate {
            public float DistanceAway;
            public ICustomEventReceiver Receiver;
            public Transform Trans;
            public int RandomId;

            public CustomEventCandidate(float distance, ICustomEventReceiver rec, Transform trans, int randomId) {
                DistanceAway = distance;
                Receiver = rec;
                Trans = trans;
                RandomId = randomId;
            }
        }

        [Serializable]
        public class AudioGroupInfo {
            public List<AudioInfo> Sources;
            public int LastFramePlayed;
            public float LastTimePlayed;
            public MasterAudioGroup Group;
            public bool PlayedForWarming;

            public AudioGroupInfo(List<AudioInfo> sources, MasterAudioGroup groupScript) {
                Sources = sources;
                LastFramePlayed = -50;
                LastTimePlayed = -50;
                Group = groupScript;
                PlayedForWarming = false;
            }
        }

        [Serializable]
        public class AudioInfo {
            public AudioSource Source;
            public float OriginalVolume;
            public float LastPercentageVolume;
            public float LastRandomVolume;
            public SoundGroupVariation Variation;

            public AudioInfo(SoundGroupVariation variation, AudioSource source, float origVol) {
                Variation = variation;
                Source = source;
                OriginalVolume = origVol;
                LastPercentageVolume = 1f;
                LastRandomVolume = 0f;
            }
        }

        [Serializable]
        public class Playlist {
            // ReSharper disable InconsistentNaming
            public bool isExpanded = true;
            public string playlistName = "new playlist";
            public SongFadeInPosition songTransitionType = SongFadeInPosition.NewClipFromBeginning;
            public List<MusicSetting> MusicSettings;
            public AudioLocation bulkLocationMode = AudioLocation.Clip;
            public CrossfadeTimeMode crossfadeMode = CrossfadeTimeMode.UseMasterSetting;
            public float crossFadeTime = 1f;
            public bool fadeInFirstSong = false;
            public bool fadeOutLastSong = false;
            public bool bulkEditMode = false;
            public bool isTemporary = false;
            public bool showMetadata = false;
            public List<SongMetadataProperty> songMetadataProps = new List<SongMetadataProperty>();
            public string newMetadataPropName = "PropertyName";
            public SongMetadataProperty.MetadataPropertyType newMetadataPropType = SongMetadataProperty.MetadataPropertyType.String;
            public bool newMetadataPropRequired = true;
            public bool newMetadataPropCanHaveMult = false;
            // ReSharper restore InconsistentNaming

            private readonly List<int> _actorInstanceIds = new List<int>();

            public enum CrossfadeTimeMode {
                UseMasterSetting,
                Override
            }

            public Playlist() {
                MusicSettings = new List<MusicSetting>();
            }

            public void AddActorInstanceId(int instanceId)
            {
                if (_actorInstanceIds.Contains(instanceId))
                {
                    return;
                }

                _actorInstanceIds.Add(instanceId);
            }

            public void RemoveActorInstanceId(int instanceId)
            {
                _actorInstanceIds.Remove(instanceId);
            }

            public bool HasLiveActors {
                get {
                    return _actorInstanceIds.Count > 0;
                }
            }
        }

        [Serializable]
        public class SoundGroupRefillInfo {
            public float LastTimePlayed;
            public float InactivePeriodSeconds;

            public SoundGroupRefillInfo(float lastTimePlayed, float inactivePeriodSeconds) {
                LastTimePlayed = lastTimePlayed;
                InactivePeriodSeconds = inactivePeriodSeconds;
            }
        }
        /*! \endcond */
        #endregion

        #region MonoDevelop events and Helpers

#if UNITY_2019_3_OR_NEWER && UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init() {
            AppIsShuttingDown = false;
        }
#endif

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once FunctionComplexityOverflow
        private void Awake() {
            var shouldDestroy = false;
            var shouldLogDestruction = false;

#if UNITY_2019_3_OR_NEWER 
            if (MasterAudioReferenceHolder.MasterAudio == null) {
                MasterAudioReferenceHolder.MasterAudio = this;
            } else {
                shouldDestroy = true;
                var olderMA = MasterAudioReferenceHolder.MasterAudio;
                if (olderMA.persistBetweenScenes && olderMA.shouldLogDestroys) {
                    shouldLogDestruction = true;
                }
            }
#else 
            var mas = FindObjectsOfType(typeof(MasterAudio));
            if (mas.Length > 1) {
                shouldDestroy = true;

                for (var i = 0; i < mas.Length; i++) {
                    MasterAudio ama = mas[i] as MasterAudio;
                    if (!ama.persistBetweenScenes) {
                        continue;
                    }

                    if (ama.shouldLogDestroys) {
                        shouldLogDestruction = true;
                        break;
                    }
                }
            }
#endif
            if (shouldDestroy) {
                Destroy(gameObject);

                if (shouldLogDestruction) {
                    Debug.Log("More than one Master Audio prefab exists in this Scene. Destroying the newer one called '" +
                              name + "'. You may wish to set up a Bootstrapper Scene so this does not occur.");
                }
                return;
            }

            AudioListener.pause = false; // in case you exited last time with it paused. You would hear nothing on next play.
            useGUILayout = false;
            _soundsLoaded = false;
            _mustRescanGroups = false;

            var listener = ListenerTrans;
            if (listener != null && deletePreviewerAudioSourceWhenPlaying) {
                var aud = listener.GetComponent<AudioSource>();
                if (aud != null) {
                    // delete the previewer
                    // ReSharper disable once ArrangeStaticMemberQualifier
                    GameObject.Destroy(aud);
                }
            }

            AmbientUtil.InitFollowerHolder();

            AudioSourcesBySoundType.Clear();
            AllBusNames.Clear();
            AllSoundGroupNames.Clear();
            GroupsToDelete.Clear();
            ValidReceivers.Clear();
            ValidReceiverCandidates.Clear();
            ControllersToPause.Clear();
            ControllersToUnpause.Clear();
            ControllersToMute.Clear();
            ControllersToUnmute.Clear();
            ControllersToToggleMute.Clear();
            ControllersToStop.Clear();
            ControllersToFade.Clear();
            ControllersToTrigNext.Clear();
            ControllersToTrigRandom.Clear();
            ControllersToStart.Clear();
            PlaylistControllersByName.Clear();
            LastTimeSoundGroupPlayed.Clear();
            ErrorNumbersLogged.Clear();
            AmbientsToTriggerNow.Clear();

            AllAudioSources.Clear();
            OcclusionSourcesInRange.Clear();
            OcclusionSourcesOutOfRange.Clear();
            OcclusionSourcesBlocked.Clear();
            QueuedOcclusionRays.Clear();
            TransFollowerColliderPositionRecalcs.Clear();
            CustomEventsToFire.Clear();
            AmbientsToDelayedTrigger.Clear();
            ProcessedColliderPositionRecalcs.Clear();
            ActiveVariationUpdaters.Clear();
            ActiveUpdatersToRemove.Clear();

            var plNames = new List<string>();
            AudioResourceOptimizer.ClearAudioClips();

            PlaylistController.Instances = null; // clear the cache
            var playlists = PlaylistController.Instances;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                var aList = playlists[i];

                if (plNames.Contains(aList.ControllerName)) {
                    Debug.LogError("You have more than 1 Playlist Controller with the name '" + aList.ControllerName +
                                   "'. You must name them all uniquely or the same-named ones will be deleted once they awake.");
                    continue;
                }

                plNames.Add(aList.ControllerName);

                PlaylistControllersByName.Add(aList.ControllerName, aList);
                if (persistBetweenScenes) {
                    DontDestroyOnLoad(aList);
                }
            }

            // start up Objects!
            if (persistBetweenScenes) {
                DontDestroyOnLoad(gameObject);
            }

            var playedStatuses = new List<int>();

            // ReSharper disable TooWideLocalVariableScope
            Transform parentGroup;
            List<AudioInfo> sources;
            AudioSource source;
            AudioGroupInfo group;
            MasterAudioGroup groupScript;
            string soundType;
            // ReSharper restore TooWideLocalVariableScope

            _randomizer = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            _randomizerOrigin = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            _randomizerLeftovers = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            _nonRandomChoices = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            _clipsPlayedBySoundTypeOldestFirst = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            var allVars = new List<SoundGroupVariation>();

            _groupsToRemove = new List<string>(Trans.childCount);

            var mutedGroups = new List<string>();

            for (var k = 0; k < Trans.childCount; k++) {
                parentGroup = Trans.GetChild(k);

                sources = new List<AudioInfo>();

                groupScript = parentGroup.GetComponent<MasterAudioGroup>();

                if (groupScript == null) {
                    if (!ArrayListUtil.IsExcludedChildName(parentGroup.name)) {
                        Debug.LogError("MasterAudio could not find 'MasterAudioGroup' script for group '" + parentGroup.name + "'. Skipping this group.");
                    }

                    continue;
                }

                soundType = groupScript.GameObjectName;

                var newWeightedChildren = new List<Transform>();

                // ReSharper disable TooWideLocalVariableScope
                SoundGroupVariation variation;
                SoundGroupVariation childVariation;
                Transform child;
                // ReSharper restore TooWideLocalVariableScope

                var allStatuses = new List<int>();

                for (var i = 0; i < parentGroup.childCount; i++) {
                    child = parentGroup.GetChild(i);
                    variation = child.GetComponent<SoundGroupVariation>();
                    source = child.GetComponent<AudioSource>();

                    var weight = variation.weight;

                    for (var j = 0; j < weight; j++) {
                        if (j > 0) {
                            // ReSharper disable once ArrangeStaticMemberQualifier
                            var extraChild = (GameObject)GameObject.Instantiate(child.gameObject, parentGroup.transform.position, Quaternion.identity);
                            extraChild.transform.name = child.gameObject.name;
                            childVariation = extraChild.GetComponent<SoundGroupVariation>();
                            childVariation.weight = 1;

                            newWeightedChildren.Add(extraChild.transform);
                            source = extraChild.GetComponent<AudioSource>();

                            sources.Add(new AudioInfo(childVariation, source, source.volume));
                            allVars.Add(childVariation);

                            switch (childVariation.audLocation) {
                                case AudioLocation.ResourceFile:
                                    AudioResourceOptimizer.AddTargetForClip(childVariation.resourceFileName, source);
                                    break;
                            }
                        } else {
                            sources.Add(new AudioInfo(variation, source, source.volume));
                            allVars.Add(variation);

                            switch (variation.audLocation) {
                                case AudioLocation.ResourceFile:
                                    var resFileName =
                                        AudioResourceOptimizer.GetLocalizedFileName(variation.useLocalization,
                                            variation.resourceFileName);
                                    AudioResourceOptimizer.AddTargetForClip(resFileName, source);
                                    break;
                            }
                        }
                    }
                }

                // attach extra children from weight property.
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < newWeightedChildren.Count; i++) {
                    newWeightedChildren[i].parent = parentGroup;
                }

                group = new AudioGroupInfo(sources, groupScript);
                if (groupScript.isSoloed) {
                    SoloedGroups.Add(groupScript);
                }

                if (mixerMuted || groupScript.isMuted) {
                    if (mutedGroups.Contains(groupScript.GameObjectName)) {
                        continue;
                    }

                    mutedGroups.Add(groupScript.GameObjectName);
                }

                if (AudioSourcesBySoundType.ContainsKey(soundType)) {
                    Debug.LogError("You have more than one SoundGroup named '" + soundType +
                                   "'. Ignoring the 2nd one. Please rename it.");
                    continue;
                }

                group.Group.OriginalVolume = group.Group.groupMasterVolume;
                // added code for persistent group volume
                var persistentVolume = PersistentAudioSettings.GetGroupVolume(soundType);
                if (persistentVolume.HasValue) {
                    group.Group.groupMasterVolume = persistentVolume.Value;
                }

                AddRuntimeGroupInfo(soundType, group);

                for (var i = 0; i < sources.Count; i++) {
                    playedStatuses.Add(i);
                }

                if (group.Group.curVariationSequence == MasterAudioGroup.VariationSequence.Randomized) {
                    ArrayListUtil.SortIntArray(ref playedStatuses);
                }

                _randomizer.Add(soundType, playedStatuses);

                allStatuses.Clear();
                allStatuses.AddRange(playedStatuses);
                _randomizerOrigin.Add(soundType, allStatuses); // must be a copy of one gets lost later

                _randomizerLeftovers.Add(soundType, new List<int>(playedStatuses.Count));
                // fill leftovers pool.
                _randomizerLeftovers[soundType].AddRange(playedStatuses);
                _clipsPlayedBySoundTypeOldestFirst.Add(soundType, new List<int>());
                _nonRandomChoices.Add(soundType, new List<int>());

                playedStatuses = new List<int>();
            }

            GroupFades.Clear();
            BusFades.Clear();
            BusDucks.Clear();

            GroupPitchGlides.Clear();
            BusPitchGlides.Clear();

            VariationOcclusionFreqChanges.Clear();

            // initialize persistent bus volumes 
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < groupBuses.Count; i++) {
                var aBus = groupBuses[i];

                aBus.OriginalVolume = aBus.volume;
                var busName = aBus.busName;
                var busVol = PersistentAudioSettings.GetBusVolume(busName);

                if (!busVol.HasValue) {
                    continue;
                }

                SetBusVolumeByName(busName, busVol.Value);
            }

            // populate ducking sounds dictionary
            duckingBySoundType.Clear();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < musicDuckingSounds.Count; i++) {
                var aDuck = musicDuckingSounds[i];
                if (duckingBySoundType.ContainsKey(aDuck.soundType)) {
                    Debug.LogWarning("You have more than one Duck Group set up with the Sound Group '" + aDuck.soundType + "'. Please delete the duplicates before running again.");
                    continue;
                }

                if (aDuck.soundType == VideoPlayerSoundGroupName)
                {
                    Debug.LogError("The specially named Sound Group for Video Players '" + VideoPlayerSoundGroupName + "' cannot be used as a Music Ducking Group. Please remove it.");
                    continue;
                }

                duckingBySoundType.Add(aDuck.soundType, aDuck);
            }

#if UNITY_2019_3_OR_NEWER && VIDEO_ENABLED
            if (videoPlayers.Count > 0)
            {
                var videoPlayerHolder = VideoPlayerSoundGroupTransform;
                if (videoPlayerHolder == null)
                {
                    Debug.LogError("You have deleted the specially named Sound Group for Video Players. Please press stop and open the Master Audio Inspector and expand the Video Player Settings section so it will be automatically created again. Otherwise the audio for your Video Players will not work properly.");
                }
                else
                {
                    VideoPlayerTrackers.Clear();
                    for (var i = 0; i < videoPlayers.Count; i++)
                    {
                        var aPlayer = videoPlayers[i];
                        if (aPlayer.clip == null)
                        {
                            Debug.LogError("Your clip for Video Player in Game Object '" + aPlayer.name + " is empty. Please assign a video clip or delete this Video Player from Master Audio.");
                            continue;
                        }
                        var childVariationGO = videoPlayerHolder.Find(aPlayer.name);
                        if (childVariationGO == null)
                        {
                            Debug.LogError("You have deleted the one or more Variations in the specially named Video Players Sound Group. Please press stop and open the Master Audio Inspector and expand the Video Player Settings section so it will be automatically created again. Otherwise the audio for your Video Players will not work properly.");
                            continue;
                        }

                        var childVariation = childVariationGO.transform.GetComponent<SoundGroupVariation>();

                        VideoPlayerTrackers.Add(new VideoPlayerTracker(aPlayer, childVariation));
                    }
                }

                var videoPlayerBus = GrabBusByName(VideoPlayerBusName);
                if (videoPlayerBus == null)
                {
                    Debug.LogError("You have deleted the specially named Video Players Bus. Please press stop and open the Master Audio Inspector and expand the Video Player Settings section so it will be automatically created again. Otherwise the audio for your Video Players will not work properly.");
                }
            }
#endif

            _soundsLoaded = true;

            if (willWarm) {
                _warming = true;

                var warmGroup = SoundGroupForWarming();
                // pre-warm the code so the first sound played for real doesn't have to JIT and be slow.
                if (!string.IsNullOrEmpty(warmGroup)) {
                    var result = PlaySound3DFollowTransform(warmGroup, Trans, 0f);
                    if (result != null && result.SoundPlayed) {
                        result.ActingVariation.Stop();
                    }
                }

                FireCustomEvent("FakeEvent", _trans);

                // ReSharper disable once ForCanBeConvertedToForeach
                // Reset stuff for people who use "Save runtime changes".
                for (var i = 0; i < customEvents.Count; i++) {
                    customEvents[i].frameLastFired = -1;
                }
                frames = 0;

                // Event Sounds warmer
                // ReSharper disable once ArrangeStaticMemberQualifier
                var evts = GameObject.FindObjectsOfType(typeof(EventSounds));
                if (evts.Length > 0) {
                    var evt = evts[0] as EventSounds;
                    evt.PlaySounds(evt.particleCollisionSound, EventSounds.EventType.UserDefinedEvent);
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < mutedGroups.Count; i++) {
                    MuteGroup(mutedGroups[i], false);
                }

                // done warming
                _warming = false;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < allVars.Count; i++) {
                allVars[i].DisableUpdater();
            }

            AmbientUtil.InitListenerFollower(); // start this up so it's available to batch occlusion stuff

            // fixed: make sure this happens before Playlists start or the volume won't be right.
            PersistentAudioSettings.RestoreMasterSettings();
        }

        // ReSharper disable once UnusedMember.Local
        private void Start() {
            // wait for Playlist Controller to initialize!
            if (musicPlaylists.Count > 0
                && musicPlaylists[0].MusicSettings != null
                && musicPlaylists[0].MusicSettings.Count > 0
                && musicPlaylists[0].MusicSettings[0].clip != null
                && PlaylistControllersByName.Count == 0) {

                Debug.Log("No Playlist Controllers exist in the Scene. Music will not play.");
            }
        }

#if UNITY_2019_3_OR_NEWER 
        void OnDestroy() {
            if (MasterAudioReferenceHolder.MasterAudio == this) {
                MasterAudioReferenceHolder.MasterAudio = null;
            }
        }
#endif

        // ReSharper disable once UnusedMember.Local
        void OnDisable() {
            var sources = GetComponentsInChildren<AudioSource>().ToList();
            StopTrackingRuntimeAudioSources(sources);
        }

        // ReSharper disable once UnusedMember.Local
        void Update() {
            frames++;

            // adjust for Inspector realtime slider.
            PerformOcclusionFrequencyChanges();
            PerformBusFades();
            PerformBusPitchGlides();

            PerformGroupFades();
            PerformGroupPitchGlides();
            PerformBusDucks();
            PerformDelayedAmbientTriggers();

            RefillInactiveGroupPools();
            FireCustomEventsWaiting();
#if UNITY_2019_3_OR_NEWER && VIDEO_ENABLED
            TrackVideoPlayers();
#endif
#if ADDRESSABLES_ENABLED
            CheckAddressablesForDelayedRelease();
#endif
        }

        // ReSharper disable once UnusedMember.Local
        void LateUpdate() {
            if (variationFollowerType != VariationFollowerType.LateUpdate) {
                return;
            }

            ManualUpdate();
        }

        // ReSharper disable once UnusedMember.Local
        void FixedUpdate() {
            if (variationFollowerType != VariationFollowerType.FixedUpdate) {
                return;
            }

            ManualUpdate();
        }

        private void ManualUpdate() {
			RecalcClosestColliderPositions();

            AmbientUtil.ManualUpdate();

            UpdateActiveVariations();
        }

        /*! \cond PRIVATE */
        public string SoundGroupForWarming() {
            string firstGroupName = null;

            for (var k = 0; k < Trans.childCount; k++) {
                var parentGroup = Trans.GetChild(k);
                if (parentGroup.name == AmbientUtil.FollowerHolderName) {
                    continue; // don't pick this, not a Sound Group
                }

                if (firstGroupName == null) {
                    firstGroupName = parentGroup.name;
                }

                for (var i = 0; i < parentGroup.childCount; i++) {
                    var variationGO = parentGroup.GetChild(i);
                    var variation = variationGO.GetComponent<SoundGroupVariation>();
                    if (variation == null) {
                        continue;
                    }

                    if (variation.audLocation == AudioLocation.Clip) {
                        return parentGroup.name;
                    }
                }
            }

            return firstGroupName;
        }

        public static void RegisterUpdaterForUpdates(SoundGroupVariationUpdater updater) {
            if (Instance.ActiveVariationUpdaters.Contains(updater)) {
                return;
            }

            Instance.ActiveVariationUpdaters.Add(updater);
        }

        public static void UnregisterUpdaterForUpdates(SoundGroupVariationUpdater updater) {
            Instance.ActiveVariationUpdaters.Remove(updater);
        }

        private void UpdateActiveVariations() {
            ActiveUpdatersToRemove.Clear();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < ActiveVariationUpdaters.Count; i++) {
                var updater = ActiveVariationUpdaters[i];
                if (updater == null || !updater.enabled) {
                    ActiveUpdatersToRemove.Add(updater);
                    continue;
                }

                updater.ManualUpdate();
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < ActiveUpdatersToRemove.Count; i++) {
                ActiveVariationUpdaters.Remove(ActiveUpdatersToRemove[i]);
            }
        }

        private static void UpdateRefillTime(string sType, float inactivePeriodSeconds) {
            if (!Instance.LastTimeSoundGroupPlayed.ContainsKey(sType)) {
                Instance.LastTimeSoundGroupPlayed.Add(sType,
                    new SoundGroupRefillInfo(Time.realtimeSinceStartup, inactivePeriodSeconds));
            } else {
                Instance.LastTimeSoundGroupPlayed[sType].LastTimePlayed = AudioUtil.Time;
            }
        }

        private static void RecalcClosestColliderPositions() {
            if (!AmbientUtil.HasListenerFollower) { 
                AmbientUtil.InitListenerFollower();
            }

            Instance.ProcessedColliderPositionRecalcs.Clear();

            var itemsToCalc = Instance.TransFollowerColliderPositionRecalcs.Count;
            if (itemsToCalc > Instance.ambientMaxRecalcsPerFrame)
            {
                itemsToCalc = Instance.ambientMaxRecalcsPerFrame;
            }

            for (var i = 0; i < itemsToCalc;) {
                if (Instance.TransFollowerColliderPositionRecalcs.Count == 0) {
                    break; // no more waiting there. Abort
                }

                var follower = Instance.TransFollowerColliderPositionRecalcs.Dequeue();
                if (follower == null || !follower.enabled) { // Updater was destroyed while waiting, or sound is done playing and Updater disabled.
                    continue;
                }

                var wasCalculated = follower.RecalcClosestColliderPosition();
                Instance.ProcessedColliderPositionRecalcs.Add(follower);

                if (wasCalculated) {
                    i++;
                } 
            }

            // put the processed ones back in the rear of the queue so they will continue to update position.
            for (var i = 0; i < Instance.ProcessedColliderPositionRecalcs.Count; i++) {
                Instance.TransFollowerColliderPositionRecalcs.Enqueue(Instance.ProcessedColliderPositionRecalcs[i]);
            }
        }

#if UNITY_2019_3_OR_NEWER && VIDEO_ENABLED
        private static void TrackVideoPlayers()
        {
            for (var i = 0; i < Instance.VideoPlayerTrackers.Count; i++)
            {
                var aPlayer = Instance.VideoPlayerTrackers[i];

                if (!aPlayer.IsPlaying)
                {
                    // enable game objects of now playing videos
                    if (aPlayer.Player.isPlaying)
                    {
                        aPlayer.StartedPlaying();
                        DTMonoHelper.SetActive(aPlayer.Variation.GameObj, true);
                        aPlayer.Variation.PlayVideo();
                    }

                    continue;
                }

                // disable game objects not now non-playing videos
                if (!aPlayer.Player.isPlaying)
                {
                    aPlayer.StoppedPlayings();
                    DTMonoHelper.SetActive(aPlayer.Variation.GameObj, false);
                    aPlayer.Variation.StopVideo();
                }
            }
        }
#endif

        private static void FireCustomEventsWaiting() {
            while (Instance.CustomEventsToFire.Count > 0) {
                var custEvent = Instance.CustomEventsToFire.Dequeue();
                FireCustomEvent(custEvent.eventName, custEvent.eventOrigin);
            }
        }

#if ADDRESSABLES_ENABLED
        private static void CheckAddressablesForDelayedRelease() {
            if (Instance.AddressablesToReleaseLater.Count == 0) {
                return;
            }

            AddressableDeadIds.Clear();

            for (var i = 0; i < Instance.AddressablesToReleaseLater.Count; i++) {
                var addToRelease = Instance.AddressablesToReleaseLater[i];
                if (Time.realtimeSinceStartup >= addToRelease.RealtimeToRelease) {
                    AddressableDeadIds.Add(addToRelease.AddressableId);
                }
            }

            foreach (var deadId in AddressableDeadIds) {
                AudioAddressableOptimizer.MaybeReleaseAddressable(deadId, true);
            }

            Instance.AddressablesToReleaseLater.RemoveAll(delegate (AddressableDelayedRelease adr) {
                return AddressableDeadIds.Contains(adr.AddressableId);
            });
        }
#endif

        private static void RefillInactiveGroupPools() {
            var groups = Instance.LastTimeSoundGroupPlayed.GetEnumerator();

            if (Instance._groupsToRemove == null) { // re-init for compile-time changes.
                Instance._groupsToRemove = new List<string>();
            }
            Instance._groupsToRemove.Clear();

            while (groups.MoveNext()) {
                var grp = groups.Current;
                if (!(grp.Value.LastTimePlayed + grp.Value.InactivePeriodSeconds < AudioUtil.Time)) {
                    continue;
                }

                RefillSoundGroupPool(grp.Key);
                Instance._groupsToRemove.Add(grp.Key);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < Instance._groupsToRemove.Count; i++) {
                Instance.LastTimeSoundGroupPlayed.Remove(Instance._groupsToRemove[i]);
            }
        }

        private static void PerformOcclusionFrequencyChanges() {
            if (!AmbientUtil.HasListenerFollower) { // this will make occlusion work when you don't have an Audio Listener in the Scene initially.
                AmbientUtil.InitListenerFollower();
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < Instance.VariationOcclusionFreqChanges.Count; i++) {
                var aFader = Instance.VariationOcclusionFreqChanges[i];
                if (!aFader.IsActive) {
                    continue;
                }

                var timeFractionElapsed = 1f - ((aFader.CompletionTime - AudioUtil.Time) / (aFader.CompletionTime - aFader.StartTime));

                timeFractionElapsed = Math.Min(timeFractionElapsed, 1f);
                timeFractionElapsed = Math.Max(timeFractionElapsed, 0f);

                var newFreq = aFader.StartFrequency + ((aFader.TargetFrequency - aFader.StartFrequency) * timeFractionElapsed);

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (aFader.TargetFrequency > aFader.StartFrequency) {
                    newFreq = Math.Min(newFreq, aFader.TargetFrequency);
                } else {
                    newFreq = Math.Max(newFreq, aFader.TargetFrequency);
                }

                aFader.ActingVariation.LowPassFilter.cutoffFrequency = newFreq;

                if (AudioUtil.Time < aFader.CompletionTime) {
                    continue;
                }

                aFader.IsActive = false;
            }
        }

        private void PerformBusFades() {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < BusFades.Count; i++) {
                BusFadeInfo aFader = BusFades[i];
                if (!aFader.IsActive) {
                    continue;
                }

                GroupBus aBus = aFader.ActingBus;
                if (aBus == null) {
                    aFader.IsActive = false;
                    continue;
                }

                var timeFractionElapsed = 1f - ((aFader.CompletionTime - AudioUtil.Time) / (aFader.CompletionTime - aFader.StartTime));

                timeFractionElapsed = Math.Min(timeFractionElapsed, 1f);
                timeFractionElapsed = Math.Max(timeFractionElapsed, 0f);

                var newVolume = aFader.StartVolume + ((aFader.TargetVolume - aFader.StartVolume) * timeFractionElapsed);

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (aFader.TargetVolume > aFader.StartVolume) {
                    newVolume = Math.Min(newVolume, aFader.TargetVolume);
                } else {
                    newVolume = Math.Max(newVolume, aFader.TargetVolume);
                }

                SetBusVolumeByName(aBus.busName, newVolume);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (AudioUtil.Time < aFader.CompletionTime) {
                    continue;
                }

                aFader.IsActive = false;

                if (stopZeroVolumeBuses && aFader.TargetVolume == 0f) {
                    StopBus(aFader.NameOfBus);
                } else if (aFader.WillStopGroupAfterFade) {
                    StopBus(aFader.NameOfBus);
                }

                if (aFader.WillResetVolumeAfterFade) {
                    SetBusVolumeByName(aBus.busName, aFader.StartVolume);
                }

                if (aFader.completionAction != null) {
                    aFader.completionAction();
                }
            }
        }

        private void PerformGroupFades() {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < GroupFades.Count; i++) {
                var aFader = GroupFades[i];
                if (!aFader.IsActive) {
                    continue;
                }

                var aGroup = aFader.ActingGroup;
                if (aGroup == null) {
                    aFader.IsActive = false;
                    continue;
                }

                var timeFractionElapsed = 1f - ((aFader.CompletionTime - AudioUtil.Time) / (aFader.CompletionTime - aFader.StartTime));

                timeFractionElapsed = Math.Min(timeFractionElapsed, 1f);
                timeFractionElapsed = Math.Max(timeFractionElapsed, 0f);

                var newVolume = aFader.StartVolume + ((aFader.TargetVolume - aFader.StartVolume) * timeFractionElapsed);

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (aFader.TargetVolume > aFader.StartVolume) {
                    newVolume = Math.Min(newVolume, aFader.TargetVolume);
                } else {
                    newVolume = Math.Max(newVolume, aFader.TargetVolume);
                }

                SetGroupVolume(aGroup.GameObjectName, newVolume);

                if (AudioUtil.Time < aFader.CompletionTime) {
                    continue;
                }

                aFader.IsActive = false;

                if (aFader.completionAction != null) {
                    aFader.completionAction();
                }

                if (stopZeroVolumeGroups && aFader.TargetVolume == 0f) {
                    StopAllOfSound(aFader.NameOfGroup);
                } else if (aFader.WillStopGroupAfterFade) {
                    StopAllOfSound(aFader.NameOfGroup);
                }

                if (aFader.WillResetVolumeAfterFade) {
                    SetGroupVolume(aGroup.GameObjectName, aFader.StartVolume);
                }
            }
        }

        /// <summary>
        /// Will trigger an Ambient Sound a frame later than it is enabled. This is to avoid it happening when not intended such as when placed on an object that is Instantiated disabled for pooling.
        /// </summary>
        public static void PerformDelayedAmbientTriggers() {
            if (AppIsShuttingDown) {
                return;
            }

            if (Instance.AmbientsToDelayedTrigger.Count == 0) {
                return;
            }

            Instance.AmbientsToTriggerNow.Clear();

            for (var i = 0; i < Instance.AmbientsToDelayedTrigger.Count; i++)
            {
                var anAmbient = Instance.AmbientsToDelayedTrigger[i];
                if (Time.frameCount >= anAmbient.frameToTrigger)
                {
                    Instance.AmbientsToTriggerNow.Add(anAmbient);
                }
            }

			if (Instance.AmbientsToTriggerNow.Count == 0) {
				return;
			}

			foreach (var ambient in Instance.AmbientsToTriggerNow) {
				if (ambient.ambient != null) {
					ambient.ambient.StartTrackers();
				}
				Instance.AmbientsToDelayedTrigger.Remove(ambient);
			}
        }


        private void PerformGroupPitchGlides() {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < GroupPitchGlides.Count; i++) {
                // ReSharper disable TooWideLocalVariableScope
                GroupPitchGlideInfo aGlider = GroupPitchGlides[i];
                if (!aGlider.IsActive) {
                    continue;
                }

                var aGroup = aGlider.ActingGroup;
                if (aGroup == null) {
                    aGlider.IsActive = false;
                    continue;
                }

                if (AudioUtil.Time < aGlider.CompletionTime) {
                    continue;
                }

                aGlider.IsActive = false;

                if (aGlider.completionAction != null) {
                    aGlider.completionAction();
                    aGlider.completionAction = null;
                }
            }

            GroupPitchGlides.RemoveAll(delegate (GroupPitchGlideInfo obj) {
                return obj.IsActive == false;
            });
        }

        private void PerformBusDucks() {
            return; // unfinished, come back to this.
            for (var i = 0; i < BusDucks.Count; i++) {
                var aDuck = BusDucks[i];
                if (!aDuck.IsActive) {
                    continue;
                }

                if (aDuck.BusesToDuck.Count == 0) {
                    aDuck.IsActive = false;
                    continue;
                }

                //Debug.Log("duck buses: " + aDuck.BusesToDuck.Count);
                //var aBus = GetBusIndex(aGlider.NameOfBus, true);
                //if (aBus < 0) {
                //    aGlider.IsActive = false;
                //    continue;
                //}

                //if (AudioUtil.Time < aGlider.CompletionTime) {
                //    continue;
                //}

                //aGlider.IsActive = false;
            }
        }

        private void PerformBusPitchGlides() {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < BusPitchGlides.Count; i++) {
                BusPitchGlideInfo aGlider = BusPitchGlides[i];
                if (!aGlider.IsActive) {
                    continue;
                }

                var aBus = GetBusIndex(aGlider.NameOfBus, true);
                if (aBus < 0) {
                    aGlider.IsActive = false;
                    continue;
                }

                if (AudioUtil.Time < aGlider.CompletionTime) {
                    continue;
                }

                aGlider.IsActive = false;

                if (aGlider.completionAction != null) {
                    aGlider.completionAction();
                    aGlider.completionAction = null;
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnApplicationQuit() {
            AppIsShuttingDown = true;
            // very important!! Dont' take this out, false debug info may show up when you stop the Player
        }
        /*! \endcond */

        #endregion

        #region Sound Playing / Stopping Methods

        /// <summary>
        /// This method allows you to play a sound in a Sound Group in the location of the Master Audio prefab. Returns bool indicating success (played) or not.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="isChaining"><b>Don't ever specify this</b> - used to control number of loops for Chained Loop Groups. MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <returns>boolean- true indicating that the sound was either played or scheduled, false otherwise.</returns>
        public static bool PlaySoundAndForget(string sType, float volumePercentage = 1f, float? pitch = null,
            float delaySoundTime = 0f, string variationName = null, double? timeToSchedulePlay = null, bool isChaining = false) {

            if (!SceneHasMasterAudio) {
                return false;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
                return false;
            }

            var psr = PlaySoundAtVolume(sType, volumePercentage, Vector3.zero, timeToSchedulePlay, pitch, null, variationName, false, delaySoundTime,
                false, false, isChaining);

            return PSRAsSuccessBool(psr);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group in the location of the Master Audio prefab. Returns a PlaySoundResult object.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="isChaining"><b>Don't ever specify this</b> - used to control number of loops for Chained Loop Groups. MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <param name="isSingleSubscribedPlay"><b>Don't ever specify this</b> - MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <returns>PlaySoundResult - this object can be used to read if the sound played or not and also gives access to the Variation object that was used.</returns>
        public static PlaySoundResult PlaySound(string sType, float volumePercentage = 1f, float? pitch = null,
            float delaySoundTime = 0f, string variationName = null,
            double? timeToSchedulePlay = null, bool isChaining = false, bool isSingleSubscribedPlay = false) {
            if (!SceneHasMasterAudio) {
                return failedResultDuringInit;
            }

            if (SoundsReady) {
                return PlaySoundAtVolume(sType, volumePercentage, Vector3.zero, timeToSchedulePlay, pitch, null, variationName, false,
                    delaySoundTime, false, true, isChaining, isSingleSubscribedPlay);
            }

            Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
            return failedResultDuringInit;
        }

        /// <summary>
		/// This method allows you to play a sound in a Sound Group from a specific Vector 3 position. Returns bool indicating success (played) or not.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourcePosition">The position you want the sound to eminate from. Required.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <returns>boolean- true indicating that the sound was either played or scheduled, false otherwise.</returns>
        public static bool PlaySound3DAtVector3AndForget(string sType, Vector3 sourcePosition,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null, double? timeToSchedulePlay = null) {
            if (!SceneHasMasterAudio) {
                return false;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
                return false;
            }

            var psr = PlaySoundAtVolume(sType, volumePercentage, sourcePosition, timeToSchedulePlay, pitch, null, variationName, false, delaySoundTime, true);

            return PSRAsSuccessBool(psr);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific Vector3 position. Returns a PlaySoundResult object.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourcePosition">The position you want the sound to eminate from. Required.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <returns>PlaySoundResult - this object can be used to read if the sound played or not and also gives access to the Variation object that was used.</returns>
        public static PlaySoundResult PlaySound3DAtVector3(string sType, Vector3 sourcePosition,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null, double? timeToSchedulePlay = null) {
            if (!SceneHasMasterAudio) {
                return failedResultDuringInit;
            }

            if (SoundsReady) {
                return PlaySoundAtVolume(sType, volumePercentage, sourcePosition, timeToSchedulePlay, pitch, null, variationName, false,
                    delaySoundTime, true, true);
            }

            Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
            return failedResultDuringInit;
        }

        /// <summary>
		/// This method allows you to play a sound in a Sound Group from a specific position - the position of a Transform you pass in. Returns bool indicating success (played) or not.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="isChaining"><b>Don't ever specify this</b> - used to control number of loops for Chained Loop Groups. MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <returns>boolean- true indicating that the sound was either played or scheduled, false otherwise.</returns>
        public static bool PlaySound3DAtTransformAndForget(string sType, Transform sourceTrans,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null, double? timeToSchedulePlay = null, bool isChaining = false) {

            if (!SceneHasMasterAudio) {
                return false;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
                return false;
            }

            var psr = PlaySoundAtVolume(sType, volumePercentage, Vector3.zero, timeToSchedulePlay, pitch, sourceTrans, variationName, false, delaySoundTime,
                false, false, isChaining);

            return PSRAsSuccessBool(psr);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - the position of a Transform you pass in.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="isChaining"><b>Don't ever specify this</b> - used to control number of loops for Chained Loop Groups. MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <param name="isSingleSubscribedPlay"><b>Don't ever specify this</b> - MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <returns>PlaySoundResult - this object can be used to read if the sound played or not and also gives access to the Variation object that was used.</returns>
        public static PlaySoundResult PlaySound3DAtTransform(string sType, Transform sourceTrans,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null,
            double? timeToSchedulePlay = null, bool isChaining = false, bool isSingleSubscribedPlay = false) {
            if (!SceneHasMasterAudio) {
                return failedResultDuringInit;
            }

            if (SoundsReady) {
                return PlaySoundAtVolume(sType, volumePercentage, Vector3.zero, timeToSchedulePlay, pitch, sourceTrans, variationName, false,
                    delaySoundTime, false, true, isChaining, isSingleSubscribedPlay);
            }

            Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
            return failedResultDuringInit;
        }

        /// <summary>
		/// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in. Returns bool indicating success (played) or not.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="isChaining"><b>Don't ever specify this</b> - used to control number of loops for Chained Loop Groups. MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <returns>boolean- true indicating that the sound was either played or scheduled, false otherwise.</returns>
        public static bool PlaySound3DFollowTransformAndForget(string sType, Transform sourceTrans,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null, double? timeToSchedulePlay = null, bool isChaining = false) {
            if (!SceneHasMasterAudio) {
                return false;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
                return false;
            }

            var psr = PlaySoundAtVolume(sType, volumePercentage, Vector3.zero, timeToSchedulePlay, pitch, sourceTrans, variationName, true,
                delaySoundTime, false, false, isChaining);
            return PSRAsSuccessBool(psr);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in, and it will follow the Transform if it moves. Returns a PlaySoundResult.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="isChaining"><b>Don't ever specify this</b> - used to control number of loops for Chained Loop Groups. MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <param name="isSingleSubscribedPlay"><b>Don't ever specify this</b> - MasterAudio will pass this parameter when it needs it. Never specify this param.</param>
        /// <returns>PlaySoundResult - this object can be used to read if the sound played or not and also gives access to the Variation object that was used.</returns>
        public static PlaySoundResult PlaySound3DFollowTransform(string sType, Transform sourceTrans,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null,
            double? timeToSchedulePlay = null, bool isChaining = false, bool isSingleSubscribedPlay = false) {
            if (!SceneHasMasterAudio) {
                return failedResultDuringInit;
            }

            if (SoundsReady) {
                return PlaySoundAtVolume(sType, volumePercentage, Vector3.zero, timeToSchedulePlay, pitch, sourceTrans, variationName, true,
                    delaySoundTime, false, true, isChaining, isSingleSubscribedPlay);
            }
            Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
            return failedResultDuringInit;
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from the location of Master Audio. This method will not return until the sound is finished (or cannot play) to continue execution. You need to call this with StartCoroutine. The sound will not be played looped, since that could cause a Coroutine that would never end.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="completedAction"><b>Optional</b> - Code to execute when the sound is finished.</param>
        public static IEnumerator PlaySoundAndWaitUntilFinished(string sType, float volumePercentage = 1f,
            // ReSharper disable once RedundantNameQualifier
            float? pitch = null, float delaySoundTime = 0f, string variationName = null, System.Action completedAction = null) {
            if (!SceneHasMasterAudio) {
                yield break;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
                yield break;
            }

            var sound = PlaySound(sType, volumePercentage, pitch, delaySoundTime, variationName, null, false, true);
            var done = false;

            if (sound == null || sound.ActingVariation == null) {
                yield break; // sound was "busy" or couldn't play for some reason.
            }
            sound.ActingVariation.SoundFinished += delegate {
                done = true;
            };

            while (!done) {
                yield return EndOfFrameDelay;
            }

            if (completedAction != null) {
                completedAction();
            }
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in. This method will not return until the sound is finished (or cannot play) to continue execution. You need to call this with StartCoroutine. The sound will not be played looped, since that could cause a Coroutine that would never end.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from. Pass null if you want to play the sound 2D.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="completedAction"><b>Optional</b> - Code to execute when the sound is finished.</param>
        public static IEnumerator PlaySound3DAtTransformAndWaitUntilFinished(string sType, Transform sourceTrans,
            // ReSharper disable once RedundantNameQualifier
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null, double? timeToSchedulePlay = null, System.Action completedAction = null) {
            if (!SceneHasMasterAudio) {
                yield break;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
                yield break;
            }

            var sound = PlaySound3DAtTransform(sType, sourceTrans, volumePercentage, pitch, delaySoundTime,
                variationName, timeToSchedulePlay, false, true);
            var done = false;

            if (sound == null || sound.ActingVariation == null) {
                yield break; // sound was "busy" or couldn't play for some reason.
            }
            sound.ActingVariation.SoundFinished += delegate {
                done = true;
            };

            while (!done) {
                yield return EndOfFrameDelay;
            }

            if (completedAction != null) {
                completedAction();
            }
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in, and it will follow the Transform if it moves. This method will not return until the sound is finished (or cannot play) to continue execution. You need to call this with StartCoroutine. The sound will not be played looped, since that could cause a Coroutine that would never end.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from. Pass null if you want to play the sound 2D.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation (or Clip id) by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="completedAction"><b>Optional</b> - Code to execute when the sound is finished.</param>
        public static IEnumerator PlaySound3DFollowTransformAndWaitUntilFinished(string sType, Transform sourceTrans,
            // ReSharper disable once RedundantNameQualifier
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null,
            double? timeToSchedulePlay = null, System.Action completedAction = null) {

            if (!SceneHasMasterAudio) {
                yield break;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot play: " + sType);
                yield break;
            }

            var sound = PlaySound3DFollowTransform(sType, sourceTrans, volumePercentage, pitch, delaySoundTime,
                variationName, timeToSchedulePlay, false, true);
            var done = false;

            if (sound == null || sound.ActingVariation == null) {
                yield break; // sound was "busy" or couldn't play for some reason.
            }
            sound.ActingVariation.SoundFinished += delegate {
                done = true;
            };

            while (!done) {
                yield return EndOfFrameDelay;
            }

            if (completedAction != null) {
                completedAction();
            }
        }

        /*! \cond PRIVATE */
        /// <summary>
        /// This method is used to convert a PSR to a bool for "AndForget" methods
        /// </summary>
        /// <returns><c>true</c>, if as success bool was PSRed, <c>false</c> otherwise.</returns>
        /// <param name="psr">Psr.</param>
        public static bool PSRAsSuccessBool(PlaySoundResult psr) {
            return psr != null && (psr.SoundPlayed || psr.SoundScheduled);
        }
        /*! \endcond */

        // ReSharper disable once FunctionComplexityOverflow
        private static PlaySoundResult PlaySoundAtVolume(string sType,
            float volumePercentage,
            Vector3 sourcePosition,
            double? timeToSchedulePlay,
            float? pitch = null,
            Transform sourceTrans = null,
            string variationName = null,
            bool attachToSource = false,
            float delaySoundTime = 0f,
            bool useVector3 = false,
            bool makePlaySoundResult = false,
            bool isChaining = false,
            bool isSingleSubscribedPlay = false,
            bool triggeredAsChildGroup = false) {

            if (!IsSoundGroupValidAndReady(sType, sourceTrans))
            {
                return null;
            }

            var group = Instance.AudioSourcesBySoundType[sType];
            var maGroup = group.Group;
            var sources = group.Sources;
            var loggingEnabledForGrp = LoggingEnabledForGroup(maGroup);

            if (!SoundGroupHasVariations(sType, sources, loggingEnabledForGrp))
            {
                return null;
            }

            group.PlayedForWarming = IsWarming;

            LogIfSilentPlay(sType, loggingEnabledForGrp, maGroup);

            if (IsReplayLimited(sType, maGroup, @group, loggingEnabledForGrp))
            {
                return null;
            }

            var isNonSpecific = string.IsNullOrEmpty(variationName);
            AudioInfo randomSource = null;

            if (IsGroupPolyphonyLimited(maGroup, @group))
            {
                randomSource = FindRetriggerableVariationInGroup(variationName, isNonSpecific, sources, maGroup);

                if (randomSource == null) 
                {
                    if (loggingEnabledForGrp || LogOutOfVoices)
                    {
                        LogMessage("Polyphony limit of group: " + @group.Group.GameObjectName +
                                    " exceeded and no playing Variation is usable for Retrigger Limit Mode. Will not play this sound for this instance.");
                    }

                    return null;
                }
            }

            var groupBus = group.Group.BusForGroup;
            SoundGroupVariation busVoiceToStop = null;

            if (IsBusVoiceLimited(groupBus))
            {
                if (!CanStopLimitedBusVoice(groupBus, loggingEnabledForGrp, @group))
                {
                    return null;
                }

                busVoiceToStop = FindBusVoiceToStop(groupBus, group.Group);
                if (busVoiceToStop == null)
                {
                    return null;
                }
            }

            var isSingleVarLoop = false;

            randomSource = UseOnlyVariationIfOnlyOne(sType, sources, loggingEnabledForGrp, randomSource, maGroup, ref isSingleVarLoop);

            List<int> choices = null;
            int? randomIndex = null;
            List<int> otherChoices = null;

            var pickedChoice = -1;
            var canUseBusVoiceToStop = busVoiceToStop != null && Instance.stopOldestBusFadeTime == 0;

            if (!CanFindVariationToPlay(sType, variationName, isNonSpecific, canUseBusVoiceToStop, sources, loggingEnabledForGrp,
                busVoiceToStop, ref randomSource, ref choices, ref randomIndex, ref pickedChoice, ref otherChoices))
            {
                return null;
            }

            if (!VariationIsUsable(randomSource))
            {
                return null;
            }

            if (IsNoClipSilentPlay(sType, volumePercentage, pitch, sourceTrans, attachToSource, isChaining,
                randomSource, loggingEnabledForGrp, @group, isNonSpecific, randomIndex, choices, pickedChoice))
            {
                return null;
            }

            if (!VariationPassesProbabilityToPlayCheck(sType, volumePercentage, pitch, sourceTrans, attachToSource,
                isChaining, randomSource, loggingEnabledForGrp, @group, isNonSpecific, randomIndex, choices,
                pickedChoice))
            {
                return null;
            }

            if (IsActorTooFarAwayToPlay(sType, sourceTrans, @group, randomSource, loggingEnabledForGrp))
            {
                return null;
            }

            if (!CanPlayDialogBasedOnImportanceOrIsNotDialog(sType, @group, loggingEnabledForGrp, randomSource))
            {
                return null;
            }

            var forgetSoundPlayedOrScheduled = false;

            var hasRefilledPool = false;
            bool soundSuccess;

            var playedState = TryPlayVariationOrOtherMatches(sType, volumePercentage, sourcePosition,
                timeToSchedulePlay, pitch, sourceTrans, attachToSource, delaySoundTime, useVector3, makePlaySoundResult,
                isChaining, isSingleSubscribedPlay, randomSource, busVoiceToStop, groupBus, canUseBusVoiceToStop,
                forgetSoundPlayedOrScheduled, @group, isNonSpecific, randomIndex, choices, pickedChoice,
                loggingEnabledForGrp, isSingleVarLoop, otherChoices, hasRefilledPool, sources, out soundSuccess);

            if (!soundSuccess) {
                if (loggingEnabledForGrp || LogOutOfVoices) {
                    if (isNonSpecific) {
                        LogMessage("All " + sources.Count + " children of " + sType +
                                   " were busy. Will not play this sound for this instance. If you need more voices, increase the 'Voices / Weight' field on the Variation(s) in your Sound Group.");
                    } 
                    else
                    { 
                        LogMessage("Child '" + randomSource.Variation.GameObjectName + "' of " + sType +
                                   " was busy. Will not play this sound for this instance. If you need more voices, increase the 'Voices / Weight' field on the Variation(s) in your Sound Group.");
                    }
                }

                return playedState;
            }

            SetLastPlayed(@group); // moved here because this means it played for sure.

            // ReSharper disable once InvertIf
            if (!triggeredAsChildGroup && !IsWarming) {
                switch (@group.Group.linkedStartGroupSelectionType) {
                    case LinkedGroupSelectionType.All:
                        // ReSharper disable once ForCanBeConvertedToForeach
                        for (var i = 0; i < @group.Group.childSoundGroups.Count; i++) {
                            var childGrpName = @group.Group.childSoundGroups[i];

                            PlaySoundAtVolume(childGrpName, volumePercentage, sourcePosition, timeToSchedulePlay, pitch, sourceTrans, null,
                                attachToSource, delaySoundTime, useVector3, false, false, false, true);
                        }
                        break;
                    case LinkedGroupSelectionType.OneAtRandom:
                        var rndIndex = UnityEngine.Random.Range(0, @group.Group.childSoundGroups.Count);
                        var childGroupName = @group.Group.childSoundGroups[rndIndex];

                        PlaySoundAtVolume(childGroupName, volumePercentage, sourcePosition, timeToSchedulePlay, pitch, sourceTrans, null,
                            attachToSource, delaySoundTime, useVector3, false, false, false, true);
                        break;
                }
            }

            if (@group.Group.soundPlayedEventActive) {
                FireCustomEvent(@group.Group.soundPlayedCustomEvent, Instance._trans);
            }

            if (!makePlaySoundResult)
            {
                return AndForgetSuccessResult;
            }

            return playedState;
        }

        private static PlaySoundResult TryPlayVariationOrOtherMatches(string sType, float volumePercentage,
            Vector3 sourcePosition, double? timeToSchedulePlay, float? pitch, Transform sourceTrans, bool attachToSource,
            float delaySoundTime, bool useVector3, bool makePlaySoundResult, bool isChaining, bool isSingleSubscribedPlay,
            AudioInfo randomSource, SoundGroupVariation busVoiceToStop, GroupBus groupBus, bool canUseBusVoiceToStop, bool forgetSoundPlayedOrScheduled,
            AudioGroupInfo @group, bool isNonSpecific, int? randomIndex, List<int> choices, int pickedChoice,
            bool loggingEnabledForGrp, bool isSingleVarLoop, List<int> otherChoices, bool hasRefilledPool, List<AudioInfo> sources,
            out bool soundSuccess)
        {
            bool isFinalExhaustivePlay;
            PlaySoundResult playedState;
            bool makePsRsuccess;
            bool doNotMakePsRsuccess;

            do
            {
                isFinalExhaustivePlay = false;

                playedState = PlaySoundIfAvailable(randomSource, sourcePosition, volumePercentage, busVoiceToStop, groupBus, canUseBusVoiceToStop,
                    ref forgetSoundPlayedOrScheduled, pitch, @group, sourceTrans, attachToSource, delaySoundTime,
                    useVector3, makePlaySoundResult, timeToSchedulePlay, isChaining, isSingleSubscribedPlay);

                makePsRsuccess = makePlaySoundResult &&
                                 (playedState != null && (playedState.SoundPlayed || playedState.SoundScheduled));

                doNotMakePsRsuccess = !makePlaySoundResult && forgetSoundPlayedOrScheduled;

                soundSuccess = makePsRsuccess || doNotMakePsRsuccess;

                if (soundSuccess)
                {
                    if (!IsWarming)
                    {
                        RemoveClipAndRefillIfEmpty(@group, isNonSpecific, randomIndex, choices, sType, pickedChoice,
                            loggingEnabledForGrp, isSingleVarLoop);
                    }

                    break;
                }

                if (isNonSpecific)
                {
                    // try the other ones
                    if (otherChoices == null)
                    {
                        continue;
                    }

                    if (otherChoices.Count <= 0)
                    {
                        if (hasRefilledPool)
                        {
                            continue;
                        }

                        // try and fix the "only remaining choices are already playing, then "give up" problem.
                        RefillSoundGroupPool(sType);
                        hasRefilledPool = true;

                        // get full range back
                        otherChoices.Clear();
                        otherChoices.AddRange(choices);
                    }

                    randomSource = sources[otherChoices[0]];

                    if (randomSource.Variation == null)
                    {
                        var variation = randomSource.Source.GetComponent<SoundGroupVariation>();

                        if (variation == null)
                        {
                            break; // couldn't repair
                        }

                        randomSource.Variation = variation;
                    }

                    if (loggingEnabledForGrp)
                    {
                        LogMessage("Child was busy. Cueing child named '" + randomSource.Variation.GameObjectName + "' of " +
                                   sType);
                    }

                    otherChoices.RemoveAt(0);
                    if (hasRefilledPool && otherChoices.Count == 0)
                    {
                        isFinalExhaustivePlay = true;
                    }
                }
                else
                {
                    if (loggingEnabledForGrp)
                    {
                        LogMessage("Child was busy. Since you requested a named Variation '" + randomSource.Variation.GameObjectName + "', no others to try. Aborting.");
                    }

                    if (otherChoices != null)
                    {
                        otherChoices.Clear();
                        break;
                    }
                }
            } while (otherChoices != null && (otherChoices.Count > 0 || !hasRefilledPool || isFinalExhaustivePlay));

            // repeat until you've either played the sound or exhausted all possibilities.
            return playedState;
        }

        private static bool CanPlayDialogBasedOnImportanceOrIsNotDialog(string sType, AudioGroupInfo @group, bool loggingEnabledForGrp, AudioInfo randomSource)
        {
            if (@group.Group.curVariationMode != MasterAudioGroup.VariationMode.Dialog)
            {
                return true;
            }

            var activeAudioInfo = @group.Sources.Find(delegate(AudioInfo info) { return info.Variation.IsPlaying; });
            if (activeAudioInfo == null)
            {
                return true;
            }

            // check for importance
            var activeVar = activeAudioInfo.Variation;
            if (activeVar.isUninterruptible)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage(string.Format(
                        "Already playing Child named '{0}' of '{1}' is marked as Uninterruptible, so not playing.",
                        randomSource.Variation.GameObjectName, sType));
                    return false;
                }
            }
            else if (randomSource.Variation.importance < activeVar.importance)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage(string.Format(
                        "Already playing Child named '{0}' of '{1} has higher Importance ({2}) than Child '{3}', so not playing.",
                        activeVar.GameObjectName, sType, randomSource.Variation.importance, randomSource.Variation.GameObjectName));
                    return false;
                }
            }
            else
            {
                if (@group.Group.useDialogFadeOut)
                {
                    FadeOutAllOfSound(@group.Group.GameObjectName, @group.Group.dialogFadeOutTime);
                }
                else
                {
                    StopAllOfSound(@group.Group.GameObjectName);
                }
            }

            return true;
        }

        private static AudioInfo UseOnlyVariationIfOnlyOne(string sType, List<AudioInfo> sources, bool loggingEnabledForGrp, AudioInfo randomSource,
            MasterAudioGroup maGroup, ref bool isSingleVarLoop)
        {
            if (sources.Count != 1)
            {
                return randomSource;
            }

            if (loggingEnabledForGrp)
            {
                LogMessage("Cueing only child of " + sType);
            }

            if (randomSource == null)
            {
                randomSource = sources[0];
            }

            if (maGroup.curVariationMode == MasterAudioGroup.VariationMode.LoopedChain)
            {
                isSingleVarLoop = true;
            }

            return randomSource;
        }

        private static bool IsActorTooFarAwayToPlay(string sType, Transform sourceTrans, AudioGroupInfo @group,
            AudioInfo randomSource, bool loggingEnabledForGrp)
        {
            if (sourceTrans != null && ListenerTrans != null && @group.Group.spatialBlend > 0 &&
                @group.Group.GroupPlayType == GroupPlayType.WhenActorInAudibleRange)
            {
                var maxRange = randomSource.Variation.VarAudio.maxDistance;
				var distanceFromListener = (ListenerTrans.position - sourceTrans.position).magnitude;
                if (distanceFromListener < maxRange)
                {
                    return false;
                }

                if (loggingEnabledForGrp)
                {
                    LogMessage(string.Format("Child named '{0}' of '{1}' is too far away to be heard{2}, so not playing.",
                        randomSource.Variation.GameObjectName, sType,
                        sourceTrans.name == null ? "" : " with Actor '" + sourceTrans.name + "'"));
                }

                return true;
            }

            return false;
        }

        private static bool VariationPassesProbabilityToPlayCheck(string sType, float volumePercentage, float? pitch,
            Transform sourceTrans, bool attachToSource, bool isChaining, AudioInfo randomSource, bool loggingEnabledForGrp,
            AudioGroupInfo @group, bool isNonSpecific, int? randomIndex, List<int> choices, int pickedChoice)
        {
            if (randomSource.Variation.probabilityToPlay >= 100)
            {
                return true;
            }

            if (UnityEngine.Random.Range(0, 100) >= randomSource.Variation.probabilityToPlay)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage(string.Format(
                        "Child named '{0}' of {1} failed its Random number check for 'Probability to Play' to it so nothing will be played this time.",
                        randomSource.Variation.GameObjectName, sType));
                }

#if UNITY_EDITOR
                if (RemoveUnplayedVariationDueToProbability)
                {
                    RemoveClipAndRefillIfEmpty(@group, isNonSpecific, randomIndex, choices, sType, pickedChoice,
                        loggingEnabledForGrp, false);
                }
#endif

                // still need to chain regardless, or it will break the Looped Chain
                MaybeChainNextVar(isChaining, randomSource.Variation, volumePercentage, pitch, sourceTrans, attachToSource);

                return false;
                // nothing played, it's silent. Don't take up voices because that could silence a Dialog Group that already has a real sound playing.
            }

            return true;
        }

        private static bool IsNoClipSilentPlay(string sType, float volumePercentage, float? pitch, Transform sourceTrans,
            bool attachToSource, bool isChaining, AudioInfo randomSource, bool loggingEnabledForGrp, AudioGroupInfo @group,
            bool isNonSpecific, int? randomIndex, List<int> choices, int pickedChoice)
        {
            if (randomSource.Variation.audLocation == AudioLocation.Clip && randomSource.Variation.VarAudio.clip == null)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage(
                        string.Format("Child named '{0}' of {1} has no audio assigned to it so nothing will be played.",
                            randomSource.Variation.GameObjectName,
                            sType));
                }

                RemoveClipAndRefillIfEmpty(@group, isNonSpecific, randomIndex, choices, sType, pickedChoice,
                    loggingEnabledForGrp, false);
                MaybeChainNextVar(isChaining, randomSource.Variation, volumePercentage, pitch, sourceTrans, attachToSource);

                return true;
                // nothing played, it's silent. Don't take up voices because that could silence a Dialog Group that already has a real sound playing.
            }

            return false;
        }

        private static bool VariationIsUsable(AudioInfo randomSource)
        {
            if (randomSource.Variation != null)
            {
                return true;
            }

            if (AppIsShuttingDown || randomSource.Source == null)
            {
                return false;
            }

            // re-fetch for deleted and undeleted Groups
            var variation = randomSource.Source.GetComponent<SoundGroupVariation>();

            if (variation == null)
            {
                return false;
            }

            randomSource.Variation = variation;

            return true;
        }

        private static bool CanFindVariationToPlay(string sType, string variationName, bool isNonSpecific, bool canUseBusVoiceToStop, List<AudioInfo> sources,
            bool loggingEnabledForGrp, SoundGroupVariation busVoiceToStop, ref AudioInfo randomSource, ref List<int> choices,
            ref int? randomIndex, ref int pickedChoice, ref List<int> otherChoices)
        {
            if (randomSource != null)
            {
                return true;
            }

            // we must get a non-busy random source!
            if (!Instance._randomizer.ContainsKey(sType))
            {
                Debug.Log("Sound Group '" + sType + "' has no active Variations.");
                return false;
            }

            if (isNonSpecific)
            {
                choices = Instance._randomizer[sType];

                randomIndex = 0;
                pickedChoice = choices[randomIndex.Value];
                randomSource = sources[pickedChoice];

                // fill list with other random sources not used yet in case the first is busy.
                otherChoices = Instance._randomizerLeftovers[sType];
                otherChoices.Remove(pickedChoice);

                if (loggingEnabledForGrp)
                {
                    LogMessage(string.Format("Cueing child {0} of {1}",
                        choices[randomIndex.Value],
                        sType));
                }

                return true;
            }

            // find source by name
            var matchesFound = 0;

            var choiceMatches = Instance._nonRandomChoices[sType];
            choiceMatches.Clear();

            for (var i = 0; i < sources.Count; i++) {
                var aSource = sources[i];

                if (!string.IsNullOrEmpty(aSource.Variation.clipAlias) && aSource.Variation.clipAlias == variationName) {
                    // name match
                } else if (aSource.Variation.GameObjectName == variationName) {
                    // name match
                } else if (aSource.Variation.VarAudio.name == variationName) { // for the odd case when GameObjectName is set to early to (clone)
                    // name match 
                    aSource.Variation.GameObjectName = variationName; // fix it for next time
                } else {
                    continue;
                }

                matchesFound++;
                if (aSource.Variation.IsAvailableToPlay || (canUseBusVoiceToStop && aSource.Variation == busVoiceToStop)) {
                    choiceMatches.Add(i);
                }
            }

            if (matchesFound == 0)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage("Can't find variation '" + variationName + "' of " + sType);
                }

                return false;
            } 
            
            if (choiceMatches.Count == 0) {
                if (loggingEnabledForGrp || LogOutOfVoices)
                {
                    LogMessage("Can't find non-busy variation '" + variationName + "' of " + sType);
                }

                return false;
            }

            if (choiceMatches.Count == 1)
            {
                randomIndex = 0;
            }
            else
            {
                randomIndex = Random.Range(0, choiceMatches.Count);
            }

            pickedChoice = choiceMatches[randomIndex.Value];
            randomSource = sources[pickedChoice];
            choiceMatches.Remove(pickedChoice);
            otherChoices = choiceMatches;

            if (loggingEnabledForGrp)
            {
                LogMessage(string.Format("Cueing child named '{0}' of {1}",
                    randomSource.Variation.GameObjectName,
                    sType));
            }

            return true;
        }

        private static SoundGroupVariation FindBusVoiceToStop(GroupBus groupBus, MasterAudioGroup group)
        {
            switch (groupBus.busVoiceLimitExceededMode)
            {
                case BusVoiceLimitExceededMode.StopOldestSound:
                    return FindOldestSoundOnBus(groupBus);
                case BusVoiceLimitExceededMode.StopFarthestSound:
                    return FindFarthestSoundOnBus(groupBus);
                case BusVoiceLimitExceededMode.StopLeastImportantSound:
                    var leastImportantVar = FindLeastImportantSoundOnBus(groupBus, group);
                    if (leastImportantVar == null)
                    {
                        if (group.LoggingEnabledForGroup || LogOutOfVoices)
                        {
                            LogMessage("Could not find a Less Important and Interruptible voice to stop on Bus '" + groupBus.busName + "', and Bus voice limit has been reached. Cannot play the sound: "
                                       + group.GameObjectName + " with Importance " + group.importance + ".");
                        }
                    }

                    return leastImportantVar;
                default:
                    return null;
            }
        }

        private static bool CanStopLimitedBusVoice(GroupBus groupBus, bool loggingEnabledForGrp, AudioGroupInfo @group)
        {
            switch (groupBus.busVoiceLimitExceededMode)
            {
                case BusVoiceLimitExceededMode.DoNotPlayNewSound:
                    if (loggingEnabledForGrp || LogOutOfVoices)
                    {
                        LogMessage("Bus voice limit has been reached. Cannot play the sound: " +
                                   @group.Group.GameObjectName +
                                   " until one voice has stopped playing. You can turn on the 'Stop Oldest Sound' or 'Stop Farthest Sound' option for the bus to change ");
                    }

                    return false;
            }

            return true;
        }

        private static bool IsBusVoiceLimited(GroupBus groupBus)
        {
            if (groupBus != null && groupBus.BusVoiceLimitReached)
            {
                return true;
            }

            return false;
        }

        private static AudioInfo FindRetriggerableVariationInGroup(string variationName, bool isNonSpecific, List<AudioInfo> sources,
            MasterAudioGroup maGroup)
        {
            AudioInfo randomSource = null;
            
            if (isNonSpecific)
            {
                randomSource = sources.Find(delegate(AudioInfo info)
                {
                    if (!info.Source.isPlaying || !info.Variation.ClipIsLoaded)
                    {
                        return false;
                    }

                    var playedPercentage = AudioUtil.GetAudioPlayedPercentage(info.Source);

                    return playedPercentage >= maGroup.retriggerPercentage;
                });
            }
            else
            {
                randomSource = sources.Find(delegate(AudioInfo info)
                {
                    if (!info.Source.isPlaying || !info.Variation.ClipIsLoaded ||
                        info.Variation.GameObjectName != variationName)
                    {
                        return false;
                    }

                    var playedPercentage = AudioUtil.GetAudioPlayedPercentage(info.Source);

                    return playedPercentage >= maGroup.retriggerPercentage;
                });
            }

            if (randomSource != null)
            {
                if (maGroup.LoggingEnabledForGroup) {
                    LogMessage("Cueing Retrigger of child named '" + randomSource.Variation.GameObjectName + "' of " + maGroup.GameObjectName);
                }
            }

            return randomSource;
        }

        private static bool IsGroupPolyphonyLimited(MasterAudioGroup maGroup, AudioGroupInfo @group)
        {
            if (maGroup.curVariationMode != MasterAudioGroup.VariationMode.Normal)
            {
                return false;
            }

            if (!@group.Group.limitPolyphony)
            {
                return false;
            }

            if (@group.Group.ActiveVoices < @group.Group.voiceLimitCount)
            {
                return false;
            }

            return true;
        }

        private static bool IsReplayLimited(string sType, MasterAudioGroup maGroup, AudioGroupInfo @group,
            bool loggingEnabledForGrp)
        {
            if (maGroup.curVariationMode != MasterAudioGroup.VariationMode.Normal)
            {
                return false;
            }

            switch (maGroup.limitMode)
            {
                case MasterAudioGroup.LimitMode.TimeBased:
                    if (maGroup.minimumTimeBetween > 0)
                    {
                        if (Time.realtimeSinceStartup < (@group.LastTimePlayed + maGroup.minimumTimeBetween))
                        {
                            if (loggingEnabledForGrp)
                            {
                                LogMessage("MasterAudio skipped playing sound: " + sType +
                                           " due to Group's Min Seconds Between setting.");
                            }

                            return true;
                        }
                    }

                    break;
                case MasterAudioGroup.LimitMode.FrameBased:
                    if (Time.frameCount - @group.LastFramePlayed < maGroup.limitPerXFrames)
                    {
                        if (loggingEnabledForGrp)
                        {
                            LogMessage("Master Audio skipped playing sound: " + sType +
                                       " due to Group's Per Frame Limit.");
                        }

                        return true;
                    }

                    break;
                case MasterAudioGroup.LimitMode.None:
                    break;
            }

            return false;
        }

        private static void LogIfSilentPlay(string sType, bool loggingEnabledForGrp, MasterAudioGroup maGroup)
        {
            if (Instance.mixerMuted)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage("MasterAudio playing sound: " + sType + " silently because the Mixer is muted.");
                }
            }
            else if (maGroup.isMuted)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage("MasterAudio playing sound: " + sType + " silently because the Group is muted.");
                }
            }

            if (Instance.SoloedGroups.Count > 0 && !Instance.SoloedGroups.Contains(maGroup))
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage("MasterAudio playing sound: " + sType +
                               " silently because there are one or more Groups soloed. This one is not.");
                }
            }
        }

        private static bool SoundGroupHasVariations(string sType, List<AudioInfo> sources, bool loggingEnabledForGrp)
        {
            if (sources.Count == 0)
            {
                if (loggingEnabledForGrp)
                {
                    LogMessage("Sound Group '" + sType + "' has no active Variations.");
                }

                return false;
            }

            return true;
        }

        private static bool IsSoundGroupValidAndReady(string sType, Transform sourceTrans)
        {
            if (!SceneHasMasterAudio)
            {
                // No MA
                return false;
            }

            if (!SoundsReady || sType == string.Empty || sType == NoGroupName)
            {
                return false;
            }

            if (sType == VideoPlayerSoundGroupName)
            {
                LogError("You cannot play sounds from the specially named Sound Group for Video Players.");
                return false;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType))
            {
                var msg = "MasterAudio could not find sound: " + sType +
                          ". If your Scene just changed, this could happen when an OnDisable or OnInvisible event sound happened to a per-scene sound, which is expected.";
                if (sourceTrans != null)
                {
                    msg += " Triggered by prefab: " + (sourceTrans.name);
                }

                LogWarning(msg);
                return false;
            }

            return true;
        }

        private static void MaybeChainNextVar(bool isChaining, SoundGroupVariation variation, float volumePercentage, float? pitch, Transform sourceTrans, bool attachToSource) {
            if (!isChaining) {
                return;
            }

            variation.DoNextChain(volumePercentage, pitch, sourceTrans, attachToSource);
        }

        private static void SetLastPlayed(AudioGroupInfo grp) {
            grp.LastTimePlayed = AudioUtil.Time;
            grp.LastFramePlayed = AudioUtil.FrameCount;
        }

        private static void RemoveClipAndRefillIfEmpty(AudioGroupInfo grp, bool isNonSpecific, int? randomIndex,
            List<int> choices, string sType, int pickedChoice, bool loggingEnabledForGrp, bool isSingleVarLoop) {

            if (isSingleVarLoop) {
                grp.Group.ChainLoopCount++; // this doesn't call Refill Sound Group where this normally occurs so this part needs to happen separately
                return;
            }

            if (isNonSpecific && randomIndex.HasValue) {
                // only if successfully played!
                choices.RemoveAt(randomIndex.Value);
                Instance._clipsPlayedBySoundTypeOldestFirst[sType].Add(pickedChoice);

                if (choices.Count == 0) {
                    if (loggingEnabledForGrp) {
                        LogMessage("Refilling Variation pool: " + sType);
                    }

                    RefillSoundGroupPool(sType);
                }
            }

            if (grp.Group.curVariationSequence == MasterAudioGroup.VariationSequence.TopToBottom &&
                grp.Group.useInactivePeriodPoolRefill) {
                UpdateRefillTime(sType, grp.Group.inactivePeriodSeconds);
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static PlaySoundResult PlaySoundIfAvailable(AudioInfo info,
            Vector3 sourcePosition,
            float volumePercentage,
            SoundGroupVariation busVoiceToStop,
            GroupBus groupBus,
            bool canUseBusVoiceToStop,
            ref bool forgetSoundPlayed,
            float? pitch = null,
            AudioGroupInfo audioGroup = null,
            Transform sourceTrans = null,
            bool attachToSource = false,
            float delaySoundTime = 0f,
            bool useVector3 = false,
            bool makePlaySoundResult = false,
            double? timeToSchedulePlay = null,
            bool isChaining = false,
            bool isSingleSubscribedPlay = false) {

            if (info.Source == null) {
                // this avoids false errors when stopping the game (from "became invisible" event callers)
                return null;
            }

            if (info.Variation.LoadStatus == VariationLoadStatus.Loading) {
                return null;
            }

            // ReSharper disable once PossibleNullReferenceException
            var maGroup = audioGroup.Group;

            var isRetriggerPlay = false;

            var isBusVoiceToStop = canUseBusVoiceToStop && info.Variation == busVoiceToStop;

            // retrigger check.
            if (!isBusVoiceToStop 
                && maGroup.curVariationMode == MasterAudioGroup.VariationMode.Normal 
                && info.Source.isPlaying 
                && info.Variation.ClipIsLoaded) {
                
                var playedPercentage = AudioUtil.GetAudioPlayedPercentage(info.Source);
                var retriggerPercent = maGroup.retriggerPercentage;

                if (playedPercentage < retriggerPercent) {
                    return null; // wait for this to stop playing or play further.
                }

                isRetriggerPlay = true;
            }

            if (!IsWarming)
            {
                info.Variation.Stop(false, true);
            }

            info.Variation.ObjectToFollow = null;

            var shouldUseClipAgePriority = Instance.prioritizeOnDistance && (Instance.useClipAgePriority || info.Variation.ParentGroup.useClipAgePriority);

            if (useVector3) {
                info.Source.transform.position = sourcePosition;
                if (Instance.prioritizeOnDistance) {
                    AudioPrioritizer.Set3DPriority(info.Variation, shouldUseClipAgePriority);
                }
            } else if (sourceTrans != null) {
                if (attachToSource) {
                    info.Variation.ObjectToFollow = sourceTrans;
                } else {
                    info.Source.transform.position = sourceTrans.position;
                    info.Variation.ObjectToTriggerFrom = sourceTrans;
                }

                if (Instance.prioritizeOnDistance) {
                    AudioPrioritizer.Set3DPriority(info.Variation, shouldUseClipAgePriority);
                }
            } else {
                // "2d manner" - from Master Audio location
                if (Instance.prioritizeOnDistance) {
                    AudioPrioritizer.Set2DSoundPriority(info.Source);
                }
                info.Source.transform.localPosition = Vector3.zero;
                // put it back in MA prefab position after being detached.
            }

            var groupVolume = maGroup.groupMasterVolume;
            var busVolume = GetBusVolume(maGroup);

            var varVol = info.OriginalVolume;

            var randomVol = 0f;
            if (info.Variation.useRandomVolume) {
                // random volume
                randomVol = UnityEngine.Random.Range(info.Variation.randomVolumeMin, info.Variation.randomVolumeMax);

                switch (info.Variation.randomVolumeMode) {
                    case SoundGroupVariation.RandomVolumeMode.AddToClipVolume:
                        varVol += randomVol;
                        break;
                    case SoundGroupVariation.RandomVolumeMode.IgnoreClipVolume:
                        varVol = randomVol;
                        break;
                }
            }

            var calcVolume = varVol * groupVolume * busVolume * Instance._masterAudioVolume;

            // set volume to percentage.
            var volume = calcVolume * volumePercentage;
            var targetVolume = volume;

            info.Source.volume = targetVolume;

            // save these for on the fly adjustments afterward
            info.LastPercentageVolume = volumePercentage;
            info.LastRandomVolume = randomVol;

            // ReSharper disable once JoinDeclarationAndInitializer
            bool isActive = info.Variation.GameObj.activeInHierarchy;

            if (!isActive) {
                DTMonoHelper.SetActive(info.Variation.GameObj, true); // enable it so it can play.
                isActive = info.Variation.GameObj.activeInHierarchy;
                if (!isActive) { // couldn't enable. Something wrong with it, won't play.
                    return null;
                }
                info.Variation.DisableUpdater(); // turn off the updater unless it's needed. Variation will take care of enabling it if so.
            }

            PlaySoundResult result = null;

            if (makePlaySoundResult) {
                result = new PlaySoundResult { ActingVariation = info.Variation };

                if (delaySoundTime > 0f) {
                    result.SoundScheduled = true;
                } else {
                    result.SoundPlayed = true;
                }
            } else {
                forgetSoundPlayed = true;
            }

            var soundType = maGroup.GameObjectName;
            var isChainLoop = maGroup.curVariationMode == MasterAudioGroup.VariationMode.LoopedChain;

            if (isChainLoop) {
                if (!isChaining) {
                    maGroup.ChainLoopCount = 0;
                }

                var objFollow = info.Variation.ObjectToFollow;

                // make sure there isn't 2 chains going, ever!
                if (maGroup.ActiveVoices > 0 && !isChaining) {
                    StopAllOfSound(soundType);
                }

                // restore this because it is lost from the Stop above;
                info.Variation.ObjectToFollow = objFollow;
            }

            if (!isRetriggerPlay)
            {
                FadeOldestOrFarthestBusVoice(busVoiceToStop, groupBus);
            }

            info.Variation.Play(pitch, targetVolume, soundType, volumePercentage, calcVolume, pitch, sourceTrans,
                attachToSource, delaySoundTime, timeToSchedulePlay, isChaining, isSingleSubscribedPlay);

            // ReSharper disable once InvertIf
            if (Instance._isStoppingMultiple) {
                Instance.VariationsStartedDuringMultiStop.Add(info.Variation);
            }

            return result;
        }

        private static void FadeOldestOrFarthestBusVoice(SoundGroupVariation busVoiceToStop, GroupBus groupBus)
        {
            if (groupBus == null || busVoiceToStop == null)
            {
                return;
            }

            switch (groupBus.busVoiceLimitExceededMode)
            {
                case BusVoiceLimitExceededMode.StopFarthestSound:
                case BusVoiceLimitExceededMode.StopOldestSound:
                case BusVoiceLimitExceededMode.StopLeastImportantSound:
                    busVoiceToStop.FadeOutNowAndStop(Instance.stopOldestBusFadeTime);
                    break;
            }
        }

        /*! \cond PRIVATE */
        public static void EndDucking(SoundGroupVariationUpdater actorUpdater)
        {
            var pcs = PlaylistController.Instances;
            for (var i = 0; i < pcs.Count; i++)
            {
                pcs[i].EndDucking(actorUpdater);
            }
        }

        public static void DuckSoundGroup(string soundGroupName, AudioSource aSource, SoundGroupVariationUpdater actorUpdater) {
            var ma = Instance;

            if (!ma.EnableMusicDucking || !ma.duckingBySoundType.ContainsKey(soundGroupName) || aSource.clip == null) {
                return;
            }

            var matchingDuck = ma.duckingBySoundType[soundGroupName];

            // duck music
            var duckLength = aSource.clip.length;
            var duckPitch = aSource.pitch;

            var pcs = PlaylistController.Instances;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < pcs.Count; i++) {
                pcs[i].DuckMusicForTime(actorUpdater, duckLength, matchingDuck.unduckTime, duckPitch, matchingDuck.riseVolStart, matchingDuck.duckedVolumeCut);
            }

            var group = GrabGroup(soundGroupName, false);
            if (group == null) {
                return;
            }
            var groupBus = group.BusForGroup;
            var groupBusName = groupBus == null ? string.Empty : groupBus.busName;

            // get or create the BusDuckInfo
            BusDuckInfo busDuck = null;

            for (var i = 0; i < Instance.BusDucks.Count; i++) {
                var aDuck = Instance.BusDucks[i];
                if (!aDuck.IsActive) {
                    busDuck = aDuck;
                    break;
                }
            }

            if (busDuck == null) {
                busDuck = new BusDuckInfo();
                Instance.BusDucks.Add(busDuck);
            }

            busDuck.IsActive = true;
            busDuck.BusesToDuck.Clear();

            // duck Buses other than the Bus for the played Sound Group (if any).
            for (var i = 0; i < GroupBuses.Count; i++) {
                var aBus = GroupBuses[i];

                if (aBus.busName == groupBusName) {
                    continue; // don't duck the played Sound Group's Bus.
                }

                for (var b = 0; b < Instance.BusFades.Count; b++) {
                    var aFade = Instance.BusFades[b];
                    if (aFade.IsActive && aFade.NameOfBus == aBus.busName) {
                        aFade.IsActive = false; // stop Bus fades for this Bus so ducking works right
                    }
                }

                busDuck.BusesToDuck.Add(aBus);
            }
        }
        /*! \endcond */

        private static void StopPauseOrUnpauseSoundsOfTransform(Transform trans, List<AudioInfo> varList, VariationCommand varCmd) {
            MasterAudioGroup grp = null;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var v = 0; v < varList.Count; v++) {
                var variation = varList[v].Variation;
                if (!variation.WasTriggeredFromTransform(trans)) {
                    continue;
                }

                if (grp == null) {
                    var sType = variation.ParentGroup.GameObjectName;
                    grp = GrabGroup(sType);
                }

                var stopEndDetector = grp != null && grp.curVariationMode == MasterAudioGroup.VariationMode.LoopedChain;

                // matched, stop or pause the audio.
                switch (varCmd) {
                    case VariationCommand.Stop:
                        variation.Stop(stopEndDetector);
                        break;
                    case VariationCommand.Pause:
                        variation.Pause();
                        break;
                    case VariationCommand.Unpause:
                        variation.Unpause();
                        break;
                }
            }
        }

        /// <summary>
        /// This method allows you to abruptly stop all sounds triggered by or following a Transform.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        public static void StopAllSoundsOfTransform(Transform sourceTrans) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            Instance.VariationsStartedDuringMultiStop.Clear();
            Instance._isStoppingMultiple = true;

            foreach (var key in Instance.AllSoundGroupNames) {
                var varList = Instance.AudioSourcesBySoundType[key].Sources;
                StopPauseOrUnpauseSoundsOfTransform(sourceTrans, varList, VariationCommand.Stop);
            }

            Instance._isStoppingMultiple = false;
        }

        /// <summary>
        /// This method allows you to abruptly stop all sounds of a particular Sound Group triggered by or following a Transform.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group to stop.</param>
		public static void StopSoundGroupOfTransform(Transform sourceTrans, string sType) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var varList = Instance.AudioSourcesBySoundType[sType].Sources;
            StopPauseOrUnpauseSoundsOfTransform(sourceTrans, varList, VariationCommand.Stop);
        }

        /// <summary>
        /// This method allows you to pause all sounds triggered by or following a Transform.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
		public static void PauseAllSoundsOfTransform(Transform sourceTrans) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            foreach (var key in Instance.AllSoundGroupNames) {
                var varList = Instance.AudioSourcesBySoundType[key].Sources;
                StopPauseOrUnpauseSoundsOfTransform(sourceTrans, varList, VariationCommand.Pause);
            }
        }

        /// <summary>
        /// This method allows you to pause all sounds of a particular Sound Group triggered by or following a Transform.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group to stop.</param>
        public static void PauseSoundGroupOfTransform(Transform sourceTrans, string sType) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var varList = Instance.AudioSourcesBySoundType[sType].Sources;
            StopPauseOrUnpauseSoundsOfTransform(sourceTrans, varList, VariationCommand.Pause);
        }

        /// <summary>
        /// This method allows you to unpause all sounds triggered by or following a Transform.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        public static void UnpauseAllSoundsOfTransform(Transform sourceTrans) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            foreach (var key in Instance.AllSoundGroupNames) {
                var varList = Instance.AudioSourcesBySoundType[key].Sources;
                StopPauseOrUnpauseSoundsOfTransform(sourceTrans, varList, VariationCommand.Unpause);
            }
        }

        /// <summary>
        /// This method allows you to unpause all sounds of a particular Sound Group triggered by or following a Transform.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group to stop.</param>
        public static void UnpauseSoundGroupOfTransform(Transform sourceTrans, string sType) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var varList = Instance.AudioSourcesBySoundType[sType].Sources;
            StopPauseOrUnpauseSoundsOfTransform(sourceTrans, varList, VariationCommand.Unpause);
        }

        /// <summary>
        /// This method allows you to fade out all sounds triggered by or following a Transform for X seconds.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="fadeTime">The amount of seconds the fading will take.</param>
		public static void FadeOutAllSoundsOfTransform(Transform sourceTrans, float fadeTime) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            var allVarsOfTransform = GetAllPlayingVariationsOfTransform(sourceTrans);

            var varGroups = new HashSet<string>();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var v = 0; v < allVarsOfTransform.Count; v++) {
                var grpName = allVarsOfTransform[v].ParentGroup.GameObjectName;
                if (varGroups.Contains(grpName)) {
                    continue;
                }

                varGroups.Add(grpName);
                FadeOutSoundGroupOfTransform(sourceTrans, grpName, fadeTime);
            }
        }

        /// <summary>
        /// This method allows you to fade out all sounds of a particular Sound Group triggered by or following a Transform for X seconds.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="fadeTime">The amount of seconds the fading will take.</param>
		public static void FadeOutSoundGroupOfTransform(Transform sourceTrans, string sType, float fadeTime) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var varList = Instance.AudioSourcesBySoundType[sType].Sources;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var v = 0; v < varList.Count; v++) {
                var variation = varList[v].Variation;
                if (!variation.WasTriggeredFromTransform(sourceTrans)) {
                    continue;
                }
                variation.FadeOutNowAndStop(fadeTime);
            }
        }

        /// <summary>
        /// This method allows you to fade a certain Sound Group triggered by or following a Transform to a target volume over a period of time.
        /// </summary>
		/// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="fadeTime">The amount of seconds the fading will take.</param>
        /// <param name="targetVolume">The end volume of the fade.</param>
		public static void FadeSoundGroupOfTransformToVolume(Transform sourceTrans, string sType, float fadeTime, float targetVolume) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            var allVarsOfTransform = GetAllPlayingVariationsOfTransform(sourceTrans);

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var v = 0; v < allVarsOfTransform.Count; v++) {
                var aVar = allVarsOfTransform[v];
                var grpName = aVar.ParentGroup.GameObjectName;

                if (grpName != sType) {
                    continue;
                }

                aVar.FadeToVolume(targetVolume, fadeTime);
            }
        }

        /// <summary>
        /// This method allows you to abruptly stop all sounds in a specified Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        public static void StopAllOfSound(string sType) {
            if (!SceneHasMasterAudio) {
                // No MA
                return;
            }

            if (sType == VideoPlayerSoundGroupName)
            {
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            var grp = GrabGroup(sType);

            var stopEndDetector = grp != null && grp.curVariationMode == MasterAudioGroup.VariationMode.LoopedChain;

            foreach (var audio in sources) {
                if (audio.Variation == null) {
                    continue; // destroyed, Group removed.
                }

                if (IsLinkedGroupPlay(audio.Variation)) {
                    continue;
                }

                audio.Variation.Stop(stopEndDetector);
            }
        }

        /// <summary>
		/// This method allows you to fade out all sounds in a specified Sound Group for X seconds. This uses each Variation's fade command. If you want to Fade a Sound Group with the Group volume, use FadeSoundGroupToVolume
		/// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="fadeTime">The amount of seconds the fading will take.</param>
        public static void FadeOutAllOfSound(string sType, float fadeTime) {
            if (!SceneHasMasterAudio) {
                // No MA
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            foreach (var audio in sources) {
                audio.Variation.FadeOutNowAndStop(fadeTime);
            }
        }

#endregion

#region Variation methods

        /// <summary>
        /// Returns a list of all Variation scripts that are currently playing a sound.
        /// </summary>
        /// <returns>List of SoundGroupVariation</returns>
        public static List<SoundGroupVariation> GetAllPlayingVariations() {
            var allPlayingVariations = new List<SoundGroupVariation>();

            foreach (var key in Instance.AllSoundGroupNames) {
                var varList = Instance.AudioSourcesBySoundType[key].Sources;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < varList.Count; i++) {
                    var aVar = varList[i].Variation;
                    if (!aVar.IsPlaying) {
                        continue;
                    }

                    allPlayingVariations.Add(aVar);
                }
            }

            return allPlayingVariations;
        }

        /// <summary>
        /// This will return a list of all playing Variations of a Transform
        /// </summary>
        /// <param name="sourceTrans">Source transform</param>
        /// <returns>List of SoundGroupVariation</returns>
        public static List<SoundGroupVariation> GetAllPlayingVariationsOfTransform(Transform sourceTrans) {
            var allPlayingVariationsInTransform = new List<SoundGroupVariation>();

            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return allPlayingVariationsInTransform;
            }

            foreach (var key in Instance.AllSoundGroupNames) {
                var varList = Instance.AudioSourcesBySoundType[key].Sources;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var v = 0; v < varList.Count; v++) {
                    var variation = varList[v].Variation;
                    if (!variation.WasTriggeredFromTransform(sourceTrans)) {
                        continue;
                    }

                    allPlayingVariationsInTransform.Add(variation);
                }

            }

            return allPlayingVariationsInTransform;
        }

        /// <summary>
        /// This will return a list of all playing Variations of a list of Transforms
        /// </summary>
        /// <param name="sourceTransList">Source transform list</param>
        /// <returns>List of SoundGroupVariation</returns>
        public static List<SoundGroupVariation> GetAllPlayingVariationsOfTransformList(List<Transform> sourceTransList) {
            var allPlayingVariationsInTransformList = new List<SoundGroupVariation>();

            if (!SceneHasMasterAudio) {
                // No MA
                return allPlayingVariationsInTransformList;
            }

            var transMap = new HashSet<Transform>();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sourceTransList.Count; i++) {
                transMap.Add(sourceTransList[i]);
            }


            foreach (var key in Instance.AllSoundGroupNames) {
                var varList = Instance.AudioSourcesBySoundType[key].Sources;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var v = 0; v < varList.Count; v++) {
                    var variation = varList[v].Variation;
                    if (!variation.WasTriggeredFromAnyOfTransformMap(transMap)) {
                        continue;
                    }

                    allPlayingVariationsInTransformList.Add(variation);
                }

            }

            return allPlayingVariationsInTransformList;
        }

        /// <summary>
        /// Returns a list of all Variation scripts that are currently playing through a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to query.</param>
        /// <returns>List of SoundGroupVariation</returns>
        public static List<SoundGroupVariation> GetAllPlayingVariationsInBus(string busName) {
            var busIndex = GetBusIndex(busName, false);

            var allPlayingVariationsInBus = new List<SoundGroupVariation>();

            if (busIndex < 0) {
                return allPlayingVariationsInBus;
            }

            for (var n = 0; n < RuntimeSoundGroupNames.Count; n++) {
                var groupName = RuntimeSoundGroupNames[n];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];

                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex) {
                    continue;
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < groupInfo.Sources.Count; i++) {
                    var aVar = groupInfo.Sources[i].Variation;
                    if (!aVar.IsPlaying) {
                        continue;
                    }

                    allPlayingVariationsInBus.Add(aVar);
                }
            }

            return allPlayingVariationsInBus;
        }


        /// <summary>
        /// This method will delete a variation from a Sound Group during runtime.
        /// </summary>
        /// <param name="sType"></param>
        /// <param name="variationName"></param>
        public static void DeleteGroupVariation(string sType, string variationName) {
            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot delete Variation clip yet.");
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var grp = Instance.AudioSourcesBySoundType[sType];

            Instance.GroupsToDelete.Clear();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < grp.Sources.Count; i++) {
                var aVar = grp.Sources[i];
                if (aVar.Variation.GameObjectName != variationName) {
                    continue;
                }
                Instance.GroupsToDelete.Add(aVar);
            }

            if (Instance.GroupsToDelete.Count == 0) {
                LogWarning("Could not find Variation for '" + sType + "' Group named '" + variationName + "'.\nWill not delete any Variations.");
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < Instance.GroupsToDelete.Count; i++) {
                var match = Instance.GroupsToDelete[i];
                var matchVar = match.Variation;

                matchVar.Stop();
                matchVar.DisableUpdater();

                switch (matchVar.audLocation) {
                    case AudioLocation.ResourceFile:
                        var clipName = matchVar.VarAudio.clip == null ? string.Empty : matchVar.VarAudio.clip.CachedName();
                        AudioResourceOptimizer.DeleteAudioSourceFromList(clipName, matchVar.VarAudio);
                        break;
#if ADDRESSABLES_ENABLED
                    case AudioLocation.Addressable:
                        if (AudioAddressableOptimizer.IsAddressableValid(matchVar.audioClipAddressable)) {
                            AudioAddressableOptimizer.RemoveAddressablePlayingClip(matchVar.audioClipAddressable, matchVar.VarAudio);
                        }
                        break;
#endif
                }

                var index = grp.Sources.IndexOf(match);
                if (index >= 0) {
                    Instance._randomizer[sType].Remove(index);

                    for (var j = 0; j < Instance._randomizer[sType].Count; j++) {
                        if (Instance._randomizer[sType][j] > index) {
                            Instance._randomizer[sType][j]--;
                        }
                    }

                    Instance._randomizerOrigin[sType].Remove(index);
                    for (var j = 0; j < Instance._randomizerOrigin[sType].Count; j++) {
                        if (Instance._randomizerOrigin[sType][j] > index) {
                            Instance._randomizerOrigin[sType][j]--;
                        }
                    }

                    Instance._randomizerLeftovers[sType].Remove(index);
                    for (var j = 0; j < Instance._randomizerLeftovers[sType].Count; j++) {
                        if (Instance._randomizerLeftovers[sType][j] > index) {
                            Instance._randomizerLeftovers[sType][j]--;
                        }
                    }

                    Instance._clipsPlayedBySoundTypeOldestFirst[sType].Remove(index);
                    grp.Sources.RemoveAt(index);
                }

                Instance.OcclusionSourcesInRange.Remove(matchVar.GameObj);
                Instance.OcclusionSourcesOutOfRange.Remove(matchVar.GameObj);
                Instance.OcclusionSourcesBlocked.Remove(matchVar.GameObj);
                RemoveFromOcclusionFrequencyTransitioning(matchVar);

                Instance.AllAudioSources.Remove(matchVar.VarAudio);

                GameObject.Destroy(matchVar.GameObj);
            }
        }


        /// <summary>
        /// This method will add the variation to a Sound Group during runtime.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="clip">The Audio Clip of the variation.</param>
        /// <param name="variationName">Use this to specify a the variation's name.</param>
        /// <param name="volume">Use this to specify a the variation's volume.</param>
        /// <param name="pitch">Use this to specify a the variation's pitch.</param>
        public static void CreateGroupVariationFromClip(string sType, AudioClip clip, string variationName,
            float volume = 1f, float pitch = 1f) {
            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot create change variation clip yet.");
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var grp = Instance.AudioSourcesBySoundType[sType];

            var matchingNameFound = false;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < grp.Sources.Count; i++) {
                var aVar = grp.Sources[i];
                if (aVar.Variation.GameObjectName != variationName) {
                    continue;
                }
                matchingNameFound = true;
                break;
            }

            if (matchingNameFound) {
                LogWarning("You already have a Variation for this Group named '" + variationName +
                           "'. \n\nPlease rename these Variations when finished to be unique, or you may not be able to play them by name if you have a need to.");
            }

            // ReSharper disable once ArrangeStaticMemberQualifier
            var newVar = (GameObject)GameObject.Instantiate(Instance.soundGroupVariationTemplate.gameObject, grp.Group.transform.position, Quaternion.identity);

            newVar.transform.name = variationName;
            newVar.transform.parent = grp.Group.transform;

            var audSrc = newVar.GetComponent<AudioSource>();
            audSrc.clip = clip;
            audSrc.pitch = pitch;

            Instance.AllAudioSources.Add(audSrc);

            var newVariation = newVar.GetComponent<SoundGroupVariation>();
            newVariation.DisableUpdater();

            var newInfo = new AudioInfo(newVariation, newVariation.VarAudio, volume);

            grp.Sources.Add(newInfo);
            grp.Group.groupVariations.Add(newVariation);

            if (!Instance._randomizer.ContainsKey(sType)) {
                return; // sanity check
            }

            var newIndex = grp.Sources.Count - 1;
            Instance._randomizer[sType].Add(newIndex);
            Instance._randomizerOrigin[sType].Add(newIndex);
            Instance._randomizerLeftovers[sType].Add(grp.Sources.Count - 1);
        }


        /// <summary>
        /// This method will change the pitch of a variation or all variations in a Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="changeAllVariations">Whether to change all variations in the Sound Group or just one.</param>
        /// <param name="variationName">Use this to specify a certain variation's name. Only that variation will be changes if you haven't passed changeAllVariations as true.</param>
        /// <param name="pitch">The new pitch of the variation.</param>
        public static void ChangeVariationPitch(string sType, bool changeAllVariations, string variationName,
            float pitch) {
            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot change variation clip yet.");
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var grp = Instance.AudioSourcesBySoundType[sType];

            var iChanged = 0;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < grp.Sources.Count; i++) {
                var aVar = grp.Sources[i];
                if (!changeAllVariations && aVar.Variation.GameObjectName != variationName) {
                    continue;
                }
                aVar.Variation.original_pitch = pitch;
                var aud = aVar.Variation.VarAudio;
                if (aud != null) {
                    aud.pitch = pitch;
                }
                iChanged++;
            }

            if (iChanged == 0 && !changeAllVariations) {
                Debug.Log("Could not find any matching variations of Sound Group '" + sType +
                          "' to change the pitch of.");
            }
        }

        /// <summary>
        /// This method will change the volume of a variation or all variations in a Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="changeAllVariations">Whether to change all variations in the Sound Group or just one.</param>
        /// <param name="variationName">Use this to specify a certain variation's name. Only that variation will be changes if you haven't passed changeAllVariations as true.</param>
        /// <param name="volume">The new volume of the variation.</param>
        public static void ChangeVariationVolume(string sType, bool changeAllVariations, string variationName,
            float volume) {
            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot change variation clip yet.");
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var grp = Instance.AudioSourcesBySoundType[sType];

            var iChanged = 0;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < grp.Sources.Count; i++) {
                var aVar = grp.Sources[i];
                if (!changeAllVariations && aVar.Variation.GameObjectName != variationName) {
                    continue;
                }
                aVar.OriginalVolume = volume;
                iChanged++;
            }

            if (iChanged == 0 && !changeAllVariations) {
                Debug.Log("Could not find any matching variations of Sound Group '" + sType +
                          "' to change the volume of.");
            }
        }

        /// <summary>
        /// This method will change the Audio Clip used by a variation into one named from a Resource file.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="changeAllVariations">Whether to change all variations in the Sound Group or just one.</param>
        /// <param name="variationName">Use this to specify a certain variation's name. Only that variation will be changes if you haven't passed changeAllVariations as true.</param>
        /// <param name="resourceFileName">The name of the file in the Resource.</param>
        public static void ChangeVariationClipFromResources(string sType, bool changeAllVariations, string variationName,
            string resourceFileName) {
            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot create change variation clip yet.");
                return;
            }

            var aClip = Resources.Load(resourceFileName) as AudioClip;

            if (aClip == null) {
                LogWarning("Resource file '" + resourceFileName + "' could not be located.");
                return;
            }

            ChangeVariationClip(sType, changeAllVariations, variationName, aClip);
        }

        /// <summary>
        /// This method will change the Audio Clip used by a variation into one you specify.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="changeAllVariations">Whether to change all variations in the Sound Group or just one.</param>
        /// <param name="variationName">Use this to specify a certain variation's name. Only that variation will be changes if you haven't passed changeAllVariations as true.</param>
        /// <param name="clip">The Audio Clip to replace the old one with.</param>
        public static void ChangeVariationClip(string sType, bool changeAllVariations, string variationName, AudioClip clip) {
            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot create change variation clip yet.");
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return;
            }

            var grp = Instance.AudioSourcesBySoundType[sType];

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < grp.Sources.Count; i++) {
                var aVar = grp.Sources[i];
                if (changeAllVariations || aVar.Variation.GameObjectName == variationName) {
                    if (aVar.Variation.IsPlaying) {
                        aVar.Variation.Stop();
                    }
                    aVar.Source.clip = clip;
                }
            }
        }

        /// <summary>
        /// This method will gradually change the cutoff frequency of an occluded Variation
        /// </summary>
        // ReSharper disable once RedundantNameQualifier
        public static void GradualOcclusionFreqChange(SoundGroupVariation variation, float fadeTime, float newCutoffFreq) {
            if (IsOcclusionFrequencyTransitioning(variation)) {
                LogWarning("Occlusion is already fading for: " + variation.GameObjectName + ". This is a bug.");
                return;
            }

            OcclusionFreqChangeInfo freqChange = null;

            for (var i = 0; i < Instance.VariationOcclusionFreqChanges.Count; i++)
            {
                var aFreqChg = Instance.VariationOcclusionFreqChanges[i];
                if (!aFreqChg.IsActive)
                {
                    freqChange = aFreqChg;
                    break;
                }
            }

            if (freqChange == null)
            {
                freqChange = new OcclusionFreqChangeInfo();
                Instance.VariationOcclusionFreqChanges.Add(freqChange);
            }

            freqChange.ActingVariation = variation;
            freqChange.CompletionTime = Time.realtimeSinceStartup + fadeTime;
            freqChange.IsActive = true;
            freqChange.StartFrequency = variation.LowPassFilter.cutoffFrequency;
            freqChange.StartTime = Time.realtimeSinceStartup;
            freqChange.TargetFrequency = newCutoffFreq;
        }

#endregion

#region Sound Group methods
        /// <summary>
        /// This returns the AudioSource for the next Variation to be played. Only works for top-to-bottom Variation Sequence. 
        /// </summary>
        /// <param name="sType"></param>
        /// <returns>Audio Source</returns>
        public static AudioSource GetNextVariationForSoundGroup(string sType) {
            var aGroup = GrabGroup(sType, false);
            if (aGroup == null || AppIsShuttingDown) {
                return null;
            }

            if (aGroup.curVariationSequence == MasterAudioGroup.VariationSequence.Randomized) {
                Debug.LogWarning("Cannot determine the next Variation of randomly sequenced Sound Group '" + sType + "'.");
                return null;
            }

            if (!Instance._randomizer.ContainsKey(sType)) {
                Debug.Log("Sound Group '" + sType + "' has no active Variations.");
                return null;
            }

            var choices = Instance._randomizer[sType];
            var sources = Instance.AudioSourcesBySoundType[sType];

            var audioInfo = sources.Sources[choices[0]];
            return audioInfo.Source;
        }

        /// <summary>
        /// Returns true or false, telling you true if a Sound Group is playing any voices.
        /// </summary>
        /// <returns>true or false</returns>
        public static bool IsSoundGroupPlaying(string sType) {
            var aGroup = GrabGroup(sType, false);
            if (aGroup == null || AppIsShuttingDown) {
                return false;
            }

            return aGroup.ActiveVoices > 0;
        }

        /// <summary>
        /// Will return whether the Sound Group you specify is played by a Transform you pass in.
        /// </summary>
        /// <param name="sType">Sound Group name</param>
        /// <param name="sourceTrans">The Transform in question</param>
        /// <returns>boolean</returns>
        public static bool IsTransformPlayingSoundGroup(string sType, Transform sourceTrans) {
            if (!SceneHasMasterAudio) {
                // No MA
                return false;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogWarning("Could not locate group '" + sType + "'.");
                return false;
            }

            var varList = Instance.AudioSourcesBySoundType[sType].Sources;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var v = 0; v < varList.Count; v++) {
                var variation = varList[v].Variation;
                if (variation.WasTriggeredFromTransform(sourceTrans)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Change the bus of a Sound Group.
        /// </summary>
        /// <param name="sType">Sound Group name</param>
        /// <param name="busName">The new bus name. Use null for "route to [No Bus]"</param>
        public static void RouteGroupToBus(string sType, string busName) {
            var grp = GrabGroup(sType);

            if (grp == null) {
                LogError("Could not find Sound Group '" + sType + "'");
                return;
            }

            var newBusIndex = 0;

            if (busName != null) {
                var busIndex = GroupBuses.FindIndex(x => x.busName == busName);
                if (busIndex < 0) {
                    LogError("Could not find bus '" + busName + "' to assign to Sound Group '" + sType + "'");
                    return;
                }

                newBusIndex = HardCodedBusOptions + busIndex;
            }

            var oldBus = GetBusByIndex(grp.busIndex);

            grp.busIndex = newBusIndex;
            GroupBus newBus = null;

            var hasChange = false;

            if (newBusIndex > 0) {
                newBus = GroupBuses.Find(x => x.busName == busName);
                if (newBus.isMuted) {
                    MuteGroup(grp.GameObjectName, false);
                    hasChange = true;
                } else if (newBus.isSoloed) {
                    SoloGroup(grp.GameObjectName, false);
                    hasChange = true;
                }
            }

            var hasVoicesPlaying = false;

            // update active voice count on the new and old bus.
            var sources = Instance.AudioSourcesBySoundType[sType].Sources;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                var aVar = sources[i].Variation;

                aVar.SetMixerGroup();
                aVar.SetSpatialBlend(); // set the spatial blend, including any overrides in the bus.

                if (!aVar.IsPlaying) {
                    continue;
                }

                if (newBus != null) { // could be "no bus"
                    newBus.AddActiveAudioSourceId(aVar.InstanceId);
                }

                if (oldBus != null) { // could be "no bus"
                    oldBus.RemoveActiveAudioSourceId(aVar.InstanceId);
                }
                hasVoicesPlaying = true;
            }

            if (hasVoicesPlaying) { // update the moved Variations to use new bus volume
                SetBusVolume(newBus, newBus != null ? newBus.volume : 0);
            }

            if (Application.isPlaying && hasChange) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        /// <summary>
        /// This method will return the length in seconds of a Variation in a Sound Group. Note that it only works for Clip type, not Resource Files or Addressables.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="variationName">Use this to specify a certain variation's name. The first match will be used</param>
        /// <returns>The time length of the Variation, taking pitch into account. If it cannot find the Variation, it returns -1 and logs the reason to the console.</returns>
        public static float GetVariationLength(string sType, string variationName) {
            var grp = GrabGroup(sType);
            if (grp == null) {
                return -1f;
            }

            SoundGroupVariation match = null;

            foreach (var sgv in grp.groupVariations) {
                if (sgv.GameObjectName != variationName) {
                    continue;
                }
                match = sgv;
                break;
            }

            if (match == null) {
                LogError("Could not find Variation '" + variationName + "' in Sound Group '" + sType + "'.");
                return -1f;
            }

            switch (match.audLocation)
            {
                case AudioLocation.ResourceFile:
                    LogError("Variation '" + variationName + "' in Sound Group '" + sType +
                             "' length cannot be determined because it's a Resource Files.");
                    return -1f;
#if ADDRESSABLES_ENABLED
                case AudioLocation.Addressable:
                    LogError("Variation '" + variationName + "' in Sound Group '" + sType +
                             "' length cannot be determined because it's an Addressable."); // can add support for this if needed. Not sure of the need.
                    return -1f;
#endif
            }

            var clip = match.VarAudio.clip;
            if (clip == null) {
                LogError("Variation '" + variationName + "' in Sound Group '" + sType + "' has no Audio Clip.");
                return -1f;
            }

            if (!(match.VarAudio.pitch <= 0f)) {
                return AudioUtil.AdjustAudioClipDurationForPitch(clip.length, match.VarAudio);
            }

            LogError("Variation '" + variationName + "' in Sound Group '" + sType +
                     "' has negative or zero pitch. Cannot compute length.");
            return -1f;
        }

        /// <summary>
        /// This method allows you to refill the pool of the Variation sounds for a Sound Group. That way you don't have to wait for all remaining random (or top to bottom) sounds to be played before it refills.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to refill the pool of.</param>
        public static void RefillSoundGroupPool(string sType) {
            var grp = GrabGroup(sType, false);
            if (grp == null) {
                return;
            }

            var choices = Instance._randomizer[sType];

            var played = Instance._clipsPlayedBySoundTypeOldestFirst[sType];

            if (choices.Count > 0) {
                // add any not played yet.
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < choices.Count; i++) {
                    var index = choices[i];
                    if (played.Contains(index)) {
                        continue;
                    }
                    played.Add(index);
                }
            }

            // for weird edge cases
            var all = Instance._randomizerOrigin[sType];
            if (played.Count < all.Count) {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < all.Count; i++) {
                    var index = all[i];
                    if (played.Contains(index)) {
                        continue;
                    }

                    played.Add(index);
                }
            }

            choices.Clear();

            if (grp.curVariationSequence == MasterAudioGroup.VariationSequence.Randomized) {
                int? lastIndexPlayed = null;

                if (grp.UsesNoRepeat) {
                    if (played.Count > 0) {
                        lastIndexPlayed = played[played.Count - 1];
                    }
                }

                ArrayListUtil.SortIntArray(ref played);

                if (lastIndexPlayed.HasValue && lastIndexPlayed.Value == played[0]) {
                    // would be a repeat of the last random choice! exchange
                    var firstIndex = played[0];
                    played.RemoveAt(0);
                    played.Insert(UnityEngine.Random.Range(1, played.Count), firstIndex);
                }
            }

            choices.AddRange(played);
            // refill leftovers pool.
            Instance._randomizerLeftovers[sType].AddRange(played);

            played.Clear();

            if (grp.curVariationMode == MasterAudioGroup.VariationMode.LoopedChain) {
                grp.ChainLoopCount++;
            }
        }

        /// <summary>
        /// This method allows you to check if a Sound Group exists.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to check.</param>
        /// <returns>Whether or not the Sound Group exists.</returns>
        public static bool SoundGroupExists(string sType) {
            var aGroup = GrabGroup(sType, false);
            return aGroup != null;
        }

        /// <summary>
        /// This method allows you to pause all Audio Sources in a Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to pause.</param>
        public static void PauseSoundGroup(string sType) {
            var aGroup = GrabGroup(sType);

            if (aGroup == null || aGroup.GameObjectName == VideoPlayerSoundGroupName) {
                return;
            }

            // ReSharper disable once TooWideLocalVariableScope
            SoundGroupVariation aVar;

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                aVar = sources[i].Variation;

                aVar.Pause();
            }
        }

        /*! \cond PRIVATE */
        /// <summary>
        /// This method is used by MasterAudio internally. You should never need to call it.
        /// </summary>
        /// <param name="sType"></param>
        public static void SetGroupSpatialBlend(string sType) {
            var aGroup = GrabGroup(sType);

            if (aGroup == null) {
                return;
            }

            // ReSharper disable once TooWideLocalVariableScope
            SoundGroupVariation aVar;

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                aVar = sources[i].Variation;

                aVar.SetSpatialBlend();
            }
        }

        public static void RouteGroupToUnityMixerGroup(string sType, AudioMixerGroup mixerGroup) {
            if (!Application.isPlaying) {
                return;
            }

            var aGroup = GrabGroup(sType, false);

            if (aGroup == null) {
                return;
            }

            // ReSharper disable once TooWideLocalVariableScope
            SoundGroupVariation aVar;

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                aVar = sources[i].Variation;
                aVar.VarAudio.outputAudioMixerGroup = mixerGroup;
            }
        }
        /*! \endcond */

        /// <summary>
        /// This method allows you to unpause all Audio Sources in a Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to unpause.</param>
        public static void UnpauseSoundGroup(string sType) {
            var aGroup = GrabGroup(sType);

            if (aGroup == null || aGroup.GameObjectName == VideoPlayerSoundGroupName) {
                return;
            }

            // ReSharper disable once TooWideLocalVariableScope
            SoundGroupVariation aVar;

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                aVar = sources[i].Variation;

                aVar.Unpause();
            }
        }

        /// <summary>
        /// This method allows you to fade the volume of a Sound Group over X seconds.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to fade.</param>
        /// <param name="newVolume">The target volume of the Sound Group.</param>
        /// <param name="fadeTime">The amount of time the fade will take.</param>
        /// <param name="completionCallback">(Optional) - a method to execute when the fade has completed.</param>
        /// <param name="willStopAfterFade">(Optional) - specify true here if you want the Sound Group to stop after the fade is complete.</param>
        /// <param name="willResetVolumeAfterFade">(Optional) - specify true here if you want the Sound Group's volume to be restored to its pre-fade volume after the fade is complete. This has no effect on a fade of less than .1 second.</param>
        // ReSharper disable once RedundantNameQualifier
        public static void FadeSoundGroupToVolume(string sType, float newVolume, float fadeTime, System.Action completionCallback = null, bool willStopAfterFade = false, bool willResetVolumeAfterFade = false) {
            if (newVolume < 0f || newVolume > 1f) {
                Debug.LogError("Illegal volume passed to FadeSoundGroupToVolume: '" + newVolume +
                               "'. Legal volumes are between 0 and 1");
                return;
            }

            if (fadeTime <= InnerLoopCheckInterval) {
                SetGroupVolume(sType, newVolume); // time really short, just do it at once.

                if (completionCallback != null) {
                    completionCallback();
                }

                if (willStopAfterFade) {
                    StopAllOfSound(sType);
                }

                return;
            }

            var aGroup = GrabGroup(sType);

            if (aGroup == null) {
                return;
            }

            if (newVolume < 0f || newVolume > 1f) {
                Debug.Log("Cannot fade Sound Group '" + sType +
                          "'. Invalid volume specified. Volume should be between 0 and 1.");
                return;
            }

            // make sure no other group fades for this group are happenning.
            for (var i = 0; i < Instance.GroupFades.Count; i++) {
                var aFade = Instance.GroupFades[i];
                if (aFade.NameOfGroup == sType && aFade.IsActive)
                {
                    aFade.IsActive = false; // start with a new one, delete old.
                    break;
                }
            }

            GroupFadeInfo groupFade = null;

            for (var i = 0; i < Instance.GroupFades.Count; i++)
            {
                var aFade = Instance.GroupFades[i];
                if (!aFade.IsActive)
                {
                    groupFade = aFade;
                    break;
                }
            }

            if (groupFade == null)
            {
                groupFade = new GroupFadeInfo();
                Instance.GroupFades.Add(groupFade);
            }

            groupFade.NameOfGroup = sType;
            groupFade.ActingGroup = aGroup;
            groupFade.StartTime = AudioUtil.Time;
            groupFade.CompletionTime = AudioUtil.Time + fadeTime;
            groupFade.StartVolume = aGroup.groupMasterVolume;
            groupFade.TargetVolume = newVolume;
            groupFade.WillStopGroupAfterFade = willStopAfterFade;
            groupFade.WillResetVolumeAfterFade = willResetVolumeAfterFade;
            groupFade.IsActive = true;

            if (completionCallback != null) {
                groupFade.completionAction = completionCallback;
            }
        }

        /// <summary>
        /// This method allows you to fade out voices on a Sound Group that have been playing for at least X seconds.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to fade.</param>
        /// <param name="minimumPlayTime">The minimum time that each Variation must have been playing to be faded.</param>
        /// <param name="fadeTime">The duration of the fade to perform.</param>
        public static void FadeOutOldSoundGroupVoices(string sType, float minimumPlayTime, float fadeTime) {
            if (!SceneHasMasterAudio) {
                // No MA
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                return; // Sound Group doesn't exist
            }

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            for (var v = 0; v < sources.Count; v++) {
                var variation = sources[v].Variation;
                if (!variation.IsPaused && !variation.IsPlaying) {
                    continue;
                }

                var timeElapsed = AudioUtil.Time - variation.LastTimePlayed;
                if (timeElapsed <= minimumPlayTime) {
                    continue;
                }

                if (fadeTime <= 0f) {
                    variation.Stop();
                } else {
                    variation.FadeOutNowAndStop(fadeTime);
                }
            }
        }

        /// <summary>
        /// This method allows you to stop voices on a Sound Group that have been playing for at least X seconds.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to stop.</param>
        /// <param name="minimumPlayTime">The minimum time that each Variation must have been playing to be stopped.</param>
        public static void StopOldSoundGroupVoices(string sType, float minimumPlayTime) {
            if (!SceneHasMasterAudio) {
                // No MA
                return;
            }

            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                return; // Sound Group doesn't exist
            }

            var sources = Instance.AudioSourcesBySoundType[sType].Sources;

            for (var v = 0; v < sources.Count; v++) {
                var variation = sources[v].Variation;
                if (!variation.IsPaused && !variation.IsPlaying) {
                    continue;
                }

                var timeElapsed = AudioUtil.Time - variation.LastTimePlayed;
                if (timeElapsed <= minimumPlayTime) {
                    continue;
                }

                variation.Stop();
            }
        }

        /// <summary>
        /// This method allows you to glide the pitch of each Variation of a Sound Group over X seconds by a specified amount.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to glide pitch.</param>
        /// <param name="pitchAddition">The amount of pitch to add to each Variation's pitch in the Sound Group.</param>
        /// <param name="glideTime">The amount of time the pitch glide will take.</param>
        /// <param name="completionCallback">(Optional) - a method to execute when the pitch glide has completed.</param>
        // ReSharper disable once RedundantNameQualifier
        public static void GlideSoundGroupByPitch(string sType, float pitchAddition, float glideTime, System.Action completionCallback = null)         {
            if (pitchAddition < -3f || pitchAddition > 3f)             {
                Debug.LogError("Illegal pitch passed to GlideSoundGroupByPitch: '" + pitchAddition + "'. Legal pitches are between -3 and 3");
                return;
            }

            if (pitchAddition == 0) { // nothing to do
                if (completionCallback != null) {
                    completionCallback();
                }
                return;
            }

            var refGroup = GrabGroup(sType);

            if (refGroup == null) {
                return;
            }

            // make sure no other group fades for this group are happenning.
            for (var i = 0; i < Instance.GroupPitchGlides.Count; i++)
            {
                var aGlide = Instance.GroupPitchGlides[i];
                if (aGlide.NameOfGroup == sType && aGlide.IsActive) {
                    aGlide.IsActive = false; // start with a new one, deactivate old.

                    if (aGlide.completionAction != null) {
                        aGlide.completionAction();
                    }
                    break;
                }
            }
            
            var aGroup = Instance.AudioSourcesBySoundType[sType];

            if (glideTime <= InnerLoopCheckInterval) { 
                // time too  short, just do it at once.
                for (var i = 0; i < aGroup.Sources.Count; i++) {
                    var aVar = aGroup.Sources[i].Variation;

                    aVar.GlideByPitch(pitchAddition, 0f);
                }

                if (completionCallback != null) {
                    completionCallback();
                }

                return;
            }

            var gliders = new List<SoundGroupVariation>();

            for (var v = 0; v < aGroup.Sources.Count; v++)
            {
                var aVar = aGroup.Sources[v].Variation;
                if (!aVar.IsPlaying)
                {
                    continue;
                }

                if (aVar.curPitchMode == SoundGroupVariation.PitchMode.Gliding)
                {
                    aVar.VariationUpdater.StopPitchGliding();
                }

                aVar.GlideByPitch(pitchAddition, glideTime);
                gliders.Add(aVar);
            }

            if (gliders.Count == 0) {
                if (completionCallback != null) {
                    completionCallback();
                }

                return; // nothing to glide
            }

            if (completionCallback == null) {
                return; // only need to set up the object for MA Update if completion action.
            }

            GroupPitchGlideInfo groupGlide = null;

            for (var i = 0; i < Instance.GroupPitchGlides.Count; i++)
            {
                var aFade = Instance.GroupPitchGlides[i];
                if (!aFade.IsActive)
                {
                    groupGlide = aFade;
                    break; 
                }
            }

            if (groupGlide == null)
            {
                groupGlide = new GroupPitchGlideInfo();
                Instance.GroupPitchGlides.Add(groupGlide);
            }

            groupGlide.NameOfGroup = sType;
            groupGlide.ActingGroup = refGroup;
            groupGlide.CompletionTime = AudioUtil.Time + glideTime;
            groupGlide.GlidingVariations.Clear();
            groupGlide.GlidingVariations.AddRange(gliders);
            groupGlide.completionAction = completionCallback;
        }

        /// <summary>
        /// This method will delete a Sound Group, and all variations from the current Scene's Master Audio object. 
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        public static void DeleteSoundGroup(string sType) {
            if (SafeInstance == null) {
                return;
            }

            var grp = GrabGroup(sType);
            if (grp == null) {
                return;
            }

            StopAllOfSound(sType); // unload Resources if any.

            var groupTrans = grp.transform;

            var ma = Instance;

            if (ma.duckingBySoundType.ContainsKey(sType)) {
                ma.duckingBySoundType.Remove(sType);
            }

            Instance._randomizer.Remove(sType);
            Instance._randomizerLeftovers.Remove(sType);
            Instance._randomizerOrigin.Remove(sType);
            Instance._nonRandomChoices.Remove(sType);
            Instance._clipsPlayedBySoundTypeOldestFirst.Remove(sType);
            RemoveRuntimeGroupInfo(sType);
            Instance.LastTimeSoundGroupPlayed.Remove(sType);

            // ReSharper disable TooWideLocalVariableScope
            AudioSource aSource;
            SoundGroupVariation aVar;
            Transform aChild;
            // ReSharper restore TooWideLocalVariableScope

            // delete resource file pointers to Audio Sources being deleted
            for (var i = 0; i < groupTrans.childCount; i++) {
                aChild = groupTrans.GetChild(i);
                aSource = aChild.GetComponent<AudioSource>();
                aVar = aChild.GetComponent<SoundGroupVariation>();

                switch (aVar.audLocation) {
                    case AudioLocation.ResourceFile:
                        AudioResourceOptimizer.DeleteAudioSourceFromList(AudioResourceOptimizer.GetLocalizedFileName(aVar.useLocalization, aVar.resourceFileName), aSource);
                        break;
#if ADDRESSABLES_ENABLED
                    case AudioLocation.Addressable:
                        if (!AudioAddressableOptimizer.IsAddressableValid(aVar.audioClipAddressable)) {
                            AudioAddressableOptimizer.RemoveAddressablePlayingClip(aVar.audioClipAddressable, aVar.VarAudio);
                        }
                        break;
#endif
                }

            }

            groupTrans.parent = null;
            // ReSharper disable once ArrangeStaticMemberQualifier 
            GameObject.Destroy(groupTrans.gameObject);

            RescanGroupsNow();
        }

        /// <summary>
        /// This method will create a new Sound Group from the Audio Clips you pass in.
        /// </summary>
        /// <param name="aGroup">The object containing all variations and group info.</param>
        /// <param name="creatorInstanceId">The InstanceId of the Game Object creating the Sound Group.</param>
        /// <param name="errorOnExisting">Whether to log an error if the Group already exists (same name).</param>
        /// <returns>Whether or not the Sound Group was created.</returns>
        public static Transform CreateSoundGroup(DynamicSoundGroup aGroup, int? creatorInstanceId, bool errorOnExisting = true) {
            if (!SceneHasMasterAudio) {
                return null;
            }

            if (!SoundsReady) {
                Debug.LogError("MasterAudio not finished initializing sounds. Cannot create new group yet.");
                return null;
            }

            var groupName = aGroup.transform.name;

            var ma = Instance;

			if (Instance.AudioSourcesBySoundType.ContainsKey(groupName)) {
                if (errorOnExisting) {
                    Debug.LogError("Cannot add a new Sound Group named '" + groupName +
                                   "' because there is already a Sound Group of that name.");
                }
                return null;
            }

            // ReSharper disable once ArrangeStaticMemberQualifier
            var newGroup = (GameObject)GameObject.Instantiate(ma.soundGroupTemplate.gameObject, ma.Trans.position, Quaternion.identity);

            var groupTrans = newGroup.transform;
            groupTrans.name = UtilStrings.TrimSpace(groupName);
            groupTrans.parent = Instance.Trans;
            groupTrans.gameObject.layer = Instance.gameObject.layer;

            SoundGroupVariation variation;
            // ReSharper disable TooWideLocalVariableScope
            DynamicGroupVariation aVariation;
            AudioClip clip;
            // ReSharper restore TooWideLocalVariableScope

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < aGroup.groupVariations.Count; i++) {
                aVariation = aGroup.groupVariations[i];

                for (var j = 0; j < aVariation.weight; j++) {
                    // ReSharper disable once ArrangeStaticMemberQualifier
                    var newVariation = (GameObject)GameObject.Instantiate(aVariation.gameObject, groupTrans.position, Quaternion.identity);
                    newVariation.transform.parent = groupTrans;
                    newVariation.transform.gameObject.layer = groupTrans.gameObject.layer;

                    // remove dynamic group variation script.
                    // ReSharper disable once ArrangeStaticMemberQualifier
                    GameObject.Destroy(newVariation.GetComponent<DynamicGroupVariation>());

                    newVariation.AddComponent<SoundGroupVariation>();
                    variation = newVariation.GetComponent<SoundGroupVariation>();

                    var clipName = variation.GameObjectName;
                    // ReSharper disable once StringIndexOfIsCultureSpecific.1
                    var cloneIndex = clipName.IndexOf("(Clone)");
                    if (cloneIndex >= 0) {
                        clipName = clipName.Substring(0, cloneIndex);
                    }

                    var aVarAudio = aVariation.GetComponent<AudioSource>();

                    switch (aVariation.audLocation) {
                        case AudioLocation.Clip:
                            clip = aVarAudio.clip;
                            variation.VarAudio.clip = clip;
                            break;
                        case AudioLocation.ResourceFile:
                            var resourceFileName = AudioResourceOptimizer.GetLocalizedFileName(aVariation.useLocalization, aVariation.resourceFileName);
                            AudioResourceOptimizer.AddTargetForClip(resourceFileName, variation.VarAudio);
                            variation.resourceFileName = aVariation.resourceFileName;
                            variation.useLocalization = aVariation.useLocalization;
                            break;
#if ADDRESSABLES_ENABLED
                        case AudioLocation.Addressable:
                            variation.audioClipAddressable = aVariation.audioClipAddressable;
                            break;
#endif
                    }

                    variation.clipAlias = aVariation.clipAlias;
                    variation.audLocation = aVariation.audLocation;

                    variation.original_pitch = aVarAudio.pitch;
                    variation.transform.name = clipName;
                    variation.isExpanded = aVariation.isExpanded;

                    variation.probabilityToPlay = aVariation.probabilityToPlay;

                    variation.isUninterruptible = aVariation.isUninterruptible;
                    variation.importance = aVariation.importance;

                    variation.useRandomPitch = aVariation.useRandomPitch;
                    variation.randomPitchMode = aVariation.randomPitchMode;
                    variation.randomPitchMin = aVariation.randomPitchMin;
                    variation.randomPitchMax = aVariation.randomPitchMax;

                    variation.useRandomVolume = aVariation.useRandomVolume;
                    variation.randomVolumeMode = aVariation.randomVolumeMode;
                    variation.randomVolumeMin = aVariation.randomVolumeMin;
                    variation.randomVolumeMax = aVariation.randomVolumeMax;

                    variation.useCustomLooping = aVariation.useCustomLooping;
                    variation.minCustomLoops = aVariation.minCustomLoops;
                    variation.maxCustomLoops = aVariation.maxCustomLoops;

                    variation.useFades = aVariation.useFades;
                    variation.fadeInTime = aVariation.fadeInTime;
                    variation.fadeOutTime = aVariation.fadeOutTime;

                    variation.useIntroSilence = aVariation.useIntroSilence;
                    variation.introSilenceMin = aVariation.introSilenceMin;
                    variation.introSilenceMax = aVariation.introSilenceMax;

                    variation.useRandomStartTime = aVariation.useRandomStartTime;
                    variation.randomStartMinPercent = aVariation.randomStartMinPercent;
                    variation.randomStartMaxPercent = aVariation.randomStartMaxPercent;
                    variation.randomEndPercent = aVariation.randomEndPercent;

                    if (Instance.addResonanceAudioSources && ResonanceAudioHelper.DarkTonicResonanceAudioPackageInstalled()) {
                        ResonanceAudioHelper.AddResonanceAudioSourceToVariation(variation);
                    } else if (Instance.addOculusAudioSources && OculusAudioHelper.DarkTonicOculusAudioPackageInstalled()) {
                        OculusAudioHelper.AddOculusAudioSourceToVariation(variation);
                    }

                    // remove unused filter FX
                    if (variation.LowPassFilter != null && !variation.LowPassFilter.enabled) {
                        // ReSharper disable once ArrangeStaticMemberQualifier
                        GameObject.Destroy(variation.LowPassFilter);
                    }
                    if (variation.HighPassFilter != null && !variation.HighPassFilter.enabled) {
                        // ReSharper disable once ArrangeStaticMemberQualifier
                        GameObject.Destroy(variation.HighPassFilter);
                    }
                    if (variation.DistortionFilter != null && !variation.DistortionFilter.enabled) {
                        // ReSharper disable once ArrangeStaticMemberQualifier
                        GameObject.Destroy(variation.DistortionFilter);
                    }
                    if (variation.ChorusFilter != null && !variation.ChorusFilter.enabled) {
                        // ReSharper disable once ArrangeStaticMemberQualifier
                        GameObject.Destroy(variation.ChorusFilter);
                    }
                    if (variation.EchoFilter != null && !variation.EchoFilter.enabled) {
                        // ReSharper disable once ArrangeStaticMemberQualifier
                        GameObject.Destroy(variation.EchoFilter);
                    }
                    if (variation.ReverbFilter != null && !variation.ReverbFilter.enabled) {
                        // ReSharper disable once ArrangeStaticMemberQualifier
                        GameObject.Destroy(variation.ReverbFilter);
                    }
                }
            }
            // added to Hierarchy!

            // populate sounds for playing!
            var groupScript = newGroup.GetComponent<MasterAudioGroup>();
            // populate other properties.
            groupScript.retriggerPercentage = aGroup.retriggerPercentage;
            if (creatorInstanceId.HasValue)
            {
                groupScript.AddActorInstanceId(creatorInstanceId.Value);
            }

            var persistentGrpVol = PersistentAudioSettings.GetGroupVolume(aGroup.name);
            groupScript.OriginalVolume = aGroup.groupMasterVolume;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (persistentGrpVol.HasValue) {
                groupScript.groupMasterVolume = persistentGrpVol.Value;
            } else {
                groupScript.groupMasterVolume = aGroup.groupMasterVolume;
            }

#if ADDRESSABLES_ENABLED
            groupScript.addressableUnusedSecondsLifespan = aGroup.addressableUnusedSecondsLifespan;
#endif

            groupScript.useClipAgePriority = aGroup.useClipAgePriority;
            groupScript.limitMode = aGroup.limitMode;
            groupScript.limitPerXFrames = aGroup.limitPerXFrames;
            groupScript.minimumTimeBetween = aGroup.minimumTimeBetween;
            groupScript.limitPolyphony = aGroup.limitPolyphony;
            groupScript.voiceLimitCount = aGroup.voiceLimitCount;
            groupScript.curVariationSequence = aGroup.curVariationSequence;
            groupScript.useInactivePeriodPoolRefill = aGroup.useInactivePeriodPoolRefill;
            groupScript.inactivePeriodSeconds = aGroup.inactivePeriodSeconds;
            groupScript.curVariationMode = aGroup.curVariationMode;
            groupScript.useNoRepeatRefill = aGroup.useNoRepeatRefill;
            groupScript.useDialogFadeOut = aGroup.useDialogFadeOut;
            groupScript.dialogFadeOutTime = aGroup.dialogFadeOutTime;
            groupScript.groupPlayType = aGroup.groupPlayType;

            groupScript.isUninterruptible = aGroup.isUninterruptible;
            groupScript.importance = aGroup.importance;

            groupScript.isUsingOcclusion = aGroup.isUsingOcclusion;
            groupScript.willOcclusionOverrideRaycastOffset = aGroup.willOcclusionOverrideRaycastOffset;
            groupScript.occlusionRayCastOffset = aGroup.occlusionRayCastOffset;
            groupScript.willOcclusionOverrideFrequencies = aGroup.willOcclusionOverrideFrequencies;
            groupScript.occlusionMaxCutoffFreq = aGroup.occlusionMaxCutoffFreq;
            groupScript.occlusionMinCutoffFreq = aGroup.occlusionMinCutoffFreq;

            groupScript.chainLoopDelayMin = aGroup.chainLoopDelayMin;
            groupScript.chainLoopDelayMax = aGroup.chainLoopDelayMax;
            groupScript.chainLoopMode = aGroup.chainLoopMode;
            groupScript.chainLoopNumLoops = aGroup.chainLoopNumLoops;

            groupScript.expandLinkedGroups = aGroup.expandLinkedGroups;
            groupScript.childSoundGroups = aGroup.childSoundGroups;
            groupScript.endLinkedGroups = aGroup.endLinkedGroups;
            groupScript.linkedStartGroupSelectionType = aGroup.linkedStartGroupSelectionType;
            groupScript.linkedStopGroupSelectionType = aGroup.linkedStopGroupSelectionType;

            groupScript.soundPlayedEventActive = aGroup.soundPlayedEventActive;
            groupScript.soundPlayedCustomEvent = aGroup.soundPlayedCustomEvent;

            groupScript.targetDespawnedBehavior = aGroup.targetDespawnedBehavior;
            groupScript.despawnFadeTime = aGroup.despawnFadeTime;

            groupScript.logSound = aGroup.logSound;
            groupScript.alwaysHighestPriority = aGroup.alwaysHighestPriority;

            groupScript.spatialBlendType = aGroup.spatialBlendType;
            groupScript.spatialBlend = aGroup.spatialBlend;

            var sources = new List<AudioInfo>();
            // ReSharper disable TooWideLocalVariableScope
            Transform aChild;
            AudioSource aSource;
            // ReSharper restore TooWideLocalVariableScope

            var playedStatuses = new List<int>();

            for (var i = 0; i < newGroup.transform.childCount; i++) {
                playedStatuses.Add(i);
                aChild = newGroup.transform.GetChild(i);
                aSource = aChild.GetComponent<AudioSource>();
                variation = aChild.GetComponent<SoundGroupVariation>();
                sources.Add(new AudioInfo(variation, aSource, aSource.volume));

                variation.DisableUpdater();
            }

            AddRuntimeGroupInfo(groupName, new AudioGroupInfo(sources, groupScript));

            if (groupScript.curVariationSequence == MasterAudioGroup.VariationSequence.Randomized) {
                ArrayListUtil.SortIntArray(ref playedStatuses);
            }

            // fill up randomizer
            Instance._randomizer.Add(groupName, playedStatuses);

            var allStatuses = new List<int>(playedStatuses.Count);
            allStatuses.AddRange(playedStatuses);
            Instance._randomizerOrigin.Add(groupName, allStatuses);

            Instance._randomizerLeftovers.Add(groupName, new List<int>(playedStatuses.Count));
            // fill leftovers
            Instance._randomizerLeftovers[groupName].AddRange(playedStatuses);
            Instance._clipsPlayedBySoundTypeOldestFirst.Add(groupName, new List<int>(playedStatuses.Count));
            Instance._nonRandomChoices.Add(groupName, new List<int>());

            RescanGroupsNow();

            if (string.IsNullOrEmpty(aGroup.busName)) {
                return groupTrans;
            }

            groupScript.busIndex = GetBusIndex(aGroup.busName, true);
            if (groupScript.BusForGroup != null && groupScript.BusForGroup.isMuted) {
                MuteGroup(groupScript.GameObjectName, false);
            } else if (Instance.mixerMuted) {
                MuteGroup(groupScript.GameObjectName, false);
            }

            return groupTrans;
        }

        /// <summary>
        /// This will return the volume of a Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <returns>The volume of the Sound Group</returns>
        public static float GetGroupVolume(string sType) {
            var aGroup = GrabGroup(sType);
            if (aGroup == null) {
                return 0f;
            }

            return aGroup.groupMasterVolume;
        }

        /// <summary>
        /// This method will set the volume of a Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="volumeLevel">The new volume level.</param>
        public static void SetGroupVolume(string sType, float volumeLevel) {
            var aGroup = GrabGroup(sType, Application.isPlaying);
            if (aGroup == null || AppIsShuttingDown) {
                return;
            }

            aGroup.groupMasterVolume = volumeLevel;

            // ReSharper disable TooWideLocalVariableScope
            AudioInfo aInfo;
            AudioSource aSource;
            // ReSharper restore TooWideLocalVariableScope

            var theGroup = Instance.AudioSourcesBySoundType[sType];

            var busVolume = GetBusVolume(aGroup);

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < theGroup.Sources.Count; i++) {
                aInfo = theGroup.Sources[i];
                aSource = aInfo.Source;

                if (aSource == null) {
                    continue;
                }

                float newVol;
                if (aInfo.Variation.useRandomVolume && aInfo.Variation.randomVolumeMode == SoundGroupVariation.RandomVolumeMode.AddToClipVolume) {
                    newVol = (aInfo.OriginalVolume * aInfo.LastPercentageVolume * aGroup.groupMasterVolume * busVolume *
                              Instance._masterAudioVolume) + aInfo.LastRandomVolume;
                } else {
                    // ignore original volume
                    newVol = (aInfo.OriginalVolume * aInfo.LastPercentageVolume * aGroup.groupMasterVolume * busVolume *
                              Instance._masterAudioVolume);
                }
                aSource.volume = newVol;
            }
        }

        /// <summary>
        /// This method will mute all variations in a Sound Group.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="shouldCheckMuteStatus">Whether or not we should immediately go and silence other non-soloed Groups after soloing this one.</param>
        public static void MuteGroup(string sType, bool shouldCheckMuteStatus = true) {
            var aGroup = GrabGroup(sType);
            if (aGroup == null) {
                return;
            }

            Instance.SoloedGroups.Remove(aGroup);
            aGroup.isSoloed = false;

            SetGroupMuteStatus(aGroup, sType, true);

            if (shouldCheckMuteStatus) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        /// <summary>
        /// This method will unmute all variations in a Sound Group
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="shouldCheckMuteStatus">Whether or not we should immediately go and silence other non-soloed Groups after soloing this one.</param>
        public static void UnmuteGroup(string sType, bool shouldCheckMuteStatus = true) {
            var aGroup = GrabGroup(sType);
            if (aGroup == null) {
                return;
            }

            SetGroupMuteStatus(aGroup, sType, false);

            if (shouldCheckMuteStatus) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        private static void AddRuntimeGroupInfo(string groupName, AudioGroupInfo groupInfo) {
            Instance.AudioSourcesBySoundType.Add(groupName, groupInfo);
            Instance.AllSoundGroupNames.Add(groupName);

            var sources = new List<AudioSource>(groupInfo.Sources.Count);
            // ReSharper disable ForCanBeConvertedToForeach
            for (var i = 0; i < groupInfo.Sources.Count; i++) {
                // ReSharper restore ForCanBeConvertedToForeach
                sources.Add(groupInfo.Sources[i].Source);
            }

            TrackRuntimeAudioSources(sources);
        }

        /*! \cond PRIVATE */

        private static void FireAudioSourcesNumberChangedEvent() {
            if (NumberOfAudioSourcesChanged != null) {
                NumberOfAudioSourcesChanged();
            }
        }

        /// <summary>
        /// This method is used internally by Master Audio. You should never need to call them.
        /// </summary>
        /// <param name="sources"></param>
        public static void TrackRuntimeAudioSources(List<AudioSource> sources) {
            var wasListChanged = false;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                var src = sources[i];
                if (Instance.AllAudioSources.Contains(src)) {
                    continue;
                }

                Instance.AllAudioSources.Add(src);
                wasListChanged = true;
            }

            if (wasListChanged) {
                FireAudioSourcesNumberChangedEvent();
            }
        }

        /// <summary>
        /// This method is used internally by Master Audio. You should never need to call them.
        /// </summary>
        /// <param name="sources"></param>
        public static void StopTrackingRuntimeAudioSources(List<AudioSource> sources) {
            if (AppIsShuttingDown || SafeInstance == null) {
                return;
            }

            var wasListChanged = false;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                var src = sources[i];

                if (!Instance.AllAudioSources.Contains(src)) {
                    continue;
                }

                Instance.AllAudioSources.Remove(src);
                wasListChanged = true;
            }

            if (wasListChanged) {
                FireAudioSourcesNumberChangedEvent();
            }
        }

        private static void RemoveRuntimeGroupInfo(string groupName) {
            var groupInfo = GrabGroup(groupName);

            if (groupInfo != null) {
                // ReSharper disable once ForCanBeConvertedToForeach
                var sources = new List<AudioSource>(groupInfo.groupVariations.Count);

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < groupInfo.groupVariations.Count; i++) {
                    sources.Add(groupInfo.groupVariations[i].VarAudio);
                }

                StopTrackingRuntimeAudioSources(sources);
            }

            Instance.AudioSourcesBySoundType.Remove(groupName);
            Instance.AllSoundGroupNames.Remove(groupName);
        }

        /*! \endcond */

        private static void RescanChildren(MasterAudioGroup group) {
            var newChildren = new List<SoundGroupVariation>();

            var childNames = new List<string>();

            for (var i = 0; i < group.transform.childCount; i++) {
                var child = group.transform.GetChild(i);

                if (childNames.Contains(child.name)) {
                    continue;
                }

                childNames.Add(child.name);

                var variation = child.GetComponent<SoundGroupVariation>();

                newChildren.Add(variation);
            }

            group.groupVariations = newChildren;
        }

        private static void SetGroupMuteStatus(MasterAudioGroup aGroup, string sType, bool isMute) {
            aGroup.isMuted = isMute;

            var theGroup = Instance.AudioSourcesBySoundType[sType];
            // ReSharper disable TooWideLocalVariableScope
            AudioInfo aInfo;
            AudioSource aSource;
            // ReSharper restore TooWideLocalVariableScope

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < theGroup.Sources.Count; i++) {
                aInfo = theGroup.Sources[i];
                aSource = aInfo.Source;

                aSource.mute = isMute;
            }
        }

        /// <summary>
        /// This method will solo a Sound Group. If anything is soloed, only soloed Sound Groups will be heard.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="shouldCheckMuteStatus">Whether or not we should immediately go and silence other non-soloed Groups after soloing this one.</param>
        public static void SoloGroup(string sType, bool shouldCheckMuteStatus = true) {
            var aGroup = GrabGroup(sType);
            if (aGroup == null) {
                return;
            }

            if (Instance.SoloedGroups.Contains(aGroup))
            {
                return;
            }

            aGroup.isMuted = false;
            aGroup.isSoloed = true;

            Instance.SoloedGroups.Add(aGroup);

            SetGroupMuteStatus(aGroup, sType, false);

            if (shouldCheckMuteStatus) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        /*! \cond PRIVATE */
        public static void SilenceOrUnsilenceGroupsFromSoloChange() {
            if (Instance.SoloedGroups.Count > 0) {
                SilenceNonSoloedGroups();
            } else {
                UnsilenceNonSoloedGroups();
            }
        }
        /*! \endcond */

        private static void UnsilenceNonSoloedGroups() {
            for (var i = 0; i < Instance.AllSoundGroupNames.Count; i++)
            {
                var groupName = Instance.AllSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                if (groupInfo.Group.isMuted)
                {
                    continue; // soloed or muted
                }

                UnsilenceGroup(groupInfo);
            }
        }

        private static void UnsilenceGroup(AudioGroupInfo grp) {
            for (var i = 0; i < grp.Sources.Count; i++) {
                grp.Sources[i].Source.mute = false;
            }
        }

        private static void SilenceNonSoloedGroups() {
            for (var i = 0; i < Instance.AllSoundGroupNames.Count; i++)
            {
                var groupName = Instance.AllSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                if (groupInfo.Group.isSoloed || groupInfo.Group.isMuted)
                {
                    continue; // soloed or muted
                }

                SilenceGroup(groupInfo);
            }
        }

        private static void SilenceGroup(AudioGroupInfo grp) {
            for (var i = 0; i < grp.Sources.Count; i++) {
                grp.Sources[i].Source.mute = true;
            }
        }

        /// <summary>
        /// This method will unsolo a Sound Group. 
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="shouldCheckMuteStatus">Whether or not we should immediately go and silence other non-soloed Groups after soloing this one.</param>
        public static void UnsoloGroup(string sType, bool shouldCheckMuteStatus = true) {
            var aGroup = GrabGroup(sType);
            if (aGroup == null) {
                return;
            }

            aGroup.isSoloed = false;

            Instance.SoloedGroups.Remove(aGroup);

            if (!shouldCheckMuteStatus) {
                return;
            }

            SilenceOrUnsilenceGroupsFromSoloChange();
        }

        /// <summary>
        /// This method will return the Sound Group settings for examination purposes.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="logIfMissing">Whether to log to the Console if Group cannot be found.</param>
        /// <returns>A MasterAudioGroup object</returns>
        public static MasterAudioGroup GrabGroup(string sType, bool logIfMissing = true) {
            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                if (logIfMissing) {
                    Debug.LogError("Could not grab Sound Group '" + sType + "' because it does not exist in this scene.");
                }
                return null;
            }

            var group = Instance.AudioSourcesBySoundType[sType];

            if (group.Group == null) {
                var grpTrans = Instance.Trans.GetChildTransform(sType);
                if (grpTrans != null) {
                    var missingGroup = grpTrans.GetComponent<MasterAudioGroup>();
                    group.Group = missingGroup; // when deleted and undeleted.
                } else {
                    return null; // fail, can't find!
                }
            }

            var maGroup = group.Group;

            if (maGroup.groupVariations.Count == 0) { // needed for Dynamic SGC's
                RescanChildren(maGroup);
            }

            return maGroup;
        }

        /// <summary>
        /// Returns total number of Audio Sources for this Sound Group.
        /// </summary>
        /// <param name="sType">Name of the Sound Group</param>
        /// <returns></returns>
        public static int VoicesForGroup(string sType) {
            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                return -1;
            }

            return Instance.AudioSourcesBySoundType[sType].Sources.Count;
        }

        /*! \cond PRIVATE */
        /// <summary>
        /// Used by Inspectors to find the Sound Group so we can select it.
        /// </summary>
        /// <param name="sType">Name of Sound Group</param>
        /// <returns>Transform</returns>
        public static Transform FindGroupTransform(string sType) {
            Transform grp;

            if (SafeInstance != null) {
                grp = Instance.Trans.GetChildTransform(sType);
                if (grp != null) {
                    return grp;
                }
            }

            var dgscs = FindObjectsOfType<DynamicSoundGroupCreator>();
            for (var i = 0; i < dgscs.Count(); i++) {
                var d = dgscs[i];
                grp = d.transform.GetChildTransform(sType);

                if (grp != null) {
                    return grp;
                }
            }

            return null;
        }

        /// <summary>
        /// This method will return all Variations of a Sound Group settings for examination purposes.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="logIfMissing">Whether to log to the Console if Group cannot be found.</param>
        /// <returns>A list of Audio Info objects</returns>
        public static List<AudioInfo> GetAllVariationsOfGroup(string sType, bool logIfMissing = true) {
            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                if (logIfMissing) {
                    Debug.LogError("Could not grab Sound Group '" + sType + "' because it does not exist in this scene.");
                }
                return null;
            }

            var group = Instance.AudioSourcesBySoundType[sType];
            return group.Sources;
        }
        /*! \endcond */

        /// <summary>
        /// This method will return the Audio Group Info settings for examination purposes. Use on during play in editor, not during edit.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <returns>an Audio Group Info object</returns>
        public static AudioGroupInfo GetGroupInfo(string sType) {
            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                return null;
            }

            var group = Instance.AudioSourcesBySoundType[sType];
            return group;
        }

        /// <summary>
        /// Use this method if you want to be notified when the last Variation in a Sound Group has finished playing (all have played).
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        /// <param name="finishedCallback">Code to execute when the last Variation finishes playing</param>
        public static void SubscribeToLastVariationPlayed(string sType, System.Action finishedCallback) {
            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                Debug.LogError("Could not grab Sound Group '" + sType + "' because it does not exist in this scene.");
                return;
            }

            var group = Instance.AudioSourcesBySoundType[sType];
            group.Group.SubscribeToLastVariationFinishedPlay(finishedCallback);
        }

        /// <summary>
        /// Use this method to cease notifications added by SubscribeToLastVariationPlayed.
        /// </summary>
        /// <param name="sType">The name of the Sound Group</param>
        public static void UnsubscribeFromLastVariationPlayed(string sType) {
            if (!Instance.AudioSourcesBySoundType.ContainsKey(sType)) {
                return;
            }

            var group = Instance.AudioSourcesBySoundType[sType];
            group.Group.UnsubscribeFromLastVariationFinishedPlay();
        }

#endregion

#region Mixer methods
        /*! \cond PRIVATE */
        public void SetSpatialBlendForMixer() {
            foreach (var key in AllSoundGroupNames) {
                SetGroupSpatialBlend(key);
            }
        }
        /*! \endcond */

        /// <summary>
        /// This method allows you to pause all Audio Sources in the mixer (everything but Playlists).
        /// </summary>
        public static void PauseMixer() {
            foreach (var key in Instance.AllSoundGroupNames) {
                PauseSoundGroup(Instance.AudioSourcesBySoundType[key].Group.GameObjectName);
            }
        }

        /// <summary>
        /// This method allows you to unpause all Audio Sources in the mixer (everything but Playlists).
        /// </summary>
        public static void UnpauseMixer() {
            foreach (var key in Instance.AllSoundGroupNames) {
                UnpauseSoundGroup(Instance.AudioSourcesBySoundType[key].Group.GameObjectName);
            }
        }

        /// <summary>
        /// This method allows you to stop all Audio Sources in the mixer (everything but Playlists).
        /// </summary>
        public static void StopMixer() {
            Instance.VariationsStartedDuringMultiStop.Clear();
            Instance._isStoppingMultiple = true;

            foreach (var key in Instance.AllSoundGroupNames) {
                StopAllOfSound(Instance.AudioSourcesBySoundType[key].Group.GameObjectName);
            }

            Instance._isStoppingMultiple = false;
        }

#endregion

#region Global Controls

        /// <summary>
        /// This method allows you to unsubscribe from all SoundFinished events in the entire MA hierarchy in your Scene.
        /// </summary>
        public static void UnsubscribeFromAllVariations() {
            foreach (var key in Instance.AllSoundGroupNames) {
                var varList = Instance.AudioSourcesBySoundType[key].Sources;
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < varList.Count; i++) {
                    varList[i].Variation.ClearSubscribers();
                }
            }
        }

        /// <summary>
        /// This method allows you to stop all Audio Sources in the mixer and Playlists as well.
        /// </summary>
        public static void StopEverything() {
            StopMixer();
            StopAllPlaylists();
        }

        /// <summary>
        /// This method allows you to pause all Audio Sources in the mixer and Playlists as well.
        /// </summary>
        public static void PauseEverything() {
            PauseMixer();
            PauseAllPlaylists();
        }

        /// <summary>
        /// This method allows you to unpause all Audio Sources in the mixer and Playlists as well.
        /// </summary>
        public static void UnpauseEverything() {
            UnpauseMixer();
            UnpauseAllPlaylists();
        }

        /// <summary>
        /// This method allows you to mute all Audio Sources in the mixer and Playlists as well.
        /// </summary>
        public static void MuteEverything() {
            MixerMuted = true;
            MuteAllPlaylists();
        }

        /// <summary>
        /// This method allows you to unmute all Audio Sources in the mixer and Playlists as well.
        /// </summary>
        public static void UnmuteEverything() {
            MixerMuted = false;
            UnmuteAllPlaylists();
        }

        /// <summary>
        /// This provides a list of of all audio clip names used in all Sound Groups, at edit time.
        /// </summary>
        /// <returns></returns>
        public static List<string> ListOfAudioClipsInGroupsEditTime() {
            var clips = new List<string>();

            for (var i = 0; i < Instance.transform.childCount; i++) {
                var aGrp = Instance.transform.GetChild(i).GetComponent<MasterAudioGroup>();
                for (var c = 0; c < aGrp.transform.childCount; c++) {
                    var aVar = aGrp.transform.GetChild(c).GetComponent<SoundGroupVariation>();

                    var clipName = string.Empty;

                    switch (aVar.audLocation) {
                        case AudioLocation.Clip:
                            var clip = aVar.VarAudio.clip;
                            if (clip != null) {
                                clipName = clip.CachedName();
                            }
                            break;
                        case AudioLocation.ResourceFile:
                            clipName = aVar.resourceFileName;
                            break;
#if ADDRESSABLES_ENABLED
                        case AudioLocation.Addressable:
                            clipName = ""; // we can add support for this if needed. Not sure of the need.
                            break;
#endif
                    }

                    if (!string.IsNullOrEmpty(clipName) && !clips.Contains(clipName)) {
                        clips.Add(clipName);
                    }
                }
            }

            return clips;
        }

#endregion

#region Bus methods

        private static int GetBusIndex(string busName, bool alertMissing) {
            if (!SceneHasMasterAudio) {
                // No MA
                return -1;
            }

            for (var i = 0; i < GroupBuses.Count; i++) {
                if (GroupBuses[i].busName == busName) {
                    return i + HardCodedBusOptions;
                }
            }

            if (alertMissing) {
                LogWarning("Could not find bus '" + busName + "'.");
            }

            return -1;
        }

        private static GroupBus GetBusByIndex(int busIndex) {
            if (busIndex < HardCodedBusOptions) {
                return null;
            }

            return GroupBuses[busIndex - HardCodedBusOptions];
        }

        /// <summary>
        /// This method allows you to change the pitch of all Variations in all Groups in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus.</param>
        /// <param name="pitch">The new pitch to use.</param>
        public static void ChangeBusPitch(string busName, float pitch) {
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++) {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                ChangeVariationPitch(aGroup.GameObjectName, true, string.Empty, pitch);
            }
        }

        /// <summary>
        /// This method allows you to mute all Groups in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to mute.</param>
        public static void MuteBus(string busName) {
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            var bus = GrabBusByName(busName);
            bus.isMuted = true;

            if (bus.isSoloed) {
                UnsoloBus(busName);
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                MuteGroup(aGroup.GameObjectName, false);
            }

            if (Application.isPlaying) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        /// <summary>
        /// This method allows you to unmute all Groups in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to unmute.</param>
        /// <param name="shouldCheckMuteStatus">Whether or not we should immediately go and silence other non-soloed Groups after soloing this one.</param>
        public static void UnmuteBus(string busName, bool shouldCheckMuteStatus = true) {
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            var bus = GrabBusByName(busName);
            bus.isMuted = false;

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                UnmuteGroup(aGroup.GameObjectName, false);
            }

            if (shouldCheckMuteStatus) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        /// <summary>
        /// This will mute the bus if unmuted, and vice versa
        /// </summary>
        /// <param name="busName">Name of the bus to toggle mute of</param>
        public static void ToggleMuteBus(string busName) {
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            var bus = GrabBusByName(busName);
            if (bus.isMuted) {
                UnmuteBus(busName);
            } else {
                MuteBus(busName);
            }
        }

        /// <summary>
        /// This method allows you to pause all Audio Sources in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to pause.</param>
        public static void PauseBus(string busName) {
            if (!SceneHasMasterAudio) {
                // No MA
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                PauseSoundGroup(aGroup.GameObjectName);
            }
        }

        /// <summary>
        /// This method allows you to solo all Groups in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to solo.</param>
        public static void SoloBus(string busName) {
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            var bus = GrabBusByName(busName);
            bus.isSoloed = true;

            if (bus.isMuted) {
                UnmuteBus(busName);
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                SoloGroup(aGroup.GameObjectName, false);
            }

            if (Application.isPlaying) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        /// <summary>
        /// This method allows you to unsolo all Groups in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to unsolo.</param>
        /// <param name="shouldCheckMuteStatus">Whether or not we should immediately go and silence other non-soloed Groups after soloing this one.</param>
        public static void UnsoloBus(string busName, bool shouldCheckMuteStatus = true) {
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            var bus = GrabBusByName(busName);
            bus.isSoloed = false;

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                UnsoloGroup(aGroup.GameObjectName, false);
            }

            if (shouldCheckMuteStatus) {
                SilenceOrUnsilenceGroupsFromSoloChange();
            }
        }

        /*! \cond PRIVATE */
        public static void RouteBusToUnityMixerGroup(string busName, AudioMixerGroup mixerGroup) {
            if (!Application.isPlaying) {
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                RouteGroupToUnityMixerGroup(aGroup.GameObjectName, mixerGroup);
            }
        }
        /*! \endcond */

        private static SoundGroupVariation FindLeastImportantSoundOnBus(GroupBus bus, MasterAudioGroup group)
        {
            var busIndex = GetBusIndex(bus.busName, true);

            if (busIndex < 0)
            {
                return null;
            }

            // ReSharper restore TooWideLocalVariableScope
            SoundGroupVariation leastImportantVar = null;
            var leastImportantVarImportance = -1f;

            for (var n = 0; n < RuntimeSoundGroupNames.Count; n++)
            {
                var groupName = RuntimeSoundGroupNames[n];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex) {
                    continue;
                }

                // group has same bus, check for time played.
                if (aGroup.ActiveVoices == 0)
                {
                    continue; // nothing playing, look in next group
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < groupInfo.Sources.Count; i++) {
                    var aVar = groupInfo.Sources[i].Variation;
                    if (!aVar.PlaySoundParm.IsPlaying) {
                        continue;
                    }

                    if (aVar.curFadeMode == SoundGroupVariation.FadeMode.FadeOutEarly) {
                        aVar.Stop();
                        continue;
                    }

                    if (aVar.ParentGroup.isUninterruptible) {
                        continue;
                    }

                    if (leastImportantVar == null) {
                        leastImportantVar = aVar;
                        leastImportantVarImportance = aVar.ParentGroup.importance;
                    } else if (aVar.ParentGroup.importance < leastImportantVarImportance) {
                        leastImportantVar = aVar;
                        leastImportantVarImportance = aVar.ParentGroup.importance;
                    }
                }
            }

            if (leastImportantVarImportance > group.importance)
            {
                return null;
            }

            return leastImportantVar;
        }

        private static SoundGroupVariation FindFarthestSoundOnBus(GroupBus bus)
        {
            var busIndex = GetBusIndex(bus.busName, true);

            if (busIndex < 0)
            {
                return null;
            }

            SoundGroupVariation farthestVar = null;
            var farthestVarDistance = -1f;

            for (var n = 0; n < RuntimeSoundGroupNames.Count; n++) {
                var groupName = RuntimeSoundGroupNames[n];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex) {
                    continue;
                }

                // group has same bus, check for time played.
                if (aGroup.ActiveVoices == 0) {
                    continue; // nothing playing, look in next group
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < groupInfo.Sources.Count; i++) {
                    var aVar = groupInfo.Sources[i].Variation;
                    if (!aVar.PlaySoundParm.IsPlaying)
                    {
                        continue;
                    }

                    if (aVar.curFadeMode == SoundGroupVariation.FadeMode.FadeOutEarly) {
                        aVar.Stop();
                        continue;
                    }

                    float distance = 0f;
                    var variationActorTransform = aVar.ObjectToFollow;
                    if (variationActorTransform != null) {
                        distance = (ListenerTrans.position - variationActorTransform.position).sqrMagnitude;
                    }

                    if (farthestVar == null) {
                        farthestVar = aVar;
                        farthestVarDistance = distance;
                        continue;
                    }

                    if (distance > farthestVarDistance) {
                        farthestVar = aVar;
                        farthestVarDistance = distance;
                    }
                }
            }

            return farthestVar;
        }

        private static SoundGroupVariation FindOldestSoundOnBus(GroupBus bus) {
            var busIndex = GetBusIndex(bus.busName, true);

            if (busIndex < 0) {
                return null;
            }

            // ReSharper restore TooWideLocalVariableScope
            SoundGroupVariation oldestVar = null;
            var oldestVarPlayTime = -1f;

            for (var n = 0; n < RuntimeSoundGroupNames.Count; n++) {
                var groupName = RuntimeSoundGroupNames[n];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex) {
                    continue;
                }

                // group has same bus, check for time played.
                if (aGroup.ActiveVoices == 0) {
                    continue; // nothing playing, look in next group
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < groupInfo.Sources.Count; i++) {
                    var aVar = groupInfo.Sources[i].Variation;
                    if (!aVar.PlaySoundParm.IsPlaying) {
                        continue;
                    }

                    if (aVar.curFadeMode == SoundGroupVariation.FadeMode.FadeOutEarly) {
                        aVar.Stop();
                        continue;
                    }

                    if (oldestVar == null) {
                        oldestVar = aVar;
                        oldestVarPlayTime = aVar.LastTimePlayed;
                    } else if (aVar.LastTimePlayed < oldestVarPlayTime) {
                        oldestVar = aVar;
                        oldestVarPlayTime = aVar.LastTimePlayed;
                    }
                }
            }

            return oldestVar;
        }

        /// <summary>
        /// This method allows you to stop all Audio Sources in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to stop.</param>
        public static void StopBus(string busName) {
            if (busName == VideoPlayerBusName)
            {
                return;
            }
            
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            Instance.VariationsStartedDuringMultiStop.Clear();
            Instance._isStoppingMultiple = true;

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                StopAllOfSound(aGroup.GameObjectName);
            }

            Instance._isStoppingMultiple = false;
        }

        /// <summary>
        /// This method allows you to unpause all paused Audio Sources in a bus.
        /// </summary>
        /// <param name="busName">The name of the bus to unpause.</param>
        public static void UnpauseBus(string busName) {
            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                UnpauseSoundGroup(aGroup.GameObjectName);
            }
        }

        /// <summary>
        /// This method will create a new bus with the name you specify.
        /// </summary>
        /// <param name="busName">The name of the new bus.</param>
        /// <param name="actorInstanceId">The actor instanceId of the creator. Used the track if another Dynamic Sound Group Creator is still active with the bus so we don't delete it yet.</param>
        /// <param name="errorOnExisting">Whether to log an error if the bus already exists (same name).</param>
		/// <param name="isTemporary">Used by DGSC to create temporary buses.</param>
		public static bool CreateBus(string busName, int? actorInstanceId, bool errorOnExisting = true, bool isTemporary = false) {
            var match = GroupBuses.FindAll(delegate (GroupBus obj) {
                return obj.busName == busName;
            });

            if (match.Count > 0) {
                if (errorOnExisting) {
                    LogError("You already have a bus named '" + busName + "'. Not creating a second one.");
                }
                return false;
            }

            var newBus = new GroupBus {
                busName = busName,
                isTemporary = isTemporary
            };

            var busVol = PersistentAudioSettings.GetBusVolume(busName);
            GroupBuses.Add(newBus);

            if (busVol.HasValue) {
                SetBusVolumeByName(busName, busVol.Value);
            }

            if (actorInstanceId.HasValue)
            {
                newBus.AddActorInstanceId(actorInstanceId.Value);
            }

            return true;
        }

        /// <summary>
        /// This method will delete a bus by name.
        /// </summary>
        /// <param name="busName">The name of the bus to delete.</param>
        public static void DeleteBusByName(string busName) {
            var index = GetBusIndex(busName, false);
            if (index > 0) {
                DeleteBusByIndex(index);
            }
        }

        /*! \cond PRIVATE */
        public static void DeleteBusByIndex(int busIndex) {
            var realIndex = busIndex - HardCodedBusOptions;

            if (Application.isPlaying) {
                var deadBus = GroupBuses[realIndex];

                if (deadBus.isSoloed) {
                    UnsoloBus(deadBus.busName, false);
                } else if (deadBus.isMuted) {
                    UnmuteBus(deadBus.busName, false);
                }
            }

            GroupBuses.RemoveAt(realIndex);

            for (var n = 0; n < RuntimeSoundGroupNames.Count; n++) {
                var groupName = RuntimeSoundGroupNames[n];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex == -1) {
                    continue;
                }

                if (aGroup.busIndex == busIndex) {
                    // this bus was just deleted!
                    aGroup.busIndex = -1;

                    RouteGroupToUnityMixerGroup(aGroup.GameObjectName, null);

                    // re-init Group for "no bus"
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < groupInfo.Sources.Count; i++)
                    {
                        var aVariation = groupInfo.Sources[i].Variation;
                        aVariation.SetSpatialBlend();
                    }

                    RecalculateGroupVolumes(groupInfo, null);
                } else if (aGroup.busIndex > busIndex) {
                    aGroup.busIndex--;
                }
            }
        }
        /*! \endcond */

        /// <summary>
        /// This method will return the bus volume of a specified Sound Group, if any. If the Group is not in a bus, this will return 1.
        /// </summary>
        /// <param name="maGroup">The Sound Group object.</param>
        /// <returns>The volume of the bus.</returns>
        public static float GetBusVolume(MasterAudioGroup maGroup) {
            var busVolume = 1f;
            if (maGroup.busIndex >= HardCodedBusOptions) {
                busVolume = GroupBuses[maGroup.busIndex - HardCodedBusOptions].volume;
            }

            return busVolume;
        }

        /// <summary>
        /// This method allows you to fade the volume of a bus over X seconds.
        /// </summary>
        /// <param name="busName">The name of the bus to fade.</param>
        /// <param name="newVolume">The target volume of the bus.</param>
        /// <param name="fadeTime">The amount of time the fade will take.</param>
        /// <param name="completionCallback">(Optional) - a method to execute when the fade has completed.</param>
        /// <param name="willStopAfterFade">(Optional) - specify true here if you want the bus to stop after the fade is complete.</param>
        /// <param name="willResetVolumeAfterFade">(Optional) - specify true here if you want the bus volume to be restored to its pre-fade volume after the fade is complete. This has no effect on a fade of less than .1 second.</param>
        // ReSharper disable once RedundantNameQualifier
        public static void FadeBusToVolume(string busName, float newVolume, float fadeTime, System.Action completionCallback = null, bool willStopAfterFade = false, bool willResetVolumeAfterFade = false) {
            if (newVolume < 0f || newVolume > 1f) {
                Debug.LogError("Illegal volume passed to FadeBusToVolume: '" + newVolume + "'. Legal volumes are between 0 and 1");
                return;
            }

            if (fadeTime <= InnerLoopCheckInterval) {
                SetBusVolumeByName(busName, newVolume); // time really short, just do it at once.

                if (completionCallback != null) {
                    completionCallback();
                }

                if (willStopAfterFade) {
                    StopBus(busName);
                }

                return;
            }

            var bus = GrabBusByName(busName);

            if (bus == null) {
                Debug.Log("Could not find bus '" + busName + "' to fade it.");
                return;
            }

            // make sure no other bus fades for this bus are happenning. Stop them
            for (var i = 0; i < Instance.BusFades.Count; i++)
            {
                var aFade = Instance.BusFades[i];
                if (aFade.IsActive && aFade.NameOfBus == busName)
                {
                    aFade.IsActive = false;
                }
            }

            BusFadeInfo busFade = null;

            for (var i = 0; i < Instance.BusFades.Count; i++) {
                var aFade = Instance.BusFades[i];
                if (!aFade.IsActive) {
                    busFade = aFade;
                    break;
                }
            }

            if (busFade == null) {
                busFade = new BusFadeInfo();
                Instance.BusFades.Add(busFade);
            }

            busFade.NameOfBus = busName;
            busFade.ActingBus = bus;
            busFade.StartVolume = bus.volume;
            busFade.TargetVolume = newVolume;
            busFade.StartTime = AudioUtil.Time;
            busFade.CompletionTime = AudioUtil.Time + fadeTime;
            busFade.WillStopGroupAfterFade = willStopAfterFade;
            busFade.WillResetVolumeAfterFade = willResetVolumeAfterFade;
            busFade.IsActive = true;

            if (completionCallback != null) {
                busFade.completionAction = completionCallback;
            }
        }

        /// <summary>
        /// This method allows you to fade out voices on a Bus that have been playing for at least X seconds.
        /// </summary>
        /// <param name="busName">The name of the bus to fade.</param>
        /// <param name="minimumPlayTime">The minimum time that each Variation must have been playing to be faded.</param>
        /// <param name="fadeTime">The duration of the fade to perform.</param>
        public static void FadeOutOldBusVoices(string busName, float minimumPlayTime, float fadeTime) {
            if (!SceneHasMasterAudio) {
                // No MA
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex) {
                    continue; // wrong bus, ignore
                }

                for (var v = 0; v < groupInfo.Sources.Count; v++) {
                    var variation = groupInfo.Sources[v].Variation;
                    if (!variation.IsPaused && !variation.IsPlaying) {
                        continue;
                    }

                    var timeElapsed = AudioUtil.Time - variation.LastTimePlayed;
                    if (timeElapsed <= minimumPlayTime) {
                        continue;
                    }

                    if (fadeTime <= 0f) {
                        variation.Stop();
                    } else {
                        variation.FadeOutNowAndStop(fadeTime);
                    }
                }
            }
        }

        /// <summary>
        /// This method allows you to stop voices on a Bus that have been playing for at least X seconds.
        /// </summary>
        /// <param name="busName">The name of the bus to fade.</param>
        /// <param name="minimumPlayTime">The minimum time that each Variation must have been playing to be stopped.</param>
        public static void StopOldBusVoices(string busName, float minimumPlayTime) {
            if (!SceneHasMasterAudio) {
                // No MA
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++) {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue; // wrong bus, ignore
                }

                for (var v = 0; v < groupInfo.Sources.Count; v++) {
                    var variation = groupInfo.Sources[v].Variation;
                    if (!variation.IsPaused && !variation.IsPlaying) {
                        continue;
                    }

                    var timeElapsed = AudioUtil.Time - variation.LastTimePlayed;
                    if (timeElapsed <= minimumPlayTime) {
                        continue;
                    }

                    variation.Stop();
                }
            }
        }

        /// <summary>
        /// This method allows you to glide the pitch of each playing Variation on each Sound Group assigned to a Bus over X seconds.
        /// </summary>
        /// <param name="busName">The name of the bus to fade.</param>
        /// <param name="pitchAddition">The amount of pitch to add to each playing Variation on each Sound Group assigned to the Bus.</param>
        /// <param name="glideTime">The amount of time the pitch glide will take.</param>
        /// <param name="completionCallback">(Optional) - a method to execute when the glide has completed.</param>
        // ReSharper disable once RedundantNameQualifier
        public static void GlideBusByPitch(string busName, float pitchAddition, float glideTime, System.Action completionCallback = null) {
            if (pitchAddition < -3f || pitchAddition > 3f) {
                Debug.LogError("Illegal pitch passed to GlideBusByPitch: '" + pitchAddition + "'. Legal pitches are between -3 and 3");
                return;
            }

            if (pitchAddition == 0) { // nothing to do
                if (completionCallback != null) {
                    completionCallback();
                }
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            if (glideTime <= InnerLoopCheckInterval) {
                for (var i = 0; i < RuntimeSoundGroupNames.Count; i++) {
                    var groupName = RuntimeSoundGroupNames[i];
                    var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                    if (groupInfo.Group.busIndex != busIndex) {
                        continue;
                    }

                    for (var g = 0; g < groupInfo.Sources.Count; g++) {
                        var aVar = groupInfo.Sources[g].Variation;
                        if (!aVar.IsPlaying) {
                            continue;
                        }

                        if (aVar.curPitchMode == SoundGroupVariation.PitchMode.Gliding) {
                            aVar.VariationUpdater.StopPitchGliding();
                        }

                        aVar.GlideByPitch(pitchAddition, 0);
                    }
                }

                if (completionCallback != null) {
                    completionCallback();
                }

                return;
            }

            // make sure no other bus fades for this bus are happenning. Stop them.
            for (var i = 0; i < Instance.BusPitchGlides.Count; i++)
            {
                var aGlide = Instance.BusPitchGlides[i];
                if (aGlide.NameOfBus == busName && aGlide.IsActive)
                {
                    aGlide.IsActive = false;
                    if (aGlide.completionAction != null)
                    {
                        aGlide.completionAction();
                        aGlide.completionAction = null;
                    }
                    break;
                }
            }

            BusPitchGlideInfo newGlide = null;

            for (var i = 0; i < Instance.BusPitchGlides.Count; i++)
            {
                var aGlide = Instance.BusPitchGlides[i];
                if (!aGlide.IsActive)
                {
                    newGlide = aGlide;
                    break;
                }
            }

            var isNewInstance = false;

            if (newGlide == null)
            {
                newGlide = new BusPitchGlideInfo();
                isNewInstance = true;
            }

            var glidingVars = new List<SoundGroupVariation>();

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                if (groupInfo.Group.busIndex != busIndex)
                {
                    continue;
                }

                for (var g = 0; g < groupInfo.Sources.Count; g++)
                {
                    var aVar = groupInfo.Sources[g].Variation;
                    if (!aVar.IsPlaying)
                    {
                        continue;
                    }

                    if (aVar.curPitchMode == SoundGroupVariation.PitchMode.Gliding)
                    {
                        aVar.VariationUpdater.StopPitchGliding();
                    }

                    aVar.GlideByPitch(pitchAddition, glideTime);
                    glidingVars.Add(aVar);
                }
            }

            if (glidingVars.Count == 0) {
                if (completionCallback != null) {
                    completionCallback();
                }
                return;
            }

            newGlide.NameOfBus = busName;
            newGlide.CompletionTime = AudioUtil.Time + glideTime;
            newGlide.GlidingVariations = glidingVars;
            newGlide.IsActive = true;

            if (completionCallback != null) {
                newGlide.completionAction = completionCallback;
            }

            if (isNewInstance) {
                Instance.BusPitchGlides.Add(newGlide);
            }
        }

        /// <summary>
        /// This method will set the volume of a bus.
        /// </summary>
        /// <param name="newVolume">The volume to set the bus to.</param>
        /// <param name="busName">The bus name.</param>
        public static void SetBusVolumeByName(string busName, float newVolume) {
            var bus = GrabBusByName(busName);
            if (bus == null) {
                Debug.LogError("bus '" + busName + "' not found!");
                return;
            }

            SetBusVolume(bus, newVolume);
        }

        private static void RecalculateGroupVolumes(AudioGroupInfo aGroup, GroupBus bus) {
            var groupBus = GetBusByIndex(aGroup.Group.busIndex);

            var hasMatchingBus = groupBus != null && bus != null && groupBus.busName == bus.busName;
            var busVolume = 1f;
            if (hasMatchingBus) {
                busVolume = bus.volume;
            } else if (groupBus != null) {
                busVolume = groupBus.volume; // buses other than the one you're adjusting.
            }

            // ReSharper disable TooWideLocalVariableScope
            AudioInfo aInfo;
            AudioSource aSource;
            // ReSharper restore TooWideLocalVariableScope

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < aGroup.Sources.Count; i++) {
                aInfo = aGroup.Sources[i];
                aSource = aInfo.Source;

                if (!aInfo.Variation.IsPlaying) {
                    continue;
                }

                var grpVol = aGroup.Group.groupMasterVolume * busVolume * Instance._masterAudioVolume;
                var newVol = (aInfo.OriginalVolume * aInfo.LastPercentageVolume * grpVol) + aInfo.LastRandomVolume;
                aSource.volume = newVol;

                var aVar = aInfo.Variation;
                aVar.SetGroupVolume = grpVol;
            }

        }

        private static void SetBusVolume(GroupBus bus, float newVolume) {
            if (bus != null) {
                bus.volume = newVolume;
            }

            // ReSharper disable TooWideLocalVariableScope
            AudioGroupInfo aGroup;
            // ReSharper restore TooWideLocalVariableScope

            foreach (var key in Instance.AllSoundGroupNames) {
                aGroup = Instance.AudioSourcesBySoundType[key];
                RecalculateGroupVolumes(aGroup, bus);
            }
        }

        /// <summary>
        /// This method will return the settings of a bus.
        /// </summary>
        /// <param name="busName">The bus name.</param>
        /// <returns>GroupBus object</returns>
        public static GroupBus GrabBusByName(string busName) {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < GroupBuses.Count; i++) {
                var aBus = GroupBuses[i];
                if (aBus.busName == busName) {
                    return aBus;
                }
            }

            return null;
        }

        /// <summary>
        /// This method allows you to pause all sounds of a particular Bus triggered by or following a Transform
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="busName">The name of the Bus.</param>
        public static void PauseBusOfTransform(Transform sourceTrans, string busName) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++) {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                PauseSoundGroupOfTransform(sourceTrans, aGroup.GameObjectName);
            }
        }

        /// <summary>
        /// This method allows you to unpause all sounds of a particular Bus triggered by or following a Transform
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="busName">The name of the Bus.</param>
        public static void UnpauseBusOfTransform(Transform sourceTrans, string busName) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                UnpauseSoundGroupOfTransform(sourceTrans, aGroup.GameObjectName);
            }
        }

        /// <summary>
        /// This method allows you to stop all sounds of a particular Bus triggered by or following a Transform
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="busName">The name of the Bus.</param>
        public static void StopBusOfTransform(Transform sourceTrans, string busName) {
            if (!SceneHasMasterAudio || sourceTrans == null) {
                // No MA
                return;
            }

            var busIndex = GetBusIndex(busName, true);

            if (busIndex < 0) {
                return;
            }

            Instance.VariationsStartedDuringMultiStop.Clear();
            Instance._isStoppingMultiple = true;

            for (var i = 0; i < RuntimeSoundGroupNames.Count; i++)
            {
                var groupName = RuntimeSoundGroupNames[i];
                var groupInfo = Instance.AudioSourcesBySoundType[groupName];
                var aGroup = groupInfo.Group;
                if (aGroup.busIndex != busIndex)
                {
                    continue;
                }

                StopSoundGroupOfTransform(sourceTrans, aGroup.GameObjectName);
            }

            Instance._isStoppingMultiple = false;
        }

#endregion

#region Ducking methods

        /// <summary>
        /// This method will allow you to add a Sound Group to the list of sounds that cause music in the Playlist to duck.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="riseVolumeStart">Percentage of time length to start unducking.</param>
        /// <param name="duckedVolCut">Amount of decimals to cut the original volume</param>
        /// <param name="unduckTime">Amount of time to return music to original volume.</param>
		/// <param name="isTemporary">Used by DSGC to create temporary duck groups.</param>
		public static void AddSoundGroupToDuckList(string sType, float riseVolumeStart, float duckedVolCut, float unduckTime, bool isTemporary = false) {
            var ma = Instance;

            if (ma.duckingBySoundType.ContainsKey(sType)) {
                return;
            }

            var newDuck = new DuckGroupInfo() {
                soundType = sType,
                riseVolStart = riseVolumeStart,
                duckedVolumeCut = duckedVolCut,
                unduckTime = unduckTime,
                isTemporary = isTemporary
            };

            ma.duckingBySoundType.Add(sType, newDuck);
            ma.musicDuckingSounds.Add(newDuck);
        }

        /// <summary>
        /// This method will allow you to remove a Sound Group from the list of sounds that cause music in the Playlist to duck.
        /// </summary>
        /// <param name="sType">The name of the Sound Group.</param>
        public static void RemoveSoundGroupFromDuckList(string sType) {
            var ma = Instance;

            if (!ma.duckingBySoundType.ContainsKey(sType)) {
                return;
            }

            var matchingDuck = ma.duckingBySoundType[sType];
            ma.musicDuckingSounds.Remove(matchingDuck);

            ma.duckingBySoundType.Remove(sType);
        }

#endregion

#region Playlist methods

        /// <summary>
        /// This method will find a Playlist by name and return it to you.
        /// </summary>
        public static Playlist GrabPlaylist(string playlistName, bool logErrorIfNotFound = true) {
            if (playlistName == NoGroupName) {
                return null;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < MusicPlaylists.Count; i++) {
                var aPlaylist = MusicPlaylists[i];
                if (aPlaylist.playlistName == playlistName) {
                    return aPlaylist;
                }
            }

            if (logErrorIfNotFound) {
                Debug.LogError("Could not find Playlist '" + playlistName + "'.");
            }

            return null;
        }

        /// <summary>
        /// This method will change the pitch of all clips in a Playlist, or a single song if you specify the song name.
        /// </summary>
        /// <param name="playlistName">The name of the Playlist.</param>
        /// <param name="pitch">The pitch to change the songs to.</param>
        /// <param name="songName">(Optional) the song name to change the pitch of. If not specified, all songs will be changed.</param>
        public static void ChangePlaylistPitch(string playlistName, float pitch, string songName = null) {
            var playlist = GrabPlaylist(playlistName);

            if (playlist == null) {
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlist.MusicSettings.Count; i++) {
                var aSong = playlist.MusicSettings[i];

                if (!string.IsNullOrEmpty(songName) && aSong.alias != songName && aSong.songName != songName) {
                    continue;
                }

                aSong.pitch = pitch;
            }
        }

#region Mute Playlist

        /// <summary>
        /// This method will allow you to mute your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void MutePlaylist() {
            MutePlaylist(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will allow you to mute a Playlist Controller by name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void MutePlaylist(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToMute.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "PausePlaylist")) {
                    return;
                }

                Instance.ControllersToMute.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToMute.Add(pl);
                }
            }

            MutePlaylists(Instance.ControllersToMute);
        }

        /// <summary>
        /// This method will allow you to mute all Playlist Controllers.
        /// </summary>
        public static void MuteAllPlaylists() {
            MutePlaylists(PlaylistController.Instances);
        }

        private static void MutePlaylists(List<PlaylistController> playlists) {
            if (playlists.Count == PlaylistController.Instances.Count) {
                PlaylistsMuted = true;
            }

            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.MutePlaylist();
            }
        }

#endregion

#region Unmute Playlist

        /// <summary>
        /// This method will allow you to unmute your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void UnmutePlaylist() {
            UnmutePlaylist(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will allow you to unmute a Playlist Controller by name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void UnmutePlaylist(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToUnmute.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "PausePlaylist")) {
                    return;
                }

                Instance.ControllersToUnmute.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToUnmute.Add(pl);
                }
            }

            UnmutePlaylists(Instance.ControllersToUnmute);
        }

        /// <summary>
        /// This method will allow you to unmute all Playlist Controllers.
        /// </summary>
        public static void UnmuteAllPlaylists() {
            UnmutePlaylists(PlaylistController.Instances);
        }

        private static void UnmutePlaylists(List<PlaylistController> playlists) {
            if (playlists.Count == PlaylistController.Instances.Count) {
                PlaylistsMuted = false;
            }

            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.UnmutePlaylist();
            }
        }

#endregion

#region Toggle Mute Playlist

        /// <summary>
        /// This method will allow you to toggle mute on your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void ToggleMutePlaylist() {
            ToggleMutePlaylist(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will allow you to toggle mute on a Playlist Controller by name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void ToggleMutePlaylist(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToToggleMute.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "PausePlaylist")) {
                    return;
                }

                Instance.ControllersToToggleMute.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToToggleMute.Add(pl);
                }
            }

            ToggleMutePlaylists(Instance.ControllersToToggleMute);
        }

        /// <summary>
        /// This method will allow you to toggle mute on all Playlist Controllers.
        /// </summary>
        public static void ToggleMuteAllPlaylists() {
            ToggleMutePlaylists(PlaylistController.Instances);
        }

        private static void ToggleMutePlaylists(List<PlaylistController> playlists) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.ToggleMutePlaylist();
            }
        }

#endregion

#region Pause Playlist

        /// <summary>
        /// This method will allow you to pause your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void PausePlaylist() {
            PausePlaylist(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will allow you to pause a Playlist Controller by name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void PausePlaylist(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToPause.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "PausePlaylist")) {
                    return;
                }

                Instance.ControllersToPause.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToPause.Add(pl);
                }
            }

            PausePlaylists(Instance.ControllersToPause);
        }

        /// <summary>
        /// This method will allow you to pause all Playlist Controllers.
        /// </summary>
        public static void PauseAllPlaylists() {
            PausePlaylists(PlaylistController.Instances);
        }

        private static void PausePlaylists(List<PlaylistController> playlists) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.PausePlaylist();
            }
        }

#endregion

#region Unpause Playlist

        /// <summary>
        /// This method will allow you to unpause a paused Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void UnpausePlaylist() {
            UnpausePlaylist(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will allow you to unpause a paused Playlist Controller by name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void UnpausePlaylist(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToUnpause.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "UnpausePlaylist")) {
                    return;
                }

                Instance.ControllersToUnpause.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToUnpause.Add(pl);
                }
            }

            UnpausePlaylists(Instance.ControllersToUnpause);
        }

        /// <summary>
        /// This method will allow you to unpause all paused Playlist Controllers.
        /// </summary>
        public static void UnpauseAllPlaylists() {
            UnpausePlaylists(PlaylistController.Instances);
        }

        private static void UnpausePlaylists(List<PlaylistController> controllers) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < controllers.Count; i++) {
                aList = controllers[i];
                aList.UnpausePlaylist();
            }
        }

#endregion

#region Stop Playlist

        /// <summary>
        /// This method will stop a Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void StopPlaylist() {
            StopPlaylist(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will stop a Playlist Controller by name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void StopPlaylist(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToStop.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "StopPlaylist")) {
                    return;
                }

                Instance.ControllersToStop.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToStop.Add(pl);
                }
            }

            StopPlaylists(Instance.ControllersToStop);
        }

        /// <summary>
        /// This method will allow you to stop all Playlist Controllers.
        /// </summary>
        public static void StopAllPlaylists() {
            StopPlaylists(PlaylistController.Instances);
        }

        private static void StopPlaylists(List<PlaylistController> playlists) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.StopPlaylist();
            }
        }

#endregion

#region Next Playlist Clip

        /// <summary>
        /// This method will advance the Playlist to the next clip in your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void TriggerNextPlaylistClip() {
            TriggerNextPlaylistClip(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will advance the Playlist to the next clip in the Playlist Controller you name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void TriggerNextPlaylistClip(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToTrigNext.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "TriggerNextPlaylistClip")) {
                    return;
                }

                Instance.ControllersToTrigNext.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToTrigNext.Add(pl);
                }
            }

            NextPlaylistClips(Instance.ControllersToTrigNext);
        }

        /// <summary>
        /// This method will allow you to advance Playlists in all Playlist Controllers to the next clip in their Playlist.
        /// </summary>
        public static void TriggerNextClipAllPlaylists() {
            NextPlaylistClips(PlaylistController.Instances);
        }

        private static void NextPlaylistClips(List<PlaylistController> playlists) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.PlayNextSong();
            }
        }

#endregion

#region Random Playlist Clip

        /// <summary>
        /// This method will play a random clip in the current Playlist for your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void TriggerRandomPlaylistClip() {
            TriggerRandomPlaylistClip(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will play a random clip in the current Playlist for the Playlist Controller you name.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void TriggerRandomPlaylistClip(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToTrigRandom.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "TriggerRandomPlaylistClip")) {
                    return;
                }

                Instance.ControllersToTrigRandom.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToTrigRandom.Add(pl);
                }
            }

            RandomPlaylistClips(Instance.ControllersToTrigRandom);
        }

        /// <summary>
        /// This method will allow you to play a random clip in all Playlist Controllers using their currenct Playlist
        /// </summary>
        public static void TriggerRandomClipAllPlaylists() {
            RandomPlaylistClips(PlaylistController.Instances);
        }

        private static void RandomPlaylistClips(List<PlaylistController> playlists) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.PlayRandomSong();
            }
        }

#endregion

#region RestartPlaylist

        /// <summary>
        /// This method will restart the current Playlist in the Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        public static void RestartPlaylist() {
            RestartPlaylist(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will restart a Playlist in the Playlist Controller. 
        /// </summary>
        /// <param name="playlistControllerName">The Playlist Controller.</param>
        public static void RestartPlaylist(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            PlaylistController controller;

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "RestartPlaylist")) {
                    return;
                }

                controller = pcs[0];
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl == null) {
                    return;
                }

                controller = pl;
            }

            if (controller != null) {
                RestartPlaylists(new List<PlaylistController>() { controller });
            }
        }

        /// <summary>
        /// This method will allow you to restart all Playlists.
        /// </summary>
        public static void RestartAllPlaylists() {
            RestartPlaylists(PlaylistController.Instances);
        }

        private static void RestartPlaylists(List<PlaylistController> playlists) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.RestartPlaylist();
            }
        }

#endregion

#region StartPlaylist

        /// <summary>
        /// This method is used to start a Playlist whether it's already loaded and playing or not.
        /// </summary>
        /// <param name="playlistName">The name of the Playlist to start</param>
        public static void StartPlaylist(string playlistName) {
            StartPlaylist(OnlyPlaylistControllerName, playlistName);
        }
        
        /// <summary>
        /// This method is used to start a Playlist whether it's already loaded and playing or not, on a specific Clip.
        /// </summary>
        /// <param name="playlistName">The name of the Playlist to start</param>
        /// <param name="clipName">The name of the Clip to play</param>
        public static void StartPlaylistOnClip(string playlistName, string clipName) {
            StartPlaylist(OnlyPlaylistControllerName, playlistName, clipName);
        }

        /// <summary>
        /// This method is used to start a Playlist whether it's already loaded and playing or not.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller to use</param>
        /// <param name="playlistName">The name of the Playlist to start</param>
        /// <param name="clipName"><b>Optional</b> - The name of the Clip to play</param>
        public static void StartPlaylist(string playlistControllerName, string playlistName, string clipName = null) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToStart.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "StartPlaylist")) {
                    return;
                }

                Instance.ControllersToStart.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToStart.Add(pl);
                }
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < Instance.ControllersToStart.Count; i++)
            {
                Instance.ControllersToStart[i].StartPlaylist(playlistName, clipName);
            }
        }

#endregion

#region Stop Looping Current Song

        /// <summary>
        /// This method will stop looping the current song on all Playlist Controllers so the next can play when it's finished (if auto-advance is on).
        /// </summary>
        public static void StopLoopingAllCurrentSongs() {
            StopLoopingCurrentSongs(PlaylistController.Instances);
        }


        /// <summary>
        /// This method will stop looping the current song so the next can play when it's finished (if auto-advance is on). Use this method when only one Playlist Controller exists.
        /// </summary>
        public static void StopLoopingCurrentSong() {
            StopLoopingCurrentSong(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will stop looping the current song so the next can play when it's finished (if auto-advance is on). Use this method when more than one Playlist Controller exists.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void StopLoopingCurrentSong(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            PlaylistController controller;

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "StopLoopingCurrentSong")) {
                    return;
                }

                controller = pcs[0];
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl == null) {
                    return;
                }

                controller = pl;
            }

            if (controller != null) {
                StopLoopingCurrentSongs(new List<PlaylistController> { controller });
            }
        }

        private static void StopLoopingCurrentSongs(List<PlaylistController> playlistControllers) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aController;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlistControllers.Count; i++) {
                aController = playlistControllers[i];
                aController.StopLoopingCurrentSong();
            }
        }

#endregion

#region Stop Playlist After Current Song

        /// <summary>
        /// This method will stop the Playlist after the current song on all Playlist Controllers.
        /// </summary>
        public static void StopAllPlaylistsAfterCurrentSongs() {
            StopPlaylistAfterCurrentSongs(PlaylistController.Instances);
        }
        
        /// <summary>
        /// This method will stop the Playlist after the current song.
        /// </summary>
        public static void StopPlaylistAfterCurrentSong() {
            StopPlaylistAfterCurrentSong(OnlyPlaylistControllerName);
        }

        /// <summary>
        /// This method will stop the Playlist after the current song. Use this method when more than one Playlist Controller exists.
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void StopPlaylistAfterCurrentSong(string playlistControllerName) {
            var pcs = PlaylistController.Instances;

            PlaylistController controller;

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "StopPlaylistAfterCurrentSong")) {
                    return;
                }

                controller = pcs[0];
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl == null) {
                    return;
                }

                controller = pl;
            }

            if (controller != null) {
                StopPlaylistAfterCurrentSongs(new List<PlaylistController> { controller });
            }
        }

        private static void StopPlaylistAfterCurrentSongs(List<PlaylistController> playlistControllers) {
            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aController;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlistControllers.Count; i++) {
                aController = playlistControllers[i];
                aController.StopPlaylistAfterCurrentSong();
            }
        }

#endregion

#region Queue Clip

        /// <summary>
        /// This method will play an Audio Clip by name that's in the current Playlist of your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter. This requires auto-advance to work.
        /// </summary>
        /// <param name="clipName">The name of the clip.</param>
        public static void QueuePlaylistClip(string clipName) {
            QueuePlaylistClip(OnlyPlaylistControllerName, clipName);
        }

        /// <summary>
        /// This method will play an Audio Clip by name that's in the current Playlist of the Playlist Controller you name, as soon as the currently playing song is over. Loop will be turned off on the current song. This requires auto-advance to work.
        /// </summary>
        /// <param name="clipName">The name of the clip.</param>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        public static void QueuePlaylistClip(string playlistControllerName, string clipName) {
            var pcs = PlaylistController.Instances;

            PlaylistController controller;

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "QueuePlaylistClip")) {
                    return;
                }

                controller = pcs[0];
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl == null) {
                    return;
                }

                controller = pl;
            }

            if (controller != null) {
                controller.QueuePlaylistClip(clipName);
            }
        }

#endregion

#region Trigger Playlist Clip

        /// <summary>
        /// This method will play an Audio Clip by name that's in the current Playlist of your Playlist Controller. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        /// <param name="clipName">The name of the clip.</param>
        /// <returns>bool - whether the song was played or not.</returns>
        public static bool TriggerPlaylistClip(string clipName) {
            return TriggerPlaylistClip(OnlyPlaylistControllerName, clipName);
        }

        /// <summary>
        /// This method will play an Audio Clip by name that's in the current Playlist of the Playlist Controller you name.
        /// </summary>
        /// <param name="clipName">The name of the clip.</param>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        /// <returns>bool - whether the song was played or not.</returns>
        public static bool TriggerPlaylistClip(string playlistControllerName, string clipName) {
            var pcs = PlaylistController.Instances;

            PlaylistController controller;

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "TriggerPlaylistClip")) {
                    return false;
                }

                controller = pcs[0];
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl == null) {
                    return false;
                }

                controller = pl;
            }

            if (controller == null) {
                return false;
            }

            return controller.TriggerPlaylistClip(clipName);
        }

#endregion

#region ChangePlaylistByName

        /// <summary>
        /// This method will change the current Playlist in the Playlist Controller to a Playlist whose name you specify. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        /// <param name="playlistName">The name of the new Playlist.</param>
        /// <param name="playFirstClip"><b>Optional</b> - defaults to True. If you specify false, the first clip in the Playlist will not automatically play.</param>
        public static void ChangePlaylistByName(string playlistName, bool playFirstClip = true) {
            ChangePlaylistByName(OnlyPlaylistControllerName, playlistName, playFirstClip);
        }

        /// <summary>
        /// This method will play an Audio Clip by name that's in the current Playlist of the Playlist Controller you name.
        /// </summary>
        /// <param name="playlistControllerName">The Playlist Controller name</param>
        /// <param name="playlistName">The name of the new Playlist.</param>
        /// <param name="playFirstClip"><b>Optional</b> - defaults to True. If you specify false, the first clip in the Playlist will not automatically play.</param>
        public static void ChangePlaylistByName(string playlistControllerName, string playlistName,
            bool playFirstClip = true) {
            var pcs = PlaylistController.Instances;

            PlaylistController controller;

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "ChangePlaylistByName")) {
                    return;
                }

                controller = pcs[0];
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl == null) {
                    return;
                }

                controller = pl;
            }

            if (controller != null) {
                controller.ChangePlaylist(playlistName, playFirstClip);
            }
        }

#endregion

#region Playlist Fade To Volume

        /// <summary>
        /// This method will fade the volume of the Playlist Controller over X seconds. You should not use this if you have more than one Playlist Controller. Use the overloaded method instead, it takes a playlistControllerName parameter.
        /// </summary>
        /// <param name="targetVolume">The target volume of the Playlist.</param>
        /// <param name="fadeTime">The time to fade completely to the target volume.</param>
        public static void FadePlaylistToVolume(float targetVolume, float fadeTime) {
            FadePlaylistToVolume(OnlyPlaylistControllerName, targetVolume, fadeTime);
        }

        /// <summary>
        /// This method will fade the volume of the Playlist Controller whose name you specify over X seconds. 
        /// </summary>
        /// <param name="playlistControllerName">The name of the Playlist Controller.</param>
        /// <param name="targetVolume">The target volume of the Playlist.</param>
        /// <param name="fadeTime">The time to fade completely to the target volume.</param>
        public static void FadePlaylistToVolume(string playlistControllerName, float targetVolume, float fadeTime) {
            var pcs = PlaylistController.Instances;

            Instance.ControllersToFade.Clear();

            if (playlistControllerName == OnlyPlaylistControllerName) {
                if (!IsOkToCallOnlyPlaylistMethod(pcs, "FadePlaylistToVolume")) {
                    return;
                }

                Instance.ControllersToFade.Add(pcs[0]);
            } else {
                // multiple playlist controllers
                var pl = PlaylistController.InstanceByName(playlistControllerName);
                if (pl != null) {
                    Instance.ControllersToFade.Add(pl);
                }
            }

            FadePlaylists(Instance.ControllersToFade, targetVolume, fadeTime);
        }

        /// <summary>
        /// This method will allow you to fade all current Playlists used by Playlist Controllers to a target volume over X seconds.
        /// </summary>
        public static void FadeAllPlaylistsToVolume(float targetVolume, float fadeTime) {
            FadePlaylists(PlaylistController.Instances, targetVolume, fadeTime);
        }

        private static void FadePlaylists(List<PlaylistController> playlists, float targetVolume, float fadeTime) {
            if (targetVolume < 0f || targetVolume > 1f) {
                Debug.LogError("Illegal volume passed to FadePlaylistToVolume: '" + targetVolume +
                               "'. Legal volumes are between 0 and 1");
                return;
            }

            // ReSharper disable once TooWideLocalVariableScope
            PlaylistController aList;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < playlists.Count; i++) {
                aList = playlists[i];
                aList.FadeToVolume(targetVolume, fadeTime);
            }
        }

#endregion


        /// <summary>
        /// This method will allow you to add a Playlist via code.
        /// </summary>
        /// <param name="playlist">The playlist with all settings included</param>
        /// <param name="errorOnDuplicate">Whether or not to log an error if the Playlist already exists (same name).</param>
        public static void CreatePlaylist(Playlist playlist, bool errorOnDuplicate) {
            var pl = GrabPlaylist(playlist.playlistName, false);

            if (pl != null) {
                if (errorOnDuplicate) {
                    Debug.LogError("You already have a Playlist Controller with the name '" + pl.playlistName +
                                   "'. You must name them all uniquely. Not adding duplicate named Playlist.");
                }

                return;
            }

            MusicPlaylists.Add(playlist);
        }

        /// <summary>
        /// This method will allow you to delete a Playlist via code.
        /// </summary>
        /// <param name="playlistName">The playlist name</param>
        public static void DeletePlaylist(string playlistName) {
            if (SafeInstance == null) {
                return;
            }

            var pl = GrabPlaylist(playlistName);

            if (pl == null) {
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < PlaylistController.Instances.Count; i++) {
                var pc = PlaylistController.Instances[i];
                if (pc.PlaylistName != playlistName) {
                    continue;
                }
                pc.StopPlaylist();
                break;
            }

            MusicPlaylists.Remove(pl);
        }

        /// <summary>
        /// This method will allow you to add a song to a Playlist by code.
        /// </summary>
        /// <param name="playlistName">The name of the Playlist to add the song to.</param>
        /// <param name="song">The Audio clip of the song.</param>
        /// <param name="loopSong">Optional - whether or not to loop the song.</param>
        /// <param name="songPitch">Optional - the pitch of the song.</param>
        /// <param name="songVolume">Optional - The volume of the song.</param>
        /// <param name="alias">Optional - The alias for the song.</param>
        public static void AddSongToPlaylist(string playlistName, AudioClip song, bool loopSong = false,
            float songPitch = 1f, float songVolume = 1f, string alias = null) {
            
            var newSong = new MusicSetting() {
                    clip = song,
                    alias = alias,
                    isExpanded = true,
                    isLoop = loopSong,
                    pitch = songPitch,
                    volume = songVolume
            };

            AddSongToPlaylist(playlistName, newSong);
        }

        /// <summary>
        /// This method will allow you to add a song to a Playlist by code.
        /// </summary>
        /// <param name="playlistName">The name of the Playlist to add the song to.</param>
        /// <param name="newSong">The fully populated MusicSetting object for the song.</param>
        public static void AddSongToPlaylist(string playlistName, MusicSetting newSong)
        {
            var pl = GrabPlaylist(playlistName);

            if (pl == null)
            {
                return;
            }

            // add default metadata 
            foreach (var property in pl.songMetadataProps)
            {
                if (!property.AllSongsMustContain)
                {
                    continue;
                }

                switch (property.PropertyType)
                {
                    case SongMetadataProperty.MetadataPropertyType.Boolean:
                        var bVal = new SongMetadataBoolValue(property);
                        newSong.metadataBoolValues.Add(bVal);
                        break;
                    case SongMetadataProperty.MetadataPropertyType.String:
                        var sVal = new SongMetadataStringValue(property);
                        newSong.metadataStringValues.Add(sVal);
                        break;
                    case SongMetadataProperty.MetadataPropertyType.Integer:
                        var iVal = new SongMetadataIntValue(property);
                        newSong.metadataIntValues.Add(iVal);
                        break;
                    case SongMetadataProperty.MetadataPropertyType.Float:
                        var fVal = new SongMetadataFloatValue(property);
                        newSong.metadataFloatValues.Add(fVal);
                        break;
                }
            }

            pl.MusicSettings.Add(newSong);
        }

        /// <summary>
        /// This Property can read and set the Playlist Master Volume. 
        /// </summary>
        public static float PlaylistMasterVolume {
            get { return Instance._masterPlaylistVolume; }
            set {
                Instance._masterPlaylistVolume = value;

                var pcs = PlaylistController.Instances;
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < pcs.Count; i++) {
                    pcs[i].UpdateMasterVolume();
                }
            }
        }

#endregion

#region Audio Listener methods
    /// <summary>
    /// Call this method if you have disabled the Audio Listener and enabled a different one, so Ambient Sounds will continue to work.
    /// </summary>
    /// <param name="listener"></param>
    public static void AudioListenerChanged(AudioListener listener) {
        _listenerTrans = listener.GetComponent<Transform>();
        var follower = AmbientUtil.ListenerFollower;
        if (follower != null) {
            follower.StartFollowing(_listenerTrans, AmbientUtil.ListenerFollowerTrigRadius);
        }
    }
#endregion

#region Custom Events

        /// <summary>
        /// Will fire a Custom Event during the next few frames. This is used by DynamicSoundGroupCreators to fire an event when it's done creating its items, since the listeners in the new Scene won't have been registered yet.
        /// </summary>
        /// <param name="customEventName"></param>
        /// <param name="eventOrigin"></param>
        public static void FireCustomEventNextFrame(string customEventName, Transform eventOrigin) {
            if (AppIsShuttingDown) {
                return;
            }

            if (NoGroupName == customEventName || string.IsNullOrEmpty(customEventName)) {
                return;
            }

            if (!CustomEventExists(customEventName) && !IsWarming) {
                Debug.LogError("Custom Event '" + customEventName + "' was not found in Master Audio.");
                return;
            }

            Instance.CustomEventsToFire.Enqueue(new CustomEventToFireInfo {
                eventName = customEventName,
                eventOrigin = eventOrigin
            });
        }

        /// <summary>
        /// This method is used by MasterAudio to keep track of enabled CustomEventReceivers automatically. This is called when then CustomEventReceiver prefab is enabled.
        /// </summary>
        /// <param name="receiver">The receiver object interface.</param>
        /// <param name="receiverTrans">The receiver object Transform.</param>
        public static void AddCustomEventReceiver(ICustomEventReceiver receiver, Transform receiverTrans) {
            if (AppIsShuttingDown) {
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            var events = receiver.GetAllEvents();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < events.Count; i++) {
                var anEvent = events[i];
                if (!receiver.SubscribesToEvent(anEvent.customEventName)) {
                    continue;
                }

                if (!Instance.ReceiversByEventName.ContainsKey(anEvent.customEventName)) {
                    Instance.ReceiversByEventName.Add(anEvent.customEventName, new Dictionary<ICustomEventReceiver, Transform>
                    {
                        {receiver, receiverTrans}
                    });
                } else {
                    var dict = Instance.ReceiversByEventName[anEvent.customEventName];
                    if (dict.ContainsKey(receiver)) {
                        continue;
                    }

                    dict.Add(receiver, receiverTrans);
                }
            }
        }

        /// <summary>
        /// This method is used by MasterAudio to keep track of enabled CustomEventReceivers automatically. This is called when then CustomEventReceiver prefab is disabled.
        /// </summary>
        /// <param name="receiver">The receiver object interface.</param>
        public static void RemoveCustomEventReceiver(ICustomEventReceiver receiver) {
            if (AppIsShuttingDown || SafeInstance == null) {
                if (SafeInstance != null) {
                    // remove it from all events if it's trying to die from Scene reload.
                    foreach (var key in Instance.ReceiversByEventName.Keys) {
                        Instance.ReceiversByEventName[key].Remove(receiver);
                    }
                }
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < Instance.customEvents.Count; i++) {
                var anEvent = Instance.customEvents[i];
                if (!receiver.SubscribesToEvent(anEvent.EventName)) {
                    continue;
                }

                var dict = Instance.ReceiversByEventName[anEvent.EventName];
                dict.Remove(receiver);
            }
        }

        /*! \cond PRIVATE */
        public static List<Transform> ReceiversForEvent(string customEventName) {
            var receivers = new List<Transform>();

            if (!Instance.ReceiversByEventName.ContainsKey(customEventName)) {
                return receivers;
            }

            var dict = Instance.ReceiversByEventName[customEventName];

            foreach (var receiver in dict.Keys) {
                if (receiver.SubscribesToEvent(customEventName)) {
                    receivers.Add(dict[receiver]);
                }
            }

            return receivers;
        }

        /// <summary>
        /// Creates the custom event category if not there.
        /// </summary>
        /// <returns>The custom event category if not there.</returns>
        /// <param name="categoryName">Category name.</param>
        /// <param name="actorInstanceId">The actor instanceId of the creator. Used the track if another Dynamic Sound Group Creator is still active with the bus so we don't delete it yet.</param>
        /// <param name="errorOnDuplicates">Will log a duplicate if you pass "true" in.</param>
        /// <param name="isTemporary">If set to <c>true</c> is temporary.</param>
        public static CustomEventCategory CreateCustomEventCategoryIfNotThere(string categoryName, int? actorInstanceId, bool errorOnDuplicates, bool isTemporary) {
            if (AppIsShuttingDown) {
                return null;
            }

            var matchingCat = Instance.customEventCategories.Find(delegate(CustomEventCategory cat)
            {
                return cat.CatName == categoryName && cat.IsTemporary == true;
            });

            if (matchingCat == null) {
                matchingCat = new CustomEventCategory() {
                    CatName = categoryName,
                    ProspectiveName = categoryName,
                    IsTemporary = isTemporary
                };
                Instance.customEventCategories.Add(matchingCat);
            } else {
                if (errorOnDuplicates)
                {
                    Debug.LogError("You already have a Custom Event Category with the name '" + categoryName +
                                   "'. You must name them all uniquely. Not adding duplicate named Custom Event Category.");
                    return null;
                }
            }

            if (actorInstanceId.HasValue)
            {
                matchingCat.AddActorInstanceId(actorInstanceId.Value);
            }

            return matchingCat;
        }
        /*! \endcond */

        /// <summary>
        /// This method is used to create a Custom Event at runtime.
        /// </summary>
        /// <param name="customEventName">The name of the custom event.</param>
        /// <param name="eventReceiveMode">The receive mode of the event.</param>
        /// <param name="distanceThreshold">The min or max distance to transmit the event to (optional).</param>
        /// <param name="receiveFilter">Type to filter by (optional).</param>
        /// <param name="filterModeQty">The number to limit the filter by (optional).</param>
        /// <param name="categoryName">The category of the custom event.</param>
        /// <param name="actorInstanceId">The actor instanceId of the creator. Used the track if another Dynamic Sound Group Creator is still active with the bus so we don't delete it yet.</param>
        /// <param name="isTemporary">Whether the category of the custom event is temporary or not.</param>
        /// <param name="errorOnDuplicate">Whether or not to log an error if the event already exists.</param>
        public static void CreateCustomEvent(string customEventName, CustomEventReceiveMode eventReceiveMode,
            float distanceThreshold, EventReceiveFilter receiveFilter, int filterModeQty,
            int? actorInstanceId,
            string categoryName = "", 
            bool isTemporary = false, bool errorOnDuplicate = true) {

            if (AppIsShuttingDown) {
                return;
            }

            CustomEvent matchingEvent = null;

            for (var i = 0; i < Instance.customEvents.Count; i++)
            {
                var anEvent = Instance.customEvents[i];
                if (anEvent.EventName == customEventName)
                {
                    matchingEvent = anEvent;
                    break;
                }
            } 

            if (matchingEvent != null) {
                if (errorOnDuplicate) {
                    Debug.LogError("You already have a Custom Event named '" + customEventName + "'. No need to add it again.");
                    return;
                }
            } else {
                if (string.IsNullOrEmpty(categoryName))
                {
                    categoryName = Instance.customEventCategories[0].CatName;
                }

                matchingEvent = new CustomEvent(customEventName)
                {
                    eventReceiveMode = eventReceiveMode,
                    distanceThreshold = distanceThreshold,
                    eventRcvFilterMode = receiveFilter,
                    filterModeQty = filterModeQty,
                    categoryName = categoryName,
                    isTemporary = isTemporary
                };

                Instance.customEvents.Add(matchingEvent);
            }

            if (actorInstanceId.HasValue)
            {
                matchingEvent.AddActorInstanceId(actorInstanceId.Value);
            }
        }

        /// <summary>
        /// This method is used to delete a temporary Custom Event at runtime.
        /// </summary>
        /// <param name="customEventName">The name of the custom event.</param>
        public static void DeleteCustomEvent(string customEventName) {
            if (AppIsShuttingDown || SafeInstance == null) {
                return;
            }

            Instance.customEvents.RemoveAll(delegate (CustomEvent obj) {
                return obj.EventName == customEventName;
            });
        }

        /// <summary>
        /// This will find a Custom Event by name.
        /// </summary>
        /// <param name="customEventName"></param>
        /// <returns></returns>
        public static CustomEvent GetCustomEventByName(string customEventName) {
            for (var i = 0; i < Instance.customEvents.Count; i++)
            {
                var anEvent = Instance.customEvents[i];
                if (anEvent.EventName == customEventName)
                {
                    return anEvent;
                }
            }

            return null;
        }

        /// <summary>
        /// Calling this method will fire a Custom Event at the originPoint position. All CustomEventReceivers with the named event specified will do whatever action is assigned to them. If there is a distance criteria applied to receivers, it will be applied.
        /// </summary>
        /// <param name="customEventName">The name of the custom event.</param>
        /// <param name="originObject">The Transform origin of the event.</param>
        /// <param name="logDupe">Whether or not to log an error with duplicate event firing.</param>
        public static void FireCustomEvent(string customEventName, Transform originObject, bool logDupe = true) {
            if (AppIsShuttingDown) {
                return;
            }

            if (NoGroupName == customEventName || string.IsNullOrEmpty(customEventName)) {
                return;
            }

            if (originObject == null) {
                Debug.LogError("Custom Event '" + customEventName +
                               "' cannot be fired without an originObject passed in.");
                return;
            }

            if (!CustomEventExists(customEventName) && !IsWarming) {
                Debug.LogError("Custom Event '" + customEventName + "' was not found in Master Audio.");
                return;
            }

            var customEvent = GetCustomEventByName(customEventName);

            if (customEvent == null) {
                // for warming
                return;
            }

            if (customEvent.frameLastFired >= AudioUtil.FrameCount) {
                if (logDupe) {
                    Debug.LogWarning("Already fired Custom Event '" + customEventName +
                                     "' this frame or later. Cannot be fired twice in the same frame.");
                }
                return;
            }

            customEvent.frameLastFired = AudioUtil.FrameCount;

            if (!Instance.disableLogging && Instance.logCustomEvents) {
                Debug.Log("Firing Custom Event: " + customEventName);
            }

            if (!Instance.ReceiversByEventName.ContainsKey(customEventName)) {
                // no receivers
                return;
            }

            var originPoint = originObject.position;

            float? sqrDist = null;

            var dict = Instance.ReceiversByEventName[customEventName];

            Instance.ValidReceivers.Clear();

            var validReceiversSet = false;

            switch (customEvent.eventReceiveMode) {
                case CustomEventReceiveMode.Never:
                    if (Instance.LogSounds) {
                        Debug.LogWarning("Custom Event '" + customEventName +
                                         "' not being transmitted because it is set to 'Never transmit'.");
                    }
                    return; // no transmission.
                case CustomEventReceiveMode.OnChildGameObject:
                    Instance.ValidReceivers.AddRange(GetChildReceivers(originObject, customEventName, false));
                    validReceiversSet = true;
                    break;
                case CustomEventReceiveMode.OnParentGameObject:
                    Instance.ValidReceivers.AddRange(GetParentReceivers(originObject, customEventName, false));
                    validReceiversSet = true;
                    break;
                case CustomEventReceiveMode.OnSameOrChildGameObject:
                    Instance.ValidReceivers.AddRange(GetChildReceivers(originObject, customEventName, true));
                    validReceiversSet = true;
                    break;
                case CustomEventReceiveMode.OnSameOrParentGameObject:
                    Instance.ValidReceivers.AddRange(GetParentReceivers(originObject, customEventName, true));
                    validReceiversSet = true;
                    break;
                case CustomEventReceiveMode.WhenDistanceLessThan:
                case CustomEventReceiveMode.WhenDistanceMoreThan:
                    sqrDist = customEvent.distanceThreshold * customEvent.distanceThreshold;
                    break;
            }

            if (!validReceiversSet) {
                // only used for "OnXGameObject" Send To Receiver modes
                foreach (var receiver in dict.Keys) {
                    switch (customEvent.eventReceiveMode) {
                        case CustomEventReceiveMode.WhenDistanceLessThan:
                            var dist1 = (dict[receiver].position - originPoint).sqrMagnitude;
                            if (dist1 > sqrDist) {
                                continue;
                            }
                            break;
                        case CustomEventReceiveMode.WhenDistanceMoreThan:
                            var dist2 = (dict[receiver].position - originPoint).sqrMagnitude;
                            if (dist2 < sqrDist) {
                                continue;
                            }
                            break;
                        case CustomEventReceiveMode.OnSameGameObject:
                            if (originObject != dict[receiver]) {
                                continue; // not same Transform
                            }
                            break;
                    }

                    Instance.ValidReceivers.Add(receiver);
                }
            }

            var mustSortAndFilter = customEvent.eventRcvFilterMode != EventReceiveFilter.All &&
                                    customEvent.filterModeQty < Instance.ValidReceivers.Count && Instance.ValidReceivers.Count > 1;

            if (!mustSortAndFilter) {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < Instance.ValidReceivers.Count; i++) {
                    Instance.ValidReceivers[i].ReceiveEvent(customEventName, originPoint);
                }
                return;
            }

            // further filter by "random" or "closest"
            Instance.ValidReceiverCandidates.Clear();

            // ReSharper disable TooWideLocalVariableScope
            Transform receiverTrans;
            int randId;
            float dist;
            // ReSharper restore TooWideLocalVariableScope

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < Instance.ValidReceivers.Count; i++) {
                var receiver = Instance.ValidReceivers[i];
                receiverTrans = dict[receiver];
                dist = 0f;
                randId = 0;

                switch (customEvent.eventRcvFilterMode) {
                    case EventReceiveFilter.Random:
                        randId = UnityEngine.Random.Range(0, 1000);
                        break;
                    case EventReceiveFilter.Closest:
                        dist = (receiverTrans.position - originPoint).sqrMagnitude;
                        break;
                }

                Instance.ValidReceiverCandidates.Add(new CustomEventCandidate(dist, receiver, receiverTrans, randId));
            }

            int firstDeadIndex;
            int countToRemove;

            // filter out based on fields above
            switch (customEvent.eventRcvFilterMode) {
                case EventReceiveFilter.Closest:
                    Instance.ValidReceiverCandidates.Sort(delegate (CustomEventCandidate x, CustomEventCandidate y) {
                        return x.DistanceAway.CompareTo(y.DistanceAway);
                    });

                    firstDeadIndex = customEvent.filterModeQty;
                    countToRemove = Instance.ValidReceiverCandidates.Count - firstDeadIndex;
                    Instance.ValidReceiverCandidates.RemoveRange(firstDeadIndex, countToRemove);

                    break;
                case EventReceiveFilter.Random:
                    Instance.ValidReceiverCandidates.Sort(delegate (CustomEventCandidate x, CustomEventCandidate y) {
                        return x.RandomId.CompareTo(y.RandomId);
                    });

                    firstDeadIndex = customEvent.filterModeQty;
                    countToRemove = Instance.ValidReceiverCandidates.Count - firstDeadIndex;
                    Instance.ValidReceiverCandidates.RemoveRange(firstDeadIndex, countToRemove);
                    break;
            }

            // filter done, fire events!
            for (var i = 0; i < Instance.ValidReceiverCandidates.Count; i++) {
                Instance.ValidReceiverCandidates[i].Receiver.ReceiveEvent(customEventName, originPoint);
            }
        }

        /// <summary>
        /// Calling this method will return whether or not the specified Custom Event exists.
        /// </summary>
        /// <param name="customEventName">The name of the custom event.</param>
        public static bool CustomEventExists(string customEventName) {
            if (AppIsShuttingDown) {
                return true;
            }

            for (var i = 0; i < Instance.customEvents.Count; i++)
            {
                var anEvent = Instance.customEvents[i];
                if (anEvent.EventName == customEventName)
                {
                    return true;
                }
            }

            return false;
        }

        /*! \cond PRIVATE */
        private static List<ICustomEventReceiver> GetChildReceivers(Transform origin, string eventName, bool includeSelf) {
            var components = origin.GetComponentsInChildren<ICustomEventReceiver>().ToList();

            components.RemoveAll(delegate (ICustomEventReceiver rec) {
                return !rec.SubscribesToEvent(eventName);
            });

            if (includeSelf) {
                return components;
            }

            return FilterOutSelf(components, origin);
        }

        private static List<ICustomEventReceiver> GetParentReceivers(Transform origin, string eventName, bool includeSelf) {
            var components = origin.GetComponentsInParent<ICustomEventReceiver>().ToList();

            components.RemoveAll(delegate (ICustomEventReceiver rec) {
                return !rec.SubscribesToEvent(eventName);
            });

            if (includeSelf) {
                return components;
            }

            return FilterOutSelf(components, origin);
        }

        private static List<ICustomEventReceiver> FilterOutSelf(List<ICustomEventReceiver> sourceList, Transform origin) {
            var matchOriginComponents = new List<ICustomEventReceiver>();

            foreach (var component in sourceList) {
                var mono = component as MonoBehaviour;
                if (mono == null || mono.transform != origin) {
                    continue;
                }

                matchOriginComponents.Add(component);
            }

            var failsafe = 0;
            while (matchOriginComponents.Count > 0 && failsafe < MaxComponents) {
                sourceList.Remove(matchOriginComponents[0]);
                matchOriginComponents.RemoveAt(0);
                failsafe++;
            }

            return sourceList;
        }
        /*! \endcond */

#endregion

#region Logging (only when turned on via Inspector)

        private static bool LoggingEnabledForGroup(MasterAudioGroup grp) {
            if (IsWarming) {
                return false;
            }

            if (Instance.disableLogging) {
                return false;
            }

            if (grp != null && grp.logSound) {
                return true;
            }

            return Instance.LogSounds;
        }

        private static void LogMessage(string message) {
            if (Instance.disableLogging) {
                return;
            }

            Debug.Log("T: " + Time.time + " - MasterAudio: " + message);
        }

        /// <summary>
        /// This gets or sets whether Logging is enabled in Master Audio
        /// </summary>
        public static bool LogSoundsEnabled {
            get { return Instance.LogSounds; }
            set { Instance.LogSounds = value; }
        }

        /// <summary>
        /// This gets or sets whether Logging Out Of Voices scenarios are enabled in Master Audio
        /// </summary>
        public static bool LogOutOfVoices {
            get { return Instance.logOutOfVoices; }
            set { Instance.logOutOfVoices = value; }
        }

        /*! \cond PRIVATE */
        public static void LogWarning(string msg) {
            if (Instance.disableLogging) {
                return;
            }

            Debug.LogWarning(msg);
        }

        public static void LogWarningIfNeverLogged(string msg, int errorNumber) {
            if (ErrorNumbersLogged.Contains(errorNumber)) {
                return;
            }

            Debug.LogWarning(msg);
            ErrorNumbersLogged.Add(errorNumber);
        }

        public static void LogError(string msg) {
            if (Instance.disableLogging) {
                return;
            }

            Debug.LogError(msg);
        }

        public static void LogNoPlaylist(string playlistControllerName, string methodName) {
            LogWarning("There is currently no Playlist assigned to Playlist Controller '" + playlistControllerName +
                       "'. Cannot call '" + methodName + "' method.");
        }
        /*! \endcond */

        private static bool IsOkToCallOnlyPlaylistMethod(List<PlaylistController> pcs, string methodName) {
            if (pcs.Count == 0) {
                LogError(string.Format("You have no Playlist Controllers in the Scene. You cannot '{0}'.", methodName));
                return false;
            } else if (pcs.Count > 1) {
                LogError(
                    string.Format(
                        "You cannot call '{0}' without specifying a Playlist Controller name when you have more than one Playlist Controller.",
                        methodName));
                return false;
            }

            return true;
        }

#endregion

#region Ambient Sound methods
        /*! \cond PRIVATE */

		public static void SetupAmbientNextFrame(AmbientSound ambient) {
			if (AppIsShuttingDown) {
				return;
			}
			
			if (ambient == null) {
				return;
			}

            for (var i = 0; i < Instance.AmbientsToDelayedTrigger.Count; i++)
            {
                var anAmbient = Instance.AmbientsToDelayedTrigger[i];
                if (anAmbient.ambient == ambient)
                {
                    // already in the list, abort
                    return;
                }
            }

			Instance.AmbientsToDelayedTrigger.Add(new AmbientSoundToTriggerInfo {
				frameToTrigger = Time.frameCount + 1,
				ambient = ambient
			});
		}

        public static void RemoveDelayedAmbient(AmbientSound ambient) {
            if (AppIsShuttingDown) {
                return;
            }

            if (ambient == null) {
                return;
            }

            int? deadIndex = null;
            for (var i = 0; i < Instance.AmbientsToDelayedTrigger.Count; i++) {
                var anAmbient = Instance.AmbientsToDelayedTrigger[i];
                if (anAmbient.ambient != null && anAmbient.ambient == ambient) {
                    deadIndex = i;
                    break;
                }
            }

            if (deadIndex.HasValue) {
                Instance.AmbientsToDelayedTrigger.RemoveAt(deadIndex.Value);
            }
        }
			
        /// <summary>
        /// Do not call this method ever. Used internally by Ambient Sounds script.
        /// </summary>
        public static void QueueTransformFollowerForColliderPositionRecalc(TransformFollower follower) {
            if (SafeInstance == null) {
                return;
            }

            foreach (var transFollower in Instance.TransFollowerColliderPositionRecalcs) {
                if (transFollower == follower) {
                    return; // already in there. Should almost never happen except under weird circumstances.
                }
            }

            Instance.TransFollowerColliderPositionRecalcs.Enqueue(follower);
        }
        /*! \endcond */

#endregion

#region Occlusion methods
        /*! \cond PRIVATE */

        public static void AddToQueuedOcclusionRays(SoundGroupVariationUpdater updater) {
            if (SafeInstance == null) {
                return;
            }


            foreach (var occlusionRay in Instance.QueuedOcclusionRays) {
                if (occlusionRay == updater) {
                    return; // already in there. Should almost never happen except under weird circumstances.
                }
            }

            Instance.QueuedOcclusionRays.Enqueue(updater);
        }

        public static void AddToOcclusionInRangeSources(GameObject src) {
            if (!Application.isEditor || SafeInstance == null || !Instance.occlusionShowCategories) {
                return;
            }

            if (!Instance.OcclusionSourcesInRange.Contains(src)) {
                Instance.OcclusionSourcesInRange.Add(src);
            }

            if (Instance.OcclusionSourcesOutOfRange.Contains(src)) {
                Instance.OcclusionSourcesOutOfRange.Remove(src);
            }
        }

        public static void AddToOcclusionOutOfRangeSources(GameObject src) {
            if (!Application.isEditor || SafeInstance == null || !Instance.occlusionShowCategories) {
                return;
            }

            if (!Instance.OcclusionSourcesOutOfRange.Contains(src)) {
                Instance.OcclusionSourcesOutOfRange.Add(src);
            }

            if (Instance.OcclusionSourcesInRange.Contains(src)) {
                Instance.OcclusionSourcesInRange.Remove(src);
            }

            // out of range means no longer blocked
            RemoveFromBlockedOcclusionSources(src);
        }

        public static void AddToBlockedOcclusionSources(GameObject src) {
            if (!Application.isEditor || SafeInstance == null || !Instance.occlusionShowCategories) {
                return;
            }

            if (!Instance.OcclusionSourcesBlocked.Contains(src)) {
                Instance.OcclusionSourcesBlocked.Add(src);
            }
        }

        public static bool HasQueuedOcclusionRays() {
            return Instance.QueuedOcclusionRays.Count > 0;
        }

        public static SoundGroupVariationUpdater OldestQueuedOcclusionRay() {
            if (SafeInstance == null) {
                return null;
            }

            return Instance.QueuedOcclusionRays.Dequeue();
        }

        public static bool IsOcclusionFrequencyTransitioning(SoundGroupVariation variation) {
            for (var i = 0; i < Instance.VariationOcclusionFreqChanges.Count; i++) {
                var occlusionFreqChange = Instance.VariationOcclusionFreqChanges[i];
                if (occlusionFreqChange.ActingVariation == variation) {
                    return occlusionFreqChange.IsActive;
                }
            }

            return false;
        }

        public static void RemoveFromOcclusionFrequencyTransitioning(SoundGroupVariation variation) {
            for (var i = 0; i < Instance.VariationOcclusionFreqChanges.Count; i++) {
                if (Instance.VariationOcclusionFreqChanges[i].ActingVariation != variation) {
                    continue;
                }

                Instance.VariationOcclusionFreqChanges.RemoveAt(i);
                break;
            }
        }

        public static void RemoveFromBlockedOcclusionSources(GameObject src) {
            if (!Application.isEditor || SafeInstance == null || !Instance.occlusionShowCategories) {
                return;
            }

            if (Instance.OcclusionSourcesBlocked.Contains(src)) {
                Instance.OcclusionSourcesBlocked.Remove(src);
            }
        }

        public static void StopTrackingOcclusionForSource(GameObject src) {
            if (!Application.isEditor || SafeInstance == null || !Instance.occlusionShowCategories) {
                return;
            }

            if (Instance.OcclusionSourcesOutOfRange.Contains(src)) {
                Instance.OcclusionSourcesOutOfRange.Remove(src);
            }

            if (Instance.OcclusionSourcesInRange.Contains(src)) {
                Instance.OcclusionSourcesInRange.Remove(src);
            }

            if (Instance.OcclusionSourcesBlocked.Contains(src)) {
                Instance.OcclusionSourcesBlocked.Remove(src);
            }
        }

        /*! \endcond */
        #endregion

/*! \cond PRIVATE */
#if ADDRESSABLES_ENABLED
        #region Addressable methods
        public static void AddAddressableForDelayedRelease(string addressableId, int unusedSecondsLifespan)
        {
            for (var i = 0; i < Instance.AddressablesToReleaseLater.Count; i++)
            {
                var anAddressable = Instance.AddressablesToReleaseLater[i];
                if (anAddressable.AddressableId == addressableId)
                {
                    // update expire time, should not happen but good safeguard
                    anAddressable.RealtimeToRelease = Time.realtimeSinceStartup + unusedSecondsLifespan;
                    return;
                }
            }
            
            Instance.AddressablesToReleaseLater.Add(new AddressableDelayedRelease(addressableId, Time.realtimeSinceStartup + unusedSecondsLifespan));  
        }

        public static void RemoveAddressableFromDelayedRelease(string addressableId) {
            Instance.AddressablesToReleaseLater.RemoveAll(delegate (AddressableDelayedRelease adr) {
                return adr.AddressableId == addressableId;
            });
        }
        #endregion
#endif
/*! \endcond */

        #region Properties

        /*! \cond PRIVATE */
        /// <summary>
        /// This tells Master Audio if the Sound Group is the specially named one for Video Players
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static bool IsVideoPlayersGroup(string groupName)
        {
            return groupName == VideoPlayerSoundGroupName;
        }

        private static bool IsLinkedGroupPlay(SoundGroupVariation variation) {
            if (!Instance._isStoppingMultiple) {
                return false;
            }

            return Instance.VariationsStartedDuringMultiStop.Contains(variation);
        }

        /// <summary>
        /// This returns the holder Transform of the Video Players Sound Group.
        /// </summary>
        /// <returns></returns>
        public static Transform VideoPlayerSoundGroupTransform
        {
            get {
                return Instance.transform.Find(VideoPlayerSoundGroupName);
            }
        }

        /// <summary>
        /// This returns a list of all Audio Sources controlled by Master Audio
        /// </summary>
        public static List<AudioSource> MasterAudioSources {
            get {
                return Instance.AllAudioSources;
            }
        }

        public static int RemainingClipsInGroup(string sType) {
            if (!Instance._randomizer.ContainsKey(sType)) {
                return 0;
            }

            return Instance._randomizer[sType].Count;
        }

        public static Transform ListenerTrans {
            get {
                // ReSharper disable once InvertIf
                if (_listenerTrans == null || !DTMonoHelper.IsActive(_listenerTrans.gameObject)) {
                    _listenerTrans = null; // to make sure

                    var listeners = FindObjectsOfType<AudioListener>();
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < listeners.Length; i++) {
                        var listener = listeners[i];
                        if (!DTMonoHelper.IsActive(listener.gameObject)) {
                            continue;
                        }

                        _listenerTrans = listener.transform;
                    }
                }

                return _listenerTrans;
            }
        }

        public static PlaylistController OnlyPlaylistController {
            get {
                var pcs = PlaylistController.Instances;
                if (pcs.Count != 0) {
                    return pcs[0];
                }
                Debug.LogError("There are no Playlist Controller in this Scene.");
                return null;
            }
        }

        public static bool IsWarming {
			get { return SafeInstance != null && Instance._warming; }
		}

#if UNITY_EDITOR

        public MixerWidthMode MixerWidth {
            get {
                return MasterAudioSettings.Instance.MixerWidthSetting;
            }
            set {
                MasterAudioSettings.Instance.MixerWidthSetting = value;
                EditorUtility.SetDirty(MasterAudioSettings.Instance);
            }
        }

        public bool BusesShownInNarrow {
            get {
                return MasterAudioSettings.Instance.BusesShownInNarrow;
            }
            set {
                MasterAudioSettings.Instance.BusesShownInNarrow = value;
                EditorUtility.SetDirty(MasterAudioSettings.Instance);
            }
        }


#endif
        /*! \endcond */


        /// <summary>
        /// This gets or sets whether the entire Mixer is muted or not.
        /// </summary>
        public static bool MixerMuted {
            get { return Instance.mixerMuted; }
            set {
                Instance.mixerMuted = value;

                if (value) {
                    foreach (var key in Instance.AllSoundGroupNames) {
                        MuteGroup(Instance.AudioSourcesBySoundType[key].Group.GameObjectName, false);
                    }
                } else {
                    foreach (var key in Instance.AllSoundGroupNames) {
                        UnmuteGroup(Instance.AudioSourcesBySoundType[key].Group.GameObjectName, false);
                    }
                }

                if (Application.isPlaying) {
                    SilenceOrUnsilenceGroupsFromSoloChange();
                }
            }
        }

        /// <summary>
        /// This gets or sets whether the all Playlists are muted or not.
        /// </summary>
        public static bool PlaylistsMuted {
            get { return Instance.playlistsMuted; }
            set {
                Instance.playlistsMuted = value;

                var pcs = PlaylistController.Instances;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < pcs.Count; i++) {
                    if (value) {
                        pcs[i].MutePlaylist();
                    } else {
                        pcs[i].UnmutePlaylist();
                    }
                }
            }
        }

        /// <summary>
        /// This gets or sets whether music ducking is enabled.
        /// </summary>
        public bool EnableMusicDucking {
            get { return enableMusicDucking; }
            set { enableMusicDucking = value; }
        }

        /// <summary>
        /// This gets the cross-fade time for Playlists
        /// </summary>
        public float MasterCrossFadeTime {
            get { return crossFadeTime; }
        }

        /// <summary>
        /// This property will return all the Playlists set up in the Master Audio game object.
        /// </summary>
        public static List<Playlist> MusicPlaylists {
            get { return Instance.musicPlaylists; }
        }

        /// <summary>
        /// This returns of list of all Buses.
        /// </summary>
        public static List<GroupBus> GroupBuses {
            get { return Instance.groupBuses; }
        }

        /// <summary>
        /// This will get you the list of all Sound Group Names at runtime only.
        /// </summary>
        public static List<string> RuntimeSoundGroupNames {
            get {
                return Instance.AllSoundGroupNames;
            }
        }

        /// <summary>
        /// This will get you the list of all Bus Names at runtime only.
        /// </summary>
        public static List<string> RuntimeBusNames {
            get {
                Instance.AllBusNames.Clear();
                
                if (!Application.isPlaying)
                {
                    return Instance.AllBusNames;
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < Instance.groupBuses.Count; i++) {
                    Instance.AllBusNames.Add(Instance.groupBuses[i].busName);
                }

                return Instance.AllBusNames;
            }
        }

        /// <summary>
        /// This property returns a reference to the Singleton instance of MasterAudio, but does not log anything to the console. This is used by PersistentAudioSettings script only.
        /// </summary>
        public static MasterAudio SafeInstance {
            get {
                if (_instance != null) {
                    return _instance;
                }

                // ReSharper disable once ArrangeStaticMemberQualifier
                _instance = (MasterAudio)GameObject.FindObjectOfType(typeof(MasterAudio));
                return _instance;
            }
        }

        /// <summary>
        /// This property returns a reference to the Singleton instance of 
        /// </summary>
        public static MasterAudio Instance {
            get {
                if (_instance != null) {
                    return _instance;
                }
                // ReSharper disable once ArrangeStaticMemberQualifier
                _instance = (MasterAudio)GameObject.FindObjectOfType(typeof(MasterAudio));

                if (_instance == null && Application.isPlaying) {
                    Debug.LogError("There is no Master Audio prefab in this Scene. Subsequent method calls will fail.");
                }

                return _instance;
            }
            // ReSharper disable once ValueParameterNotUsed
            set {
                _instance = null; // to not cache for Inspectors
            }
        }

        /// <summary>
        /// This returns true if MasterAudio is initialized and ready to use, false otherwise.
        /// </summary>
        public static bool SoundsReady {
            get { return Instance != null && Instance._soundsLoaded; }
        }

        /// <summary>
        /// This property is used to prevent bogus Unity errors while the editor is stopping play. You should never need to read or set 
        /// </summary>
        public static bool AppIsShuttingDown { get; set; }

        /// <summary>
        /// This will return a list of all the Sound Group names.
        /// </summary>
        public List<string> GroupNames {
            get {
                var groupNames = SoundGroupHardCodedNames;

                var others = new List<string>(Trans.childCount);
                for (var i = 0; i < Trans.childCount; i++) {
                    var childName = Trans.GetChild(i).name;

                    if (ArrayListUtil.IsExcludedChildName(childName)) {
                        continue;
                    }

                    others.Add(childName);
                }

                var creators = FindObjectsOfType(typeof(DynamicSoundGroupCreator)) as DynamicSoundGroupCreator[];
                // ReSharper disable once PossibleNullReferenceException
                foreach (var dsgc in creators) {
                    var trans = dsgc.transform;
                    for (var i = 0; i < trans.childCount; ++i) {
                        var group = trans.GetChild(i).GetComponent<DynamicSoundGroup>();
                        if (group == null) {
                            continue;
                        }

                        if (others.Contains(group.name)) {
                            continue;
                        }

                        others.Add(group.name);
                    }
                }

                others.Sort();
                groupNames.AddRange(others);

                return groupNames;
            }
        }

        /*! \cond PRIVATE */
        /// <summary>
        /// Only used internally, do not use this property
        /// </summary>
        public static List<string> SoundGroupHardCodedNames {
            get {
                return new List<string> { DynamicGroupName, NoGroupName };
            }
        }
        /*! \endcond */

        /// <summary>
        /// This will return a list of all the Bus names, including the selectors for "type in" and "no bus".
        /// </summary>
        public List<string> BusNames {
            get {
                var busNames = new List<string> { DynamicGroupName, NoGroupName };

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < groupBuses.Count; i++) {
                    busNames.Add(groupBuses[i].busName);
                }

                return busNames;
            }
        }

        /// <summary>
        /// This will return a list of all the Playlists, including the selectors for "type in" and "no bus".
        /// </summary>
        public List<string> PlaylistNames {
            get {
                var playlistNames = new List<string> { DynamicGroupName, NoPlaylistName };

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < musicPlaylists.Count; i++) {
                    playlistNames.Add(musicPlaylists[i].playlistName);
                }

                return playlistNames;
            }
        }

        /// <summary>
        /// This will return a list of all the Playlists, not including the selectors for "type in" and "no bus".
        /// </summary>
        public List<string> PlaylistNamesOnly {
            get {
                var playlistNames = new List<string>(musicPlaylists.Count);

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < musicPlaylists.Count; i++) {
                    playlistNames.Add(musicPlaylists[i].playlistName);
                }

                return playlistNames;
            }
        }

        /*! \cond PRIVATE */
        public Transform Trans {
            get {
                if (_trans != null) {
                    return _trans;
                }

                _trans = GetComponent<Transform>();

                return _trans;
            }
        }

        public bool ShouldShowUnityAudioMixerGroupAssignments {
            get {
                return showUnityMixerGroupAssignment;
            }
        }
        /*! \endcond */

        /// <summary>
        /// This will return a list of all the Custom Events you have defined, including the selectors for "type in" and "none".
        /// </summary>
        public List<string> CustomEventNames {
            get {
                var customEventNames = CustomEventHardCodedNames;

                var custEvents = Instance.customEvents;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < custEvents.Count; i++) {
                    customEventNames.Add(custEvents[i].EventName);
                }

                return customEventNames;
            }
        }

        /// <summary>
        /// This will return a list of all the Custom Events you have defined, not including the selectors for "type in" and "none".
        /// </summary>
        public List<string> CustomEventNamesOnly {
            get {
                var customEventNames = new List<string>(customEvents.Count);

                var custEvents = Instance.customEvents;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < custEvents.Count; i++) {
                    customEventNames.Add(custEvents[i].EventName);
                }

                return customEventNames;
            }
        }

        /*! \cond PRIVATE */
        /// <summary>
        /// Only used internally, do not use this property
        /// </summary>
        public static List<string> CustomEventHardCodedNames {
            get {
                return new List<string> { DynamicGroupName, NoGroupName };
            }
        }
        /*! \endcond */

        /// <summary>
        /// This is the overall master volume level which can change the relative volume of all buses and Sound Groups - not Playlist Controller songs though, they have their own master volume.
        /// </summary>
        public static float MasterVolumeLevel {
            get { return Instance._masterAudioVolume; }
            set {
                Instance._masterAudioVolume = value;

                if (!Application.isPlaying) {
                    return;
                }

                for (var i = 0; i < RuntimeSoundGroupNames.Count; i++) {
                    var groupName = RuntimeSoundGroupNames[i];
                    var group = Instance.AudioSourcesBySoundType[groupName].Group;
                    SetGroupVolume(group.GameObjectName, group.groupMasterVolume);
                    // set to same volume, but it recalcs based on master volume level.
                }
            }
        }

        /*! \cond PRIVATE */
        private static bool SceneHasMasterAudio {
            get { return Instance != null; }
        }

        public static bool IgnoreTimeScale {
            get { return Instance.ignoreTimeScale; }
        }

        /*! \endcond */

        /// <summary>
        /// This gets or sets the "Dynamic Language" (needs to be set at runtime based on the user's selection) for use with localized Resource Files.
        /// </summary>
        public static SystemLanguage DynamicLanguage {
            get {
                if (!PlayerPrefs.HasKey(StoredLanguageNameKey) || string.IsNullOrEmpty(PlayerPrefs.GetString(StoredLanguageNameKey))) {
                    PlayerPrefs.SetString(StoredLanguageNameKey, SystemLanguage.Unknown.ToString());
                }

                return
                    (SystemLanguage)Enum.Parse(typeof(SystemLanguage), PlayerPrefs.GetString(StoredLanguageNameKey));
            }
            set {
                PlayerPrefs.SetString(StoredLanguageNameKey, value.ToString());
                AudioResourceOptimizer.ClearSupportLanguageFolder();
            }
        }

        /*! \cond PRIVATE */
#if UNITY_EDITOR
        public static bool RemoveUnplayedVariationDueToProbability {
            get {
                return MasterAudioSettings.Instance.RemoveUnplayedDueToProbabilityVariation;
            }
            set {
                MasterAudioSettings.Instance.RemoveUnplayedDueToProbabilityVariation = value;
                EditorUtility.SetDirty(MasterAudioSettings.Instance);
            }
        }

        public static bool UseDbScaleForVolume {
            get {
                return MasterAudioSettings.Instance.UseDbScale;
            }
            set {
                MasterAudioSettings.Instance.UseDbScale = value;
                EditorUtility.SetDirty(MasterAudioSettings.Instance);
            }
        }

        public static bool UseCentsForPitch {
            get {
                return MasterAudioSettings.Instance.UseCentsPitch;
            }
            set {
                MasterAudioSettings.Instance.UseCentsPitch = value;
                EditorUtility.SetDirty(MasterAudioSettings.Instance);
            }
        }

        public static bool HideLogoNav {
            get {
                return MasterAudioSettings.Instance.HideLogoNav;
            }
            set {
                MasterAudioSettings.Instance.HideLogoNav = value;
                EditorUtility.SetDirty(MasterAudioSettings.Instance);
            }
        }
#endif

        public static float ReprioritizeTime {
            get {
                if (Instance._repriTime < 0) {
                    Instance._repriTime = (Instance.rePrioritizeEverySecIndex + 1) * 0.1f;
                }

                return Instance._repriTime;
            }
        }

        public static void RescanGroupsNow() {
            Instance._mustRescanGroups = true;
        }

        public static void DoneRescanningGroups() {
            Instance._mustRescanGroups = false;
        }

        public static bool ShouldRescanGroups {
            get {
                if (SafeInstance == null) {
                    return false;
                }

                return Instance._mustRescanGroups;
            }
        }

        public static string ProspectiveMAPath {
            get {
                return _prospectiveMAFolder;
            }
            set {
                _prospectiveMAFolder = value;
            }
        }

#if UNITY_EDITOR
        public static string MasterAudioFolderPath {
            get {
                return MasterAudioSettings.Instance.InstallationFolderPath;
            }
            set {
                MasterAudioSettings.Instance.InstallationFolderPath = value;
                EditorUtility.SetDirty(MasterAudioSettings.Instance);
            }
        }

        public static string GroupTemplateFolder {
            get {
                return MasterAudioFolderPath + "/Sources/Prefabs/GroupTemplates/";
            }
        }

        public static string AudioSourceTemplateFolder {
            get {
                return MasterAudioFolderPath + "/Sources/Prefabs/AudioSourceTemplates/";
            }
        }

        public static List<GameObject> InRangeOcclusionSources {
            get {
                return Instance.OcclusionSourcesInRange;
            }
        }

        public static List<GameObject> OutOfRangeOcclusionSources {
            get {
                return Instance.OcclusionSourcesOutOfRange;
            }
        }

        public static List<GameObject> BlockedOcclusionSources {
            get {
                return Instance.OcclusionSourcesBlocked;
            }
        }
#endif
        /*! \endcond */

#endregion

#region Prefab Creation
        /*! \cond PRIVATE */

        /// <summary>
        /// Creates the master audio prefab in the current Scene.
        /// </summary>
        public static GameObject CreateMasterAudio() {
#if UNITY_EDITOR
            var ma = AssetDatabase.LoadAssetAtPath(MasterAudioFolderPath + "/Prefabs/MasterAudio.prefab",
                typeof(GameObject));
#else
				var ma = Resources.Load(MasterAudioDefaultFolder + "/Prefabs/MasterAudio.prefab", typeof(GameObject));
#endif
            if (ma == null) {
                Debug.LogError(
                    "Could not find MasterAudio prefab. Please update the Installation Path in the Master Audio Manager window if you have moved the folder from its default location, then try again.");
                return null;
            }

            // ReSharper disable once ArrangeStaticMemberQualifier
            var go = GameObject.Instantiate(ma) as GameObject;
            // ReSharper disable once PossibleNullReferenceException
            go.name = "MasterAudio";
            return go;
        }

        /// <summary>
        /// Creates a Playlist Controller prefab instance in the current Scene.
        /// </summary>
        public static GameObject CreatePlaylistController() {
#if UNITY_EDITOR
            var pc = AssetDatabase.LoadAssetAtPath(MasterAudioFolderPath + "/Prefabs/PlaylistController.prefab",
                typeof(GameObject));
#else
			var pc = Resources.Load(MasterAudioDefaultFolder + "/Prefabs/PlaylistController.prefab", typeof(GameObject));
#endif
            if (pc == null) {
                Debug.LogError(
                    "Could not find PlaylistController prefab. Please update the Installation Path in the Master Audio Manager window if you have moved the folder from its default location, then try again.");
                return null;
            }

            // ReSharper disable once ArrangeStaticMemberQualifier
            var go = GameObject.Instantiate(pc) as GameObject;
            // ReSharper disable once PossibleNullReferenceException
            go.name = "PlaylistController";
            return go;
        }

        /// <summary>
        /// Creates a Dynamic Sound Group Creator prefab instance in the current Scene.
        /// </summary>
        public static GameObject CreateDynamicSoundGroupCreator() {
#if UNITY_EDITOR
            var pc = AssetDatabase.LoadAssetAtPath(MasterAudioFolderPath + "/Prefabs/DynamicSoundGroupCreator.prefab",
                typeof(GameObject));
#else
				var pc = Resources.Load(MasterAudioDefaultFolder + "/Prefabs/DynamicSoundGroupCreator.prefab", typeof(GameObject));
#endif
            if (pc == null) {
                Debug.LogError(
                    "Could not find DynamicSoundGroupCreator prefab. Please update the Installation Path in the Master Audio Manager window if you have moved the folder from its default location, then try again.");
                return null;
            }
            // ReSharper disable once ArrangeStaticMemberQualifier
            var go = GameObject.Instantiate(pc) as GameObject;
            // ReSharper disable once PossibleNullReferenceException
            go.name = "DynamicSoundGroupCreator";
            return go;
        }

        /// <summary>
        /// Creates a Sound Group Organizer prefab instance in the current Scene.
        /// </summary>
        public static GameObject CreateSoundGroupOrganizer() {
#if UNITY_EDITOR
            var pc = AssetDatabase.LoadAssetAtPath(MasterAudioFolderPath + "/Prefabs/SoundGroupOrganizer.prefab",
                typeof(GameObject));
#else
				var pc = Resources.Load(MasterAudioDefaultFolder + "/Prefabs/SoundGroupOrganizer.prefab", typeof(GameObject));
#endif
            if (pc == null) {
                Debug.LogError(
                    "Could not find SoundGroupOrganizer prefab. Please update the Installation Path in the Master Audio Manager window if you have moved the folder from its default location, then try again.");
                return null;
            }
            // ReSharper disable once ArrangeStaticMemberQualifier
            var go = GameObject.Instantiate(pc) as GameObject;
            // ReSharper disable once PossibleNullReferenceException
            go.name = "SoundGroupOrganizer";
            return go;
        }
        /*! \endcond */

#endregion
    }
}