//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationReflectionValues.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using Sirenix.Serialization;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public static class OdinLocalizationReflectionValues
	{
		public const string TABLE_ENTRY_DATA__METADATA__PATH = "m_Metadata";
		public const string METADATA_COLLECTION__ITEMS__PATH = "m_Items";
		public const string TABLE_ENTRY__DATA__PATH = "Data";

		public static readonly bool HasAPIForCustomUndo = false;

		public static readonly PropertyInfo TableEntry_Data_Property;
		public static readonly PropertyInfo AssetTableEntry_Data_Property;
		public static readonly PropertyInfo StringTableEntry_Data_Property;

		public static readonly FieldInfo TableEntryData_m_Metadata_Field;
		public static readonly FieldInfo MetadataCollection_m_Items_Field;

		public static readonly PropertyInfo LocalizationEditorSettings_Instance;
		public static readonly MethodInfo LocalizationEditorSettings_GetAddressableAssetSettings;
		public static readonly MethodInfo LocalizationEditorSettings_IsTableNameValid;

		public static readonly MethodInfo RaiseTableEntryModified_Method;
		public static readonly MethodInfo RaiseTableEntryAdded_Method;
		public static readonly MethodInfo RaiseTableEntryRemoved_Method;
		public static readonly MethodInfo RaiseAssetTableEntryAdded_Method;
		public static readonly MethodInfo RaiseAssetTableEntryRemoved_Method;
		public static readonly MethodInfo AssetTableCollection_SetEntryAssetType_PrivateMethod;
		public static readonly MethodInfo AssetTableCollection_RemoveEntryAssetType_PrivateMethod;

		public static readonly Type AddressHelper;
		public static readonly MethodInfo FormatAssetLabelMethod;
		public static readonly MethodInfo UpdateAssetGroupMethod;

		private const BindingFlags INSTANCE_ALL = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		private const BindingFlags INSTANCE_NON_PUBLIC = BindingFlags.Instance | BindingFlags.NonPublic;
		private const BindingFlags STATIC_NON_PUBLIC = BindingFlags.Static | BindingFlags.NonPublic;

		internal static Func<bool, AddressableAssetSettings> LocalizationEditorSettings_GetAddressableAssetSettingsFunc;

		static OdinLocalizationReflectionValues()
		{
			TableEntry_Data_Property = typeof(TableEntry).GetProperty(TABLE_ENTRY__DATA__PATH, INSTANCE_ALL);
			AssetTableEntry_Data_Property = typeof(AssetTableEntry).BaseType.GetProperty(TABLE_ENTRY__DATA__PATH, INSTANCE_ALL);
			StringTableEntry_Data_Property = typeof(StringTableEntry).BaseType.GetProperty(TABLE_ENTRY__DATA__PATH, INSTANCE_ALL);

			Type tableEntryDataType = TwoWaySerializationBinder.Default.BindToType("UnityEngine.Localization.Tables.TableEntryData, Unity.Localization");

			TableEntryData_m_Metadata_Field = tableEntryDataType?.GetField(TABLE_ENTRY_DATA__METADATA__PATH, INSTANCE_ALL);
			MetadataCollection_m_Items_Field = typeof(MetadataCollection).GetField(METADATA_COLLECTION__ITEMS__PATH, INSTANCE_ALL);

			LocalizationEditorSettings_Instance = typeof(LocalizationEditorSettings).GetProperty("Instance", STATIC_NON_PUBLIC);
			LocalizationEditorSettings_GetAddressableAssetSettings = typeof(LocalizationEditorSettings).GetMethod("GetAddressableAssetSettings",
					 INSTANCE_NON_PUBLIC);
			LocalizationEditorSettings_IsTableNameValid = typeof(LocalizationEditorSettings).GetMethod("IsTableNameValid", INSTANCE_NON_PUBLIC);


			RaiseTableEntryModified_Method = LocalizationEditorSettings.EditorEvents.GetType().GetMethod("RaiseTableEntryModified", INSTANCE_ALL);
			RaiseTableEntryAdded_Method = LocalizationEditorSettings.EditorEvents.GetType().GetMethod("RaiseTableEntryAdded", INSTANCE_ALL);
			RaiseTableEntryRemoved_Method = LocalizationEditorSettings.EditorEvents.GetType().GetMethod("RaiseTableEntryRemoved", INSTANCE_ALL);
			RaiseAssetTableEntryAdded_Method = LocalizationEditorSettings.EditorEvents.GetType().GetMethod("RaiseAssetTableEntryAdded", INSTANCE_ALL);
			RaiseAssetTableEntryRemoved_Method = LocalizationEditorSettings.EditorEvents.GetType().GetMethod("RaiseAssetTableEntryRemoved", INSTANCE_ALL);
			AssetTableCollection_SetEntryAssetType_PrivateMethod = typeof(AssetTableCollection).GetMethod("SetEntryAssetType", INSTANCE_NON_PUBLIC);
			AssetTableCollection_RemoveEntryAssetType_PrivateMethod = typeof(AssetTableCollection).GetMethod("RemoveEntryAssetType", INSTANCE_NON_PUBLIC);

			if (LocalizationEditorSettings_Instance != null && LocalizationEditorSettings_GetAddressableAssetSettings != null)
			{
				LocalizationEditorSettings_GetAddressableAssetSettingsFunc =
					(Func<bool, AddressableAssetSettings>) Delegate.CreateDelegate(typeof(Func<bool, AddressableAssetSettings>),
																										LocalizationEditorSettings_Instance.GetValue(null),
																										LocalizationEditorSettings_GetAddressableAssetSettings);
			}

			AddressHelper = TwoWaySerializationBinder.Default.BindToType("UnityEngine.Localization.AddressHelper");

			FormatAssetLabelMethod = AddressHelper?.GetMethod("FormatAssetLabel",
																			  BindingFlags.Static | BindingFlags.Public,
																			  null,
																			  new[] {typeof(LocaleIdentifier)},
																			  null);

			UpdateAssetGroupMethod = typeof(AssetTableCollection).GetMethod("UpdateAssetGroup",
																								 BindingFlags.Instance | BindingFlags.NonPublic,
																								 null,
																								 new[]
																								 {
																									 typeof(AddressableAssetSettings),
																									 typeof(AddressableAssetEntry),
																									 typeof(bool)
																								 },
																								 null);

			HasAPIForCustomUndo = AddressHelper != null &&
										 FormatAssetLabelMethod != null &&
										 UpdateAssetGroupMethod != null &&
										 LocalizationEditorSettings_GetAddressableAssetSettingsFunc != null;
		}

		public static Action<SharedTableData.SharedTableEntry> Create_RaiseTableEntryModified_Method_Delegate(LocalizationEditorEvents events)
		{
			return (Action<SharedTableData.SharedTableEntry>) Delegate.CreateDelegate(typeof(Action<SharedTableData.SharedTableEntry>), 
																											  events,
																											  RaiseTableEntryModified_Method);
		}

		public static Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> Create_RaiseTableEntryAdded_Method_Delegate(
			LocalizationEditorEvents events)
		{
			return (Action<LocalizationTableCollection, SharedTableData.SharedTableEntry>)
				Delegate.CreateDelegate(typeof(Action<LocalizationTableCollection, SharedTableData.SharedTableEntry>),
												events,
												RaiseTableEntryAdded_Method);
		}

		public static Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> Create_RaiseTableEntryRemoved_Method_Delegate(
			LocalizationEditorEvents events)
		{
			return (Action<LocalizationTableCollection, SharedTableData.SharedTableEntry>)
				Delegate.CreateDelegate(typeof(Action<LocalizationTableCollection, SharedTableData.SharedTableEntry>),
												events,
												RaiseTableEntryRemoved_Method);
		}

		public static Action<AssetTableCollection, AssetTable, AssetTableEntry> Create_AssetTableEntryAdded_Method_Delegate(
			LocalizationEditorEvents events)
		{
			return (Action<AssetTableCollection, AssetTable, AssetTableEntry>)
				Delegate.CreateDelegate(typeof(Action<AssetTableCollection, AssetTable, AssetTableEntry>),
												events,
												RaiseAssetTableEntryAdded_Method);
		}

		public static Action<AssetTableCollection, AssetTable, AssetTableEntry, string> Create_AssetTableEntryRemoved_Method_Delegate(
			LocalizationEditorEvents events)
		{
			return (Action<AssetTableCollection, AssetTable, AssetTableEntry, string>)
				Delegate.CreateDelegate(typeof(Action<AssetTableCollection, AssetTable, AssetTableEntry, string>),
												events,
												RaiseAssetTableEntryRemoved_Method);
		}

		public static Action<TableEntryReference, Type, string> Create_AssetTableCollection_SetEntryAssetType_PrivateMethod_Delegate(AssetTableCollection collection)
		{
			return (Action<TableEntryReference, Type, string>) Delegate.CreateDelegate(typeof(Action<TableEntryReference, Type, string>),
																												collection,
																												AssetTableCollection_SetEntryAssetType_PrivateMethod);
		}

		public static Action<TableEntryReference, string> Create_AssetTableCollection_RemoveEntryAssetType_PrivateMethod_Delegate(AssetTableCollection collection)
		{
			return (Action<TableEntryReference, string>) Delegate.CreateDelegate(typeof(Action<TableEntryReference, string>),
																										collection,
																										AssetTableCollection_RemoveEntryAssetType_PrivateMethod);
		}

#if SIRENIX_INTERNAL
		public static void EnsureInit() { }
#endif
	}
}