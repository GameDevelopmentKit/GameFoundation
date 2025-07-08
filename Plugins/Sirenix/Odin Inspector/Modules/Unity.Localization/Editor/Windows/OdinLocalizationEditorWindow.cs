//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationEditorWindow.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinLocalizationCreateTableMenu
	{
		[Serializable]
		public class LocaleItem
		{
			[HideInInspector]
			public Locale Locale;

			[HideLabel]
			public bool Enabled;
		}

		public enum TableCollectionType
		{
			StringTableCollection,
			AssetTableCollection
		}

		private string FolderPath => string.IsNullOrEmpty(this.Folder) ? "Assets" : $"Assets/{this.Folder}";

		[ValidateInput(nameof(ValidateName), "@this." + nameof(nameErrorMessage))]
		[VerticalGroup("Split/Left")]
		[PropertySpace(SpaceAfter = 2, SpaceBefore = 2)]
		public string Name;

		[EnableIf("@this." + nameof(EnableFolder))]
		[InfoBox("The directory is not found, this will create a new directory on creation.", visibleIfMemberName: nameof(ShowFolderInfoBox))]
		[HorizontalGroup("Split")]
		[VerticalGroup("Split/Left")]
		[FolderPath(ParentFolder = "Assets")]
		public string Folder;

		[VerticalGroup("Split/Left")]
		[PropertySpace(SpaceAfter = 2, SpaceBefore = 2)]
		[HideLabel]
		[EnumToggleButtons]
		public TableCollectionType Type;

		[EnableIf(nameof(EnableCreateIf))]
		[VerticalGroup("Split/Left")]
		[PropertySpace(SpaceBefore = 4)]
		[Button(ButtonSizes.Large)]
		public void Create()
		{
			var localizationWindow = EditorWindow.focusedWindow as OdinLocalizationEditorWindow;
					
			if (!this.HasAnyLocaleSelected())
			{
				if (localizationWindow)
				{
					localizationWindow.ShowToast(ToastPosition.BottomLeft,
														  SdfIconType.ExclamationOctagonFill,
														  "At least 1 Locale must be selected.",
														  new Color(0.68f, 0.2f, 0.2f),
														  5.0f);
				}
				
				return;
			}

			if (!Directory.Exists(this.FolderPath))
			{
				Directory.CreateDirectory(this.FolderPath);
			}
			
			var collectionLocales = new List<Locale>(this.Locales.Count);

			foreach (LocaleItem localeItem in this.Locales)
			{
				if (localeItem.Enabled)
				{
					collectionLocales.Add(localeItem.Locale);
				}
			}

			switch (this.Type)
			{
				case TableCollectionType.StringTableCollection:
					LocalizationEditorSettings.CreateStringTableCollection(this.Name, this.FolderPath, collectionLocales);
					break;

				case TableCollectionType.AssetTableCollection:
					LocalizationEditorSettings.CreateAssetTableCollection(this.Name, this.FolderPath, collectionLocales);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (localizationWindow)
			{
				string typeNiceName;

				switch (this.Type)
				{
					case TableCollectionType.StringTableCollection:
						typeNiceName = "String Table Collection";
						break;

					case TableCollectionType.AssetTableCollection:
						typeNiceName = "Asset Table Collection";
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				localizationWindow.ShowToast(ToastPosition.BottomLeft,
													  SdfIconType.Check2,
													  $"{typeNiceName} '{this.Name}' created at: {this.FolderPath}.",
													  new Color(0.29f, 0.57f, 0.42f),
													  16.0f);
			}
		}

		[HorizontalGroup("Split")]
		[VerticalGroup("Split/Right")]
		[InlineProperty]
		[ListDrawerSettings(ListElementLabelName = "@this.Locale.LocaleName",
								  HideAddButton = true,
								  HideRemoveButton = true,
								  DefaultExpandedState = true,
								  ShowFoldout = false,
								  ShowItemCount = false,
								  DraggableItems = false)]
		public List<LocaleItem> Locales = new List<LocaleItem>();

		[HorizontalGroup("Split/Right/Split")]
		[Button]
		public void LocaleGenerator()
		{
			try
			{
				TwoWaySerializationBinder.Default.BindToType("UnityEditor.Localization.UI.LocaleGeneratorWindow, Unity.Localization.Editor")
												 .GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.Public)
												 .Invoke(null, null);
			}
			catch (NullReferenceException nullReferenceException)
			{
				Debug.LogError($"[Odin]: Failed to find LocaleGeneratorWindow.ShowWindow.\n{nullReferenceException.Message}");
			}
		}

		[HorizontalGroup("Split/Right/Split")]
		[Button]
		public void SelectNone()
		{
			for (var i = 0; i < this.Locales.Count; i++)
			{
				this.Locales[i].Enabled = false;
			}
		}

		[HorizontalGroup("Split/Right/Split")]
		[Button]
		public void SelectAll()
		{
			for (var i = 0; i < this.Locales.Count; i++)
			{
				this.Locales[i].Enabled = true;
			}
		}

		[HideInInspector]
		internal bool EnableFolder = true;

		private string nameErrorMessage = string.Empty;

		private bool ValidateName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				this.nameErrorMessage = $"{nameof(this.Name)} can't be empty.";
				return false;
			}

			Type collectionType;

			switch (this.Type)
			{
				case TableCollectionType.StringTableCollection:
					collectionType = typeof(StringTableCollection);
					break;

				case TableCollectionType.AssetTableCollection:
					collectionType = typeof(AssetTableCollection);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			bool isTableNameValid = OdinLocalizationEditorSettings.IsTableNameValid(collectionType, name, out string localizationErrorMsg);

			if (isTableNameValid)
			{
				return true;
			}

			this.nameErrorMessage = localizationErrorMsg;

			return false;
		}

		private bool ShowFolderInfoBox()
		{
			if (string.IsNullOrEmpty(this.Folder))
			{
				return false;
			}

			return !Directory.Exists(this.FolderPath);
		}

		private bool EnableCreateIf() => this.Locales.Count > 0 && this.ValidateName(this.Name);

		private bool HasAnyLocaleSelected()
		{
			for (var i = 0; i < this.Locales.Count; i++)
			{
				if (this.Locales[i].Enabled)
				{
					return true;
				}
			}

			return false;
		}
	}

	public class OdinLocalizationEditorWindow : OdinMenuEditorWindow, IDisposable
	{
		public enum RightMenuTopTabs
		{
			[LabelText(SdfIconType.Braces)]
			Metadata,

			[LabelText(SdfIconType.GearFill)]
			Settings
		}

		public enum RightMenuBottomTabs
		{
			[LabelText(SdfIconType.FlagFill)]
			Locale,

#if false
			[LabelText(SdfIconType.BorderWidth)]
			Template,
#endif

			[LabelText(SdfIconType.GearFill)]
			Settings
		}

		public class WindowState : IDisposable
		{
			public static string EditorPrefsKey = "OdinLocalizationEditorWindow_EditorPrefs";
			
			public RightMenuTopTabs CurrentTopTab;
			public RightMenuBottomTabs CurrentBottomTab;
			public PropertyTree MetadataTree = null;
			public bool ShowSharedMetadata = true;

			public float LeftMenuWidth;
			public float RightMenuWidth;
			public float RightMenuTopPanelHeight;
			public float LastOpenRightMenuWidth;

			public void Save()
			{
				EditorPrefs.SetFloat($"{EditorPrefsKey}_LeftMenuWidth", this.LeftMenuWidth);
				EditorPrefs.SetFloat($"{EditorPrefsKey}_RightMenuWidth", this.RightMenuWidth);
				EditorPrefs.SetFloat($"{EditorPrefsKey}_RightMenuTopHeight", this.RightMenuTopPanelHeight);
				EditorPrefs.SetFloat($"{EditorPrefsKey}_LastOpenRightMenuWidth", this.LastOpenRightMenuWidth);
			}

			public void Load()
			{
				this.LeftMenuWidth = EditorPrefs.GetFloat($"{EditorPrefsKey}_LeftMenuWidth", 300);
				this.RightMenuWidth = EditorPrefs.GetFloat($"{EditorPrefsKey}_RightMenuWidth", 300);
				this.RightMenuTopPanelHeight = EditorPrefs.GetFloat($"{EditorPrefsKey}_RightMenuTopHeight");
				this.LastOpenRightMenuWidth = EditorPrefs.GetFloat($"{EditorPrefsKey}_LastOpenRightMenuWidth");
			}

			public void Dispose()
			{
				this.MetadataTree?.Dispose();
				this.MetadataTree = null;
			}
		}

		[MenuItem("Tools/Odin/Localization Editor", priority = 10_100)]
		public static void OpenFromMenu()
		{
			var wnd = GetWindow<OdinLocalizationEditorWindow>();
			wnd.MenuWidth = 300.0f;
		}

		public WindowState State;

		private object lastSelection;

		protected override void Initialize()
		{
			this.State = new WindowState();
			this.State.Load();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			this.DisposeActiveCollection();
			this.State.Dispose();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			this.State.Save();

			this.DisposeActiveCollection();
			this.State.Dispose();
		}

		protected override void OnImGUI()
		{
			if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
			{
				Rect popupPosition = this.position.SetPosition(Vector2.zero).AlignCenter(360, 160);

				if (EditorGUIUtility.isProSkin)
				{
					//OdinLocalizationGUI.DrawRoundGlowRect(popupPosition.Expand(54), FancyColor.CreateHex(0x323232));
					OdinLocalizationGUI.DrawRoundBlur6(popupPosition, new Color(0, 0, 0, 0.025f));
					SirenixEditorGUI.DrawRoundRect(popupPosition, FancyColor.CreateHex(0x383838), 5.0f);
				}
				else
				{
					OdinLocalizationGUI.DrawRoundBlur6(popupPosition, new Color(0, 0, 0, 0.02f));
					SirenixEditorGUI.DrawRoundRect(popupPosition, new FancyColor(0.84f), 5.0f); //, new Color(0, 0, 0, 0.2f), 1);

					SirenixEditorGUI.DrawRoundRect(popupPosition.AlignBottom(32 + 12 + 8 + 6), new Color(1, 1, 1, 0.2f), 0.0f, 0.0f, 5.0f, 5.0f);
				}

				popupPosition = popupPosition.Padding(12);

				Rect buttonsArea = popupPosition.TakeFromBottom(32);

				popupPosition.height -= 16;

				GUIStyle labelStyle = EditorGUIUtility.isProSkin ? SirenixGUIStyles.WhiteLabelCentered : SirenixGUIStyles.BlackLabelCentered;

				if (EditorGUIUtility.isProSkin)
				{
					GUI.Label(popupPosition, "No Localization Settings found in project.", labelStyle);
				}
				else
				{
					GUIHelper.PushColor(new Color(1, 1, 1, 0.75f));
					GUI.Label(popupPosition, "No Localization Settings found in project.", SirenixGUIStyles.BlackLabelCentered);
				}

				if (OdinLocalizationGUI.OverlaidButton(buttonsArea.AlignCenter(120), "Create", labelStyle: labelStyle, invert: true))
				{
					if (OdinLocalizationEditorSettings.CreateDefaultLocalizationSettingsAsset())
					{
						this.ShowToast(ToastPosition.BottomLeft,
											SdfIconType.GearWide,
											"Default Localization Settings created.",
											new Color(0.13f, 0.26f, 0.39f),
											8.0f);
					}
				}

				if (!EditorGUIUtility.isProSkin)
				{
					GUIHelper.PopColor();
				}

				this.Repaint();
				return;
			}

			base.OnImGUI();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree
			{
				Config =
				{
					AutoHandleKeyboardNavigation = false,
					DrawSearchToolbar = true
				},
				DefaultMenuStyle =
				{
					Height = 28,
					AlignTriangleLeft = true,
					TrianglePadding = 0.0f
				}
			};

			this.MenuBackgroundColor = OdinLocalizationGUI.MenuBackground;

			if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
			{
				return tree;
			}
			
			var createMenu = new OdinLocalizationCreateTableMenu();

			tree.Add("Create Table", createMenu, SdfIconType.Plus);
			tree.Add("User Config", OdinLocalizationConfig.Instance, SdfIconType.GearFill);

#if true
			tree.Selection.SelectionChanged += type =>
			{
				switch (type)
				{
					case SelectionChangedType.ItemAdded:
						if (this.lastSelection != null)
						{
							switch (this.lastSelection)
							{
								case OdinAssetTableCollectionEditor assetCollection:
								{
									assetCollection.DetachEvents();
									break;
								}

								case OdinStringTableCollectionEditor stringCollection:
								{
									stringCollection.DetachEvents();
									break;
								}
							}

							this.State.MetadataTree?.Dispose();
							this.State.MetadataTree = null;
						}

						switch (tree.Selection.SelectedValue)
						{
							case OdinAssetTableCollectionEditor assetCollection:
							{
								assetCollection.OnSelectInWindow();
								
								if (assetCollection.SelectionType == OdinTableSelectionType.TableEntry && this.State.CurrentTopTab == RightMenuTopTabs.Metadata)
								{
									assetCollection.UpdateMetadataViewForEntry(assetCollection.CurrentSelectedEntry);
								}

								break;
							}

							case OdinStringTableCollectionEditor stringCollection:
							{
								stringCollection.OnSelectInWindow();

								if (stringCollection.SelectionType == OdinTableSelectionType.TableEntry && this.State.CurrentTopTab == RightMenuTopTabs.Metadata)
								{
									stringCollection.UpdateMetadataViewForEntry(stringCollection.CurrentSelectedEntry);
								}

								break;
							}

							case OdinLocalizationCreateTableMenu createTableMenu:
								createTableMenu.Locales.Clear();

								foreach (Locale locale in LocalizationEditorSettings.GetLocales())
								{
									createTableMenu.Locales.Add(new OdinLocalizationCreateTableMenu.LocaleItem {Locale = locale, Enabled = true});
								}

								break;
						}

						this.lastSelection = this.MenuTree.Selection.SelectedValue;

						break;
				}
			};
#endif

			string[] collectionGUIDs = AssetDatabase.FindAssets($"t:{nameof(LocalizationTableCollection)}");

			for (var i = 0; i < collectionGUIDs.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(collectionGUIDs[i]);
				
				var collection = AssetDatabase.LoadAssetAtPath<LocalizationTableCollection>(assetPath);

				AssetTableCollection assetTableCollection = LocalizationEditorSettings.GetAssetTableCollection(collection.TableCollectionNameReference);

				if (assetTableCollection != null)
				{
					var guiCollection = new OdinAssetTableCollectionEditor(assetTableCollection, this, this.State);

					assetPath = assetPath.Replace(".asset", string.Empty);

					if (assetPath.StartsWith("Assets/"))
					{
						assetPath = assetPath.Remove(0, "Assets/".Length);
					}

					tree.Add(assetPath, guiCollection, SdfIconType.Table);

					continue;
				}

				StringTableCollection stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(collection.TableCollectionNameReference);

				if (stringTableCollection != null)
				{
					var guiCollection = new OdinStringTableCollectionEditor(stringTableCollection, this, this.State);

					assetPath = assetPath.Replace(".asset", string.Empty);

					if (assetPath.StartsWith("Assets/"))
					{
						assetPath = assetPath.Remove(0, "Assets/".Length);
					}

					tree.Add(assetPath, guiCollection, SdfIconType.LayoutTextWindow);

					continue;
				}
			}

			foreach (OdinMenuItem treeMenuItem in tree.EnumerateTree())
			{
				if (treeMenuItem.Value != null)
				{
					if (treeMenuItem.Value is OdinAssetTableCollectionEditor assetEditor)
					{
						treeMenuItem.Name = assetEditor.Collection.SharedData.TableCollectionName;

						assetEditor.MenuItem = treeMenuItem;
						
						treeMenuItem.OnDrawItem += item =>
						{
							if (Event.current.OnMouseDown(item.Rect, 0, false))
							{
								if (Event.current.clickCount > 1)
								{
									EditorGUIUtility.PingObject(assetEditor.Collection);
								}
							}
						};

						continue;
					}
					
					if (treeMenuItem.Value is OdinStringTableCollectionEditor stringEditor)
					{
						treeMenuItem.Name = stringEditor.Collection.SharedData.TableCollectionName;

						stringEditor.MenuItem = treeMenuItem;
						
						treeMenuItem.OnDrawItem += item =>
						{
							if (Event.current.OnMouseDown(item.Rect, 0, false))
							{
								if (Event.current.clickCount > 1)
								{
									EditorGUIUtility.PingObject(stringEditor.Collection);
								}
							}
						};

						continue;
					}
					
					continue;
				}

				treeMenuItem.Value = createMenu;

				treeMenuItem.SdfIcon = SdfIconType.FolderFill;

				treeMenuItem.OnDrawItem += item =>
				{
					Rect addTableRect = item.Rect.AlignRight(20).SubX(14);

					bool isMouseOver = Event.current.IsMouseOver(addTableRect);

					if (EditorGUIUtility.isProSkin)
					{
						SdfIcons.DrawIcon(addTableRect.AlignCenter(16, 16),
												SdfIconType.Plus,
												isMouseOver ? new Color(1, 1, 1, 0.8f) : new Color(1, 1, 1, 0.4f));
					}
					else
					{
						SdfIcons.DrawIcon(addTableRect.AlignCenter(16, 16),
												SdfIconType.Plus,
												isMouseOver ? new Color(0, 0, 0, 0.8f) : new Color(0, 0, 0, 0.4f));
					}

					if (Event.current.OnMouseDown(item.Rect, 0, false))
					{
						createMenu.Folder = treeMenuItem.GetFullPath();
					}
				};
			}
			
			return tree;
		}

		public void Dispose()
		{
			this.DisposeActiveCollection();
			this.State.Dispose();
		}

		private void DisposeActiveCollection()
		{
			if (this.MenuTree == null)
			{
				return;
			}
			
			switch (this.MenuTree.Selection.SelectedValue)
			{
				case OdinAssetTableCollectionEditor assetCollection:
					assetCollection.DetachEvents();
					break;

				case OdinStringTableCollectionEditor stringCollection:
					stringCollection.DetachEvents();
					break;
			}
		}
	}
}