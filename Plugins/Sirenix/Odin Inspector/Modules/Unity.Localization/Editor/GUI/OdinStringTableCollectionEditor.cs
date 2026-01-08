//-----------------------------------------------------------------------
// <copyright file="OdinStringTableCollectionEditor.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define USING_WIDTH_NON_PERCENT

using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinStringTableCollectionEditor : OdinTableCollectionEditor<StringTableCollection, StringTable, StringTableEntry>
	{
		private string currentSyntaxSource;
		private string currentSyntaxHighlightedText;
		private string currentSyntaxErrorMessage;
		private Exception currentSyntaxException;
		private bool currentSyntaxHasErrors;

		public OdinStringTableCollectionEditor(StringTableCollection collection, OdinMenuEditorWindow relatedWindow,
															OdinLocalizationEditorWindow.WindowState windowState) :
			base(collection, relatedWindow, windowState) { }

		protected override void OnInitialize()
		{
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

#if false
				this.ControlIds[sharedEntry] = GUIUtility.GetControlID(FocusType.Keyboard);

				for (var j = 0; j < this.GUITables.Count; j++)
				{
					OdinGUITable<StringTable> table = this.GUITables[j];

					if (table.Type == OdinGUITable<StringTable>.GUITableType.Key)
					{
						continue;
					}

					StringTableEntry entry = table.Table.GetEntry(sharedEntry.Id);

					if (entry is null)
					{
						table.Table.AddEntry(sharedEntry.Id, string.Empty);

						entry = table.Table.GetEntry(sharedEntry.Id);
					}

					this.ControlIds[entry] = GUIUtility.GetControlID(FocusType.Keyboard);
				}
#endif
			}
		}

		protected override void DrawItems(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			this.MeasureVisibleEntries(ref visibleItems);

			float scrollSpeed = OdinLocalizationConfig.Instance.scrollSpeed;

			this.EntryScrollView.BeginScrollView(offset: new Vector2(this.PinnedWidth, OdinLocalizationConstants.COLUMN_HEIGHT),
															 addViewSize: new Vector2(-this.PinnedWidth, 0),
															 scrollSpeed: scrollSpeed);
			{
				this.DrawEntries(ref visibleItems, false);
			}
			this.EntryScrollView.EndScrollView();

			this.EntryScrollView.BeginClip(offset: new Vector2(0.0f, OdinLocalizationConstants.COLUMN_HEIGHT), ignoreScrollX: true);
			{
				this.DrawEntries(ref visibleItems, true);
			}
			this.EntryScrollView.EndClip();
		}

		private void DrawEntries(ref OdinGUIScrollView.VisibleItems visibleItems, bool pinned)
		{
			for (var i = 0; i < visibleItems.Length; i++)
			{
				if (!visibleItems.HasAssociatedData(i))
				{
					continue;
				}

				int hint = visibleItems.Offset + i + this.ControlIdHint;
				
				Rect position = visibleItems.GetRect(i);

				var sharedEntry = visibleItems.GetAssociatedData<SharedTableData.SharedTableEntry>(i);

				bool isEven = (visibleItems.Offset + i) % 2 == 0;

				for (var j = 0; j < this.GUITables.Count; j++)
				{
					OdinGUITable<StringTable> table = this.GUITables[j];

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
						GUIUtility.GetControlID(hint, FocusType.Keyboard);
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
						case OdinGUITable<StringTable>.GUITableType.Key:
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

						case OdinGUITable<StringTable>.GUITableType.Default:
							StringTableEntry entry = table.Asset.GetEntry(sharedEntry.Id);

							isSelected = this.IsEntrySelected(entry);

							if (isSelected)
							{
								this.SelectionAnimFloat.Move(1 / 0.18f, Easing.InSine);

								FancyColor start = FancyColor.Gray;

								FancyColor end = OdinLocalizationGUI.Selected;

								if (entry.IsSmart && OdinLocalizationConfig.Instance.useSyntaxHighlighter)
								{
									if (this.currentSyntaxSource != entry.Value)
									{
										this.currentSyntaxHighlightedText = OdinLocalizationSyntaxHighlighter.HighlightAsRichText(entry.Value);
										this.currentSyntaxErrorMessage = OdinLocalizationSyntaxHighlighter.GetErrorMessage(entry.Value, out bool foundError, out Exception exception);
										this.currentSyntaxHasErrors = foundError;
										this.currentSyntaxException = exception;
										this.currentSyntaxSource = entry.Value;
									}

									if (this.currentSyntaxHasErrors)
									{
										FancyColor.PushBlend(start.Lerp(new FancyColor(0.68f, 0.2f, 0.2f), this.SelectionAnimFloat), FancyColor.BlendMode.Overlay);
									}
									else
									{
										FancyColor.PushBlend(start.Lerp(end, this.SelectionAnimFloat), FancyColor.BlendMode.Overlay);
									}
								}
								else
								{
									FancyColor.PushBlend(start.Lerp(end, this.SelectionAnimFloat), FancyColor.BlendMode.Overlay);
								}
							}

							isCellPressed = DrawCell(entryRect, isEven);

							this.DrawEntry(entryRect, entry, GUIUtility.GetControlID(hint, FocusType.Keyboard), table, sharedEntry);

							if (isSelected)
							{
								if (OdinLocalizationConfig.Instance.useSyntaxHighlighter && entry.IsSmart && this.currentSyntaxHasErrors)
								{
									Rect errorRect = entryRect.AlignLeft(OdinLocalizationConstants.ROW_MENU_WIDTH).AlignMiddle(16);

									SdfIcons.DrawIcon(errorRect, SdfIconType.ExclamationOctagonFill,
															Event.current.IsMouseOver(errorRect) ? new Color(1, 1, 1, 1f) : new Color(1, 1, 1, 0.6f));

									if (Event.current.OnMouseDown(errorRect, 0))
									{
										this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
																			  SdfIconType.ExclamationOctagonFill,
																			  this.currentSyntaxErrorMessage,
																			  new Color(0.68f, 0.2f, 0.2f),
																			  20.0f);

										if (this.currentSyntaxException != null)
										{
											Debug.LogException(this.currentSyntaxException);
										}
									}
								}

								FancyColor.PopBlend();
							}

							if (isCellPressed)
							{
								if (entry is null)
								{
									entry = table.Asset.AddEntry(sharedEntry.Id, string.Empty);
								}

								this.SelectEntry(entry);
							}

							break;
					}
				}
			}
		}

		private void DrawEntry(Rect position, StringTableEntry entry, int id, OdinGUITable<StringTable> table, SharedTableData.SharedTableEntry sharedEntry)
		{
			bool changed;
			string value;

			Rect smartToggleRect = position.TakeFromRight(OdinLocalizationConstants.ROW_MENU_WIDTH);
			position.TakeFromLeft(OdinLocalizationConstants.ROW_MENU_WIDTH);

			if (entry?.Value is null)
			{
				value = OdinLocalizationGUI.TextField(position, string.Empty, out changed, id);
			}
			else if (OdinLocalizationConfig.Instance.useSyntaxHighlighter && entry.IsSmart && entry == this.CurrentSelectedEntry)
			{
				value = OdinLocalizationGUI.TextFieldSyntaxHighlighted(position, entry.Value, this.currentSyntaxHighlightedText, out changed, id);

				if (changed)
				{
					this.currentSyntaxHighlightedText = OdinLocalizationSyntaxHighlighter.HighlightAsRichText(value);
					this.currentSyntaxErrorMessage = OdinLocalizationSyntaxHighlighter.GetErrorMessage(value, out bool foundError, out Exception exception);
					this.currentSyntaxHasErrors = foundError;
					this.currentSyntaxException = exception;
					this.currentSyntaxSource = value;
				}
			}
			else
			{
				value = OdinLocalizationGUI.TextField(position, entry.Value, out changed, id);
			}

			if (changed)
			{
				if (entry == null)
				{
					entry = table.Asset.AddEntry(sharedEntry.Id, value);
				}

				Undo.RecordObject(entry.Table, "Modified String Table Entry Text");
				entry.Value = value;
				OdinLocalizationEvents.RaiseTableEntryModified(entry.SharedEntry);
				EditorUtility.SetDirty(entry.Table);
			}

			smartToggleRect = smartToggleRect.AlignMiddle(16);

			if (entry == null)
			{
				SdfIcons.DrawIcon(smartToggleRect, SdfIconType.Lightbulb, new Color(1, 1, 1, Event.current.IsMouseOver(smartToggleRect) ? 0.8f : 0.3f));

				if (Event.current.OnMouseDown(smartToggleRect, 0))
				{
					Undo.RecordObject(table.Asset, "Added String Table Entry By Smart Toggle");
					entry = table.Asset.AddEntry(sharedEntry.Id, string.Empty);

					entry.IsSmart = !entry.IsSmart;
					EditorUtility.SetDirty(table.Asset);
				}
			}
			else
			{
				SdfIcons.DrawIcon(smartToggleRect,
										entry.IsSmart ? SdfIconType.LightbulbFill : SdfIconType.Lightbulb,
										new Color(1, 1, 1, Event.current.IsMouseOver(smartToggleRect) ? 0.8f : 0.3f));

				if (Event.current.OnMouseDown(smartToggleRect, 0))
				{
					Undo.RecordObject(entry.Table, "Toggled Smart Flag On String Entry");
					entry.IsSmart = !entry.IsSmart;
					EditorUtility.SetDirty(entry.Table);
				}
			}

			GUI.Label(smartToggleRect, GUIHelper.TempContent(string.Empty, "Toggle Smart String"));
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
			float height = OdinLocalizationConstants.ROW_HEIGHT;

			for (var i = 0; i < this.GUITables.Count; i++)
			{
				OdinGUITable<StringTable> currentTable = this.GUITables[i];

				switch (currentTable.Type)
				{
					case OdinGUITable<StringTable>.GUITableType.Default:
						StringTableEntry strEntry = currentTable.Asset.GetEntry(sharedEntry.Id);

						if (strEntry is null)
						{
							continue;
						}

#if USING_WIDTH_NON_PERCENT
						float strEntryHeight = MeasureText(strEntry.Value, currentTable.Width);
#else
						float strEntryHeight = MeasureText(strEntry.Value, currentTable.Width);
#endif

						if (strEntryHeight > height)
						{
							height = strEntryHeight;
						}

						break;

					case OdinGUITable<StringTable>.GUITableType.Key:
#if USING_WIDTH_NON_PERCENT
						float keyHeight = MeasureText(sharedEntry.Key, currentTable.Width);
#else
						float keyHeight = MeasureText(sharedEntry.Key, currentTable.Width);
#endif

						if (keyHeight > height)
						{
							height = keyHeight;
						}

						break;
				}
			}

			this.SharedEntryHeights[sharedEntry.Id] = height;
		}

	}
}