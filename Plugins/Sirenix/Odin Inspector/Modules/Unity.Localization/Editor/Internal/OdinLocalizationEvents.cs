//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationEvents.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public static class OdinLocalizationEvents
	{
		private static readonly Action<SharedTableData.SharedTableEntry> RaiseTableEntryModifiedAction;

		private static readonly Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> RaiseTableEntryAddedAction;

		private static readonly Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> RaiseTableEntryRemovedAction;

		private static readonly Action<AssetTableCollection, AssetTable, AssetTableEntry> RaiseAssetTableEntryAddedAction;

		private static readonly Action<AssetTableCollection, AssetTable, AssetTableEntry, string> RaiseAssetTableEntryRemovedAction;

		static OdinLocalizationEvents()
		{
			RaiseTableEntryModifiedAction = OdinLocalizationReflectionValues.Create_RaiseTableEntryModified_Method_Delegate(LocalizationEditorSettings.EditorEvents);
			RaiseTableEntryAddedAction = OdinLocalizationReflectionValues.Create_RaiseTableEntryAdded_Method_Delegate(LocalizationEditorSettings.EditorEvents);
			RaiseTableEntryRemovedAction = OdinLocalizationReflectionValues.Create_RaiseTableEntryRemoved_Method_Delegate(LocalizationEditorSettings.EditorEvents);
			RaiseAssetTableEntryAddedAction =
				OdinLocalizationReflectionValues.Create_AssetTableEntryAdded_Method_Delegate(LocalizationEditorSettings.EditorEvents);
			RaiseAssetTableEntryRemovedAction =
				OdinLocalizationReflectionValues.Create_AssetTableEntryRemoved_Method_Delegate(LocalizationEditorSettings.EditorEvents);
		}

		public static void RaiseTableEntryModified(SharedTableData.SharedTableEntry sharedEntry) => RaiseTableEntryModifiedAction(sharedEntry);

		public static void RaiseTableEntryAdded(LocalizationTableCollection collection, SharedTableData.SharedTableEntry sharedEntry) =>
			RaiseTableEntryAddedAction(collection, sharedEntry);

		public static void RaiseTableEntryRemoved(LocalizationTableCollection collection, SharedTableData.SharedTableEntry sharedEntry) =>
			RaiseTableEntryRemovedAction(collection, sharedEntry);

		public static void RaiseAssetTableEntryAdded(AssetTableCollection assetCollection, AssetTable table, AssetTableEntry entry) =>
			RaiseAssetTableEntryAddedAction(assetCollection, table, entry);

		public static void RaiseAssetTableEntryRemoved(AssetTableCollection assetCollection, AssetTable table, AssetTableEntry entry, string guid) =>
			RaiseAssetTableEntryRemovedAction(assetCollection, table, entry, guid);
	}
}