using UnityEngine;

#if UNITY_XBOXONE
    using PlayerPrefs = DarkTonic.MasterAudio.FilePlayerPrefs;
#endif


// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class allows you to set defaults that each Master Audio prefab will use during its Start event when the Scene loads up, only if you set the values via code. Useful for setting global SFX and music levels, as well as other more granular settings.
    /// </summary>
    // ReSharper disable once CheckNamespace
    public static class PersistentAudioSettings {
        /*! \cond PRIVATE */
        public const string SfxVolKey = "MA_sfxVolume";
        public const string MusicVolKey = "MA_musicVolume";
        public const string SfxMuteKey = "MA_sfxMute";
        public const string MusicMuteKey = "MA_musicMute";
        public const string BusVolKey = "MA_BusVolume_";
        public const string GroupVolKey = "MA_GroupVolume_";
        
        public const string BusKeysKey = "MA_BusKeys";
        public const string GroupKeysKey = "MA_GroupsKeys";
        public const string Separator = ";";
        /*! \endcond */

        /// <summary>
        /// Sets the bus's volume. During startup (Awake event), the Master Audio prefab will assign any buses that match to the levels you specify here. This will also set the Bus's Volume in the current Scene's Master Audio prefab, if a match exists.
        /// </summary>
        /// <param name="busName">Bus name</param>
        /// <param name="vol">Volume</param>
        public static void SetBusVolume(string busName, float vol) {
            var busKey = MakeBusKey(busName);

            PlayerPrefs.SetFloat(busKey, vol);

            var ma = MasterAudio.SafeInstance;
            if (ma == null) {
                return;
            }
            if (MasterAudio.GrabBusByName(busName) != null) {
                MasterAudio.SetBusVolumeByName(busName, vol);
            }

            if (BusesUpdatedKeys.Contains(Separator + busName + Separator)) {
                return;
            }

            BusesUpdatedKeys += busName + Separator;
        }

        /*! \cond PRIVATE */
        public static string BusesUpdatedKeys {
            get {
                if (!PlayerPrefs.HasKey(BusKeysKey)) {
                    PlayerPrefs.SetString(BusKeysKey, ";");
                }

                return PlayerPrefs.GetString(BusKeysKey);
            }
            set {
                PlayerPrefs.SetString(BusKeysKey, value);
            }
        }

        public static string MakeBusKey(string busName) {
            return BusVolKey + busName;
        }
        /*! \endcond */

        /// <summary>
        /// Gets the bus volume (used by Master Audio prefab during Awake event to set persistent levels).
        /// </summary>
        /// <returns>The group volume.</returns>
        /// <param name="busName">Group name.</param>
        public static float? GetBusVolume(string busName) {
            var busKey = MakeBusKey(busName);

            if (!PlayerPrefs.HasKey(busKey)) {
                return null;
            }

            return PlayerPrefs.GetFloat(busKey);
        }

        /*! \cond PRIVATE */
        public static string GetGroupKey(string groupName) {
            return GroupVolKey + groupName;
        }

        public static string GroupsUpdatedKeys {
            get {
                if (!PlayerPrefs.HasKey(GroupKeysKey)) {
                    PlayerPrefs.SetString(GroupKeysKey, ";");
                }

                return PlayerPrefs.GetString(GroupKeysKey);
            }
            set {
                PlayerPrefs.SetString(GroupKeysKey, value);
            }
        }
        /*! \endcond */

        /// <summary>
        /// Sets the group's volume. During startup (Awake event), the Master Audio prefab will assign any Sound Groups that match to the levels you specify here. This will also set the Group's Volume in the current Scene's Master Audio prefab, if a match exists.
        /// </summary>
        /// <param name="grpName">Group name</param>
        /// <param name="vol">Volume</param>
        public static void SetGroupVolume(string grpName, float vol) {
            var groupKey = GetGroupKey(grpName);

            PlayerPrefs.SetFloat(groupKey, vol);

            var ma = MasterAudio.SafeInstance;
            if (ma == null) {
                return;
            }

            if (MasterAudio.GrabGroup(grpName, false) != null) {
                MasterAudio.SetGroupVolume(grpName, vol);
            }

            if (GroupsUpdatedKeys.Contains(Separator + grpName + Separator)) {
                return;
            }

            GroupsUpdatedKeys += grpName + Separator;
        }

        /// <summary>
        /// Gets the group volume (used by Master Audio prefab during Awake event to set persistent levels).
        /// </summary>
        /// <returns>The group volume.</returns>
        /// <param name="grpName">Group name.</param>
        public static float? GetGroupVolume(string grpName) {
            var groupKey = GetGroupKey(grpName);

            if (!PlayerPrefs.HasKey(groupKey)) {
                return null;
            }

            return PlayerPrefs.GetFloat(groupKey);
        }

        /// <summary>
        /// Gets or sets the persistent Master Mixer Muted value. If this value is set (via code), each Master Audio prefab will read from it and set the Master Mixer Muted value to this value, during the Scene's start event. This will also set the Master Mixer Muted value in the current Scene's Master Audio prefab, if any.
        /// </summary>
        /// <value>The mixer mute setting.</value>
        public static bool? MixerMuted {
            get {
                if (!PlayerPrefs.HasKey(SfxMuteKey)) {
                    return null;
                }

                return PlayerPrefs.GetInt(SfxMuteKey) != 0;
            }
            set {
                if (!value.HasValue) {
                    PlayerPrefs.DeleteKey(SfxMuteKey);
                    return;
                }

                var newVal = value.Value;
                PlayerPrefs.SetInt(SfxMuteKey, newVal ? 1 : 0);
                var ma = MasterAudio.SafeInstance;
                if (ma != null) {
                    MasterAudio.MixerMuted = newVal;
                }
            }
        }

        /// <summary>
        /// Gets or sets the persistent Master Mixer Volume value. If this value is set (via code), each Master Audio prefab will read from it and set the Master Mixer Volume to this value, during the Scene's start event. This will also set the Master Mixer Volume in the current Scene's Master Audio prefab, if any.
        /// </summary>
        /// <value>The mixer volume.</value>
        public static float? MixerVolume {
            get {
                if (!PlayerPrefs.HasKey(SfxVolKey)) {
                    return null;
                }

                return PlayerPrefs.GetFloat(SfxVolKey);
            }
            set {
                if (!value.HasValue) {
                    PlayerPrefs.DeleteKey(SfxVolKey);
                    return;
                }

                var newVal = value.Value;
                PlayerPrefs.SetFloat(SfxVolKey, newVal);
                var ma = MasterAudio.SafeInstance;
                if (ma != null) {
                    MasterAudio.MasterVolumeLevel = newVal;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Master Playlist Muted value. If this value is set, each Master Audio prefab will read from it and set the Master Playlist Muted to this value, during the Scene's start event. This will also set the Master Playlist Muted value in the current Scene's Master Audio prefab, if any.
        /// </summary>
        /// <value>The music mute setting.</value>
        public static bool? MusicMuted {
            get {
                if (!PlayerPrefs.HasKey(MusicMuteKey)) {
                    return null;
                }

                return PlayerPrefs.GetInt(MusicMuteKey) != 0;
            }
            set {
                if (!value.HasValue) {
                    PlayerPrefs.DeleteKey(MusicMuteKey);
                    return;
                }

                var newVal = value.Value;
                PlayerPrefs.SetInt(MusicMuteKey, newVal ? 1 : 0);
                var ma = MasterAudio.SafeInstance;
                if (ma != null) {
                    MasterAudio.PlaylistsMuted = newVal;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Master Playlist Volume. If this value is set, each Master Audio prefab will read from it and set the Master Playlist Volume to this value, during the Scene's start event. This will also set the Master Playlist Volume in the current Scene's Master Audio prefab, if any.
        /// </summary>
        /// <value>The mixer volume.</value>
        public static float? MusicVolume {
            get {
                if (!PlayerPrefs.HasKey(MusicVolKey)) {
                    return null;
                }

                return PlayerPrefs.GetFloat(MusicVolKey);
            }
            set {
                if (!value.HasValue) {
                    PlayerPrefs.DeleteKey(MusicVolKey);
                    return;
                }

                var newVal = value.Value;
                PlayerPrefs.SetFloat(MusicVolKey, newVal);
                var ma = MasterAudio.SafeInstance;
                if (ma != null) {
                    MasterAudio.PlaylistMasterVolume = newVal;
                }
            }
        }

        /*! \cond PRIVATE */
        public static void RestoreMasterSettings() {
            if (MixerVolume.HasValue) {
                MasterAudio.MasterVolumeLevel = MixerVolume.Value;
            }

            if (MixerMuted.HasValue) {
                MasterAudio.MixerMuted = MixerMuted.Value;
            }

            if (MusicVolume.HasValue) {
                MasterAudio.PlaylistMasterVolume = MusicVolume.Value;
            }

            if (MusicMuted.HasValue) {
                MasterAudio.PlaylistsMuted = MusicMuted.Value;
            }
        }
        /*! \endcond */
    }
}