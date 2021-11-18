/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
	public class SongMetadataIntValue {
		public string PropertyName;
		public int Value;

        public SongMetadataIntValue(SongMetadataProperty prop) {
			if (prop  == null) {
				return;
			}

			PropertyName = prop.PropertyName;
        }
    }
}
/*! \endcond */