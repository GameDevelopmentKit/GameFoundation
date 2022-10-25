/*! \cond PRIVATE */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
	[Serializable]
	// ReSharper disable once CheckNamespace
	public class CustomEventCategory {
		public string CatName = MasterAudio.NoCategory;
		public bool IsExpanded = true;
		public bool IsEditing = false;
		public bool IsTemporary = false;
		public string ProspectiveName = MasterAudio.NoCategory;

        private readonly List<int> _actorInstanceIds = new List<int>();

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