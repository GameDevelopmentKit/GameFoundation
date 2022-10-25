/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
	[Serializable]
    // ReSharper disable once CheckNamespace
	public class SongMetadataProperty {
        public enum MetadataPropertyType {
            Boolean,
            String,
            Integer,
            Float
        }

        public MetadataPropertyType PropertyType;
		public string PropertyName;
        public string ProspectiveName;
        public bool IsEditing;
        public bool PropertyExpanded = true;
		public bool AllSongsMustContain = true;
		public bool CanSongHaveMultiple = false;

		public SongMetadataProperty(string propertyName, MetadataPropertyType propertyType, bool allSongsMustContain, bool canSongHaveMultiple) {
			PropertyName = propertyName;
            ProspectiveName = propertyName;
			PropertyType = propertyType;
            AllSongsMustContain = allSongsMustContain;
			CanSongHaveMultiple = canSongHaveMultiple;
        }
    }
}
/*! \endcond */