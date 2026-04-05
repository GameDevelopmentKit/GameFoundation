namespace DataManager.LocalSave.Provider
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Low-level storage provider interface.
    /// Implementations handle raw JSON persistence to a specific backend.
    /// No caching, no profile logic — just read/write/delete of raw strings.
    /// 
    /// Encryption note: The manager (HandleLocalDataServices) handles encryption centrally.
    /// Each provider declares via <see cref="UseEncryption"/> whether data should be
    /// encrypted before writing and decrypted after reading. For example:
    /// - FileStorageProvider: true (data on disk should be encrypted)
    /// - PlayerPrefsStorageProvider: true (data in PlayerPrefs should be encrypted)
    /// - CloudStorageProvider: false (UGS encrypts at rest, no need for double encryption)
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Human-readable name for logging (e.g., "File", "PlayerPrefs", "Cloud")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Whether this provider is currently available for operations.
        /// Local providers always return true.
        /// Cloud providers return false when offline or not authenticated.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Whether the manager should encrypt data before writing to this provider
        /// and decrypt data after reading from this provider.
        /// </summary>
        bool UseEncryption { get; }

        /// <summary>
        /// Initialize the provider (create directories, authenticate, etc.)
        /// Called once during HandleLocalDataServices.InitializeAsync().
        /// </summary>
        UniTask InitializeAsync();

        /// <summary>
        /// Save raw JSON string for a given profile and key.
        /// </summary>
        /// <param name="profileId">Profile ID. Null for global data (e.g., ProfileRegistry).</param>
        /// <param name="key">Data key (e.g., "LD-WalletData", "ProfileMetadata").</param>
        /// <param name="json">Raw or encrypted JSON string to persist.</param>
        UniTask SaveAsync(string profileId, string key, string json);

        /// <summary>
        /// Load raw JSON string for a given profile and key.
        /// Returns null if not found.
        /// </summary>
        /// <param name="profileId">Profile ID. Null for global data.</param>
        /// <param name="key">Data key.</param>
        /// <returns>JSON string, or null if not found.</returns>
        UniTask<string> LoadAsync(string profileId, string key);

        /// <summary>
        /// Delete data for a given profile and key.
        /// </summary>
        /// <param name="profileId">Profile ID. Null for global data.</param>
        /// <param name="key">Data key.</param>
        UniTask DeleteAsync(string profileId, string key);
    }
}
