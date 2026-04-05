namespace DataManager.LocalSave
{
    using System;
    using GameConfigs;
    using UnityEngine;

    /// <summary>
    /// Configuration for the Data Manager system.
    /// Add this to the GDKConfig's game configs list in the Inspector.
    /// 
    /// The provider is configured via a <see cref="StorageProviderConfig"/>
    /// block that declares the provider type, encryption preference, and any type-specific params.
    ///
    /// Cloud backup is handled by the separate Backup module (BackupConfig).
    /// </summary>
    [Serializable]
    public class LocalSaveConfig : ScriptableObject, IGameConfig
    {
        [Header("Primary Provider (Read + Write)")]
        [Tooltip("The main storage provider. All reads come from here. Writes go here first.")]
        [SerializeField] private StorageProviderConfig primaryProvider = StorageProviderConfig.DefaultFileBased();

        [Header("Legacy Migration")]
        [Tooltip("Version to assign to legacy PlayerPrefs data that has no version metadata. " +
                 "This determines where the migration chain starts for upgrading users. " +
                 "Set to the oldest production version (e.g., '0.1.0').")]
        [SerializeField] private string lastKnownLegacyVersion = "0.1.0";

        [Header("Encryption")]
        [Tooltip("Salt prefix used for AES key derivation. Must be unique per game. " +
                 "Changing this after release will make existing saves unreadable!")]
        [SerializeField] private string encryptionSaltPrefix = "BackpackAdventures";

        #region Properties

        /// <summary>
        /// Configuration for the primary (read + write) storage provider.
        /// </summary>
        public StorageProviderConfig PrimaryProvider => this.primaryProvider;

        /// <summary>
        /// Version stamp for legacy PlayerPrefs data.
        /// When old data is detected without version metadata, this version is used
        /// as the starting point for the migration chain.
        /// </summary>
        public string LastKnownLegacyVersion => this.lastKnownLegacyVersion;

        /// <summary>
        /// Salt prefix for AES key derivation. Must be unique per game.
        /// Changing after release will make existing encrypted saves unreadable!
        /// </summary>
        public string EncryptionSaltPrefix => this.encryptionSaltPrefix;

        #endregion
    }

    /// <summary>
    /// Serializable configuration for a single storage provider instance.
    /// Contains all parameters any provider type might need.
    /// The installer reads this to construct the correct IStorageProvider.
    /// </summary>
    [Serializable]
    public class StorageProviderConfig
    {
        [Tooltip("Which storage backend this provider uses.")]
        [SerializeField] private StorageProviderType providerType = StorageProviderType.FileBased;

        [Tooltip("Whether data should be encrypted before writing to this provider.")]
        [SerializeField] private bool enableEncryption = true;

        [Header("File-Based Settings (only used when providerType = FileBased)")]
        [Tooltip("Subfolder name under Application.persistentDataPath.")]
        [SerializeField] private string saveDataFolderName = "SaveData";

        #region Properties

        /// <summary>
        /// The storage backend type for this provider.
        /// </summary>
        public StorageProviderType ProviderType => this.providerType;

        /// <summary>
        /// Whether to encrypt data for this provider.
        /// </summary>
        public bool EnableEncryption => this.enableEncryption;

        /// <summary>
        /// Folder name for file-based storage (under Application.persistentDataPath).
        /// Only used when ProviderType == FileBased.
        /// </summary>
        public string SaveDataFolderName => this.saveDataFolderName;

        #endregion

        #region Factory Presets

        /// <summary>
        /// Creates a default file-based provider config (encrypted, "SaveData" folder).
        /// </summary>
        public static StorageProviderConfig DefaultFileBased()
        {
            return new StorageProviderConfig
            {
                providerType = StorageProviderType.FileBased,
                enableEncryption = true,
                saveDataFolderName = "SaveData"
            };
        }

        /// <summary>
        /// Creates a default PlayerPrefs provider config (encrypted).
        /// </summary>
        public static StorageProviderConfig DefaultPlayerPrefs()
        {
            return new StorageProviderConfig
            {
                providerType = StorageProviderType.PlayerPrefs,
                enableEncryption = true,
                saveDataFolderName = ""
            };
        }



        #endregion
    }

    /// <summary>
    /// Available storage provider types.
    /// </summary>
    public enum StorageProviderType
    {
        /// <summary>
        /// File-based storage using Application.persistentDataPath.
        /// Recommended for production. Supports encryption and profile isolation.
        /// </summary>
        FileBased,

        /// <summary>
        /// Unity PlayerPrefs - simple K/V store.
        /// Good for editor testing, NOT recommended for production.
        /// </summary>
        PlayerPrefs
    }
}
