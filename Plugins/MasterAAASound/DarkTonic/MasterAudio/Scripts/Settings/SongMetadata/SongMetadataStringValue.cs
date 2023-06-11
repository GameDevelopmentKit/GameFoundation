/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
	public class SongMetadataStringValue {
        public string PropertyName;
        public string Value;

        public SongMetadataStringValue(SongMetadataProperty prop) {
			if (prop == null) {
				return;
			}

			PropertyName = prop.PropertyName;
        }
    }
}
/*! \endcond */