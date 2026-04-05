using DataManager.Blueprint.BlueprintController;

namespace DataManager.MasterData
{
    using System.Collections.Generic;
    using DataManager.LocalSave;
    using DataManager.LocalSave.Encryption;
    using DataManager.LocalSave.Handler;
    using DataManager.LocalSave.Provider;
    using GameConfigs;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class DataManagerInstaller : Installer<DataManagerInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.DeclareSignal<MasterDataReadySignal>();
            this.Container.DeclareSignal<MasterDataRegisterSignal>();

            this.Container.BindInterfacesAndSelfTo<MasterDataManager>().AsSingle().NonLazy();
            this.Container.BindExecutionOrder<MasterDataManager>(-9999);

            BlueprintServicesInstaller.Install(this.Container);

            // Bind DataManagerConfig from GDKConfig
            this.Container.Bind<LocalSaveConfig>()
                .FromResolveGetter<GDKConfig>(config => config.GetGameConfig<LocalSaveConfig>())
                .AsCached();

            // Bind encryption service — salt prefix from config for per-game uniqueness
            this.Container.Bind<IEncryptionService>()
                .FromMethod(ctx =>
                {
                    var cfg = ctx.Container.TryResolve<LocalSaveConfig>();
                    var saltPrefix = cfg != null ? cfg.EncryptionSaltPrefix : null;
                    return new AesEncryptionService(saltPrefix);
                })
                .AsSingle();

            // Bind HandleLocalDataServices — providers built from config
            this.Container.Bind<IHandleLocalDataServices>()
                .FromMethod(ctx =>
                {
                    var logService = ctx.Container.Resolve<ILogService>();
                    var config = ctx.Container.TryResolve<LocalSaveConfig>();
                    var encryptionService = ctx.Container.TryResolve<IEncryptionService>();

                    // ── Primary Provider ──────────────────────────────────
                    // Built from config.PrimaryProvider (Inspector-configured)
                    var primaryConfig = config?.PrimaryProvider ?? StorageProviderConfig.DefaultFileBased();
                    var primaryProvider = StorageProviderFactory.Create(primaryConfig, logService);

                    // Encryption: only passed if the config enables it globally.
                    // Per-provider encryption is controlled by each StorageProviderConfig.EnableEncryption.
                    var encryption = (config == null || config.PrimaryProvider.EnableEncryption) ? encryptionService : null;

                    return new HandleLocalDataServices(
                        logService, encryption,
                        primaryProvider);
                })
                .AsSingle();
        }
    }
}
