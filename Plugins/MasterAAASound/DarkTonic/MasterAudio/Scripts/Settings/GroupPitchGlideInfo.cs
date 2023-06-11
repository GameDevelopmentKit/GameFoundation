/*! \cond PRIVATE */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
	public class GroupPitchGlideInfo {
        public MasterAudioGroup ActingGroup;
        public string NameOfGroup;
        public float CompletionTime;
        public bool IsActive = true;
        public List<SoundGroupVariation> GlidingVariations = new List<SoundGroupVariation>();

		// ReSharper disable once InconsistentNaming
        // ReSharper disable once RedundantNameQualifier
        public System.Action completionAction;
    }
}
/*! \endcond */