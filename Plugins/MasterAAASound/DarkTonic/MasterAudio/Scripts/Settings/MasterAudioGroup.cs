using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class contains settings for the basic sound effects unit in Master Audio, known on the Sound Group.
    /// </summary>
    public class MasterAudioGroup : MonoBehaviour {
        /*! \cond PRIVATE */
        // ReSharper disable InconsistentNaming
#pragma warning disable 1591
        public const float UseCurveSpatialBlend = -99f;
        public const string NoBus = "[NO BUS]";
        public const int MinNoRepeatVariations = 3;

        public int busIndex = -1;

#if DISABLE_3D_SOUND
        public MasterAudio.ItemSpatialBlendType spatialBlendType = MasterAudio.ItemSpatialBlendType.ForceTo2D;
#else
        public MasterAudio.ItemSpatialBlendType spatialBlendType = MasterAudio.ItemSpatialBlendType.ForceTo3D;
#endif
        public float spatialBlend = 1f;

        public MasterAudio.DefaultGroupPlayType groupPlayType = MasterAudio.DefaultGroupPlayType.Always;

        public bool isSelected = false;
        public bool isExpanded = true;
        public float groupMasterVolume = 1f;
        public int retriggerPercentage = 100;
        public VariationMode curVariationMode = VariationMode.Normal;
        public bool alwaysHighestPriority = false;

        public bool ignoreListenerPause = false;

        [Range(0f, 10f)]
        public int importance = 5;
        public bool isUninterruptible;

        public float chainLoopDelayMin;
        public float chainLoopDelayMax;
        public ChainedLoopLoopMode chainLoopMode = ChainedLoopLoopMode.Endless;
        public int chainLoopNumLoops = 0;
        public bool useDialogFadeOut = false;
        public float dialogFadeOutTime = .5f;

        public VariationSequence curVariationSequence = VariationSequence.Randomized;
        public bool useNoRepeatRefill = true;
        public bool useInactivePeriodPoolRefill = false;
        public float inactivePeriodSeconds = 5f;
        public List<SoundGroupVariation> groupVariations = new List<SoundGroupVariation>();
        public MasterAudio.AudioLocation bulkVariationMode = MasterAudio.AudioLocation.Clip;
        public string comments;
        public bool logSound = false;

        public bool copySettingsExpanded = false;

        public bool expandLinkedGroups = false;
        public List<string> childSoundGroups = new List<string>();
        public List<string> endLinkedGroups = new List<string>();
        public MasterAudio.LinkedGroupSelectionType linkedStartGroupSelectionType = MasterAudio.LinkedGroupSelectionType.All;
        public MasterAudio.LinkedGroupSelectionType linkedStopGroupSelectionType = MasterAudio.LinkedGroupSelectionType.All;

        public LimitMode limitMode = LimitMode.None;
        public int limitPerXFrames = 1;
        public float minimumTimeBetween = 0.1f;
        public bool useClipAgePriority = false;

        public bool limitPolyphony = false;
        public int voiceLimitCount = 1;

        public TargetDespawnedBehavior targetDespawnedBehavior = TargetDespawnedBehavior.FadeOut;
        public float despawnFadeTime = .3f;

        public bool isUsingOcclusion;
        public bool willOcclusionOverrideRaycastOffset;
        public float occlusionRayCastOffset = 0f;
        public bool willOcclusionOverrideFrequencies;
        public float occlusionMaxCutoffFreq = AudioUtil.DefaultMaxOcclusionCutoffFrequency;
        public float occlusionMinCutoffFreq = AudioUtil.DefaultMinOcclusionCutoffFrequency;

        public bool isSoloed = false;
        public bool isMuted = false;

        public bool soundPlayedEventActive = false;
        public string soundPlayedCustomEvent = string.Empty;

        public bool willCleanUpDelegatesAfterStop = true;

#if ADDRESSABLES_ENABLED
        public int addressableUnusedSecondsLifespan = 0;
#endif
        /*! \endcond */

        /// <summary>
        /// Subscribe to this event to be notified when the last Variation stops playing (i.e. zero Variations are playing)
        /// </summary>
        public event System.Action LastVariationFinishedPlay;

        /*! \cond PRIVATE */
        public int frames = 0;
        // ReSharper restore InconsistentNaming

        private List<int> _activeAudioSourcesIds = new List<int>();
        private string _objectName = string.Empty;
        private Transform _trans;
        private float _originalVolume = 1;
        private readonly List<int> _actorInstanceIds = new List<int>();

        public enum TargetDespawnedBehavior {
            None,
            Stop,
            FadeOut
        }

        public enum VariationSequence {
            Randomized,
            TopToBottom
        }

        public enum VariationMode {
            Normal,
            LoopedChain,
            Dialog
        }

        public enum ChainedLoopLoopMode {
            Endless,
            NumberOfLoops
        }

        public enum LimitMode {
            None,
            FrameBased,
            TimeBased
        }

        // ReSharper restore InconsistentNaming

        // ReSharper disable once UnusedMember.Local
        private void Start() {
            // time to rename!
            _objectName = name;
            var childCount = ActiveAudioSourceIds.Count; // time to create clones
            if (childCount > 0) {
            } // to get rid of warning

            if (Trans.parent != null) {
                gameObject.layer = Trans.parent.gameObject.layer;
            }
        }

        public void AddActiveAudioSourceId(int varInstanceId) {
            if (ActiveAudioSourceIds.Contains(varInstanceId)) {
                return;
            }

            ActiveAudioSourceIds.Add(varInstanceId);

            var bus = BusForGroup;
            if (bus != null) {
                bus.AddActiveAudioSourceId(varInstanceId);
            }
        }

        public void RemoveActiveAudioSourceId(int varInstanceId) {
            ActiveAudioSourceIds.Remove(varInstanceId);

            var bus = BusForGroup;
            if (bus != null) {
                bus.RemoveActiveAudioSourceId(varInstanceId);
            }
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

        public float SpatialBlendForGroup {
            get {
#if DISABLE_3D_SOUND
                return MasterAudio.SpatialBlend_2DValue;
#else
                switch (MasterAudio.Instance.mixerSpatialBlendType) {
                    case MasterAudio.AllMixerSpatialBlendType.ForceAllTo2D:
                        return MasterAudio.SpatialBlend_2DValue;
                    case MasterAudio.AllMixerSpatialBlendType.ForceAllTo3D:
                        return MasterAudio.SpatialBlend_3DValue;
                    case MasterAudio.AllMixerSpatialBlendType.ForceAllToCustom:
                        return MasterAudio.Instance.mixerSpatialBlend;
                    // ReSharper disable once RedundantCaseLabel
                    case MasterAudio.AllMixerSpatialBlendType.AllowDifferentPerGroup:
                    default:
                        switch (spatialBlendType) {
                            case MasterAudio.ItemSpatialBlendType.ForceTo2D:
                                return MasterAudio.SpatialBlend_2DValue;
                            case MasterAudio.ItemSpatialBlendType.ForceTo3D:
                                return MasterAudio.SpatialBlend_3DValue;
                            case MasterAudio.ItemSpatialBlendType.ForceToCustom:
                                return spatialBlend;
                            // ReSharper disable once RedundantCaseLabel
                            case MasterAudio.ItemSpatialBlendType.UseCurveFromAudioSource:
                            default:
                                return UseCurveSpatialBlend;
                        }
                }
#endif
            }
        }
        /*! \endcond */

#region public properties
        /// <summary>
        /// This property will return the number of Activate voices in this Sound Group.
        /// </summary>
        public int ActiveVoices {
            get { return ActiveAudioSourceIds.Count; }
        }

#if ADDRESSABLES_ENABLED
        /// <summary>
        /// This property will return the number of seconds the unused Addressable (if this Variation uses Addressables) will wait before being released from memory.
        /// </summary>
        public int AddressableUnusedSecondsLifespan {
            get {
                return addressableUnusedSecondsLifespan;
            }
        }
#endif

        /// <summary>
        /// This property will return the total number of voices available in this Sound Group.
        /// </summary>
        public int TotalVoices {
            get { return transform.childCount; }
        }

        /// <summary>
        /// This property can be set to false to cancel the auto-delegate cleanup on all Variations in this Sound Group. So if you subscribe to SoundFinished, you will get notified of all times it finishes until you unsubscribe.
        /// </summary>
        public bool WillCleanUpDelegatesAfterStop {
            set {
                willCleanUpDelegatesAfterStop = value;
            }
        }

        /// <summary>
        /// This property will return the Bus for the Sound Group, if any is assigned.
        /// </summary>
        public GroupBus BusForGroup {
            get {
                if (busIndex < MasterAudio.HardCodedBusOptions) {
                    return null; // no bus
                }

                var index = busIndex - MasterAudio.HardCodedBusOptions;

                if (index >= MasterAudio.GroupBuses.Count) {
                    // this happens only with Dynamic SGC item removal
                    return null;
                }

                return MasterAudio.GroupBuses[index];
            }
        }

        /// <summary>
        /// This property will return the original volume of the Sound Group.
        /// </summary>
        public float OriginalVolume {
            get {
                // ReSharper disable once PossibleInvalidOperationException
                return _originalVolume;
            }
            set {
                _originalVolume = value;
            }
        }

        /*! \cond PRIVATE */
        public bool LoggingEnabledForGroup {
            get { return logSound || MasterAudio.LogSoundsEnabled; }
        }

        public void FireLastVariationFinishedPlay() {
            if (LastVariationFinishedPlay != null) {
                LastVariationFinishedPlay();
            }
        }

        public void SubscribeToLastVariationFinishedPlay(System.Action finishedCallback) {
            LastVariationFinishedPlay = null; // clear any old subscribers
            LastVariationFinishedPlay += finishedCallback;
        }

        public void UnsubscribeFromLastVariationFinishedPlay() {
            LastVariationFinishedPlay = null; // clear any old subscribers
        }

        public int ChainLoopCount { get; set; }

        public string GameObjectName {
            get {
                if (string.IsNullOrEmpty(_objectName)) {
                    _objectName = name;
                }

                return _objectName;
            }
        }

        public MasterAudio.GroupPlayType GroupPlayType {
            get {
                if (MasterAudio.Instance.groupPlayType != MasterAudio.GroupPlayType.AllowDifferentPerGroup)
                {
                    return MasterAudio.Instance.groupPlayType;
                }

                switch (groupPlayType)
                {
                    case MasterAudio.DefaultGroupPlayType.Always:
                        return MasterAudio.GroupPlayType.Always;
                    case MasterAudio.DefaultGroupPlayType.WhenActorInAudibleRange:
                        return MasterAudio.GroupPlayType.WhenActorInAudibleRange;
                    default:
                        throw new System.Exception("Illegal Group Play Type: " + groupPlayType);
                }
            }
        }

        /// <summary>
        /// This property returns the number of live actors (Dynamic Sound Group Creators) still in the Scene.
        /// </summary>
        public bool HasLiveActors {
            get {
                return _actorInstanceIds.Count > 0;
            }
        }

        public bool UsesNoRepeat {
            get { return curVariationSequence == VariationSequence.Randomized && groupVariations.Count >= MinNoRepeatVariations && useNoRepeatRefill; }
        }

#endregion

#region private properties
        private Transform Trans {
            get {
                if (_trans != null) {
                    return _trans;
                }
                _trans = transform;

                return _trans;
            }
        }

        private List<int> ActiveAudioSourceIds {
            get {
                if (_activeAudioSourcesIds != null) {
                    return _activeAudioSourcesIds;
                }
                _activeAudioSourcesIds = new List<int>(Trans.childCount);

                return _activeAudioSourcesIds;
            }
        }
#endregion
        /*! \endcond */
    }
}
