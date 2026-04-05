namespace DataManager.LocalSave.Migration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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
                        if (CompareVersions(attribute.ToVersion, fromVersion) <= 0)
                        {
                            continue; // Too old
                        }

                        // Skip if migration is after our target
                        if (CompareVersions(attribute.FromVersion, toVersion) > 0)
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
            // sort by from version using CompareVersions
            var sortedMigrations = migrationsByFromVersion.Keys.ToList();
            sortedMigrations.Sort(CompareVersions);
            foreach (var migration in sortedMigrations)
            {
                path.Add(new KeyValuePair<string, List<MigrationInfo>>(migration, migrationsByFromVersion[migration]));
            }

            return path;
        }


        private Dictionary<string, int[]> versionCache = new Dictionary<string, int[]>();

        /// <summary>
        /// Compare two semantic version strings (e.g., "1.2.3").
        /// Returns: -1 if v1 is less than v2, 0 if equal, 1 if v1 is greater than v2.
        /// </summary>
        private int CompareVersions(string v1, string v2)
        {
            if (v1 == v2) return 0;
            if (string.IsNullOrEmpty(v1)) return -1;
            if (string.IsNullOrEmpty(v2)) return 1;

            if (!this.versionCache.TryGetValue(v1, out var parts1))
            {
                parts1 = v1.Split('.').Select(p => int.TryParse(p, out var num) ? num : 0).ToArray();
                this.versionCache[v1] = parts1;
            }

            if (!this.versionCache.TryGetValue(v2, out var parts2))
            {
                parts2 = v2.Split('.').Select(p => int.TryParse(p, out var num) ? num : 0).ToArray();
                this.versionCache[v2] = parts2;
            }

            var maxLength = Math.Max(parts1.Length, parts2.Length);

            for (var i = 0; i < maxLength; i++)
            {
                var part1 = i < parts1.Length ? parts1[i] : 0;
                var part2 = i < parts2.Length ? parts2[i] : 0;

                if (part1 < part2) return -1;
                if (part1 > part2) return 1;
            }

            return 0;
        }
    }
}
