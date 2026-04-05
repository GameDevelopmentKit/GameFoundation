namespace DataManager.LocalSave.Migration
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Single entry in migration history tracking.
    /// Used for debugging and support.
    /// </summary>
    [Serializable]
    public class MigrationHistoryEntry
    {
        /// <summary>
        /// Game version migrated FROM
        /// </summary>
        [JsonProperty("fromVersion")]
        public string FromVersion { get; set; }

        /// <summary>
        /// Game version migrated TO
        /// </summary>
        [JsonProperty("toVersion")]
        public string ToVersion { get; set; }

        /// <summary>
        /// When migration occurred
        /// </summary>
        [JsonProperty("migratedAt")]
        public DateTime MigratedAt { get; set; }

        /// <summary>
        /// How long migration took (milliseconds)
        /// </summary>
        [JsonProperty("durationMs")]
        public long DurationMs { get; set; }

        /// <summary>
        /// Description of what was migrated
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// List of data types that were migrated
        /// </summary>
        [JsonProperty("migratedTypes")]
        public string[] MigratedTypes { get; set; }

        /// <summary>
        /// Whether migration succeeded
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Error message if migration failed
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
