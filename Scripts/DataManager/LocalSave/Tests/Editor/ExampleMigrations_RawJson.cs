// namespace DataManager.LocalSave.Tests.Editor
// {
//     using System;
//     using System.Collections.Generic;
//     using Cysharp.Threading.Tasks;
//     using DataManager.LocalSave.Migration;
//     using DataManager.UserData;
//     using Newtonsoft.Json.Linq;

    /// <summary>
    /// Example migrations demonstrating raw JSON usage for complex schema changes
    /// </summary>
    // public class UserCommonDataTest : IUserData
    // {
    //     public string                  Username           { get; set; }
    //     public int                     Level              { get; set; }
    //     public List<InventoryItemTest> Items              { get; set; }
    //     public int                     Gold               { get; set; }
    //     public int                     Experience         { get; set; }
    //     public int                     BestScore          { get; set; }
    //     public SubscriptionTier        SubscriptionTier   { get; set; }
    //     public DateTime?               SubscriptionExpiry { get; set; }
    //     public bool                    TutorialCompleted  { get; set; }
    // }
    //
    // public class InventoryItemTest
    // {
    //     public string Id { get; set; }
    //     public int Count { get; set; }
    // }
    //
    // public enum SubscriptionTier
    // {
    //     Free,
    //     Premium
    // }


    /// <summary>
    /// Example migrations demonstrating raw JSON usage for complex schema changes
    /// </summary>

    // Example 1: Field Rename
    // [Migration("1.0.0", "1.1.0", "Rename PlayerName to Username")]
    // public class RenamePlayerNameMigration : BaseDataMigration<UserCommonDataTest>
    // {
    //     public override UniTask Migrate(UserCommonDataTest data, string rawJson, IMigrationContext context)
    //     {
    //         // Parse raw JSON to access old field name
    //         var json = JObject.Parse(rawJson);
    //
    //         // Check if old field exists
    //         if (json["PlayerName"] != null && string.IsNullOrEmpty(data.Username))
    //         {
    //             data.Username = json["PlayerName"].Value<string>();
    //             context.Log($"Migrated PlayerName → Username: {data.Username}");
    //         }
    //         
    //         return UniTask.CompletedTask;
    //     }
    // }

    // Example 2: Field Type Change (string → int)
    // [Migration("1.1.0", "1.2.0", "Convert Level from string to int")]
    // public class ConvertLevelTypeMigration : BaseDataMigration<UserCommonDataTest>
    // {
    //     public override UniTask Migrate(UserCommonDataTest data, string rawJson, IMigrationContext context)
    //     {
    //         // If deserialization failed (Level is 0), parse from raw JSON
    //         if (data.Level == 0)
    //         {
    //             var json = JObject.Parse(rawJson);
    //             if (json["Level"]?.Type == JTokenType.String)
    //             {
    //                 var levelString = json["Level"].Value<string>();
    //                 if (int.TryParse(levelString, out int level))
    //                 {
    //                     data.Level = level;
    //                     context.Log($"Converted Level from string \"{levelString}\" to int {level}");
    //                 }
    //                 else
    //                 {
    //                     data.Level = 1; // Safe default
    //                     context.Log($"Failed to parse Level \"{levelString}\", defaulted to 1");
    //                 }
    //             }
    //         }
    //         
    //         return UniTask.CompletedTask;
    //     }
    // }

    // Example 3: Complex Nested Structure Migration
    // [Migration("1.2.0", "1.3.0", "Migrate old inventory structure to new format")]
    // public class MigrateInventoryStructureMigration : BaseDataMigration<UserCommonDataTest>
    // {
    //     public override UniTask Migrate(UserCommonDataTest data, string rawJson, IMigrationContext context)
    //     {
    //         var json = JObject.Parse(rawJson);
    //
    //         // OLD structure: { "Items": [ "item1", "item2" ] }
    //         // NEW structure: { "Items": [ { "Id": "item1", "Count": 1 } ] }
    //
    //         if (json["Items"]?.Type == JTokenType.Array)
    //         {
    //             var oldItems = json["Items"] as JArray;
    //
    //             // Check if old format (array of strings)
    //             if (oldItems?.Count > 0 && oldItems[0].Type == JTokenType.String)
    //             {
    //                 data.Items = new List<InventoryItemTest>();
    //                 foreach (var item in oldItems)
    //                 {
    //                     data.Items.Add(new InventoryItemTest
    //                     {
    //                         Id = item.Value<string>(),
    //                         Count = 1 // Default count
    //                     });
    //                 }
    //
    //                 context.Log($"Migrated {oldItems.Count} items from old format");
    //             }
    //         }
    //         
    //         return UniTask.CompletedTask;
    //     }
    // }

    // Example 4: Multiple Field Renames
    // [Migration("1.3.0", "1.4.0", "Rename multiple fields to match new naming convention")]
    // public class MultiFieldRenameMigration : BaseDataMigration<UserCommonDataTest>
    // {
    //     public override UniTask Migrate(UserCommonDataTest data, string rawJson, IMigrationContext context)
    //     {
    //         var json = JObject.Parse(rawJson);
    //
    //         // Map of old name → new value setter
    //         var fieldMappings = new Dictionary<string, Action<JToken>>
    //         {
    //             ["GoldCoins"] = token => data.Gold = token.Value<int>(),
    //             ["PlayerExp"] = token => data.Experience = token.Value<int>(),
    //             ["HighScore"] = token => data.BestScore = token.Value<int>()
    //         };
    //
    //         foreach (var mapping in fieldMappings)
    //         {
    //             if (json[mapping.Key] != null)
    //             {
    //                 mapping.Value(json[mapping.Key]);
    //                 context.Log($"Migrated {mapping.Key}");
    //             }
    //         }
    //         
    //         return UniTask.CompletedTask;
    //     }
    // }

    // Example 5: Conditional Migration Based on Old Data
    // [Migration("1.4.0", "1.5.0", "Convert legacy subscription status")]
    // public class ConvertSubscriptionMigration : BaseDataMigration<UserCommonDataTest>
    // {
    //     public override UniTask Migrate(UserCommonDataTest data, string rawJson, IMigrationContext context)
    //     {
    //         var json = JObject.Parse(rawJson);
    //
    //         // OLD: IsPremium (bool) + PremiumExpiryDate (string)
    //         // NEW: SubscriptionTier (enum) + SubscriptionExpiry (DateTime?)
    //
    //         if (json["IsPremium"] != null)
    //         {
    //             bool isPremium = json["IsPremium"].Value<bool>();
    //
    //             if (isPremium)
    //             {
    //                 data.SubscriptionTier = SubscriptionTier.Premium;
    //
    //                 // Try to parse expiry date
    //                 if (json["PremiumExpiryDate"] != null)
    //                 {
    //                     var expiryString = json["PremiumExpiryDate"].Value<string>();
    //                     if (DateTime.TryParse(expiryString, out DateTime expiry))
    //                     {
    //                         data.SubscriptionExpiry = expiry;
    //                     }
    //                 }
    //
    //                 context.Log($"Converted premium status: {data.SubscriptionTier}");
    //             }
    //             else
    //             {
    //                 data.SubscriptionTier = SubscriptionTier.Free;
    //             }
    //         }
    //         
    //         return UniTask.CompletedTask;
    //     }
    // }

    // Example 6: Safe Fallback for Missing Data
    // [Migration("1.5.0", "1.6.0", "Add default tutorial state for existing users")]
    // public class AddTutorialStateMigration : BaseDataMigration<UserCommonDataTest>
    // {
    //     public override UniTask Migrate(UserCommonDataTest data, string rawJson, IMigrationContext context)
    //     {
    //         // New field: TutorialCompleted
    //         // For existing users (no field in JSON), mark as completed
    //         // For new users (field exists), keep as is
    //
    //         var json = JObject.Parse(rawJson);
    //
    //         if (json["TutorialCompleted"] == null)
    //         {
    //             // Old user - skip tutorial
    //             data.TutorialCompleted = true;
    //             context.Log("Marked existing user as tutorial completed");
    //         }
    //         // else: new user, TutorialCompleted already deserialized from JSON
    //         
    //         return UniTask.CompletedTask;
    //     }
    // }
// }
