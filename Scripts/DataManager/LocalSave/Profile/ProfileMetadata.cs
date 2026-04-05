namespace DataManager.LocalSave.Profile
{
    using System;
    using System.Collections.Generic;
    using DataManager.LocalSave.Migration;
    using DataManager.UserData;
    using Newtonsoft.Json;

    /// <summary>
    /// Combined profile metadata and save manifest.
    /// Contains all information about a save profile including version, migrations, and tracked data keys.
    /// </summary>
    [Serializable]
    public class ProfileMetadata : IUserData
    {
        #region Profile Information

        /// <summary>
        /// Unique profile identifier (e.g., "default", "profile_1", "profile_2")
        /// </summary>
        [JsonProperty("profileId")]
        public string ProfileId { get; set; }

        /// <summary>
        /// Human-readable display name (e.g., "Main Save", "Hard Mode", "Speed Run")
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Timestamp when profile was created
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of last time this profile was played/loaded
        /// </summary>
        [JsonProperty("lastPlayedAt")]
        public DateTime LastPlayedAt { get; set; }

        /// <summary>
        /// Timestamp of last save operation
        /// </summary>
        [JsonProperty("lastSavedAt")]
        public DateTime LastSavedAt { get; set; }

        /// <summary>
        /// Total playtime in seconds for this profile
        /// </summary>
        [JsonProperty("playTimeSeconds")]
        public long PlayTimeSeconds { get; set; }

        /// <summary>
        /// Number of save operations performed on this profile
        /// </summary>
        [JsonProperty("saveCount")]
        public int SaveCount { get; set; }

        #endregion

        #region Version & Device Information

        /// <summary>
        /// Current game version (from Application.version)
        /// Used to trigger migrations when version changes
        /// </summary>
        [JsonProperty("gameVersion")]
        public string GameVersion { get; set; }

        /// <summary>
        /// Device identifier that created this profile
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        #endregion

        #region Backup

        /// <summary>
        /// Monotonically increasing counter for backup operations.
        /// Incremented on every successful backup.
        /// Used to detect which side (local vs cloud) has newer data.
        /// </summary>
        [JsonProperty("backupVersion")]
        public int BackupVersion { get; set; }

        /// <summary>
        /// Timestamp of the last successful backup to cloud.
        /// Null if this profile has never been backed up.
        /// </summary>
        [JsonProperty("lastBackupTimestamp")]
        public DateTime? LastBackupTimestamp { get; set; }



        #endregion

        #region Migration Information

        /// <summary>
        /// Complete migration history for this profile (detailed for debugging)
        /// </summary>
        [JsonProperty("migrationHistory")]
        public List<MigrationHistoryEntry> MigrationHistory { get; set; } = new List<MigrationHistoryEntry>();

        #endregion

        #region Data Tracking

        /// <summary>
        /// List of all data item keys currently stored in this profile.
        /// Updated whenever data is saved or deleted.
        /// Allows enumeration without needing to scan all possible keys.
        /// Example: ["LD-PlayerData", "LD-InventoryData", "LD-ProgressData"]
        /// </summary>
        [JsonProperty("dataKeys")]
        public HashSet<string> DataKeys { get; set; } = new HashSet<string>();

        #endregion

        #region Constructors
        /// <summary>
        /// Create a new profile metadata
        /// </summary>
        public static ProfileMetadata CreateNew(string profileId, string displayName, string gameVersion, string deviceId)
        {
            return new ProfileMetadata
            {
                ProfileId = profileId,
                DisplayName = string.IsNullOrEmpty(displayName) ? profileId : displayName,
                CreatedAt = DateTime.UtcNow,
                LastPlayedAt = DateTime.UtcNow,
                LastSavedAt = DateTime.UtcNow,
                PlayTimeSeconds = 0,
                SaveCount = 0,
                GameVersion = gameVersion,
                DeviceId = deviceId,
                MigrationHistory = new List<MigrationHistoryEntry>(),
                DataKeys = new HashSet<string>()
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Track a new data key (called when saving data)
        /// </summary>
        public void SetDataKey(string key)
        {
            this.DataKeys.Add(key);
        }

        /// <summary>
        /// Remove a data key (called when deleting data)
        /// </summary>
        public void RemoveDataKey(string key)
        {
            this.DataKeys.Remove(key);
        }

        /// <summary>
        /// Check if a data key is tracked
        /// </summary>
        public bool HasDataKey(string key)
        {
            return this.DataKeys.Contains(key);
        }

        /// <summary>
        /// Increment save counter and update timestamp
        /// </summary>
        public void RecordSave()
        {
            this.SaveCount++;
            this.LastSavedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Update last played timestamp (called when profile is loaded/switched to)
        /// </summary>
        public void RecordPlay()
        {
            this.LastPlayedAt = DateTime.UtcNow;
        }

        #endregion
    }
}
