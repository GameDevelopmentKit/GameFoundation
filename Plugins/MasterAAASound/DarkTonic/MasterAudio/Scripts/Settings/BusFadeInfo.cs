/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
    public class BusFadeInfo {
        public string NameOfBus;
        public GroupBus ActingBus;
        public float StartVolume;
        public float TargetVolume;
        public float StartTime;
        public float CompletionTime;
        public bool IsActive = true;
		public bool WillStopGroupAfterFade = false;
		public bool WillResetVolumeAfterFade = false;
			// ReSharper disable once InconsistentNaming
        // ReSharper disable once RedundantNameQualifier
        public System.Action completionAction;
    }
}
/*! \endcond */