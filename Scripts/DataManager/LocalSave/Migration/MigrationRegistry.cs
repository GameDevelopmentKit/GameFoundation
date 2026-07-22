namespace DataManager.LocalSave.Migration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;

    /// <summary>
    /// Registry that auto-discovers all migrations in assemblies and organizes them by version.
    /// </summary>
    public class MigrationRegistry
    {
        private readonly ILogService logService;

        public MigrationRegistry(ILogService logService)
        {
            this.logService = logService;
        }

        /// <summary>
        /// Information about a discovered migration
        /// </summary>
        public class MigrationInfo
        {
            public string FromVersion { get; set; }
            public string ToVersion { get; set; }
            public string Description { get; set; }
            public int Priority { get; set; }
            public Type MigrationType { get; set; }
            public Type TargetDataType { get; set; }
            public IDataMigration Instance { get; set; }
        }

        /// <summary>
        /// Lazy discovery: only discover migrations when needed, filtered by version range.
        /// </summary>
        /// <param name="fromVersion">Starting version</param>
        /// <param name="toVersion">Target version</param>
        private Dictionary<string, List<MigrationInfo>> DiscoverMigrationsIfNeeded(string fromVersion, string toVersion)
        {
            this.logService.Log(
                $"[MigrationRegistry] Discovering migrations for range: {fromVersion} → {toVersion}...");

            var migrationsByFromVersion = new Dictionary<string, List<MigrationInfo>>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var migrationCount = 0;

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(IDataMigration).IsAssignableFrom(t));

                    foreach (var type in types)
                    {
                        var attribute = type.GetCustomAttribute<MigrationAttribute>();
                        if (attribute == null)
                        {
                            this.logService.Warning(
                                $"Migration {type.Name} implements IDataMigration but missing [Migration] attribute");
                            continue;
                        }

                        // Filter: Only discover migrations in the required range
                        // Skip if migration is before our starting point
                        if (VersionComparer.Compare(attribute.ToVersion, fromVersion) <= 0)
                        {
                            continue; // Too old
                        }

                        // Skip if migration is after our target
                        if (VersionComparer.Compare(attribute.FromVersion, toVersion) > 0)
                        {
                            continue; // Too new
                        }

                        // Get target data type from IDataMigration<T>
                        var targetDataType = this.GetTargetDataType(type);
                        if (targetDataType == null)
                        {
                            this.logService.Warning($"Migration {type.Name} must implement IDataMigration<T>");
                            continue;
                        }

                        // Create instance
                        IDataMigration instance;
                        try
                        {
                            instance = (IDataMigration)Activator.CreateInstance(type);
                        }
                        catch (Exception ex)
                        {
                            this.logService.Error($"Failed to create instance of {type.Name}: {ex.Message}");
                            continue;
                        }

                        var info = new MigrationInfo
                        {
                            FromVersion = attribute.FromVersion,
                            ToVersion = attribute.ToVersion,
                            Description = attribute.Description,
                            Priority = attribute.Priority,
                            MigrationType = type,
                            TargetDataType = targetDataType,
                            Instance = instance
                        };

                        // Add to registry
                        if (!migrationsByFromVersion.ContainsKey(info.FromVersion))
                        {
                            migrationsByFromVersion[info.FromVersion] = new List<MigrationInfo>();
                        }

                        migrationsByFromVersion[info.FromVersion].Add(info);
                        migrationCount++;

                        this.logService.Log(
                            $"  Registered: {type.Name} ({info.FromVersion} → {info.ToVersion}) - {info.Description}");
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Some assemblies can't be reflected, skip them
                    this.logService.Warning($"Could not load types from assembly {assembly.FullName}: {ex.Message}");
                }
            }

            // Sort each version's migrations by priority
            foreach (var list in migrationsByFromVersion.Values)
            {
                list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            this.logService.LogWithColor($"[MigrationRegistry] Discovered {migrationCount} migrations in range",
                Color.cyan);

            return migrationsByFromVersion;
        }

        /// <summary>
        /// Get target data type from IDataMigration<T> interface
        /// </summary>
        private Type GetTargetDataType(Type migrationType)
        {
            var interfaces = migrationType.GetInterfaces();
            var genericInterface = interfaces.FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataMigration<>));

            return genericInterface?.GetGenericArguments()[0];
        }

        /// <summary>   
        /// Build migration path from one version to another (sequential)
        /// </summary>
        public List<KeyValuePair<string, List<MigrationInfo>>> BuildMigrationPath(string fromVersion,
            string targetVersion)
        {
            // Lazy discovery: only discover migrations when needed
            var migrationsByFromVersion = this.DiscoverMigrationsIfNeeded(fromVersion, targetVersion);

            var path = new List<KeyValuePair<string, List<MigrationInfo>>>();
            var sortedMigrations = migrationsByFromVersion.Keys.ToList();
            sortedMigrations.Sort(VersionComparer.Compare);
            foreach (var migration in sortedMigrations)
            {
                path.Add(new KeyValuePair<string, List<MigrationInfo>>(migration, migrationsByFromVersion[migration]));
            }

            return path;
        }

    }
}
