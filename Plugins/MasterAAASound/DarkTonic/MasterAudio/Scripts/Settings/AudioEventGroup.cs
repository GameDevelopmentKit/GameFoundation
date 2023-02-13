/*! \cond PRIVATE */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
	[Serializable]
	// ReSharper disable once CheckNamespace
	public class AudioEventGroup {
		// tag / layer filters
		// ReSharper disable InconsistentNaming
		public bool isExpanded = true;
#if MULTIPLAYER_ENABLED
		public bool multiplayerBroadcast;
#endif
		public bool useLayerFilter = false;
		public bool useTagFilter = false;
		public List<int> matchingLayers = new List<int>() { 0 };
		public List<string> matchingTags = new List<string>() { "Default" };
		
		// for custom events only
		public bool customSoundActive = false;
		public bool isCustomEvent = false;
		public string customEventName = string.Empty;
		
		// for mechanim events only
		public bool mechanimEventActive = false;
		public bool isMechanimStateCheckEvent = false;
		public string mechanimStateName = string.Empty;
		public bool mechEventPlayedForState = false;
		
		public List<AudioEvent> SoundEvents = new List<AudioEvent>();
		
		public EventSounds.PreviousSoundStopMode mouseDragStopMode = EventSounds.PreviousSoundStopMode.None;
		public float mouseDragFadeOutTime = 1f;
		
		// retrigger limit
		public EventSounds.RetriggerLimMode retriggerLimitMode = EventSounds.RetriggerLimMode.None;
		public int limitPerXFrm = 0;
		public float limitPerXSec = 0f;
		public int triggeredLastFrame = -100;
		public float triggeredLastTime = -100f;

        public float triggerStayForTime;
        public bool doesTriggerStayRepeat = true;

        public float sliderValue = 0f;
		// ReSharper restore InconsistentNaming
		
	}
}
/*! \endcond */