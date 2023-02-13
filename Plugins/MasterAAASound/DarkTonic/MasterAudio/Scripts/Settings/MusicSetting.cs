using System;
using System.Collections.Generic;
using UnityEngine;
#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
#endif


// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class is used to populate a song for a PlaylistController through code if necessary.
    /// </summary>
    [Serializable]
    // ReSharper disable once CheckNamespace
    public class MusicSetting {
        // ReSharper disable InconsistentNaming
        
        /// <summary>
        /// The alias for the song
        /// </summary>
        public string alias = string.Empty;
        
        /// <summary>
        /// This setting allows you to choose Audio Clip, Resource File or Addressable
        /// </summary>
        public MasterAudio.AudioLocation audLocation = MasterAudio.AudioLocation.Clip;
        
        /// <summary>
        /// The Audio Clip for the song, if you're using AudioLocation of Clip
        /// </summary>
        public AudioClip clip;

        /// <summary>
        /// Do not set this. It is calculated from the clip's name or alias if it has one.
        /// </summary>
        public string songName = string.Empty;

        /// <summary>
        /// This is the path to the Resource File if you're using AudioLocation of Resource File
        /// </summary>
        public string resourceFileName = string.Empty;

#if ADDRESSABLES_ENABLED
        /// <summary>
        /// This is the AssetReference to the clip if you're using AudioLocation of Addressable
        /// </summary>
        public AssetReference audioClipAddressable;
#endif
        /// <summary>
        /// The volume to use when playing the song.
        /// </summary>
        public float volume = 1f;

        /// <summary>
        /// The pitch to play the song at.
        /// </summary>
        public float pitch = 1f;

        /// <summary>
        /// Do not set this. It is for Inspector only.
        /// </summary>
        public bool isExpanded = true;

        /// <summary>
        /// Whether to loop the song or not.
        /// </summary>
        public bool isLoop;

        /// <summary>
        /// Do not set this, it is for Inspector only
        /// </summary>
        public bool isChecked = true;

        /// <summary>
        /// Do not set this, it is for Inspector only
        /// </summary>
        public List<SongMetadataStringValue> metadataStringValues = new List<SongMetadataStringValue>();

        /// <summary>
        /// Do not set this, it is for Inspector only
        /// </summary>
        public List<SongMetadataBoolValue> metadataBoolValues = new List<SongMetadataBoolValue>();

        /// <summary>
        /// Do not set this, it is for Inspector only
        /// </summary>
        public List<SongMetadataIntValue> metadataIntValues = new List<SongMetadataIntValue>();

        /// <summary>
        /// Do not set this, it is for Inspector only
        /// </summary>
        public List<SongMetadataFloatValue> metadataFloatValues = new List<SongMetadataFloatValue>();

        /// <summary>
        /// Do not set this, it is for Inspector only
        /// </summary>
        public bool metadataExpanded = true;

        /// <summary>
        /// This controls where the song starts from.
        /// </summary>
        public MasterAudio.CustomSongStartTimeMode songStartTimeMode = MasterAudio.CustomSongStartTimeMode.Beginning;
        
        /// <summary>
        /// If you choose Random Time for Begin Song Time Node, it will start between customStartTime (min) and customStartTimeMax, randomly.
        /// </summary>
        public float customStartTime;

        /// <summary>
        /// If you choose Random Time for Begin Song Time Node, it will start between customStartTime (min) and customStartTimeMax, randomly.
        /// </summary>
        public float customStartTimeMax;

        /// <summary>
        /// Do not set this value, used by "New Clip From Last Known Position" mode of Song Transition Mode and set automatically.
        /// </summary>
        public int lastKnownTimePoint = 0;

        /// <summary>
        /// Do not set this value, used by "New Clip From Last Known Position" mode of Song Transition Mode and set automatically.
        /// </summary>
		public bool wasLastKnownTimePointSet = false;

        /// <summary>
        /// Set this uniquely for each song as consecutive integers, used to keep track of which songs haven't played yet.
        /// </summary>
		public int songIndex = 0;

        /// <summary>
        /// This is used for loopable section of a song.
        /// </summary>
        public float sectionStartTime = 0f;

        /// <summary>
        /// This is used for loopable section of a song.
        /// </summary>
        public float sectionEndTime = 0f;

        /// <summary>
        /// Set this to true if you are going to use songStartedCustomEvent 
        /// </summary>
        public bool songStartedEventExpanded;

        /// <summary>
        /// This is the name of a Custom Event to fire when the song is started.
        /// </summary>
        public string songStartedCustomEvent = string.Empty;
        
        /// <summary>
        /// Set this to true if you are going to use songChangedCustomEvent
        /// </summary>
        public bool songChangedEventExpanded;

        /// <summary>
        /// This is the name of a Custom Event to fire when the song is changed to another.
        /// </summary>
        public string songChangedCustomEvent = string.Empty;

        public MusicSetting() {
            songChangedEventExpanded = false;
        }

        /*! \cond PRIVATE */
        public bool HasMetadataProperties {
            get {
                return MetadataPropertyCount > 0;
            }
        }

        public int MetadataPropertyCount {
            get {
                return metadataStringValues.Count + metadataBoolValues.Count + metadataIntValues.Count + metadataFloatValues.Count;
            }
        }

        public float SongStartTime {
            get {
                switch (songStartTimeMode) {
                    default:
                    case MasterAudio.CustomSongStartTimeMode.Beginning:
                        return 0f;
                    case MasterAudio.CustomSongStartTimeMode.SpecificTime:
                        return customStartTime;
                    case MasterAudio.CustomSongStartTimeMode.RandomTime:
                        return UnityEngine.Random.Range(customStartTime, customStartTimeMax);
                    case MasterAudio.CustomSongStartTimeMode.Section:
                        return sectionStartTime;
                }
            }
        }
        /*! \endcond */

        /*! \cond PRIVATE */
        public static MusicSetting Clone(MusicSetting mus, MasterAudio.Playlist aList) {
            var clone = new MusicSetting {
                alias = mus.alias,
                audLocation = mus.audLocation,
                clip = mus.clip,
                songName = mus.songName,
                resourceFileName = mus.resourceFileName,
                volume = mus.volume,
                pitch = mus.pitch,
                isExpanded = mus.isExpanded,
                isLoop = mus.isLoop,
                isChecked = mus.isChecked,
                customStartTime = mus.customStartTime,
                songStartedEventExpanded = mus.songStartedEventExpanded,
                songStartedCustomEvent = mus.songStartedCustomEvent,
                songChangedEventExpanded = mus.songChangedEventExpanded,
                songChangedCustomEvent = mus.songChangedCustomEvent,
                metadataExpanded = mus.metadataExpanded
            };

            SongMetadataProperty prop = null;

            for (var i = 0; i < mus.metadataStringValues.Count; i++) {
                var valToClone = mus.metadataStringValues[i];
                prop = aList.songMetadataProps.Find(delegate (SongMetadataProperty p) {
                    return p.PropertyName == valToClone.PropertyName;
                });
                var sVal = new SongMetadataStringValue(prop);
                sVal.Value = valToClone.Value;
                clone.metadataStringValues.Add(sVal);
            }

            for (var i = 0; i < mus.metadataFloatValues.Count; i++) {
                var valToClone = mus.metadataFloatValues[i];
                prop = aList.songMetadataProps.Find(delegate (SongMetadataProperty p) {
                    return p.PropertyName == valToClone.PropertyName;
                });
                var fVal = new SongMetadataFloatValue(prop);
                fVal.Value = valToClone.Value;
                clone.metadataFloatValues.Add(fVal);
            }

            for (var i = 0; i < mus.metadataBoolValues.Count; i++) {
                var valToClone = mus.metadataBoolValues[i];
                prop = aList.songMetadataProps.Find(delegate (SongMetadataProperty p) {
                    return p.PropertyName == valToClone.PropertyName;
                });
                var bVal = new SongMetadataBoolValue(prop);
                bVal.Value = valToClone.Value;
                clone.metadataBoolValues.Add(bVal);
            }

            for (var i = 0; i < mus.metadataIntValues.Count; i++) {
                var valToClone = mus.metadataIntValues[i];
                prop = aList.songMetadataProps.Find(delegate (SongMetadataProperty p) {
                    return p.PropertyName == valToClone.PropertyName;
                });
                var iVal = new SongMetadataIntValue(prop);
                iVal.Value = valToClone.Value;
                clone.metadataIntValues.Add(iVal);
            }

            return clone;
            // ReSharper restore InconsistentNaming
        }
        /*! \endcond */
    }
}