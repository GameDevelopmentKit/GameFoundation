/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
	public class SongMetadataBoolValue {
		public string PropertyName;
		public bool Value;

        public SongMetadataBoolValue(SongMetadataProperty prop) {
			if (prop == null) {
				return;
			}

            PropertyName = prop.PropertyName;
        }
    }
}
/*! \endcond */