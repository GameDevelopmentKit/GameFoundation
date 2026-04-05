namespace DataManager.LocalSave.Provider
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// PlayerPrefs-based storage provider.
    /// Stores JSON strings in Unity PlayerPrefs with profile-scoped keys.
    /// 
    /// Key format:
    ///   Global data (profileId == null): key as-is (e.g., "__ProfileRegistry__")
    ///   Profile data: "{profileId}_{key}" (e.g., "default_LD-WalletData")
    /// 
    /// NOTE: Primarily for editor testing and backward compatibility.
    /// Use FileStorageProvider for production builds.
    /// </summary>
    public class PlayerPrefsStorageProvider : IStorageProvider
    {
        public string ProviderName => "PlayerPrefs";
        public bool IsAvailable => true;
        public bool UseEncryption { get; }

        /// <summary>
        /// Create from a StorageProviderConfig.
        /// </summary>
        public PlayerPrefsStorageProvider(StorageProviderConfig config)
        {
            this.UseEncryption = config?.EnableEncryption ?? true;
        }

        /// <summary>
        /// Create directly (used by editor tools and migration utilities).
        /// </summary>
        public PlayerPrefsStorageProvider(bool useEncryption = true)
        {
            this.UseEncryption = useEncryption;
        }

        public UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        public UniTask SaveAsync(string profileId, string key, string json)
        {
            var finalKey = BuildKey(profileId, key);
            PlayerPrefs.SetString(finalKey, json);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        public UniTask<string> LoadAsync(string profileId, string key)
        {
            var finalKey = BuildKey(profileId, key);
            return UniTask.FromResult(PlayerPrefs.GetString(finalKey, null));
        }

        public UniTask DeleteAsync(string profileId, string key)
        {
            var finalKey = BuildKey(profileId, key);
            PlayerPrefs.DeleteKey(finalKey);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        private static string BuildKey(string profileId, string key)
        {
            return string.IsNullOrEmpty(profileId) ? key : $"{profileId}_{key}";
        }
    }
}
