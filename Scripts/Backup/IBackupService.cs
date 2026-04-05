namespace GameFoundation.Scripts.Backup
{
    using System;
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Orchestrates backup and recovery using an <see cref="IBackupProvider"/>.
    /// Conflict resolution is handled internally via <see cref="ConflictResolution.ConflictResolutionHandler"/>.
    ///
    /// Consumers only need to call:
    /// <code>
    /// await backupService.CheckAndRecoverAsync();        // on startup
    /// await backupService.BackupCurrentProfileAsync();   // on app pause
    /// await backupService.RestoreProfileAsync(profileId); // on account switch
    /// </code>
    /// </summary>
    public interface IBackupService
    {
        BackupStatus Status { get; }

        /// <summary>
        /// Push current local profile to backup.
        /// </summary>
        UniTask BackupCurrentProfileAsync();

        /// <summary>
        /// On login: compare local vs backup, restore if needed.
        /// Conflict resolution is handled internally — shows UI popup if both sides have changes.
        /// </summary>
        UniTask<RecoveryResult> CheckAndRecoverAsync();

        /// <summary>
        /// Manually restore a specific profile from backup.
        /// Used by SettingTabPresenter on account switch.
        /// </summary>
        UniTask RestoreProfileAsync(string profileId);

        /// <summary>Fired after recovery completes (regardless of outcome).</summary>
        event Action<RecoveryResult> OnRecoveryCompleted;
    }
}
