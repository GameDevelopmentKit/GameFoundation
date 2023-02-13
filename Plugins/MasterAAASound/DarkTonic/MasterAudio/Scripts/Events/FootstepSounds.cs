/*! \cond PRIVATE */
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [AddComponentMenu("Dark Tonic/Master Audio/Footstep Sounds")]
    // ReSharper disable once CheckNamespace
    public class FootstepSounds : MonoBehaviour {
        // ReSharper disable InconsistentNaming
        public MasterAudio.SoundSpawnLocationMode soundSpawnMode = MasterAudio.SoundSpawnLocationMode.AttachToCaller;
        public FootstepTriggerMode footstepEvent = FootstepTriggerMode.None;

        public List<FootstepGroup> footstepGroups = new List<FootstepGroup>();

        // retrigger limit
        public EventSounds.RetriggerLimMode retriggerLimitMode = EventSounds.RetriggerLimMode.None;
        public int limitPerXFrm = 0;
        public float limitPerXSec = 0f;
        public int triggeredLastFrame = -100;
        public float triggeredLastTime = -100f;
        // ReSharper restore InconsistentNaming

        private Transform _trans;

        public enum FootstepTriggerMode {
            None,
            OnCollision,
            OnTriggerEnter,
            OnCollision2D,
            OnTriggerEnter2D
        }

#if PHY3D_ENABLED
        // ReSharper disable once UnusedMember.Local
        private void OnTriggerEnter(Collider other) {
            if (footstepEvent != FootstepTriggerMode.OnTriggerEnter) {
                return;
            }

            PlaySoundsIfMatch(other.gameObject);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnCollisionEnter(Collision collision) {
            if (footstepEvent != FootstepTriggerMode.OnCollision) {
                return;
            }

            PlaySoundsIfMatch(collision.gameObject);
        }
#endif

#if PHY2D_ENABLED
        // ReSharper disable once UnusedMember.Local
        private void OnCollisionEnter2D(Collision2D collision) {
            if (footstepEvent != FootstepTriggerMode.OnCollision2D) {
                return;
            }

            PlaySoundsIfMatch(collision.gameObject);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnTriggerEnter2D(Collider2D other) {
            if (footstepEvent != FootstepTriggerMode.OnTriggerEnter2D) {
                return;
            }

            PlaySoundsIfMatch(other.gameObject);
        }
#endif

        private bool CheckForRetriggerLimit() {
            // check for limiting restraints
            switch (retriggerLimitMode) {
                case EventSounds.RetriggerLimMode.FrameBased:
                    if (triggeredLastFrame > 0 && AudioUtil.FrameCount - triggeredLastFrame < limitPerXFrm) {
                        return false;
                    }
                    break;
                case EventSounds.RetriggerLimMode.TimeBased:
                    if (triggeredLastTime > 0 && AudioUtil.Time - triggeredLastTime < limitPerXSec) {
                        return false;
                    }
                    break;
            }

            return true;
        }

        private void PlaySoundsIfMatch(GameObject go) {
            if (!CheckForRetriggerLimit()) {
                return;
            }

            // set the last triggered time or frame
            switch (retriggerLimitMode) {
                case EventSounds.RetriggerLimMode.FrameBased:
                    triggeredLastFrame = AudioUtil.FrameCount;
                    break;
                case EventSounds.RetriggerLimMode.TimeBased:
                    triggeredLastTime = AudioUtil.Time;
                    break;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < footstepGroups.Count; i++) {
                var aGroup = footstepGroups[i];

                // check filters for matches if turned on
                if (aGroup.useLayerFilter && !aGroup.matchingLayers.Contains(go.layer)) {
                    continue;
                }

                if (aGroup.useTagFilter && !aGroup.matchingTags.Contains(go.tag)) {
                    continue;
                }

                var volume = aGroup.volume;
                float? pitch = aGroup.pitch;
                if (!aGroup.useFixedPitch) {
                    pitch = null;
                }

                string variationName = null;
                if (aGroup.variationType == EventSounds.VariationType.PlaySpecific) {
                    variationName = aGroup.variationName;
                }

                switch (soundSpawnMode) {
                    case MasterAudio.SoundSpawnLocationMode.CallerLocation:
                        MasterAudio.PlaySound3DAtTransformAndForget(aGroup.soundType, Trans, volume, pitch, aGroup.delaySound,
                            variationName);
                        break;
                    case MasterAudio.SoundSpawnLocationMode.AttachToCaller:
                        MasterAudio.PlaySound3DFollowTransformAndForget(aGroup.soundType, Trans, volume, pitch, aGroup.delaySound,
                            variationName);
                        break;
                    case MasterAudio.SoundSpawnLocationMode.MasterAudioLocation:
                        MasterAudio.PlaySoundAndForget(aGroup.soundType, volume, pitch, aGroup.delaySound, variationName);
                        break;
                }
            }
        }

        private Transform Trans {
            get {
                if (_trans != null) {
                    return _trans;
                }

                _trans = transform;

                return _trans;
            }
        }
    }
}
/*! \endcond */