/*! \cond PRIVATE */
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    public static class SpatializerHelper {
        private const string OculusSpatializer = "OculusSpatializer";
        private const string ResonanceAudioSpatializer = "Resonance Audio";

        public static bool IsSupportedSpatializer {
            get {
                switch (SelectedSpatializer) {
                    case OculusSpatializer:
                        return true;
                    case ResonanceAudioSpatializer:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool IsOculusAudioSpatializer {
            get {
                return SelectedSpatializer == OculusSpatializer;
            }
        }

        public static bool IsResonanceAudioSpatializer {
            get {
                return SelectedSpatializer == ResonanceAudioSpatializer;
            }
        }

        public static string SelectedSpatializer {
            get {
                return AudioSettings.GetSpatializerPluginName();
            }
        }

        public static void TurnOnSpatializerIfEnabled(AudioSource source) {
            if (MasterAudio.SafeInstance == null) {
                SetSpatializerToggleOnSource(source, false);
                return;
            }

            if (!MasterAudio.Instance.useSpatializer) {
                SetSpatializerToggleOnSource(source, false);
                return;
            }

            SetSpatializerToggleOnSource(source, true);

            if (!ResonanceAudioHelper.ResonanceAudioOptionExists) {
                return;
            }

            if (!MasterAudio.Instance.useSpatializerPostFX) {
                return;
            }

            source.spatializePostEffects = true;
        }

        private static void SetSpatializerToggleOnSource(AudioSource source, bool enabled)
        {
            if (enabled) {
				enabled = source.spatialBlend != 0;        
            }       

            source.spatialize = enabled;
        }
    }
}
/*! \endcond */
