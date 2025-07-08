//-----------------------------------------------------------------------
// <copyright file="OdinAssetTableCollectionEditor.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define USING_WIDTH_NON_PERCENT

using System;
using System.Collections.ObjectModel;
using System.IO;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinAssetTableCollectionEditor : OdinTableCollectionEditor<AssetTableCollection, AssetTable, AssetTableEntry>
	{
		public Type UserSpecifiedCollectionType;

		private AssetTableEntry pickedEntry;
		private string pickedEntryOriginalGuid;

		private Action<TableEntryReference, Type, string> setEntryAssetTypeAction;
		private Action<TableEntryReference, string> removeEntryAssetTypeAction;

		public OdinAssetTableCollectionEditor(AssetTableCollection collection, OdinMenuEditorWindow relatedWindow,
														  OdinLocalizationEditorWindow.WindowState windowState) :
			base(collection, relatedWindow, windowState) { }

		public void SetEntryAssetType(AssetTableEntry entry, Type assetType)
		{
			if (assetType == null || assetType == typeof(UnityEngine.Object))
			{
				this.removeEntryAssetTypeAction(entry.KeyId, entry.Table.LocaleIdentifier.Code);

				return;
			}

			this.setEntryAssetTypeAction(entry.KeyId, assetType, entry.Table.LocaleIdentifier.Code);
		}

		protected override void OnInitialize()
		{
			this.setEntryAssetTypeAction = OdinLocalizationReflectionValues.Create_AssetTableCollection_SetEntryAssetType_PrivateMethod_Delegate(this.Collection);

			this.removeEntryAssetTypeAction =
				OdinLocalizationReflectionValues.Create_AssetTableCollection_RemoveEntryAssetType_PrivateMethod_Delegate(this.Collection);

			for (var i = 0; i < this.SharedEntries.Length; i++)
			{
				SharedTableData.SharedTableEntry sharedEntry = this.SharedEntries[i];

				this.MeasureEntry(sharedEntry);
			}

			//this.SharedEntries.OnSharedEntryAdded += (i, sharedEntry) => { this.MeasureEntry(sharedEntry); };

			//this.SharedEntries.OnSharedEntryRemoved += (i, sharedEntry) => { this.SharedEntryHeights.Remove(sharedEntry.Id); };

			this.OnTableEntryModified = sharedEntry =>
			{
				if (!this.Collection.SharedData.Contains(sharedEntry.Id))
				{
					return;
				}

				int index = this.SharedEntries.GetIndex(sharedEntry);

				this.MeasureEntry(sharedEntry);

				this.EntryScrollView.ReallocateRect(index, this.SharedEntryHeights[sharedEntry.Id], sharedEntry);
			};

			this.OnAssetTableEntryAdded = (collection, table, entry) =>
			{
				if (this.Collection != collection)
				{
					return;
				}

				this.MeasureEntry(entry.SharedEntry);
			};

			this.OnAssetTableEntryRemoved = (collection, table, entry, guid) =>
			{
				if (this.Collection != collection)
				{
					return;
				}

				if (entry == null)
				{
					return;
				}

				SharedTableData.SharedTableEntry sharedEntry = table.SharedData.GetEntry(entry.KeyId);

				if (sharedEntry == null)
				{
					return;
				}

				this.MeasureEntry(sharedEntry);
			};
		}

		protected override void AllocateItems()
		{
			for (var i = 0; i < this.SharedEntries.Length; i++)
			{
				SharedTableData.SharedTableEntry sharedEntry = this.SharedEntries[i];

				if (!this.SharedEntries.IsVisible(sharedEntry))
				{
					continue;
				}

				if (!this.SharedEntryHeights.ContainsKey(sharedEntry.Id))
				{
					this.MeasureEntry(sharedEntry);
				}

				this.EntryScrollView.AllocateRect(this.SharedEntryHeights[sharedEntry.Id], sharedEntry);

				//this.ControlIds[sharedEntry] = GUIUtility.GetControlID(FocusType.Keyboard);
			}
		}

		protected override void DrawItems(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			this.HandleObjectPickerUpdates();

			this.MeasureVisibleEntries(ref visibleItems);

			float scrollSpeed = OdinLocalizationConfig.Instance.scrollSpeed;
			
			this.EntryScrollView.BeginScrollView(offset: new Vector2(this.PinnedWidth, OdinLocalizationConstants.COLUMN_HEIGHT),
															 addViewSize: new Vector2(-this.PinnedWidth, 0),
															 scrollSpeed: scrollSpeed);
			{
				this.DrawEntries(ref visibleItems, false);
			}
			this.EntryScrollView.EndScrollView();

			this.EntryScrollView.BeginClip(offset: new Vector2(0, OdinLocalizationConstants.COLUMN_HEIGHT), ignoreScrollX: true);
			{
				this.DrawEntries(ref visibleItems, true);
			}
			this.EntryScrollView.EndClip();
		}

		protected override void MeasureAllEntries()
		{
			for (var i = 0; i < this.SharedEntries.Length; i++)
			{
				this.MeasureEntry(this.SharedEntries[i]);
			}

			this.HasGUIChanged = true;
		}

		protected override void MeasureVisibleEntries(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			int dataOffset = visibleItems.Offset;
			
			for (var i = 0; i < visibleItems.Length; i++)
			{
				if (!visibleItems.HasAssociatedData(i))
				{
					continue;
				}

				var sharedEntry = visibleItems.GetAssociatedData<SharedTableData.SharedTableEntry>(i);

				this.MeasureEntry(sharedEntry);

				this.EntryScrollView.ReallocateRect(dataOffset + i, this.SharedEntryHeights[sharedEntry.Id], sharedEntry);
			}
		}

		private void MeasureEntry(SharedTableData.SharedTableEntry sharedEntry)
		{
			var keyColumnWidth = 0.0f;

			for (var i = 0; i < this.GUITables.Count; i++)
			{
				OdinGUITable<AssetTable> table = this.GUITables[i];

				if (table.Type != OdinGUITable<AssetTable>.GUITableType.Key)
				{
					continue;
				}

#if USING_WIDTH_NON_PERCENT
				keyColumnWidth = table.Width;
#else
				keyColumnWidth = this.GUITables[i].Width;
#endif
				break;
			}

			float keyHeight = MeasureText(sharedEntry.Key, keyColumnWidth);

			Type assetSharedType = this.Collection.GetEntryAssetType(sharedEntry.Id);
			float assetHeight;

			if (assetSharedType == null || assetSharedType == typeof(UnityEngine.Object))
			{
				assetHeight = OdinLocalizationConstants.EMPTY_ASSET_ROW_HEIGHT;
			}
			else
			{
				assetHeight = EditorGUIUtility.HasObjectThumbnail(assetSharedType)
									  ? OdinLocalizationConstants.AssetPreviewRowHeight
									  : OdinLocalizationConstants.ASSET_ROW_HEIGHT;
			}

			this.SharedEntryHeights[sharedEntry.Id] = Mathf.Max(keyHeight, assetHeight);
		}

		private void DrawEntries(ref OdinGUIScrollView.VisibleItems visibleItems, bool pinned)
		{
			bool isDraggingControls = this.IsDraggingAnything();

			for (var i = 0; i < visibleItems.Length; i++)
			{
				if (!visibleItems.HasAssociatedData(i))
				{
					continue;
				}
				
				Rect position = visibleItems.GetRect(i);

				int hint = visibleItems.Offset + i + this.ControlIdHint;

				var sharedEntry = visibleItems.GetAssociatedData<SharedTableData.SharedTableEntry>(i);

				bool isEven = (visibleItems.Offset + i) % 2 == 0;

				for (var j = 0; j < this.GUITables.Count; j++)
				{
					OdinGUITable<AssetTable> table = this.GUITables[j];

					if (!table.IsVisible)
					{
						continue;
					}

					if (table.IsPinned != pinned)
					{
						continue;
					}

					if (!this.GUITables.TablesWithinVisibleBounds.Contains(table))
					{
						position.TakeFromLeft(table.Width).Padding(OdinLocalizationConstants.ENTRY_PADDING);
						continue;
					}

#if USING_WIDTH_NON_PERCENT
					Rect entryRect = position.TakeFromLeft(table.Width).Padding(OdinLocalizationConstants.ENTRY_PADDING);
#else
					Rect entryRect = position.TakeFromLeft(table.Width).Padding(OdinLocalizationConstants.ENTRY_PADDING);
#endif

					bool isCellPressed, isSelected;

					switch (table.Type)
					{
						case OdinGUITable<AssetTable>.GUITableType.Key:
							isSelected = this.IsSharedEntrySelected(sharedEntry);

							if (isSelected)
							{
								this.SelectionAnimFloat.Move(1 / 0.18f, Easing.InSine);

								FancyColor start = FancyColor.Gray;

								var end = OdinLocalizationGUI.Selected;

								FancyColor.PushBlend(start.Lerp(end, this.SelectionAnimFloat), FancyColor.BlendMode.Overlay);
							}

							isCellPressed = DrawCell(entryRect, isEven);

							this.DrawKey(entryRect, sharedEntry, GUIUtility.GetControlID(hint, FocusType.Keyboard));

							if (isSelected)
							{
								FancyColor.PopBlend();
							}

							if (isCellPressed)
							{
								this.SelectSharedEntry(sharedEntry);
							}

							break;

						case OdinGUITable<AssetTable>.GUITableType.Default:
							AssetTableEntry entry = table.Asset.GetEntry(sharedEntry.Id);

							isSelected = this.IsEntrySelected(entry);

							if (isSelected)
							{
								this.SelectionAnimFloat.Move(1 / 0.18f, Easing.InSine);

								FancyColor start = FancyColor.Gray;

								FancyColor end = OdinLocalizationGUI.Selected;

								FancyColor.PushBlend(start.Lerp(end, this.SelectionAnimFloat), FancyColor.BlendMode.Overlay);
							}

							isCellPressed = DrawCell(entryRect, isEven);

							this.DrawEntry(entryRect, sharedEntry, entry, table, isDraggingControls);

							if (isSelected)
							{
								FancyColor.PopBlend();
							}

							if (isCellPressed)
							{
								if (entry is null)
								{
									entry = table.Asset.AddEntry(sharedEntry.Id, string.Empty);
								}

								GUIUtility.keyboardControl = 0;

								this.SelectEntry(entry);
							}

							if (isSelected && Event.current.type == EventType.KeyDown && EditorWindow.focusedWindow == this.RelatedWindow)
							{
								switch (Event.current.keyCode)
								{
									case KeyCode.Delete:
									case KeyCode.Backspace:
										if (entry == null)
										{
											break;
										}

										this.AssignObjectToSharedEntry(sharedEntry, table.Asset, null);

										Event.current.Use();
										break;

									case KeyCode.Space:
									case KeyCode.Return:
										if (entry == null)
										{
											entry = table.Asset.AddEntry(sharedEntry.Id, string.Empty);
										}

										this.ShowObjectPickerForEntry(entry);

										Event.current.Use();
										break;
								}
							}

							break;
					}
				}
			}
		}

		private void DrawEntry(Rect rect, SharedTableData.SharedTableEntry sharedEntry, AssetTableEntry entry, OdinGUITable<AssetTable> table,
									  bool isDraggingControls)
		{
			UnityEngine.Object asset = null;

			Type entryAssetType = this.Collection.GetEntryAssetType(sharedEntry.Id);

			if (entry != null && !entry.IsEmpty)
			{
				asset = OdinLocalizationAssetCache.Get(entry.Guid, entryAssetType);
			}

			Rect fullRect = rect;

			Rect rightMenuRect = rect.TakeFromRight(OdinLocalizationConstants.ROW_MENU_WIDTH);
			rect.TakeFromLeft(OdinLocalizationConstants.ROW_MENU_WIDTH);

			rightMenuRect = rightMenuRect.Padding(0, 6);

			Rect openInspectorRect = rightMenuRect.AlignTop(14);
			Rect openExplorerRect = rightMenuRect.AlignBottom(14);

			bool isMouseOverInspector = Event.current.IsMouseOver(openInspectorRect);
			bool isMouseOverExplorer = Event.current.IsMouseOver(openExplorerRect);

			// TODO: some caveats to just adding this
#if true
			var dragAndDropId = 0;

			if (!isDraggingControls && !(asset is DefaultAsset))
			{
				EditorGUI.BeginChangeCheck();

				var dropValue = DragAndDropUtilities.DragAndDropZone(fullRect, asset, entryAssetType, true, false, false) as UnityEngine.Object;

				dragAndDropId = DragAndDropUtilities.PrevDragAndDropId;

				if (EditorGUI.EndChangeCheck() && dropValue != asset)
				{
					if (!(dropValue is DefaultAsset))
					{
						entry = this.AssignObjectToSharedEntry(sharedEntry, table.Asset, dropValue);

						asset = dropValue;

						entryAssetType = this.Collection.GetEntryAssetType(sharedEntry.Id);
					}
					else
					{
						this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
															  SdfIconType.ExclamationTriangleFill,
															  "Default Assets (such as Folders) cannot be used for Asset Entries.",
															  SirenixGUIStyles.RedErrorColor,
															  10.0f);
					}
				}
			}
#endif

			bool hasThumbnail = EditorGUIUtility.HasObjectThumbnail(entryAssetType);

			if (asset != null)
			{
				if (Event.current.OnMouseDown(rect, 0, false) && Event.current.clickCount >= 2)
				{
					EditorGUIUtility.PingObject(asset);
				}
			}


			if (entry == null || asset == null)
			{
				GUI.Label(rect, $"None ({entryAssetType.Name})", SirenixGUIStyles.LabelCentered);
			}
			else if (!hasThumbnail)
			{
				GUI.Label(rect.AlignMiddle(20),
							 GUIHelper.TempContent($"{asset.name} ({entryAssetType.Name})", AssetPreview.GetMiniThumbnail(asset)),
							 SirenixGUIStyles.LabelCentered);

				if (entryAssetType != typeof(UnityEngine.Object))
				{
					var c = EditorGUIUtility.isProSkin
								  ? new Color(1, 1, 1, isMouseOverInspector ? 0.8f : 0.3f)
								  : new Color(0, 0, 0, isMouseOverInspector ? 0.8f : 0.3f);

					var c1 = EditorGUIUtility.isProSkin
									? new Color(1, 1, 1, isMouseOverExplorer ? 0.8f : 0.3f)
									: new Color(0, 0, 0, isMouseOverExplorer ? 0.8f : 0.3f);

					SdfIcons.DrawIcon(openInspectorRect, SdfIconType.PencilFill, c);
					SdfIcons.DrawIcon(openExplorerRect, SdfIconType.FolderFill, c1);

					GUI.Label(openInspectorRect, GUIHelper.TempContent(string.Empty, "Inspect Object"));
					GUI.Label(openExplorerRect, GUIHelper.TempContent(string.Empty, "Show In Explorer"));

					if (Event.current.OnMouseDown(openInspectorRect, 0))
					{
						GUIHelper.OpenInspectorWindow(asset);
					}

					if (Event.current.OnMouseDown(openExplorerRect, 0))
					{
						string assetAbsPath = Path.GetFullPath(AssetDatabase.GetAssetPath(asset));

						if (Directory.Exists(assetAbsPath) || File.Exists(assetAbsPath))
						{
							EditorUtility.RevealInFinder(assetAbsPath);

#if SIRENIX_INTERNAL
							this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
																  SdfIconType.Tools,
																  $"(DEVELOPER) Opened asset path: {asset.name} ({assetAbsPath})",
																  Color.black,
																  15.0f);
#endif
						}
						else
						{
							this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
																  SdfIconType.ExclamationTriangleFill,
																  $"Failed to find asset: {asset.name}",
																  SirenixGUIStyles.RedErrorColor,
																  5.0f);
						}

						FancyColor.PopBlend();
						GUIHelper.ExitGUI(false);
					}
				}
			}
			else
			{
				Rect previewRect = rect.Padding(12);

				Texture preview;

				if (asset is Sprite || asset is Cubemap)
				{
					preview = AssetPreview.GetAssetPreview(asset);
				}
				else
				{
					preview = AssetPreview.GetMiniThumbnail(asset);
				}

				if (preview == null)
				{
					preview = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(asset));
				}

				float previewHeight = preview.height;
				float aspect = (float) preview.width / preview.height;

				if (previewHeight < previewRect.height)
				{
					previewRect = previewRect.AlignMiddle(previewHeight < 32 ? 32 : previewHeight);
				}
				else
				{
					previewHeight = previewRect.height;
				}

				float previewWidth = previewHeight * aspect;

				if (previewWidth > previewRect.width)
				{
					previewWidth = previewRect.width;
					previewHeight = previewWidth / aspect;
				}

				previewRect = previewRect.AlignCenter(previewWidth, previewHeight);

				previewRect.x = Mathf.Round(previewRect.x);
				previewRect.y = Mathf.Round(previewRect.y);
				previewRect.width = Mathf.Round(previewRect.width);
				previewRect.height = Mathf.Round(previewRect.height);

				if (Event.current.type == EventType.Repaint)
				{
					if (this.IsEntrySelected(entry))
					{
						GUIHelper.PushColor(OdinLocalizationGUI.Selected * new FancyColor(1.05f));
					}

					EditorStyles.objectFieldThumb.Draw(previewRect, GUIContent.none, false, false, false, false);

					if (this.IsEntrySelected(entry))
					{
						GUIHelper.PopColor();
					}
				}

				GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);

				GUI.DrawTexture(previewRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1.0f, new FancyColor(0.17f), 1, 0);

				var c = EditorGUIUtility.isProSkin
							  ? new Color(1, 1, 1, isMouseOverInspector ? 0.8f : 0.3f)
							  : new Color(0, 0, 0, isMouseOverInspector ? 0.8f : 0.3f);

				var c1 = EditorGUIUtility.isProSkin
								? new Color(1, 1, 1, isMouseOverExplorer ? 0.8f : 0.3f)
								: new Color(0, 0, 0, isMouseOverExplorer ? 0.8f : 0.3f);

				SdfIcons.DrawIcon(openInspectorRect, SdfIconType.PencilFill, c);
				SdfIcons.DrawIcon(openExplorerRect, SdfIconType.FolderFill, c1);

				GUI.Label(openInspectorRect, GUIHelper.TempContent(string.Empty, "Inspect Object"));
				GUI.Label(openExplorerRect, GUIHelper.TempContent(string.Empty, "Show In Explorer"));

				if (Event.current.OnMouseDown(openInspectorRect, 0))
				{
					GUIHelper.OpenInspectorWindow(asset);
				}

				if (Event.current.OnMouseDown(openExplorerRect, 0))
				{
					string assetAbsPath = Path.GetFullPath(AssetDatabase.GetAssetPath(asset));

					if (Directory.Exists(assetAbsPath) || File.Exists(assetAbsPath))
					{
						EditorUtility.RevealInFinder(assetAbsPath);

#if SIRENIX_INTERNAL
						this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
															  SdfIconType.Tools,
															  $"(DEVELOPER) Opened asset path: {asset.name} ({assetAbsPath})",
															  Color.black,
															  15.0f);
#endif
					}
					else
					{
						this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
															  SdfIconType.ExclamationOctagonFill,
															  $"Failed to find asset: {asset.name} ({assetAbsPath})",
															  SirenixGUIStyles.RedErrorColor,
															  5.0f);
					}

					FancyColor.PopBlend();
					GUIHelper.ExitGUI(false);
				}
			}

			Rect pickerRect = rightMenuRect.AlignMiddle(14);

			if (OdinLocalizationGUI.ObjectPickerButton(pickerRect))
			{
				if (entry == null)
				{
					entry = table.Asset.AddEntry(sharedEntry.Id, string.Empty);
				}
				
				this.ShowObjectPickerForEntry(entry);
			}

			GUI.Label(pickerRect, GUIHelper.TempContent(string.Empty, "Select Object"));

#if true
			if (!isDraggingControls && dragAndDropId != 0 && DragAndDropUtilities.IsDragging && DragAndDropUtilities.HoveringAcceptedDropZone == dragAndDropId)
			{
				GUI.DrawTexture(fullRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, new Color(0, 0.5f, 0.8f, 0.25f), 0, 2.5f);
			}
#endif
		}

		public override void RemoveKey(SharedTableData.SharedTableEntry sharedEntry)
		{
			bool hasAssets = this.DoesSharedEntryHaveAssets(sharedEntry);

			if (!hasAssets)
			{
				base.RemoveKey(sharedEntry);
				return;
			}

			this.SharedEntryHeights.Remove(sharedEntry.Id);

			for (var i = 0; i < this.GUITables.Count; i++)
			{
				OdinGUITable<AssetTable> table = this.GUITables[i];

				if (table.Type == OdinGUITable<AssetTable>.GUITableType.Key)
				{
					continue;
				}

				AssetTableEntry entry = table.Asset.GetEntry(sharedEntry.Id);

				if (entry == null || entry.IsEmpty)
				{
					continue;
				}

				this.Collection.RemoveAssetFromTable(table.Asset, sharedEntry.Id);
			}

			this.Collection.RemoveEntry(sharedEntry.Id);

			this.GUITables.SetDirty(this.Collection.SharedData);
		}

		private bool DoesSharedEntryHaveAssets(SharedTableData.SharedTableEntry sharedEntry)
		{
			Type sharedType = this.Collection.GetEntryAssetType(sharedEntry.Id);

			for (var i = 0; i < this.GUITables.Count; i++)
			{
				OdinGUITable<AssetTable> table = this.GUITables[i];

				if (table.Type == OdinGUITable<AssetTable>.GUITableType.Key)
				{
					continue;
				}

				if (OdinLocalizationAssetCache.Get(sharedEntry, table.Asset, sharedType) != null)
				{
					return true;
				}
			}

			return false;
		}

		private void ShowObjectPickerForEntry(AssetTableEntry entry)
		{
			this.pickedEntry = entry;

			this.pickedEntryOriginalGuid = entry.Guid;

			UnityEngine.Object obj = null;

			Type objType = this.UserSpecifiedCollectionType ?? this.Collection.GetEntryAssetType(entry.KeyId);

			UnityEngine.Object asset = OdinLocalizationAssetCache.Get(entry.Guid, objType);

			if (asset != null)
			{
				obj = asset;
			}

			OdinObjectSelector.Show(this.RelatedWindow, OdinObjectSelectorIds.LOCALIZATION_EDITOR, obj, objType, false);
			//SirenixObjectPickerUtilities.ShowObjectPicker(obj, objType, false, string.Empty, this.RelatedWindowId);
		}

		private void HandleObjectPickerUpdates()
		{
			if (this.pickedEntry == null)
			{
				return;
			}

#if true
			object selectedObj = OdinObjectSelector.SelectorObject;

			try
			{
				if (OdinObjectSelector.IsReadyToClaim(this.RelatedWindow, OdinObjectSelectorIds.LOCALIZATION_EDITOR))
				{
					object claimedObject = OdinObjectSelector.Claim();

					if (!(claimedObject is DefaultAsset))
					{
						this.AssignObjectToSelectorEntry(claimedObject as UnityEngine.Object);
					}
					else
					{
						this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
															  SdfIconType.ExclamationTriangleFill,
															  "Default Assets (such as Folders) cannot be used for Asset Entries.",
															  SirenixGUIStyles.RedErrorColor,
															  10.0f);
					}

					Event.current.Use();
				}
				else
				{
					if (!OdinObjectSelector.IsOpen)
					{
						this.pickedEntry.Guid = this.pickedEntryOriginalGuid;
						this.pickedEntry = null;
						this.pickedEntryOriginalGuid = string.Empty;
					}

					EditorGUI.BeginChangeCheck();
					var changedValue = OdinObjectSelector.GetChangedObject<UnityEngine.Object>(null, this.RelatedWindow, OdinObjectSelectorIds.LOCALIZATION_EDITOR);
					if (EditorGUI.EndChangeCheck())
					{
						if (changedValue == null)
						{
							this.pickedEntry.Guid = string.Empty;
						}
						else
						{
							this.pickedEntry.Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(changedValue));
						}
					}
				}
			}
			catch (NullReferenceException exception)
			{
				Debug.LogError($"{nameof(NullReferenceException)}: The asset '{selectedObj ?? "<ASSET NOT FOUND>"}' is NULL.\n{exception}");
			}
#else
			if (Event.current.type != EventType.ExecuteCommand ||
				 EditorGUIUtility.GetObjectPickerControlID() != this.RelatedWindowId ||
				 this.RelatedWindowId == 0)
			{
				return;
			}

			UnityEngine.Object selectedObj = null;

			try
			{
				if (Event.current.commandName == "ObjectSelectorClosed")
				{
					Vector2 lastScrollPosition = this.EntryScrollView.Position;

					this.pickedEntry.Guid = this.pickedEntryOriginalGuid;

					selectedObj = EditorGUIUtility.GetObjectPickerObject();

					var table = (AssetTable) this.pickedEntry.Table;

					SharedTableData.SharedTableEntry sharedEntry = this.pickedEntry.SharedEntry;

					if (selectedObj == null)
					{
						this.Collection.RemoveAssetFromTable((AssetTable) this.pickedEntry.Table,
																		 this.pickedEntry.SharedEntry.Id,
																		 OdinLocalizationConfig.Instance.recordUndosForAssetTableEntries);

						this.pickedEntry = table.GetEntry(sharedEntry.Id) ?? table.AddEntry(sharedEntry.Id, string.Empty);
					}
					else
					{
						this.Collection.AddAssetToTable((AssetTable) this.pickedEntry.Table,
																  this.pickedEntry.SharedEntry.Id,
																  selectedObj,
																  OdinLocalizationConfig.Instance.recordUndosForAssetTableEntries);

						this.pickedEntry = table.GetEntry(sharedEntry.Id);
					}

					EditorUtility.SetDirty(this.Collection.SharedData);
					EditorUtility.SetDirty(table);

					this.EntryScrollView.Position = lastScrollPosition;

					this.pickedEntry = null;
					this.pickedEntryOriginalGuid = string.Empty;
					Event.current.Use();
					return;
				}

				if (Event.current.commandName != "ObjectSelectorUpdated")
				{
					return;
				}

				selectedObj = EditorGUIUtility.GetObjectPickerObject();

				this.pickedEntry.Guid = selectedObj == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(selectedObj));

				Event.current.Use();
			}
			catch (NullReferenceException exception)
			{
				// NOTE: edge cases for when the user selects assets that appear in the object picker, but in reality are null.
				Debug.LogError($"{nameof(NullReferenceException)}: The asset '{(selectedObj == null ? "<ASSET NOT FOUND>" : selectedObj.name)}' is NULL.\n{exception}");
			}
#endif
		}

		// TODO: implement when drag'n'drop areas are done and keyboard commands
#if true
		private AssetTableEntry AssignObjectToSharedEntry(SharedTableData.SharedTableEntry sharedEntry, AssetTable table, UnityEngine.Object obj)
		{
			Vector2 lastScrollPosition = this.EntryScrollView.Position;

			AssetTableEntry entry;

			if (obj == null)
			{
				this.CustomRemoveAssetFromTable(table, sharedEntry.Id, true);

				entry = table.GetEntry(sharedEntry.Id) ?? table.AddEntry(sharedEntry.Id, string.Empty);
			}
			else
			{
				this.Collection.AddAssetToTable(table, sharedEntry.Id, obj, true);

				entry = table.GetEntry(sharedEntry.Id);
			}

			this.EntryScrollView.Position = lastScrollPosition;

			return entry;
		}
#endif

		private void AssignObjectToSelectorEntry(UnityEngine.Object obj)
		{
			object selectedObj = OdinObjectSelector.SelectorObject;

			try
			{
				Vector2 lastScrollPosition = this.EntryScrollView.Position;

				this.pickedEntry.Guid = this.pickedEntryOriginalGuid;

				var table = (AssetTable) this.pickedEntry.Table;

				SharedTableData.SharedTableEntry sharedEntry = this.pickedEntry.SharedEntry;

				if (obj == null)
				{
					this.CustomRemoveAssetFromTable((AssetTable) this.pickedEntry.Table, this.pickedEntry.SharedEntry.Id, true);

					this.pickedEntry = table.GetEntry(sharedEntry.Id) ?? table.AddEntry(sharedEntry.Id, string.Empty);
				}
				else
				{
					this.Collection.AddAssetToTable((AssetTable) this.pickedEntry.Table, this.pickedEntry.SharedEntry.Id, obj, true);

					this.pickedEntry = table.GetEntry(sharedEntry.Id);
				}

				EditorUtility.SetDirty(this.Collection.SharedData);
				EditorUtility.SetDirty(table);

				this.EntryScrollView.Position = lastScrollPosition;

				this.pickedEntry = null;
				this.pickedEntryOriginalGuid = string.Empty;
			}
			catch (NullReferenceException exception)
			{
				Debug.LogError($"{nameof(NullReferenceException)}: The asset '{selectedObj ?? "<ASSET NOT FOUND>"}' is NULL.\n{exception}");
			}
		}

		public void CustomRemoveAssetFromTable(AssetTable table, TableEntryReference entryReference, bool createUndo = false)
		{
			if (!OdinLocalizationReflectionValues.HasAPIForCustomUndo || !OdinLocalizationConfig.Instance.useCustomUndoHandlingForAssetCollections)
			{
				this.Collection.RemoveAssetFromTable(table, entryReference, createUndo);

				return;
			}

			var groupIndex = 0;

			if (createUndo)
			{
				groupIndex = Undo.GetCurrentGroup();
				Undo.IncrementCurrentGroup();
				Undo.SetCurrentGroupName("Remove asset from table");
			}

			if (createUndo)
			{
				Undo.RecordObjects(new UnityEngine.Object[] {table, table.SharedData}, "Remove asset from table");
			}

			AssetTableEntry tableEntry = table.GetEntryFromReference(entryReference);

			if (tableEntry == null)
			{
				return;
			}

			string removedAssetGuid = tableEntry.Guid;

			tableEntry.Guid = string.Empty;

			AddressableAssetSettings addressableAssetSettings = OdinLocalizationEditorSettings.GetAddressableAssetSettings(false);

			if (addressableAssetSettings == null)
			{
				return;
			}

			EditorUtility.SetDirty(table);
			EditorUtility.SetDirty(table.SharedData);

			this.SetEntryAssetType(tableEntry, null);

			if (tableEntry.MetadataEntries.Count == 0)
			{
				table.RemoveEntry(tableEntry.KeyId);
			}

			ReadOnlyCollection<AssetTableCollection> assetTableCollections = LocalizationEditorSettings.GetAssetTableCollections();

			foreach (AssetTableCollection collection in assetTableCollections)
			{
				if (collection.GetTable(table.LocaleIdentifier) is AssetTable tableWithMatchingLocaleId &&
					 tableWithMatchingLocaleId.ContainsValue(removedAssetGuid))
				{
					return;
				}
			}

			AddressableAssetEntry assetEntry = addressableAssetSettings.FindAssetEntry(removedAssetGuid);

			if (assetEntry != null)
			{
				if (createUndo)
				{
					Undo.RecordObject(assetEntry.parentGroup, "Remove asset from table");
				}

				var assetLabel = (string) OdinLocalizationReflectionValues.FormatAssetLabelMethod.Invoke(null, new object[] {table.LocaleIdentifier});

				assetEntry.SetLabel(assetLabel, false);

				OdinLocalizationReflectionValues.UpdateAssetGroupMethod.Invoke(this.Collection, new object[] {addressableAssetSettings, assetEntry, createUndo});
			}

			OdinLocalizationEvents.RaiseAssetTableEntryRemoved(this.Collection, table, tableEntry, removedAssetGuid);

			if (createUndo)
			{
				Undo.CollapseUndoOperations(groupIndex);
			}
		}
	}
}