/*! \cond PRIVATE */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
    public class FootstepGroup {
        // ReSharper disable InconsistentNaming
        public bool isExpanded = true;

        // tag / layer filters
        public bool useLayerFilter = false;
        public bool useTagFilter = false;
        public List<int> matchingLayers = new List<int>() { 0 };
        public List<string> matchingTags = new List<string>() { "Default" };

        public string soundType = MasterAudio.NoGroupName;
        public EventSounds.VariationType variationType = EventSounds.VariationType.PlayRandom;
        public string variationName = string.Empty;
        public float volume = 1.0f;
        public bool useFixedPitch = false;
        public float pitch = 1f;
        public float delaySound = 0f;
        // ReSharper restore InconsistentNaming
    }
}
/*! \endcond */