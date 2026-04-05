namespace GameFoundation.Scripts.Backup
{
    using System;
    using System.Collections.Generic;
    using DataManager.LocalSave.Profile;
    using Newtonsoft.Json;

    /// <summary>
    /// Stored as a single cloud key (<c>backup_manifest</c>).
    /// Contains all information needed to determine whether any profile
    /// needs backup or recovery — without downloading heavy data payloads.
    ///
    /// Typically ~2-5 KB even with multiple profiles.
    ///
    /// Cloud key layout:
    ///   "backup_manifest"          → this object (lightweight)
    ///   "backup_data_{profileId}"  → Dictionary&lt;string, string&gt; (heavy data per profile)
    /// </summary>
    [Serializable]
    public class BackupManifest
    {
        /// <summary>
        /// Registry of all backed-up profiles (mirrors local ProfileRegistry).
        /// </summary>
        [JsonProperty("registry")]
        public ProfileRegistry Registry { get; set; }

        /// <summary>
        /// Metadata for each backed-up profile, keyed by profileId.
        /// Used for comparison: BackupVersion, LastBackupTimestamp, SaveCount, PlayTimeSeconds, etc.
        /// </summary>
        [JsonProperty("profileMetadata")]
        public Dictionary<string, ProfileMetadata> ProfileMetadataMap { get; set; } = new();

        /// <summary>
        /// When this manifest was last written to the cloud.
        /// </summary>
        [JsonProperty("lastUpdatedAt")]
        public DateTime LastUpdatedAt { get; set; }

        /// <summary>
        /// Get metadata for a specific profile. Returns null if not in manifest.
        /// </summary>
        public ProfileMetadata GetMetadata(string profileId)
        {
            return this.ProfileMetadataMap.TryGetValue(profileId, out var m) ? m : null;
        }

        /// <summary>
        /// Update or add metadata for a profile.
        /// </summary>
        public void SetMetadata(string profileId, ProfileMetadata metadata)
        {
            this.ProfileMetadataMap[profileId] = metadata;
        }

        /// <summary>
        /// Remove a profile from the manifest.
        /// </summary>
        public void RemoveProfile(string profileId)
        {
            this.ProfileMetadataMap.Remove(profileId);
            this.Registry?.RemoveProfile(profileId);
        }

        /// <summary>
        /// Check if a profile exists in the manifest.
        /// </summary>
        public bool HasProfile(string profileId)
        {
            return this.ProfileMetadataMap.ContainsKey(profileId);
        }
    }
}
