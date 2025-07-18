//-----------------------------------------------------------------------
// <copyright file="OdinTemplateMetadata.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
#if false
	[HideInInspector] // ReadOnly
	[Metadata(AllowedTypes = MetadataType.SharedTableData, AllowMultiple = false)]
	public class OdinTemplateMetadata : IMetadata
	{
		public Type TableAssetType;
		public List<Type> MetadataExpected = new List<Type>();
	}
#endif
}