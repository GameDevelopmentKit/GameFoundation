/*! \cond PRIVATE */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
    public class CustomEvent {
        public string EventName;
        public string ProspectiveName;
        public bool IsEditing;
        // ReSharper disable InconsistentNaming
        public bool eventExpanded = true;
        public MasterAudio.CustomEventReceiveMode eventReceiveMode = MasterAudio.CustomEventReceiveMode.Always;
        public float distanceThreshold = 1f;
        public MasterAudio.EventReceiveFilter eventRcvFilterMode = MasterAudio.EventReceiveFilter.All;
        public int filterModeQty = 1;
		public bool isTemporary = false;
		public int frameLastFired = -1;
        public string categoryName = MasterAudio.NoCategory;
        // ReSharper restore InconsistentNaming

        private readonly List<int> _actorInstanceIds = new List<int>();

        public CustomEvent(string eventName) {
            EventName = eventName;
            ProspectiveName = eventName;
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
}
/*! \endcond */