/*! \cond PRIVATE */
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public static class AudioPrioritizer {
        private const int MaxPriority = 0;
        private const int HighestPriority = 16;
        private const int LowestPriority = 128;

        public static void Set2DSoundPriority(AudioSource audio) {
            audio.priority = HighestPriority;
        }

        public static void SetSoundGroupInitialPriority(AudioSource audio) {
            audio.priority = LowestPriority;
        }

        public static void SetPreviewPriority(AudioSource audio) {
            audio.priority = MaxPriority;
        }

        public static void Set3DPriority(SoundGroupVariation variation, bool useClipAgePriority) {
            if (MasterAudio.ListenerTrans == null) {
                // can't prioritize.
                return;
            }

            var audio = variation.VarAudio;

            if (audio.spatialBlend == 0f) {
                // handle 2D sound if we end here with it.
                Set2DSoundPriority(variation.VarAudio);
                return;
            }

            var distanceToListener = Vector3.Distance(audio.transform.position, MasterAudio.ListenerTrans.position);
            float perceivedVolume;

            switch (audio.rolloffMode) {
                case AudioRolloffMode.Logarithmic:
                    perceivedVolume = audio.volume / Mathf.Max(audio.minDistance, distanceToListener - audio.minDistance);
                    // Unity seems to just use a 1/distance model for this
                    break;
                case AudioRolloffMode.Linear:
                    perceivedVolume = Mathf.Lerp(audio.volume, 0,
                        Mathf.Max(0, distanceToListener - audio.minDistance) / (audio.maxDistance - audio.minDistance));
                    // Linearly interpolate from max volume to zero as we go from the minimum distance to the max
                    break;
                default:
                    perceivedVolume = Mathf.Lerp(audio.volume, 0,
                        Mathf.Max(0, distanceToListener - audio.minDistance) / (audio.maxDistance - audio.minDistance));
                    // Not possible to deal with custom rolloffs since it's not accessible by script.  Let's pretend it's linear.
                    break;
            }

            if (useClipAgePriority && !audio.loop) {
                //Don't make looping sounds lessen in priority over time
                perceivedVolume = Mathf.Lerp(perceivedVolume, perceivedVolume * 0.1f,
                    AudioUtil.GetAudioPlayedPercentage(audio) * .01f);
                //Set the factor lower when this non-looping sound has played for a few seconds so that newer sounds get a slightly higher priority.
            }

            audio.priority = (int)Mathf.Lerp(HighestPriority, LowestPriority, Mathf.InverseLerp(1f, 0f, perceivedVolume));
            // Transform our perceived volume from the [0...1] range to the [16...128] range so that the higher the perceived volume the lower the priority number.
        }
    }
}
/*! \endcond */