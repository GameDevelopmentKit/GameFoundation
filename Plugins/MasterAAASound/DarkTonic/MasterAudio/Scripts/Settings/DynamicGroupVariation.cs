/*! \cond PRIVATE */
using UnityEngine;
#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
#endif

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public class DynamicGroupVariation : MonoBehaviour {
        // ReSharper disable InconsistentNaming
		[Range(0f, 1f)]
		public int probabilityToPlay = 100;

        [Range(0f, 10f)]
        public int importance = 5;
        public bool isUninterruptible;

        public bool useLocalization = false;
        public bool useRandomPitch = false;
        public SoundGroupVariation.RandomPitchMode randomPitchMode = SoundGroupVariation.RandomPitchMode.AddToClipPitch;
        public float randomPitchMin = 0f;
        public float randomPitchMax = 0f;

        public bool useRandomVolume = false;

        public SoundGroupVariation.RandomVolumeMode randomVolumeMode =
            SoundGroupVariation.RandomVolumeMode.AddToClipVolume;

        public float randomVolumeMin = 0f;
        public float randomVolumeMax = 0f;

        public int weight = 1;
        public string clipAlias;
        public MasterAudio.AudioLocation audLocation = MasterAudio.AudioLocation.Clip;
        public string resourceFileName;
#if ADDRESSABLES_ENABLED
        public AssetReference audioClipAddressable;
#endif
        public bool isExpanded = true;
        public bool isChecked = true;

        public bool useFades = false;
        public float fadeInTime = 0f;
        public float fadeOutTime = 0f;

        public bool useCustomLooping = false;
        public int minCustomLoops = 1;
        public int maxCustomLoops = 5;

        public bool useIntroSilence;
        public float introSilenceMin;
        public float introSilenceMax;

        public bool useRandomStartTime = false;
        public float randomStartMinPercent = 0f;
		public float randomStartMaxPercent = 100f;
        public float randomEndPercent = 100f;
        // ReSharper restore InconsistentNaming

        private AudioDistortionFilter _distFilter;
        private AudioEchoFilter _echoFilter;
        private AudioHighPassFilter _hpFilter;
        private AudioLowPassFilter _lpFilter;
        private AudioReverbFilter _reverbFilter;
        private AudioChorusFilter _chorusFilter;
        private DynamicSoundGroup _parentGroupScript;
        private Transform _trans;
        private AudioSource _aud;

        /// <summary>
        /// This property returns you a lazy-loaded reference to the Unity Distortion Filter FX component.
        /// </summary>
        public AudioDistortionFilter DistortionFilter {
            get {
                if (_distFilter != null) {
                    return _distFilter;
                }
                _distFilter = GetComponent<AudioDistortionFilter>();

                return _distFilter;
            }
        }

        /// <summary>
        /// This property returns you a lazy-loaded reference to the Unity Reverb Filter FX component.
        /// </summary>
        public AudioReverbFilter ReverbFilter {
            get {
                if (_reverbFilter != null) {
                    return _reverbFilter;
                }
                _reverbFilter = GetComponent<AudioReverbFilter>();

                return _reverbFilter;
            }
        }

        /// <summary>
        /// This property returns you a lazy-loaded reference to the Unity Chorus Filter FX component.
        /// </summary>
        public AudioChorusFilter ChorusFilter {
            get {
                if (_chorusFilter != null) {
                    return _chorusFilter;
                }
                _chorusFilter = GetComponent<AudioChorusFilter>();

                return _chorusFilter;
            }
        }

        /// <summary>
        /// This property returns you a lazy-loaded reference to the Unity Echo Filter FX component.
        /// </summary>
        public AudioEchoFilter EchoFilter {
            get {
                if (_echoFilter != null) {
                    return _echoFilter;
                }
                _echoFilter = GetComponent<AudioEchoFilter>();

                return _echoFilter;
            }
        }

        /// <summary>
        /// This property returns you a lazy-loaded reference to the Unity Low Pass Filter FX component.
        /// </summary>
        public AudioLowPassFilter LowPassFilter {
            get {
                if (_lpFilter != null) {
                    return _lpFilter;
                }
                _lpFilter = GetComponent<AudioLowPassFilter>();

                return _lpFilter;
            }
        }

        /// <summary>
        /// This property returns you a lazy-loaded reference to the Unity High Pass Filter FX component.
        /// </summary>
        public AudioHighPassFilter HighPassFilter {
            get {
                if (_hpFilter != null) {
                    return _hpFilter;
                }
                _hpFilter = GetComponent<AudioHighPassFilter>();

                return _hpFilter;
            }
        }

        public DynamicSoundGroup ParentGroup {
            get {
                if (_parentGroupScript == null) {
                    _parentGroupScript = Trans.parent.GetComponent<DynamicSoundGroup>();
                }

                if (_parentGroupScript == null) {
                    Debug.LogError("The Group that Dynamic Sound Variation '" + name +
                                   "' is in does not have a DynamicSoundGroup script in it!");
                }

                return _parentGroupScript;
            }
        }

        public Transform Trans {
            get {
                if (_trans != null) {
                    return _trans;
                }
                _trans = transform;

                return _trans;
            }
        }

        public bool HasActiveFXFilter {
            get {
                if (HighPassFilter != null && HighPassFilter.enabled) {
                    return true;
                }
                if (LowPassFilter != null && LowPassFilter.enabled) {
                    return true;
                }
                if (ReverbFilter != null && ReverbFilter.enabled) {
                    return true;
                }
                if (DistortionFilter != null && DistortionFilter.enabled) {
                    return true;
                }
                if (EchoFilter != null && EchoFilter.enabled) {
                    return true;
                }
                if (ChorusFilter != null && ChorusFilter.enabled) {
                    return true;
                }

                return false;
            }
        }

        public AudioSource VarAudio {
            get {
                if (_aud != null) {
                    return _aud;
                }
                _aud = GetComponent<AudioSource>();

                return _aud;
            }
        }
    }
}
/*! \endcond */