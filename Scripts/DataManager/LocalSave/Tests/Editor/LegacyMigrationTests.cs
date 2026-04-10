// namespace DataManager.LocalSave.Tests.Editor
// {
//     using System.IO;
//     using DataManager.LocalSave.Handler;
//     using DataManager.LocalSave.Profile;
//     using DataManager.LocalSave.Provider;
//     using GameFoundation.Scripts.Utilities.LogService;
//     using Newtonsoft.Json;
//     using NUnit.Framework;
//     using UnityEngine;
//
//     /// <summary>
//     /// EditMode tests for <see cref="LegacyPlayerPrefsMigrator"/>.
//     /// Verifies detection, migration, version stamping, idempotency, and skip scenarios.
//     /// 
//     /// These tests use a real <see cref="FileStorageProvider"/> writing to a temp directory
//     /// under Application.temporaryCachePath so we can verify actual file I/O.
//     /// </summary>
//     [TestFixture]
//     public class LegacyMigrationTests
//     {
//         private const string MigrationCompleteKey = "__LegacyPlayerPrefsMigrationDone__";
//         private string testSaveFolder;
//         private FileStorageProvider fileProvider;
//         private LocalSaveConfig config;
//
//         [SetUp]
//         public void SetUp()
//         {
//             // Clear any migration flags from previous runs
//             PlayerPrefs.DeleteKey(MigrationCompleteKey);
//
//             // Clear all known legacy keys
//             foreach (var key in KnownLegacyKeys)
//             {
//                 PlayerPrefs.DeleteKey(key);
//             }
//
//             PlayerPrefs.Save();
//
//             // Create a unique temp folder for each test
//             testSaveFolder = $"LegacyMigrationTest_{System.Guid.NewGuid():N}";
//             fileProvider = new FileStorageProvider(
//                 new TestLogService(),
//                 testSaveFolder,
//                 useEncryption: false);
//
//             // Create config ScriptableObject for testing
//             config = ScriptableObject.CreateInstance<LocalSaveConfig>();
//         }
//
//         [TearDown]
//         public void TearDown()
//         {
//             // Clean up PlayerPrefs
//             PlayerPrefs.DeleteKey(MigrationCompleteKey);
//             foreach (var key in KnownLegacyKeys)
//             {
//                 PlayerPrefs.DeleteKey(key);
//             }
//
//             PlayerPrefs.Save();
//
//             // Clean up temp files
//             var testPath = Path.Combine(Application.persistentDataPath, testSaveFolder);
//             if (Directory.Exists(testPath))
//             {
//                 Directory.Delete(testPath, recursive: true);
//             }
//
//             // Destroy ScriptableObject
//             if (config != null)
//             {
//                 Object.DestroyImmediate(config);
//             }
//         }
//
//         #region Test 1: Fresh Install — No Legacy Keys
//
//         [Test]
//         public void FreshInstall_NoLegacyKeys_MarksCompleteAndSkips()
//         {
//             // Arrange: Empty PlayerPrefs (fresh install)
//             // Act
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert: Migration flag is set
//             Assert.IsTrue(PlayerPrefs.HasKey(MigrationCompleteKey),
//                 "Migration complete flag should be set even on fresh install");
//
//             // Assert: No profile files created (InitializeAsync will create them)
//             var defaultDir = Path.Combine(Application.persistentDataPath, testSaveFolder, "default");
//             Assert.IsFalse(Directory.Exists(defaultDir),
//                 "No profile directory should be created for fresh installs");
//         }
//
//         #endregion
//
//         #region Test 2: Already Migrated — Skip Immediately
//
//         [Test]
//         public void AlreadyMigrated_FlagSet_SkipsImmediately()
//         {
//             // Arrange: Set the migration complete flag
//             PlayerPrefs.SetInt(MigrationCompleteKey, 1);
//             PlayerPrefs.Save();
//
//             // Add a legacy key to verify it's NOT processed
//             PlayerPrefs.SetString("LD-WalletData", "{\"Balances\":{\"gold\":{\"Value\":500}}}");
//             PlayerPrefs.Save();
//
//             // Act
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert: No files created (skipped)
//             var defaultDir = Path.Combine(Application.persistentDataPath, testSaveFolder, "default");
//             Assert.IsFalse(Directory.Exists(defaultDir),
//                 "Should not create files when already migrated");
//         }
//
//         #endregion
//
//         #region Test 3: Legacy Keys Detected — Data Migrated to Files
//
//         [Test]
//         public void LegacyKeysExist_CopiesDataToFiles()
//         {
//             // Arrange: Populate old-format PlayerPrefs keys
//             var walletJson = "{\"Balances\":{\"gold\":{\"Id\":\"gold\",\"Value\":500}}}";
//             var commonJson = "{\"UnlockedZone\":[\"Zone_1\",\"Zone_2\"]}";
//             PlayerPrefs.SetString("LD-WalletData", walletJson);
//             PlayerPrefs.SetString("LD-UserCommonData", commonJson);
//             PlayerPrefs.Save();
//
//             // Act
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert: Files exist with correct content
//             var walletFile = Path.Combine(Application.persistentDataPath, testSaveFolder, "default", "LD-WalletData.json");
//             var commonFile = Path.Combine(Application.persistentDataPath, testSaveFolder, "default", "LD-UserCommonData.json");
//
//             Assert.IsTrue(File.Exists(walletFile), "LD-WalletData.json should exist");
//             Assert.IsTrue(File.Exists(commonFile), "LD-UserCommonData.json should exist");
//
//             Assert.AreEqual(walletJson, File.ReadAllText(walletFile),
//                 "Wallet data content should match original JSON");
//             Assert.AreEqual(commonJson, File.ReadAllText(commonFile),
//                 "Common data content should match original JSON");
//         }
//
//         #endregion
//
//         #region Test 4: ProfileMetadata Stamped with Legacy Version (NOT Application.version)
//
//         [Test]
//         public void LegacyMigration_StampsCorrectLegacyVersion()
//         {
//             // Arrange
//             PlayerPrefs.SetString("LD-WalletData", "{\"Balances\":{}}");
//             PlayerPrefs.Save();
//
//             // Act
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert: Load ProfileMetadata and check version
//             var metadataFile = Path.Combine(Application.persistentDataPath, testSaveFolder, "default", "ProfileMetadata.json");
//             Assert.IsTrue(File.Exists(metadataFile), "ProfileMetadata.json should exist");
//
//             var metadataJson = File.ReadAllText(metadataFile);
//             var metadata = JsonConvert.DeserializeObject<ProfileMetadata>(metadataJson);
//
//             Assert.AreEqual("0.1.0", metadata.GameVersion,
//                 "GameVersion should be the legacy version '0.1.0', NOT Application.version");
//             Assert.AreNotEqual(Application.version, metadata.GameVersion,
//                 "GameVersion must NOT be Application.version — that's the bug we're fixing");
//         }
//
//         #endregion
//
//         #region Test 5: ProfileRegistry Created Correctly
//
//         [Test]
//         public void LegacyMigration_CreatesProfileRegistry()
//         {
//             // Arrange
//             PlayerPrefs.SetString("LD-WalletData", "{\"Balances\":{}}");
//             PlayerPrefs.Save();
//
//             // Act
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert: ProfileRegistry exists and has correct structure
//             var registryFile = Path.Combine(Application.persistentDataPath, testSaveFolder, "_global", "__ProfileRegistry__.json");
//             Assert.IsTrue(File.Exists(registryFile), "__ProfileRegistry__.json should exist");
//
//             var registryJson = File.ReadAllText(registryFile);
//             var registry = JsonConvert.DeserializeObject<ProfileRegistry>(registryJson);
//
//             Assert.AreEqual("default", registry.CurrentProfileId,
//                 "Current profile should be 'default'");
//             Assert.IsTrue(registry.HasProfile("default"),
//                 "Profile 'default' should exist in registry");
//         }
//
//         #endregion
//
//         #region Test 6: DataKeys Tracked in Metadata
//
//         [Test]
//         public void LegacyMigration_TracksDataKeysInMetadata()
//         {
//             // Arrange: 3 legacy keys
//             PlayerPrefs.SetString("LD-WalletData", "{\"Balances\":{}}");
//             PlayerPrefs.SetString("LD-UserCommonData", "{\"Data\":1}");
//             PlayerPrefs.SetString("LD-UserInventory", "{\"Items\":[]}");
//             PlayerPrefs.Save();
//
//             // Act
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert
//             var metadataFile = Path.Combine(Application.persistentDataPath, testSaveFolder, "default", "ProfileMetadata.json");
//             var metadata = JsonConvert.DeserializeObject<ProfileMetadata>(File.ReadAllText(metadataFile));
//
//             Assert.IsTrue(metadata.DataKeys.Contains("LD-WalletData"), "Should track LD-WalletData");
//             Assert.IsTrue(metadata.DataKeys.Contains("LD-UserCommonData"), "Should track LD-UserCommonData");
//             Assert.IsTrue(metadata.DataKeys.Contains("LD-UserInventory"), "Should track LD-UserInventory");
//             Assert.AreEqual(3, metadata.DataKeys.Count, "Should have exactly 3 data keys");
//         }
//
//         #endregion
//
//         #region Test 7: Idempotent — Second Run is No-Op
//
//         [Test]
//         public void LegacyMigration_Idempotent_SecondRunIsNoOp()
//         {
//             // Arrange
//             var walletJson = "{\"Balances\":{\"gold\":{\"Value\":500}}}";
//             PlayerPrefs.SetString("LD-WalletData", walletJson);
//             PlayerPrefs.Save();
//
//             // Act: Run migration twice
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Record file state after first migration
//             var walletFile = Path.Combine(Application.persistentDataPath, testSaveFolder, "default", "LD-WalletData.json");
//             var firstRunContent = File.ReadAllText(walletFile);
//             var firstRunWriteTime = File.GetLastWriteTimeUtc(walletFile);
//
//             // Small delay to detect timestamp changes
//             System.Threading.Thread.Sleep(50);
//
//             // Act: Second run
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert: File unchanged (flag prevented re-run)
//             Assert.AreEqual(firstRunContent, File.ReadAllText(walletFile),
//                 "File content should not change on second run");
//             Assert.AreEqual(firstRunWriteTime, File.GetLastWriteTimeUtc(walletFile),
//                 "File timestamp should not change on second run (no-op)");
//         }
//
//         #endregion
//
//         #region Test 8: Old PlayerPrefs Keys Preserved After Migration
//
//         [Test]
//         public void LegacyMigration_PreservesOldPlayerPrefsKeys()
//         {
//             // Arrange
//             var walletJson = "{\"Balances\":{\"gold\":{\"Value\":500}}}";
//             PlayerPrefs.SetString("LD-WalletData", walletJson);
//             PlayerPrefs.Save();
//
//             // Act
//             LegacyPlayerPrefsMigrator.MigrateIfNeeded(config, fileProvider).GetAwaiter().GetResult();
//
//             // Assert: Old key still exists (safety net for rollback)
//             Assert.AreEqual(walletJson, PlayerPrefs.GetString("LD-WalletData"),
//                 "Old PlayerPrefs key should NOT be deleted after migration (safety net)");
//         }
//
//         #endregion
//
//         #region Helpers
//
//         /// <summary>
//         /// Known legacy keys — same list as in LegacyPlayerPrefsMigrator.
//         /// </summary>
//         private static readonly string[] KnownLegacyKeys =
//         {
//             "LD-UserProfile", "LD-UserCommonData", "LD-WalletData", "LD-UserInventory",
//             "LD-GameSettingData", "LD-SoundSetting", "LD-AnalyticData", "LD-UserSynergy",
//             "LD-UserTown", "LD-UserWorker", "LD-UserMiner", "LD-UserResourceGenerator",
//             "LD-TutorialLocalData", "LD-QuestJournal", "LD-TrackingQuestData",
//             "LD-ShopData", "LD-InterstitialData", "LD-BlueprintInfoData",
//             "LD-BattlePassMileStoneData",
//         };
//
//         /// <summary>
//         /// Minimal ILogService for test isolation.
//         /// </summary>
//         private class TestLogService : ILogService
//         {
//             public void Log(string logContent, LogLevel logLevel = LogLevel.LOG) => Debug.Log(logContent);
//             public void LogWithColor(string logContent, Color? c = null) => Debug.Log(logContent);
//             public void Warning(string logContent) => Debug.LogWarning(logContent);
//             public void Error(string logContent) => Debug.LogError(logContent);
//             public void Exception(System.Exception exception) => Debug.LogException(exception);
//             public void Exception(System.Exception exception, string message) => Debug.LogError($"{message}: {exception}");
//         }
//
//         #endregion
//     }
// }
