/*! \cond PRIVATE */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
	public class BusPitchGlideInfo {
        public string NameOfBus;
        public float CompletionTime;
        public bool IsActive = true;
        public List<SoundGroupVariation> GlidingVariations;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once RedundantNameQualifier
        public System.Action completionAction;
    }
}
/*! \endcond */