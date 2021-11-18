/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
	public class SongMetadataFloatValue {
		public string PropertyName;
		public float Value;

        public SongMetadataFloatValue(SongMetadataProperty prop) {
			if (prop  == null) {
                return;
			}

			PropertyName = prop.PropertyName;
        }
    }
}
/*! \endcond */