namespace GameFoundation.Scripts.Backup
{
    using DataManager.LocalSave.Profile;

    /// <summary>
    /// Information about a conflict between local and cloud data.
    /// Passed to <see cref="IBackupService.OnConflictDetected"/> subscribers
    /// so the UI can display a comparison and let the player choose.
    /// </summary>
    public class ConflictInfo
    {
        /// <summary>Local profile metadata (current device).</summary>
        public ProfileMetadata LocalMetadata { get; set; }

        /// <summary>Cloud profile metadata (from backup).</summary>
        public ProfileMetadata CloudMetadata { get; set; }
    }

    /// <summary>
    /// Player's choice when resolving a backup conflict.
    /// </summary>
    public enum ConflictChoice
    {
        /// <summary>Keep local data, overwrite cloud backup with local.</summary>
        KeepLocal,

        /// <summary>Keep cloud data, overwrite local with cloud backup.</summary>
        KeepCloud,
    }
}
