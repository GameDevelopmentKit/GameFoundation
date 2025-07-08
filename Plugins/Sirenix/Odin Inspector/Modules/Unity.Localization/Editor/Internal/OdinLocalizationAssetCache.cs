//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationAssetCache.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Sirenix.Config;
using UnityEditor;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public static class OdinLocalizationAssetCache
	{
		private readonly struct AssetIdentifier
		{
			public readonly Type AssetType;
			public readonly string Guid;

			public AssetIdentifier(Type assetType, string guid)
			{
				this.AssetType = assetType;
				this.Guid = guid;
			}
		}

		private static readonly Dictionary<AssetIdentifier, UnityEngine.Object> Assets = new Dictionary<AssetIdentifier, UnityEngine.Object>();

		public static UnityEngine.Object Get(string guid, Type assetType)
		{
			if (string.IsNullOrEmpty(guid))
			{
				return null;
			}

			var identifier = new AssetIdentifier(assetType, guid);

			if (Assets.TryGetValue(identifier, out UnityEngine.Object result))
			{
				return result;
			}

			string path = AssetDatabase.GUIDToAssetPath(guid);

			result = AssetDatabase.LoadAssetAtPath(path, assetType);

			Assets.Add(identifier, result);

			return result;
		}

		public static UnityEngine.Object Get(SharedTableData.SharedTableEntry sharedEntry, AssetTable assetTable, Type assetType)
		{
			AssetTableEntry entry = assetTable.GetEntry(sharedEntry.Id);

			if (entry == null || entry.IsEmpty)
			{
				return null;
			}

			return Get(entry.Guid, assetType);
		}

		public static void Clear() => Assets.Clear();
	}
}