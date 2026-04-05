namespace DataManager.LocalSave.Migration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Handler;
    using DataManager.LocalSave.Profile;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;

    /// <summary>
    /// Executes migration chains with rollback support and detailed history tracking.
    /// </summary>
    public class MigrationExecutor
    {
        private readonly MigrationRegistry registry;
        private readonly ILogService logService;

        public MigrationExecutor(ILogService logService)
        {
            this.registry = new MigrationRegistry(logService);
            this.logService = logService;
        }

        /// <summary>
        /// Execute migrations from one version to another.
        /// Returns updated manifest with migration history.
        /// </summary>
        /// <param name="dataServices">Data services for loading/saving data</param>
        /// <param name="manifest">Current save manifest</param>
        /// <param name="targetVersion">Target game version</param>
        /// <returns>Updated manifest, or null if migration failed</returns>
        public async UniTask ExecuteMigrations(
            IHandleLocalDataServices dataServices,
            ProfileMetadata manifest,
            string targetVersion)
        {
            var fromVersion = manifest.GameVersion;

            if (fromVersion == targetVersion)
            {
                this.logService.Log($"[Migration] Already at version {targetVersion}, no migration needed");
                return;
            }

            this.logService.LogWithColor($"[Migration] Planning migration: {fromVersion} → {targetVersion}",
                Color.yellow);

            // Build migration path
            var migrationPath = this.registry.BuildMigrationPath(fromVersion, targetVersion);
            if (migrationPath == null || migrationPath.Count == 0)
            {
                this.logService.Error($"[Migration] No migration path found from {fromVersion} to {targetVersion}");
                return;
            }

            this.logService.Log(
                $"[Migration] Migration path: {string.Join(" → ", migrationPath.Select(m => $"{m.Key}→{m.Value.First().ToVersion}"))}");
            this.logService.Log($"[Migration] Total migrations to run: {migrationPath.Count}");

            // Create context ONCE with data services
            // Migrations will load data directly from the service
            var context = new MigrationContext(dataServices);

            foreach (var migrationGroup in migrationPath)
            {
                var groupFromVersion = migrationGroup.Key;
                var groupToVersion = migrationGroup.Value.First().ToVersion;
                var migrations = migrationGroup.Value;

                this.logService.LogWithColor(
                    $"[Migration] Executing version step: {groupFromVersion} → {groupToVersion}", Color.cyan);

                var versionStopwatch = Stopwatch.StartNew();
                var migratedTypes = new List<string>();

                try
                {
                    // Run all migrations for this version step
                    foreach (var migration in migrations)
                    {
                        this.logService.Log($"  Running: {migration.MigrationType.Name} - {migration.Description}");

                        var migrationStopwatch = Stopwatch.StartNew();

                        // Get target data (loaded from service)
                        var targetData = await context.DataServices.LoadData(migration.TargetDataType);
                        if (targetData == null)
                        {
                            this.logService.Warning(
                                $"Target data {migration.TargetDataType.Name} not found, skipping migration");
                            continue;
                        }

                        // Get raw JSON for manual field access if needed
                        var rawJson = await context.DataServices.GetRawDataJson(migration.TargetDataType);

                        // Execute migration
                        await migration.Instance.Migrate(targetData, rawJson, context);

                        // Save modified data back to service
                        await context.DataServices.SaveData(targetData, force: true);

                        migrationStopwatch.Stop();
                        migratedTypes.Add(migration.TargetDataType.Name);

                        this.logService.Log($"    Completed in {migrationStopwatch.ElapsedMilliseconds}ms");
                    }

                    versionStopwatch.Stop();

                    // Record successful migration
                    var historyEntry = new MigrationHistoryEntry
                    {
                        FromVersion = groupFromVersion,
                        ToVersion = groupToVersion,
                        MigratedAt = DateTime.UtcNow,
                        DurationMs = versionStopwatch.ElapsedMilliseconds,
                        Description = string.Join("; ", migrations.Select(m => m.Description)),
                        MigratedTypes = migratedTypes.ToArray(),
                        Success = true,
                        Error = null
                    };

                    manifest.MigrationHistory.Add(historyEntry);

                    this.logService.LogWithColor(
                        $"[Migration] Version step completed: {groupFromVersion} → {groupToVersion} ({versionStopwatch.ElapsedMilliseconds}ms)",
                        Color.green);
                }
                catch (Exception ex)
                {
                    versionStopwatch.Stop();

                    // Record failed migration
                    var failedEntry = new MigrationHistoryEntry
                    {
                        FromVersion = groupFromVersion,
                        ToVersion = groupToVersion,
                        MigratedAt = DateTime.UtcNow,
                        DurationMs = versionStopwatch.ElapsedMilliseconds,
                        Description = string.Join("; ", migrations.Select(m => m.Description)),
                        MigratedTypes = migratedTypes.ToArray(),
                        Success = false,
                        Error = ex.Message
                    };

                    manifest.MigrationHistory.Add(failedEntry);

                    this.logService.Error($"[Migration] FAILED at {groupFromVersion} → {groupToVersion}: {ex.Message}");
                    this.logService.Error($"[Migration] Stack trace: {ex.StackTrace}");

                    // Return null to signal failure (caller should restore backup)
                    return;
                }
            }

            // Update manifest
            manifest.GameVersion = targetVersion;
            manifest.LastSavedAt = DateTime.UtcNow;

            // Log migration details if available
            var combinedLog = context.GetLog();
            if (!string.IsNullOrEmpty(combinedLog))
            {
                this.logService.Log($"Migration Log:\n{combinedLog}");
            }

            await dataServices.SaveCurrentProfileMetadataAsync();

            this.logService.LogWithColor(
                $"[Migration] ✓ Migration completed successfully: {fromVersion} → {targetVersion}", Color.green);
            this.logService.Log($"[Migration] Total time: {manifest.MigrationHistory.Sum(e => e.DurationMs)}ms");
            this.logService.Log(
                $"[Migration] Migrated types: {string.Join(", ", manifest.MigrationHistory.SelectMany(e => e.MigratedTypes).Distinct())}");
        }
    }
}
