namespace DataManager.LocalSave.Provider
{
    using System;
    using GameFoundation.Scripts.Utilities.LogService;

    /// <summary>
    /// Factory that creates <see cref="IStorageProvider"/> instances from
    /// <see cref="StorageProviderConfig"/> config blocks.
    /// 
    /// Used by the DI installer to construct providers based on Inspector configuration.
    /// When adding a new provider type, add a case here and a new enum value in
    /// <see cref="StorageProviderType"/>.
    /// </summary>
    public static class StorageProviderFactory
    {
        /// <summary>
        /// Create an IStorageProvider instance from a config block.
        /// </summary>
        /// <param name="config">The provider config from the Inspector.</param>
        /// <param name="logService">Log service for providers that need it.</param>
        /// <returns>A configured IStorageProvider instance.</returns>
        /// <exception cref="ArgumentNullException">If config is null.</exception>
        /// <exception cref="NotSupportedException">If the provider type is not yet implemented.</exception>
        public static IStorageProvider Create(StorageProviderConfig config, ILogService logService)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            return config.ProviderType switch
            {
                StorageProviderType.FileBased => new FileStorageProvider(config, logService),
                StorageProviderType.PlayerPrefs => new PlayerPrefsStorageProvider(config),
                _ => throw new NotSupportedException($"Unknown provider type: {config.ProviderType}")
            };
        }
    }
}
