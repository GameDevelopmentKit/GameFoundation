//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationMetadataRegistry.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Localization.Metadata;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public class OdinLocalizationMetadataRegistry
	{
		public static readonly List<Type> AssetEntryMetadataTypes = new List<Type>();

		public static readonly List<Type> StringEntryMetadataTypes = new List<Type>();

		public static readonly Dictionary<Type, bool> MetadataAllowsMultiple = new Dictionary<Type, bool>();

		static OdinLocalizationMetadataRegistry()
		{
			TypeCache.TypeCollection metadataTypes = TypeCache.GetTypesDerivedFrom(typeof(IMetadata));

			for (var i = 0; i < metadataTypes.Count; i++)
			{
				Type currentType = metadataTypes[i];

				var attr = currentType.GetCustomAttribute<MetadataAttribute>();

				if (attr is null)
				{
					MetadataAllowsMultiple[currentType] = true;
					continue;
				}

				MetadataAllowsMultiple[currentType] = attr.AllowMultiple;

				MetadataType currentAllowedTypes = attr.AllowedTypes;

				if (currentAllowedTypes.HasFlag(MetadataType.StringTableEntry))
				{
					StringEntryMetadataTypes.Add(currentType);
				}

				if (currentAllowedTypes.HasFlag(MetadataType.AssetTableEntry))
				{
					AssetEntryMetadataTypes.Add(currentType);
				}
			}
		}
	}
}