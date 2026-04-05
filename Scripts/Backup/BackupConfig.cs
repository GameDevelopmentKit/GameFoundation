namespace GameFoundation.Scripts.Backup
{
    using System;
    using GameConfigs;
    using UnityEngine;

    /// <summary>
    /// Configuration for the Backup system.
    /// Add this to the GDKConfig's game configs list in the Inspector.
    ///
    /// Replaces the old <c>CloudSyncConfig</c>.
    /// </summary>
    [Serializable]
    public class BackupConfig : ScriptableObject, IGameConfig
    {
        [Header("Backup")]
        [Tooltip("Master toggle for backup. When disabled, no backup operations are performed.")]
        [SerializeField] private bool enableBackup = true;

        [Tooltip("Automatically backup when the app pauses (goes to background).")]
        [SerializeField] private bool backupOnAppPause = true;

        [Tooltip("Automatically check for recovery when the app resumes from background.")]
        [SerializeField] private bool checkOnAppResume = true;

        [Tooltip("Automatically check for recovery during game initialization (after auth completes).")]
        [SerializeField] private bool checkOnLaunch = true;

        [Header("Cloud Keys")]
        [Tooltip("The cloud key for the backup manifest (registry + all profile metadata). " +
                 "Changing this after release will orphan existing cloud data!")]
        [SerializeField] private string manifestCloudKey = "backup_manifest";

        [Tooltip("Prefix for per-profile data keys. The profileId is appended. " +
                 "Example: backup_data_default")]
        [SerializeField] private string profileDataKeyPrefix = "backup_data_";

        [Header("Safety")]
        [Tooltip("Log a warning if the profile data payload exceeds this size (in KB). " +
                 "UGS limit is 5 MiB per key. Typical RPG save is 50–200 KB.")]
        [SerializeField] private int maxSnapshotSizeWarningKB = 1024;

        [Tooltip("Timeout in seconds for conflict resolution. " +
                 "If the player doesn't respond, defaults to KeepLocal.")]
        [SerializeField] private int conflictTimeoutSeconds = 120;

        #region Properties

        /// <summary>Whether backup is enabled. When false, all backup operations are no-ops.</summary>
        public bool EnableBackup => this.enableBackup;

        /// <summary>Whether to automatically backup when the app pauses.</summary>
        public bool BackupOnAppPause => this.backupOnAppPause;

        /// <summary>Whether to automatically check for recovery on app resume.</summary>
        public bool CheckOnAppResume => this.checkOnAppResume;

        /// <summary>Whether to automatically check for recovery during initialization.</summary>
        public bool CheckOnLaunch => this.checkOnLaunch;

        /// <summary>The cloud key for the backup manifest.</summary>
        public string ManifestCloudKey => this.manifestCloudKey;

        /// <summary>Prefix for per-profile data keys.</summary>
        public string ProfileDataKeyPrefix => this.profileDataKeyPrefix;

        /// <summary>Warning threshold for profile data size in KB.</summary>
        public int MaxSnapshotSizeWarningKB => this.maxSnapshotSizeWarningKB;

        /// <summary>Timeout in seconds for conflict resolution popup.</summary>
        public int ConflictTimeoutSeconds => this.conflictTimeoutSeconds;

        #endregion
    }
}
