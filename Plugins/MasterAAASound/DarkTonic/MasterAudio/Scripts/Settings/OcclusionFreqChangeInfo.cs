/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
	public class OcclusionFreqChangeInfo {
        public SoundGroupVariation ActingVariation;
        public float StartFrequency;
        public float TargetFrequency;
        public float StartTime;
        public float CompletionTime;
        public bool IsActive = true;
    }
}
/*! \endcond */