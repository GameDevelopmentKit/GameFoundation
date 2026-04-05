namespace GameFoundation.Scripts.Backup
{
    using DataManager.LocalSave.Handler;
    using GameConfigs;
    using GameFoundation.Scripts.Auth;
    using GameFoundation.Scripts.Backup.UGS;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    /// <summary>
    /// Zenject installer for the Backup module.
    /// Resolves <see cref="BackupConfig"/> from <see cref="GDKConfig"/> and binds
    /// <see cref="IBackupService"/> to <see cref="BackupManager"/>.
    ///
    /// Replaces the old <c>CloudSyncInstaller</c>.
    ///
    /// Install in GameProjectInstaller (after AuthInstaller and DataManagerInstaller):
    ///   BackupInstaller.Install(this.Container);
    ///
    /// Conflict resolution is decoupled via <see cref="BackupConflictSignal"/>:
    ///   BackupManager fires the signal when a conflict is detected.
    ///   The game-side ConflictResolutionService subscribes and resolves the TCS.
    ///   No UIModule dependency here — zero cyclic assembly risk.
    ///
    /// NOTE: Callers must also DeclareSignal&lt;BackupConflictSignal&gt;() in their container.
    ///   This is done in GameProjectInstaller.DeclareSignal().
    /// </summary>
    public class BackupInstaller : Installer<BackupInstaller>
    {
        public override void InstallBindings()
        {
            // Bind BackupConfig from GDKConfig (optional — may not be added yet)
            this.Container.Bind<BackupConfig>()
                .FromMethod(ctx =>
                {
                    var gdkConfig = ctx.Container.TryResolve<GDKConfig>();
                    if (gdkConfig != null && gdkConfig.HasGameConfig<BackupConfig>())
                    {
                        return gdkConfig.GetGameConfig<BackupConfig>();
                    }

                    // No BackupConfig found — return null (manager uses defaults)
                    return null;
                })
                .AsCached();

            // Bind IBackupProvider → UGSBackupProvider
            this.Container.Bind<IBackupProvider>()
                .FromMethod(ctx =>
                {
                    var config = ctx.Container.TryResolve<BackupConfig>();
                    var logService = ctx.Container.Resolve<ILogService>();
                    return new UGSBackupProvider(config, logService);
                })
                .AsSingle();

            // Bind IBackupService → BackupManager
            // Conflict resolution is handled via BackupConflictSignal fired on SignalBus.
            // The game-side ConflictResolutionService listens and resolves the TCS.
            this.Container.Bind<IBackupService>()
                .FromMethod(ctx =>
                {
                    var backupProvider = ctx.Container.Resolve<IBackupProvider>();
                    var localDataServices = ctx.Container.Resolve<IHandleLocalDataServices>();
                    var authService = ctx.Container.Resolve<IAuthService>();
                    var config = ctx.Container.TryResolve<BackupConfig>();
                    var logService = ctx.Container.Resolve<ILogService>();
                    var signalBus = ctx.Container.Resolve<SignalBus>();

                    return new BackupManager(backupProvider, localDataServices, authService, config, logService,
                        signalBus);
                })
                .AsSingle();

            // Persistent MonoBehaviour for lifecycle events (OnApplicationPause, etc.)
            // Survives scene transitions via DontDestroyOnLoad.
            this.Container.Bind<BackupLifecycleHandler>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("[BackupLifecycle]")
                .AsSingle()
                .NonLazy();

            this.Container.DeclareSignal<BackupConflictSignal>();
        }
    }
}
