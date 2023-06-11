/*! \cond PRIVATE */
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public class DynamicSoundGroup : MonoBehaviour {
        // ReSharper disable InconsistentNaming
        public GameObject variationTemplate;

        public bool useClipAgePriority = false;
        public bool alwaysHighestPriority = false;
        public float groupMasterVolume = 1f;
        public int retriggerPercentage = 50;
        public MasterAudioGroup.VariationSequence curVariationSequence = MasterAudioGroup.VariationSequence.Randomized;
        public bool useNoRepeatRefill = true;

        public bool useInactivePeriodPoolRefill = false;
        public float inactivePeriodSeconds = 5f;
        public MasterAudioGroup.VariationMode curVariationMode = MasterAudioGroup.VariationMode.Normal;
        public MasterAudio.AudioLocation bulkVariationMode = MasterAudio.AudioLocation.Clip;

        public float chainLoopDelayMin;
        public float chainLoopDelayMax;
        public MasterAudioGroup.ChainedLoopLoopMode chainLoopMode = MasterAudioGroup.ChainedLoopLoopMode.Endless;
        public int chainLoopNumLoops = 0;
        public bool useDialogFadeOut = false;
        public float dialogFadeOutTime = .5f;

        public string comments;
        public bool logSound = false;

        public bool soundPlayedEventActive = false;
        public string soundPlayedCustomEvent = string.Empty;

        public int busIndex = -1;

        public bool ignoreListenerPause = false;

        [Range(0f, 10f)]
        public int importance = 5;
        public bool isUninterruptible;

#if DISABLE_3D_SOUND
        public MasterAudio.ItemSpatialBlendType spatialBlendType = MasterAudio.ItemSpatialBlendType.ForceTo2D;
#else
        public MasterAudio.ItemSpatialBlendType spatialBlendType = MasterAudio.ItemSpatialBlendType.ForceTo3D;
#endif
        public float spatialBlend = 1f;

        public MasterAudio.DefaultGroupPlayType groupPlayType = MasterAudio.DefaultGroupPlayType.Always;

        public string busName = string.Empty; // only used to remember the bus name during group creation.
		public bool isExistingBus; // marked from DGSC's only
		public bool isCopiedFromDGSC; // so we know if it's existing Buses or new.

        public MasterAudioGroup.LimitMode limitMode = MasterAudioGroup.LimitMode.None;
        public int limitPerXFrames = 1;
        public float minimumTimeBetween = 0.1f;
        public bool limitPolyphony = false;
        public int voiceLimitCount = 1;

        public MasterAudioGroup.TargetDespawnedBehavior targetDespawnedBehavior =
            MasterAudioGroup.TargetDespawnedBehavior.FadeOut;

        public float despawnFadeTime = .3f;

        public bool isUsingOcclusion;
        public bool willOcclusionOverrideRaycastOffset;
        public float occlusionRayCastOffset = 0f;
        public bool willOcclusionOverrideFrequencies;
        public float occlusionMaxCutoffFreq = AudioUtil.DefaultMaxOcclusionCutoffFrequency;
        public float occlusionMinCutoffFreq = AudioUtil.DefaultMinOcclusionCutoffFrequency;

        public bool copySettingsExpanded = false;

        public bool expandLinkedGroups = false;
        public List<string> childSoundGroups = new List<string>();
        public List<string> endLinkedGroups = new List<string>();
        public MasterAudio.LinkedGroupSelectionType linkedStartGroupSelectionType = MasterAudio.LinkedGroupSelectionType.All;
        public MasterAudio.LinkedGroupSelectionType linkedStopGroupSelectionType = MasterAudio.LinkedGroupSelectionType.All;

#if ADDRESSABLES_ENABLED
        public int addressableUnusedSecondsLifespan = 0;
#endif

        public List<DynamicGroupVariation> groupVariations = new List<DynamicGroupVariation>();
        // filled and used by Inspector only

        // ReSharper restore InconsistentNaming
    }
}
/*! \endcond */