//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationEditorSettings.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	public static class OdinLocalizationEditorSettings
	{
		public static LocalizationEditorSettings Instance =>
			(LocalizationEditorSettings) OdinLocalizationReflectionValues.LocalizationEditorSettings_Instance.GetValue(null);

		public static AddressableAssetSettings GetAddressableAssetSettings(bool create)
		{
			return OdinLocalizationReflectionValues.LocalizationEditorSettings_GetAddressableAssetSettingsFunc(create);
		}

		public static bool IsTableNameValid(Type collectionType, string name, out string errorMessage)
		{
			errorMessage = (string) OdinLocalizationReflectionValues.LocalizationEditorSettings_IsTableNameValid.Invoke(Instance,
					 new object[] {collectionType, name});

			return string.IsNullOrEmpty(errorMessage);
		}

		public static bool IsTableNameValid(Type collectionType, string name)
		{
			return IsTableNameValid(collectionType, name, out string _);
		}

		public static bool CreateDefaultLocalizationSettingsAsset()
		{
			if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
			{
				return false;
			}

			string localizationSettingsPath = EditorUtility.SaveFilePanelInProject("Create Localization Settings",
																										  "Localization Settings",
																										  "asset",
																										  "Create the Localization Settings asset");

			if (string.IsNullOrEmpty(localizationSettingsPath))
			{
				return false;
			}

			var settings = ScriptableObject.CreateInstance<LocalizationSettings>();
			settings.name = "Default Localization Settings";


			AssetDatabase.CreateAsset(settings, localizationSettingsPath);
			AssetDatabase.SaveAssets();

			LocalizationEditorSettings.ActiveLocalizationSettings = settings;

			return true;
		}
	}
}