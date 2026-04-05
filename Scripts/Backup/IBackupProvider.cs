namespace GameFoundation.Scripts.Backup
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Provider;

    /// <summary>
    /// Profile-level backup provider interface.
    /// Independent from <see cref="IStorageProvider"/>
    /// (per-key local I/O vs per-profile bulk backup I/O).
    ///
    /// Implementations: UGSBackupProvider, (future: iCloudBackupProvider, etc.)
    /// </summary>
    public interface IBackupProvider
    {
        string ProviderName { get; }
        bool IsAvailable { get; }

        /// <summary>
        /// Fetch the backup manifest (registry + all profile metadata).
        /// Single lightweight read — enough to compare all profiles without downloading data.
        /// Returns null if no backup exists.
        /// </summary>
        UniTask<BackupManifest> FetchManifestAsync();

        /// <summary>
        /// Save the backup manifest (registry + all profile metadata).
        /// </summary>
        UniTask SaveManifestAsync(BackupManifest manifest);

        /// <summary>
        /// Push a profile's data payload to the backup destination.
        /// The manifest is saved separately via <see cref="SaveManifestAsync"/>.
        /// </summary>
        UniTask BackupProfileDataAsync(string profileId, Dictionary<string, string> profileData);

        /// <summary>
        /// Pull a profile's data payload from the backup destination.
        /// Returns null if no backup data exists for this profile.
        /// </summary>
        UniTask<Dictionary<string, string>> RestoreProfileDataAsync(string profileId);

        /// <summary>
        /// Delete a backed-up profile's data key.
        /// </summary>
        UniTask DeleteProfileDataAsync(string profileId);
    }
}
