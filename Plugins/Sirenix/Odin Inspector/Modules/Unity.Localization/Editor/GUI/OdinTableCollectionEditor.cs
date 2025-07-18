//-----------------------------------------------------------------------
// <copyright file="OdinTableCollectionEditor.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define USING_WIDTH_NON_PERCENT
//#undef USING_WIDTH_NON_PERCENT

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public enum OdinTableSelectionType
	{
		None,
		SharedEntry,
		SharedTable,
		Table,
		TableEntry
	}

	public abstract class OdinTableCollectionEditor<TCollection, TTable, TEntry> where TCollection : LocalizationTableCollection
																										  where TTable : LocalizationTable
																										  where TEntry : TableEntry
	{
		public const int SELECTOR_ID = Int32.MinValue + 14085;
		
		internal struct DragInfo
		{
			public static DragInfo None => new DragInfo {Index = -1};

			public bool IsNone => this.Index == -1;

			public int Index;
		}

		[HideInInspector]
		public TCollection Collection;

		[HideInInspector]
		public OdinGUITableCollection<TTable> GUITables;

		[HideInInspector]
		public OdinSharedEntryCollection SharedEntries;

		[HideInInspector]
		public Dictionary<long, float> SharedEntryHeights;

		[HideInInspector]
		public OdinGUIScrollView EntryScrollView;

		[HideInInspector]
		public Dictionary<Locale, OdinGUITable<TTable>> LocaleTableMap;

		[HideInInspector]
		public SearchField SearchField = new SearchField();

		[HideInInspector]
		public OdinTableSelectionType SelectionType = OdinTableSelectionType.None;

		[HideInInspector]
		public OdinGUITable<TTable> CurrentSelectedTable;

		[HideInInspector]
		public SharedTableData.SharedTableEntry CurrentSelectedSharedEntry;

		[HideInInspector]
		public TEntry CurrentSelectedEntry;

		[HideInInspector]
		public OdinMenuItem MenuItem;

		protected OdinMenuEditorWindow RelatedWindow;
		protected int RelatedWindowId;
		protected OdinLocalizationEditorWindow.WindowState WindowState;

		protected float PinnedWidth;

		protected OdinGUITable<TTable> KeyTable;

		protected SirenixAnimationUtility.InterpolatedFloat SelectionAnimFloat = 0.0f;

		protected int ControlIdHint = "Odin_LocalizationEditor_Control".GetHashCode();
		protected int DragDropIdHint = "Odin_LocalizationEditor_DropId".GetHashCode();

		private bool isForceDeleteKey;
		private SharedTableData.SharedTableEntry keyToRemove;
		//private bool isDraggingNonHandle;

		protected Action<SharedTableData.SharedTableEntry> OnTableEntryModified;

		protected Action<AssetTableCollection, AssetTable, AssetTableEntry> OnAssetTableEntryAdded;

		protected Action<AssetTableCollection, AssetTable, AssetTableEntry, string> OnAssetTableEntryRemoved;

		//protected Dictionary<object, int> guiIDs;
		//protected Dictionary<object, int> ControlIds;

		protected bool HasHandledCurrentModifiedEntry;
		protected TEntry CurrentModifiedEntry;

		private Action<LocalizationTableCollection, LocalizationTable> OnTableAddedToCollection;

		private Action<LocalizationTableCollection, LocalizationTable> OnTableRemovedFromCollection;

		private Undo.UndoRedoCallback OnUndoRedoPerformed;

		private float rightMenuWidth;

		private OdinGUITable<TTable> sortedTable;

		protected readonly List<LocalizationTable> LooseTables = new List<LocalizationTable>();
		
		public OdinTableCollectionEditor(TCollection collection, OdinMenuEditorWindow relatedWindow, OdinLocalizationEditorWindow.WindowState windowState)
		{
			this.Collection = collection;
			this.WindowState = windowState;

			this.GUITables = new OdinGUITableCollection<TTable>(this.Collection.Tables.Count);

#if USING_WIDTH_NON_PERCENT
			this.GUITables.AddKeyTable();
#else
			float averageWidthPercent = 1.0f / (this.Collection.Tables.Count + 1);

			this.GUITables.AddKeyTable(averageWidthPercent);
#endif
			this.KeyTable = this.GUITables.Last();

			this.LocaleTableMap = new Dictionary<Locale, OdinGUITable<TTable>>(this.Collection.Tables.Count);

			for (var i = 0; i < this.Collection.Tables.Count; i++)
			{
				var tableAsset = (TTable) this.Collection.Tables[i].asset;

				Locale tableLocale = LocalizationEditorSettings.GetLocale(tableAsset.LocaleIdentifier);

				if (tableLocale == null)
				{
					Debug.LogWarning($"No locale found for {tableAsset.name} in {this.Collection.name}, searched for: {tableAsset.LocaleIdentifier}");
					continue;
				}

#if USING_WIDTH_NON_PERCENT
				OdinGUITable<TTable> table = OdinGUITable<TTable>.CreateTable(tableAsset, tableLocale);
#else
				OdinGUITable<TTable> table = OdinGUITable<TTable>.CreateTable(tableAsset, averageWidthPercent);
#endif

				this.GUITables.Add(table);

				this.LocaleTableMap[LocalizationEditorSettings.GetLocale(tableAsset.LocaleIdentifier)] = table;
			}

			this.SharedEntries = new OdinSharedEntryCollection(this.Collection);

			//this.ControlIds = new Dictionary<object, int>(this.SharedEntries.Length + 64);

			this.EntryScrollView = new OdinGUIScrollView(this.SharedEntries.Length + 64, adjustViewForVerticalScrollBar: false);

			this.RelatedWindow = relatedWindow;
			this.RelatedWindowId = this.RelatedWindow.GetInstanceID();
			this.keyToRemove = null;
			this.isForceDeleteKey = false;

			this.LooseTables.Clear();
			LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(this.Collection.SharedData, this.LooseTables);
			this.rightMenuWidth = EditorPrefs.GetFloat("OdinTableCollectionEditor_RightMenuWidth", 300.0f);

			this.OnUndoRedoPerformed += () =>
			{
				this.Collection.RefreshAddressables();
				this.MenuItem.Name = this.Collection.SharedData.TableCollectionName;
			};
		}

		protected abstract void OnInitialize();

		private bool hasInitialized;

		public void Initialize()
		{
			if (this.hasInitialized)
			{
				return;
			}

			this.SharedEntryHeights = new Dictionary<long, float>(this.SharedEntries.Length + 128);

			this.OnTableAddedToCollection += (collection, table) =>
			{
				if (this.Collection != collection)
				{
					return;
				}

				Locale locale = LocalizationEditorSettings.GetLocale(table.LocaleIdentifier);

				if (locale == null)
				{
					Debug.LogWarning($"No locale found for {table.name} in {collection.name}, searched for: {table.LocaleIdentifier}");
					return;
				}

				if (this.LocaleTableMap.ContainsKey(locale))
				{
					return;
				}

#if USING_WIDTH_NON_PERCENT
				OdinGUITable<TTable> guiTable = OdinGUITable<TTable>.CreateTable((TTable) table, locale);
#else
				float lastAveragePercent = 1.0f / this.GUITables.Count;

				float newAveragePercent = 1.0f / (this.GUITables.Count + 1);

				for (var i = 0; i < this.GUITables.Count; i++)
				{
					this.GUITables[i].WidthPercentage *= newAveragePercent / lastAveragePercent;
				}

				OdinGUITable<TTable> guiTable = OdinGUITable<TTable>.CreateTable((TTable) table, newAveragePercent);
#endif

				this.GUITables.Add(guiTable);

				this.LocaleTableMap[locale] = guiTable;

				this.LooseTables.Clear();
				LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(this.Collection.SharedData, this.LooseTables);
			};

			this.OnTableRemovedFromCollection += (collection, table) =>
			{
				if (this.Collection != collection)
				{
					return;
				}

#if !USING_WIDTH_NON_PERCENT
				float lastAveragePercent = 1.0f / this.GUITables.Count;
#endif

				Locale locale = LocalizationEditorSettings.GetLocale(table.LocaleIdentifier);

				this.GUITables.Remove(this.LocaleTableMap[locale]);

				this.LocaleTableMap.Remove(locale);

#if !USING_WIDTH_NON_PERCENT
				float newAveragePercent = 1.0f / this.GUITables.Count;

				for (var i = 0; i < this.GUITables.Count; i++)
				{
					this.GUITables[i].WidthPercentage *= newAveragePercent / lastAveragePercent;
				}
#endif

				this.LooseTables.Clear();
				LocalizationEditorSettings.FindLooseStringTablesUsingSharedTableData(this.Collection.SharedData, this.LooseTables);
			};

#if false
			this.UndoHandler = () =>
			{
				switch (Undo.GetCurrentGroupName())
				{
					case "Add table to collection":
						for (var i = 0; i < this.Collection.Tables.Count; i++)
						{
							LocalizationTable tableAsset = this.Collection.Tables[i].asset;
							Locale locale = LocalizationEditorSettings.GetLocale(tableAsset.LocaleIdentifier);

							if (locale == null)
							{
								Debug.LogWarning($"No locale found for {tableAsset.name} in {this.Collection.name}, searched for: {tableAsset.LocaleIdentifier}");
								continue;
							}

							if (this.LocaleTableMap.ContainsKey(locale))
							{
								continue;
							}

							OdinGUITable<TTable> table = OdinGUITable<TTable>.CreateTable((TTable) tableAsset, locale);

							this.GUITables.Add(table);

							this.LocaleTableMap.Add(locale, table);

							this.Collection.RemoveTable(tableAsset);
							this.Collection.AddTable(tableAsset);
						}

						var localesToRemove = new Stack<Locale>();

						foreach (KeyValuePair<Locale, OdinGUITable<TTable>> kvp in this.LocaleTableMap)
						{
							if (!this.Collection.ContainsTable(kvp.Key.Identifier))
							{
								localesToRemove.Push(kvp.Key);
							}
						}

						while (localesToRemove.Count > 0)
						{
							Locale locale = localesToRemove.Pop();
							OdinGUITable<TTable> table = this.LocaleTableMap[locale];

							this.GUITables.Remove(table);
							this.LocaleTableMap.Remove(locale);
						}

						break;
				}
			};
#endif

			this.OnInitialize();

			this.hasInitialized = true;
		}

		private bool needsToCheckForErrors = false;

		public void OnSelectInWindow()
		{
			this.needsToCheckForErrors = true;

			this.Initialize();

			this.AttachEvents();
		}

		private bool hasAttachedEvents;

		public void AttachEvents()
		{
			if (this.hasAttachedEvents)
			{
				return;
			}

			// this.SharedEntries.AttachEvents();

			if (this.OnTableEntryModified != null)
			{
				LocalizationEditorSettings.EditorEvents.TableEntryModified += this.OnTableEntryModified;
			}

			if (this.OnAssetTableEntryAdded != null)
			{
				LocalizationEditorSettings.EditorEvents.AssetTableEntryAdded += this.OnAssetTableEntryAdded;
			}

			if (this.OnAssetTableEntryRemoved != null)
			{
				LocalizationEditorSettings.EditorEvents.AssetTableEntryRemoved += this.OnAssetTableEntryRemoved;
			}

			if (this.OnTableAddedToCollection != null)
			{
				LocalizationEditorSettings.EditorEvents.TableAddedToCollection += this.OnTableAddedToCollection;
			}

			if (this.OnTableRemovedFromCollection != null)
			{
				LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection += this.OnTableRemovedFromCollection;
			}

			if (this.OnUndoRedoPerformed != null)
			{
				Undo.undoRedoPerformed += this.OnUndoRedoPerformed;
			}

			this.hasAttachedEvents = true;
		}

		public void DetachEvents()
		{
			if (!this.hasAttachedEvents)
			{
				return;
			}

			// this.SharedEntries.DetachEvents();

			if (this.OnTableEntryModified != null)
			{
				LocalizationEditorSettings.EditorEvents.TableEntryModified -= this.OnTableEntryModified;
			}

			if (this.OnAssetTableEntryAdded != null)
			{
				LocalizationEditorSettings.EditorEvents.AssetTableEntryAdded -= this.OnAssetTableEntryAdded;
			}

			if (this.OnAssetTableEntryRemoved != null)
			{
				LocalizationEditorSettings.EditorEvents.AssetTableEntryRemoved -= this.OnAssetTableEntryRemoved;
			}

			if (this.OnTableAddedToCollection != null)
			{
				LocalizationEditorSettings.EditorEvents.TableAddedToCollection -= this.OnTableAddedToCollection;
			}

			if (this.OnTableRemovedFromCollection != null)
			{
				LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection -= this.OnTableRemovedFromCollection;
			}

			if (this.OnUndoRedoPerformed != null)
			{
				Undo.undoRedoPerformed -= this.OnUndoRedoPerformed;
			}

			this.hasAttachedEvents = false;
		}

		public virtual void RemoveKey(SharedTableData.SharedTableEntry sharedEntry)
		{
			this.SharedEntryHeights.Remove(sharedEntry.Id);
			
			this.GUITables.UndoRecordCollection(this.Collection.SharedData, "Removed Shared Table Entry from Collection");

			this.Collection.RemoveEntry(sharedEntry.Id);

			this.GUITables.SetDirty(this.Collection.SharedData);
		}

		public void SelectEntry(TEntry entry)
		{
#if false
			if (this.Collection.SharedData.Metadata.HasMetadata<OdinTemplateMetadata>())
			{
				var templateMetadata = this.Collection.SharedData.Metadata.GetMetadata<OdinTemplateMetadata>();

				if (templateMetadata.MetadataExpected.Count > 0)
				{
					for (var i = 0; i < templateMetadata.MetadataExpected.Count; i++)
					{
						if (this.HasMetadataAmountOfType(entry.MetadataEntries, templateMetadata.MetadataExpected[i], templateMetadata))
						{
							continue;
						}

						entry.AddMetadata((IMetadata) templateMetadata.MetadataExpected[i].InstantiateDefault(false));
					}
				}
			}
#endif
			
			OdinTableSelectionType lastSelectionType = this.SelectionType;

			this.SelectionType = OdinTableSelectionType.TableEntry;

			TEntry lastSelection = this.CurrentSelectedEntry;

			this.CurrentSelectedEntry = entry;

			if (lastSelection == entry && this.SelectionType == lastSelectionType)
			{
				return;
			}

			this.SelectionAnimFloat = 0.0f;
			this.SelectionAnimFloat.Destination = 1.0f;

			if (this.WindowState.CurrentTopTab != OdinLocalizationEditorWindow.RightMenuTopTabs.Metadata)
			{
				return;
			}

			this.WindowState.ShowSharedMetadata = false;

			this.UpdateMetadataViewForEntry(entry);
		}

		public void SelectSharedEntry(SharedTableData.SharedTableEntry sharedEntry)
		{
			this.SelectionType = OdinTableSelectionType.SharedEntry;

			if (this.CurrentSelectedSharedEntry != sharedEntry)
			{
				this.SelectionAnimFloat = 0.0f;
				this.SelectionAnimFloat.Destination = 1.0f;
			}

			this.CurrentSelectedSharedEntry = sharedEntry;

			if (this.WindowState.CurrentTopTab != OdinLocalizationEditorWindow.RightMenuTopTabs.Metadata)
			{
				return;
			}

			this.WindowState.ShowSharedMetadata = true;

			this.WindowState.MetadataTree?.Dispose();

			this.WindowState.MetadataTree = PropertyTree.Create(sharedEntry);
		}

		public void SelectTable(OdinGUITable<TTable> table)
		{
			this.SelectionType = table.Type == OdinGUITable<TTable>.GUITableType.Key
				? OdinTableSelectionType.SharedTable
				: OdinTableSelectionType.Table;
			this.CurrentSelectedTable = table;
		}

		public void UpdateMetadataViewForEntry(TEntry entry)
		{
			this.WindowState.MetadataTree?.Dispose();

			object metadataData = null;

			if (this.WindowState.ShowSharedMetadata)
			{
				metadataData = entry.SharedEntry;
			}
			else
			{
				if (typeof(TEntry) == typeof(AssetTableEntry))
				{
					metadataData = OdinLocalizationReflectionValues.AssetTableEntry_Data_Property.GetValue(entry);
				}

				if (typeof(TEntry) == typeof(StringTableEntry))
				{
					metadataData = OdinLocalizationReflectionValues.StringTableEntry_Data_Property.GetValue(entry);
				}
			}

			if (metadataData != null)
			{
				this.WindowState.MetadataTree = PropertyTree.Create(metadataData);
			}
		}

		public bool IsSharedEntrySelected(SharedTableData.SharedTableEntry sharedEntry)
		{
			return this.SelectionType == OdinTableSelectionType.SharedEntry && this.CurrentSelectedSharedEntry == sharedEntry;
		}

		public bool IsEntrySelected(TEntry entry)
		{
			if (entry == null)
			{
				return false;
			}
			
			return this.SelectionType == OdinTableSelectionType.TableEntry && this.CurrentSelectedEntry == entry;
		}

		public bool IsTableSelected(OdinGUITable<TTable> table)
		{

			return (this.SelectionType == OdinTableSelectionType.Table ||
			        this.SelectionType == OdinTableSelectionType.SharedTable) && this.CurrentSelectedTable == table;
		}

		public void ClearSelection()
		{
			GUIUtility.hotControl = 0;
			GUIUtility.keyboardControl = 0;
			this.SelectionType = OdinTableSelectionType.None;
			this.WindowState.MetadataTree?.Dispose();
			this.WindowState.MetadataTree = null;
		}

		public void ClearFocus()
		{
			GUIUtility.hotControl = 0;
			GUIUtility.keyboardControl = 0;
		}

		protected bool HasGUIChanged = true;
		private int lastGUIEntryCount;


		private bool firstTimeSeeingTable = true;

		private AddressableEntryNotFoundException tableAddressableException;
		private string exceptionHeaderMsg = string.Empty;
		private string exceptionMsg = string.Empty;


		[OnInspectorGUI]
		public void DrawAndHandleExceptions()
		{
			if (this.tableAddressableException != null)
			{
				const float SPACING = 10;
				
				Rect rect = GUILayoutUtility.GetRect(0, 0, GUILayoutOptions.ExpandWidth().ExpandHeight());

				rect = rect.AlignCenter(520, 200);

				Color shadowColor = EditorGUIUtility.isProSkin ? new Color(1, 0, 0, 0.2588235f) : new Color(1, 0, 0, 0.3137255f);

				OdinLocalizationGUI.DrawRoundBlur20(rect, shadowColor);

				Color backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.6037736f, 0.1566394f, 0.1566394f) : new Color(0.8301887f, 0.238875f, 0.238875f);

				SirenixEditorGUI.DrawRoundRect(rect, backgroundColor, 7.5f);

				rect = rect.Padding(14);

				Rect buttonsArea = rect.TakeFromBottom(32);

				GUI.BeginClip(rect.Expand(14));
				{
					Rect watermarkPosition = rect.SetPosition(Vector2.zero).AlignRight(80).Expand(34).AddX(30).SubY(20);

					SdfIcons.DrawIcon(watermarkPosition, SdfIconType.ExclamationDiamondFill, new Color(1, 1, 1, 0.075f));
				}
				GUI.EndClip();

				rect.height -= SPACING;

				float msgHeight = OdinLocalizationGUI.CardTitleWhite.CalcHeight(this.exceptionHeaderMsg, rect.width);
				GUI.Label(rect.TakeFromTop(msgHeight), this.exceptionHeaderMsg, OdinLocalizationGUI.CardTitleWhite);

				rect.yMin += SPACING;
				
				GUI.Label(rect, this.exceptionMsg, SirenixGUIStyles.MultiLineWhiteLabel);

				if (OdinLocalizationGUI.OverlaidButton(buttonsArea.TakeFromRight(120), "Fix All", SdfIconType.Tools))
				{
					this.Collection.RefreshAddressables();
					this.tableAddressableException = null;

					this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
														  SdfIconType.Tools,
														  $"Refreshed Addressables for '{this.Collection.name}'.",
														  new Color(0.26f, 0.51f, 0.44f),
														  12.0f);
				}

				buttonsArea.width -= SPACING;

				if (OdinLocalizationGUI.OverlaidButton(buttonsArea.TakeFromRight(160), "Fix And Preload All", SdfIconType.Tools))
				{
					this.Collection.RefreshAddressables();
					this.Collection.SetPreloadTableFlag(true);
					this.tableAddressableException = null;

					this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
														  SdfIconType.Tools,
														  $"Refreshed Addressables and Preloaded All Tables for '{this.Collection.name}'.",
														  new Color(0.26f, 0.51f, 0.44f),
														  12.0f);
				}

				return;
			}

			try
			{
				this.Draw();

				if (this.needsToCheckForErrors)
				{
					// NOTE: this attempts to catch any AddressableEntryNotFoundException errors, by fetching the Addressables for every table.
					this.Collection.IsPreloadTableFlagSet();

					this.needsToCheckForErrors = false;
				}
			}
			catch (AddressableEntryNotFoundException e)
			{
				this.tableAddressableException = e;

				this.exceptionHeaderMsg = e.Message;
				this.exceptionMsg = $"There could be multiple other tables facing the same issue in '{this.Collection.name}', " +
										  "this can potentially be resolved by refreshing the Addressables.";

				GUIHelper.ExitGUI(false);
			}
		}
		
		public void Draw()
		{
			if (Event.current.type == EventType.MouseUp)
			{
				SharedUniqueControlId.SetInactive();
			}
			
			if (this.EntryScrollView.IsDraggingMouse)
			{
				EditorGUIUtility.AddCursorRect(this.EntryScrollView.InteractRect, MouseCursor.Pan);
			}
			
			if (this.HasHandledCurrentModifiedEntry)
			{
				this.CurrentModifiedEntry = null;
			}

			//	if (Event.current.type == EventType.MouseUp)
			//	{
			//		this.isDraggingNonHandle = false;
			//	}
			//	
			bool shouldClearSelection = Event.current.OnKeyDown(KeyCode.Escape, false);

			//this.SharedEntries.UpdateIfChangesArePresent();

			Rect position = GUILayoutUtility.GetRect(0, 0, GUILayoutOptions.ExpandWidth().ExpandHeight());

			position = this.RelatedWindow.position.SetPosition(Vector2.zero);
			position.TakeFromRight(this.RelatedWindow.MenuWidth);

			var leftMenuSliderRect = position.TakeFromLeft(10).SubX(1);
			var rightMenuRect = position.TakeFromRight(this.WindowState.RightMenuWidth);
			var rightMenuSliderRect = position.TakeFromRight(11);

			this.RelatedWindow.MenuWidth += this.VerticalSlideRect(leftMenuSliderRect.AddXMax(1), false);
			this.RelatedWindow.MenuWidth = Mathf.Max(this.RelatedWindow.MenuWidth, 1);

			if (Event.current.clickCount > 1 && Event.current.IsMouseOver(leftMenuSliderRect))
			{
				this.RelatedWindow.MenuWidth = 1;

				if (Event.current.control || Event.current.alt || Event.current.shift)
				{
					this.WindowState.RightMenuWidth = 0;
				}
			}

			if (Event.current.clickCount > 1 && Event.current.IsMouseOver(rightMenuSliderRect))
			{
				if (Event.current.control || Event.current.alt || Event.current.shift)
				{
					if (this.WindowState.RightMenuWidth > 0)
					{
						this.WindowState.RightMenuWidth = 0;
						this.WindowState.LeftMenuWidth = 0;
					}
					else
					{
						this.WindowState.RightMenuWidth = this.WindowState.LastOpenRightMenuWidth;
						this.RelatedWindow.MenuWidth = this.WindowState.LastOpenRightMenuWidth;
					}
				}
				else
				{
					if (this.WindowState.RightMenuWidth > 0)
					{
						this.WindowState.RightMenuWidth = 0;
					}
					else
					{
						this.WindowState.RightMenuWidth = this.WindowState.LastOpenRightMenuWidth;
					}
				}
			}

			Rect toolbarRect = position.TakeFromTop(OdinLocalizationConstants.TOOLBAR_HEIGHT);

			Rect dragHandleRect = position.TakeFromLeft(OdinLocalizationConstants.DRAG_HANDLE_WIDTH);

			this.DrawToolbar(toolbarRect);

			this.GUITables.Sort();

#if USING_WIDTH_NON_PERCENT
			for (var i = 0; i < this.GUITables.Count; i++)
			{
				this.GUITables[i].Width = this.GUITables[i].Width;
			}
#endif

			float viewWidth = position.width;

#if USING_WIDTH_NON_PERCENT
			float columnsWidth = this.GUITables.GetVisibleWidth();

			if (columnsWidth >= viewWidth)
			{
				viewWidth = columnsWidth;
			}
#else
			int columnsMinTotalWidth = this.GUITables.GetVisibleCount() * OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH;

			if (columnsMinTotalWidth > viewWidth)
			{
				viewWidth = columnsMinTotalWidth;
			}
#endif

			if (position != this.EntryScrollView.InteractRect)
			{
				this.HasGUIChanged = true;
			}

			bool isEntryCountChanged = this.Collection.SharedData.Entries.Count != this.lastGUIEntryCount;

			if (this.HasGUIChanged || isEntryCountChanged)
			{
#if false
				if (isEntryCountChanged)
				{
					if (this.SharedEntries.IsSorted)
					{
						this.Resort();
					}

					if (this.SharedEntries.IsSearching)
					{
						this.SharedEntries.UpdateSearchTerm(this.SharedEntries.SearchTerm, this.GUITables, this.Collection);
					}
				}
#endif
				
				this.HasGUIChanged = false;

				if (isEntryCountChanged)
				{
					if (this.SharedEntries.IsSorted)
					{
						switch (this.sortedTable.Type)
						{
							case OdinGUITable<TTable>.GUITableType.Default:
								switch (this.sortedTable.Asset)
								{
									case AssetTable assetTable:
										this.SharedEntries.SortByAssetTable(this.Collection as AssetTableCollection, assetTable, false);
										break;

									case StringTable stringTable:
										this.SharedEntries.SortByStringTable(stringTable, false);
										break;
								}

								break;

							case OdinGUITable<TTable>.GUITableType.Key:
								this.SharedEntries.SortByKeys(false);
								break;

							default:
								throw new ArgumentOutOfRangeException();
						}
					}

					if (this.SharedEntries.IsSearching)
					{
						this.SharedEntries.UpdateSearchTerm(this.SharedEntries.SearchTerm, this.GUITables, this.Collection, true);
					}
				}

				this.lastGUIEntryCount = this.Collection.SharedData.Entries.Count;

				float previousY = this.EntryScrollView.PositionY;

				this.EntryScrollView.SetBounds(position, viewWidth);

				this.EntryScrollView.BeginAllocations();
				{
					this.AllocateItems();
				}
				this.EntryScrollView.EndAllocations();

				if (this.adjustViewForSeparatorChange &&
					 this.lastViewHeight != 0.0f &&
					 Math.Abs(this.lastViewHeight - this.EntryScrollView.ViewRect.height) > 0.01f)
				{
					float newHeight = this.EntryScrollView.ViewRect.height;

					float change = previousY / this.lastViewHeight;

					this.EntryScrollView.PositionY = change * newHeight;

					this.adjustViewForSeparatorChange = false;
				}
			}
			else
			{
				this.EntryScrollView.SetBoundsForCurrentAllocations(position, viewWidth);
			}

#if !USING_WIDTH_NON_PERCENT
			this.GUITables.CalcWidths(this.EntryScrollView);
#endif

			this.PinnedWidth = 0.0f;

			for (var i = 0; i < this.GUITables.Count; i++)
			{
				OdinGUITable<TTable> table = this.GUITables[i];

				if (!table.IsVisible || !table.IsPinned)
				{
					continue;
				}
				
#if USING_WIDTH_NON_PERCENT
				this.PinnedWidth += table.Width;
#else
					this.PinnedWidth += this.GUITables[i].Width;
#endif
			}

			if (this.PinnedWidth > this.EntryScrollView.Bounds.width)
			{
				this.PinnedWidth = this.EntryScrollView.Bounds.width;
				this.GUITables.ResizePinnedToFit(this.EntryScrollView.Bounds.width);
			}

			this.GUITables.UpdateVisibleTables(this.EntryScrollView, this.PinnedWidth);

			if (this.firstTimeSeeingTable && columnsWidth < viewWidth)
			{
				this.GUITables.ResizeToFit(this.EntryScrollView.Bounds.width - this.PinnedWidth);
				this.firstTimeSeeingTable = false;
			}


			OdinGUIScrollView.VisibleItems visibleItems = this.EntryScrollView.GetVisibleItems();

			this.DrawRows(ref visibleItems);

			this.DrawPseudoRows();

			this.DrawItems(ref visibleItems);

			this.DrawColumnsAndSeparators(ref visibleItems);

			this.DrawDragHandles(dragHandleRect, ref visibleItems);

			this.DrawRightMenu(rightMenuRect);

			if (this.keyToRemove != null)
			{
				if (this.isForceDeleteKey ||
					 EditorUtility.DisplayDialog("Odin Table Collection Editor", $"Are you sure you want to remove entry: {this.keyToRemove.Key}?", "Yes", "No"))
				{
					this.RemoveKey(this.keyToRemove);
				}

				this.keyToRemove = null;
				this.isForceDeleteKey = false;
			}

			if (shouldClearSelection)
			{
				this.ClearSelection();
			}

			this.EntryScrollView.HandleMiddleMouseDrag(inverted: OdinLocalizationConfig.Instance.invertMouseDragNavigation,
																	 speed: OdinLocalizationConfig.Instance.mouseDragSpeed);


			this.WindowState.RightMenuWidth -= this.VerticalSlideRect(rightMenuSliderRect, true);
			this.WindowState.RightMenuWidth = Mathf.Max(this.WindowState.RightMenuWidth, 0);

			if (this.WindowState.RightMenuWidth > 338)
			{
				this.WindowState.LastOpenRightMenuWidth = this.WindowState.RightMenuWidth;
			}
		}

		protected abstract void AllocateItems();

		protected abstract void DrawItems(ref OdinGUIScrollView.VisibleItems visibleItems);

		protected abstract void MeasureAllEntries();

		protected abstract void MeasureVisibleEntries(ref OdinGUIScrollView.VisibleItems visibleItems);

		// NOTE: returns true if pressed, TODO add xml comments later
		protected static bool DrawCell(Rect rect, bool isEven)
		{
			Color background = isEven ? OdinLocalizationGUI.RowEvenBackground2 : OdinLocalizationGUI.RowOddBackground2;

			GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, background, 0, 2.5f);

			if (Event.current.IsMouseOver(rect) && !DragAndDropUtilities.IsDragging)
			{
				GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, OdinLocalizationGUI.RowBorderHover, 1, 2.5f);
			}
			else
			{
				GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, OdinLocalizationGUI.RowBorder, 1, 2.5f);
			}

			rect.x += OdinLocalizationConstants.ROW_MENU_WIDTH;
			rect.width -= OdinLocalizationConstants.ROW_MENU_WIDTH + OdinLocalizationConstants.ROW_MENU_WIDTH;
			bool isPressed = Event.current.OnMouseDown(rect, 0, false);

			return isPressed;
		}

		protected void DrawKey(Rect rect, SharedTableData.SharedTableEntry sharedEntry, int id)
		{
			Rect removeRect = rect.TakeFromLeft(OdinLocalizationConstants.ROW_MENU_WIDTH);
			Rect copyKeyIdRect = rect.TakeFromRight(OdinLocalizationConstants.ROW_MENU_WIDTH);

			Color removeBgColor = Event.current.IsMouseOver(removeRect) ? new Color(0.8f, 0.1f, 0.1f, 0.8f) : new Color(0, 0, 0, 0.2f);

			GUI.DrawTexture(removeRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1.0f, removeBgColor, Vector4.zero,
								 new Vector4(2.5f, 0.0f, 0.0f, 2.5f));

			if (Event.current.OnMouseDown(removeRect, 0))
			{
				this.keyToRemove = sharedEntry;

				if (Event.current.modifiers == EventModifiers.Shift)
				{
					this.isForceDeleteKey = true;
				}

				this.ClearSelection();

				return;
			}

			Color removeFgColor = Event.current.IsMouseOver(removeRect) ? new Color(1, 1, 1, 0.8f) : new Color(1, 1, 1, 0.5f);

			removeRect = removeRect.AlignCenter(14, 14);

			SdfIcons.DrawIcon(removeRect, SdfIconType.X, removeFgColor);

			// copyKeyIdRect.x -= 20;

			copyKeyIdRect = copyKeyIdRect.AlignCenter(16, 16);

			var isMouseOverKeyRect = Event.current.IsMouseOver(copyKeyIdRect);

			Matrix4x4 m = GUI.matrix;
			GUIUtility.RotateAroundPivot(45.0f, copyKeyIdRect.center);
			SdfIcons.DrawIcon(copyKeyIdRect, SdfIconType.KeyFill, new Color(1, 1, 1, isMouseOverKeyRect ? 0.8f : 0.3f));
			GUI.matrix = m;

			if (isMouseOverKeyRect)
			{
				GUI.Label(copyKeyIdRect, GUIHelper.TempContent(string.Empty, "Copy Shared Entry Id"));
			}

			if (Event.current.OnMouseDown(copyKeyIdRect, 0))
			{
				this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
													  SdfIconType.Clipboard,
													  $"Copied Shared Entry Id '{sharedEntry.Id}' to the clipboard.",
													  new Color(0.23f, 0.36f, 0.68f),
													  8.0f);

				Clipboard.Copy(sharedEntry.Id.ToString());

				this.ClearSelection();
			}

			string result = OdinLocalizationGUI.TextField(rect, sharedEntry.Key, out bool changed, id);

			if (!changed)
			{
				return;
			}

			if (this.Collection.SharedData.Contains(result) && result != sharedEntry.Key)
			{
				this.RelatedWindow.ShowToast(ToastPosition.BottomLeft,
													  SdfIconType.ExclamationOctagonFill,
													  $"Key '{result}' already exists in the collection.",
													  new Color(0.68f, 0.2f, 0.2f),
													  8.0f);
			}
			else
			{
				Undo.RecordObject(this.Collection.SharedData, "Renamed Shared Table Entry Key");
				this.Collection.SharedData.RenameKey(sharedEntry.Id, result);
				OdinLocalizationEvents.RaiseTableEntryModified(sharedEntry);
				EditorUtility.SetDirty(this.Collection.SharedData);
			}
		}

		protected static float MeasureText(string text, float width)
		{
			// TODO: get rid of this magic number
			width -= 20 + 20 + 8 + 16;

			float rowHeightWithoutText = OdinLocalizationConstants.ROW_HEIGHT - SirenixGUIStyles.MultiLineCenteredLabel.lineHeight;

			float heightOfText = SirenixGUIStyles.MultiLineCenteredLabel.CalcHeight(text, width) - SirenixGUIStyles.MultiLineCenteredLabel.padding.vertical;

			return rowHeightWithoutText + heightOfText;
		}

		private void DrawToolbar(Rect position)
		{
			Rect originalPosition = position;
			
			Rect resizeToFitButtonRect = position.TakeFromRight(180f);
			Rect addButtonRect = position.TakeFromRight(180f);

			position = position.Padding(4);

			if (GUI.Button(resizeToFitButtonRect, "Resize Columns To Fit", SirenixGUIStyles.ToolbarButton))
			{
				this.GUITables.ResizeToFit(this.EntryScrollView.Bounds.width - this.PinnedWidth);
				this.HasGUIChanged = true;
			}
			
			if (GUI.Button(addButtonRect, "Add Shared Entry", SirenixGUIStyles.ToolbarButton))
			{
				this.GUITables.UndoRecordCollection(this.Collection.SharedData, "Added Shared Entry To Collection");
				SharedTableData.SharedTableEntry sharedEntry = this.Collection.SharedData.AddKey();

				OdinLocalizationEvents.RaiseTableEntryAdded(this.Collection, sharedEntry);
				this.GUITables.SetDirty(this.Collection.SharedData);
			}

			string searchTerm = this.SearchField.Draw(position, this.SharedEntries.SearchTerm, "Search for item(s)...");

			if (this.SharedEntries.UpdateSearchTerm(searchTerm, this.GUITables, this.Collection))
			{
				this.HasGUIChanged = true;
			}

			if (!EditorGUIUtility.isProSkin)
			{
				EditorGUI.DrawRect(originalPosition, new Color(0, 0, 0, 0.05f));
			}
		}

		private void DrawRows(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			Rect clipRect = this.EntryScrollView.GetClipRect();

			clipRect.x -= OdinLocalizationConstants.DRAG_HANDLE_WIDTH;
			clipRect.width += OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

			this.EntryScrollView.BeginClip(clipRect, offset: new Vector2(0, OdinLocalizationConstants.COLUMN_HEIGHT));
			{
				for (var i = 0; i < visibleItems.Length; i++)
				{
					Rect rect = visibleItems.GetRect(i);

					Rect dropZoneRect = rect;

					rect.width += OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

					bool isEven = (visibleItems.Offset + i) % 2 == 0;

					EditorGUI.DrawRect(rect, isEven ? OdinLocalizationGUI.RowEvenBackground : OdinLocalizationGUI.RowOddBackground);

					this.HandleDropZone(dropZoneRect, visibleItems.Offset + i);
				}
			}
			this.EntryScrollView.EndClip();
		}

		private void DrawPseudoRows()
		{
			if (this.EntryScrollView.IsBeyondVerticalBounds)
			{
				return;
			}

			var remainderRect = new Rect(this.EntryScrollView.Bounds.x - OdinLocalizationConstants.DRAG_HANDLE_WIDTH,
												  this.EntryScrollView.Bounds.y + this.EntryScrollView.ViewRect.height + OdinLocalizationConstants.COLUMN_HEIGHT,
												  this.EntryScrollView.Bounds.width + OdinLocalizationConstants.DRAG_HANDLE_WIDTH,
												  this.EntryScrollView.Bounds.height - this.EntryScrollView.ViewRect.height - OdinLocalizationConstants.COLUMN_HEIGHT);

			Rect maintainedRemainderRect = remainderRect;

			bool isNextEven = this.SharedEntries.Length % 2 == 0;

			while (remainderRect.height > 0)
			{
				Rect rect = remainderRect.TakeFromTop(OdinLocalizationConstants.ROW_HEIGHT);
				Color color = isNextEven ? OdinLocalizationGUI.RowEvenBackground : OdinLocalizationGUI.RowOddBackground;

				EditorGUI.DrawRect(rect, color);

				isNextEven = !isNextEven;
			}

			EditorGUI.DrawRect(maintainedRemainderRect, new Color(0, 0, 0, 0.25f));
		}

		private void DrawColumnsAndSeparators(ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			EditorGUI.DrawRect(this.EntryScrollView.Bounds.AlignTop(OdinLocalizationConstants.COLUMN_HEIGHT), OdinLocalizationGUI.ColumnBackground);

			Rect columnArea = this.EntryScrollView.ViewRect;

			if (!this.EntryScrollView.IsBeyondVerticalBounds)
			{
				columnArea.height = this.EntryScrollView.Bounds.height;
			}

			int lastIndex = this.GUITables.GetLastVisibleIndex();

			this.EntryScrollView.BeginClip(offset: new Vector2(this.PinnedWidth, 0), ignoreScrollY: true);
			{
				this.DrawColumns(ref visibleItems, columnArea.AlignRight(columnArea.width), false, lastIndex);
			}
			this.EntryScrollView.EndClip();

			int lastPinnedIndex = this.GUITables.GetLastVisiblePinnedIndex();

			this.EntryScrollView.BeginClip(ignoreScrollX: true, ignoreScrollY: true);
			{
				this.DrawColumns(ref visibleItems, columnArea, true, lastPinnedIndex);
			}
			this.EntryScrollView.EndClip();

			if (this.PinnedWidth > 0.0f && this.PinnedWidth + 20 <= this.EntryScrollView.Bounds.width)
			{
				Rect shadowRect = this.EntryScrollView.Bounds;

				shadowRect.x += this.PinnedWidth;
				shadowRect.width = 24;

				GUI.DrawTexture(shadowRect, OdinLocalizationGUITextures.LeftToRightFade, ScaleMode.StretchToFill, true, 1.0f, new Color(0, 0, 0, 0.35f), 0, 0);
			}
		}

		private void DrawColumns(ref OdinGUIScrollView.VisibleItems visibleItems, Rect columnArea, bool pinned, int lastIndex)
		{
			for (var i = 0; i < this.GUITables.Count; i++)
			{
				OdinGUITable<TTable> table = this.GUITables[i];

				if (table.IsPinned != pinned)
				{
					continue;
				}

				if (!this.GUITables.TablesWithinVisibleBounds.Contains(table))
				{
					columnArea.TakeFromLeft(table.Width);
					continue;
				}

#if USING_WIDTH_NON_PERCENT
				Rect columnRect = columnArea.TakeFromLeft(table.Width);
#else
				Rect columnRect = columnArea.TakeFromLeft(table.Width);
#endif

				Rect columnHeaderRect = columnRect.AlignTop(OdinLocalizationConstants.COLUMN_HEIGHT);

				bool isSelected = this.IsTableSelected(table);

				if (isSelected)
				{
					FancyColor.PushBlend(FancyColor.Gray.Lerp(OdinLocalizationGUI.Selected, 0.5f), FancyColor.BlendMode.Overlay);
					EditorGUI.DrawRect(columnHeaderRect, OdinLocalizationGUI.ColumnBackground);
				}

				Rect interactColumnRect = columnHeaderRect.Padding(4, 0);

				if (Event.current.IsMouseOver(interactColumnRect))
				{
					EditorGUI.DrawRect(columnHeaderRect, new Color(1.0f, 1.0f, 1.0f, 0.035f));
				}

				Rect pinRect = columnHeaderRect.TakeFromRight(30).SubXMax(10).AlignMiddle(18);
				SdfIconType pinIcon = table.IsPinned ? SdfIconType.PinAngleFill : SdfIconType.PinAngle;

				GUI.Label(pinRect, GUIHelper.TempContent(string.Empty, "Pin Table"));

				if (Event.current.IsMouseOver(pinRect))
				{
					SdfIcons.DrawIcon(pinRect, pinIcon, Color.white);
				}
				else
				{
					SdfIcons.DrawIcon(pinRect, pinIcon);
				}

				if (Event.current.OnMouseDown(pinRect, 0))
				{
					table.IsPinned = !table.IsPinned;

					this.ClearFocus();
				}

				float columnTextWidth = SirenixGUIStyles.LabelCentered.CalcWidth(table.DisplayName);

				Rect minSortRect = columnHeaderRect.TakeFromLeft(30).AddXMin(10);

				Rect sortRect = columnHeaderRect.AlignCenter(20, 18);

				sortRect.x -= columnTextWidth * 0.5f + 12.0f;

				if (sortRect.x < minSortRect.x)
				{
					sortRect = minSortRect.AlignMiddle(18);
				}

				sortRect = sortRect.AlignCenter(16, 16);

				if (EditorGUIUtility.isProSkin)
				{
					GUI.Label(columnHeaderRect, table.DisplayName, SirenixGUIStyles.LabelCentered);
				}
				else
				{
					var t = SirenixGUIStyles.LabelCentered.normal.textColor;
					SirenixGUIStyles.LabelCentered.normal.textColor = new Color(0, 0, 0, 0.7f);
					GUI.Label(columnHeaderRect, table.DisplayName, SirenixGUIStyles.LabelCentered);
					SirenixGUIStyles.LabelCentered.normal.textColor = t;
				}

				SdfIconType sortIcon;

				if (this.sortedTable == table)
				{
					switch (this.SharedEntries.CurrentSortOrderState)
					{
						case OdinSharedEntryCollection.SortOrderState.Unsorted:
							sortIcon = SdfIconType.ArrowDownUp;
							break;

						case OdinSharedEntryCollection.SortOrderState.Ascending:
							sortIcon = SdfIconType.ArrowDown;
							break;

						case OdinSharedEntryCollection.SortOrderState.Descending:
							sortIcon = SdfIconType.ArrowUp;
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				else
				{
					sortIcon = SdfIconType.ArrowDownUp;
				}

				if (Event.current.IsMouseOver(sortRect))
				{
					SdfIcons.DrawIcon(sortRect, sortIcon, Color.white);
				}
				else
				{
					SdfIcons.DrawIcon(sortRect, sortIcon);
				}

				GUI.Label(sortRect, GUIHelper.TempContent(string.Empty, "Sort Table"));

				if (Event.current.OnMouseDown(sortRect, 0))
				{
					bool wasSorted = this.SharedEntries.IsSorted && this.sortedTable != table;
					
					if (this.sortedTable == table || this.SharedEntries.CurrentSortOrderState == OdinSharedEntryCollection.SortOrderState.Unsorted)
					{
						this.SharedEntries.GotoNextSortOrderState();
					}

					switch (table.Type)
					{
						case OdinGUITable<TTable>.GUITableType.Default:
							switch (table.Asset)
							{
								case AssetTable assetTable:
									this.SharedEntries.SortByAssetTable(this.Collection as AssetTableCollection, assetTable, wasSorted);
									break;

								case StringTable stringTable:
									this.SharedEntries.SortByStringTable(stringTable, wasSorted);
									break;
							}

							break;

						case OdinGUITable<TTable>.GUITableType.Key:
							this.SharedEntries.SortByKeys(wasSorted);
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}

					this.sortedTable = table;

					this.ClearFocus();

					this.HasGUIChanged = true;
				}

				if (Event.current.OnMouseDown(interactColumnRect, 0))
				{
					this.SelectTable(table);
				}

				if (isSelected)
				{
					FancyColor.PopBlend();
				}

#if !USING_WIDTH_NON_PERCENT
				if (i == lastIndex)
				{
					continue;
				}
#endif

				this.DrawSeparator(ref visibleItems, columnRect, table, i, lastIndex);
			}
		}

		private bool hasSeparatorChanged = false;
		private bool adjustViewForSeparatorChange = false;
		private float lastViewHeight;

		private void DrawSeparator(ref OdinGUIScrollView.VisibleItems visibleItems, Rect columnRect, OdinGUITable<TTable> table, int index, int lastIndex)
		{
			Rect separatorRect = columnRect.AlignRight(1);

			EditorGUI.DrawRect(separatorRect, OdinLocalizationGUI.RowBorder);

			Rect separatorMouseRect = separatorRect.Expand(1, 0);

			switch (Event.current.type)
			{
				case EventType.MouseDown:
					if (Event.current.button == 0 && Event.current.IsMouseOver(separatorMouseRect))
					{
						//this.isDraggingNonHandle = true;
						this.ClearFocus();
					}

					break;

				case EventType.MouseUp:
					//if (this.isDraggingNonHandle)
					//{
					// NOTE: we only adjust the ones we can see while we drag separators, to avoid unnecessary computations.
					if (this.hasSeparatorChanged)
					{
						this.MeasureAllEntries();
						this.hasSeparatorChanged = false;

						this.adjustViewForSeparatorChange = true;
					}
					//}

					//this.isDraggingNonHandle = false;
					break;
			}

			Vector2 slideAmount = table.HandleSlider(separatorMouseRect);

			if (slideAmount.x == 0.0f)
			{
				return;
			}

			if (!this.hasSeparatorChanged)
			{
				this.lastViewHeight = this.EntryScrollView.ViewRect.height;
			}
			
			
			this.hasSeparatorChanged = true;

#if USING_WIDTH_NON_PERCENT
			this.AppendWidth(slideAmount.x, index, lastIndex);
#else
			float newWidth = table.Width + slideAmount.x;

			OdinGUITable<TTable> nextTable = this.GUITables.GetNextVisible(index);

			float nextNewWidth = nextTable.Width - slideAmount.x;

			if (nextNewWidth < OdinLocalizationConstants.MIN_COLUMN_WIDTH)
			{
				float diff = OdinLocalizationConstants.MIN_COLUMN_WIDTH - nextNewWidth;

				newWidth -= diff;

				nextNewWidth += diff;
			}

			if (newWidth < OdinLocalizationConstants.MIN_COLUMN_WIDTH)
			{
				float diff = OdinLocalizationConstants.MIN_COLUMN_WIDTH - newWidth;

				newWidth += diff;

				nextNewWidth -= diff;
			}

			table.WidthPercentage *= newWidth / table.Width;

			nextTable.WidthPercentage *= nextNewWidth / nextTable.Width;
#endif
			this.MeasureVisibleEntries(ref visibleItems);
		}

		private void AppendWidth(float change, int index, int lastIndex)
		{
			OdinGUITable<TTable> table = this.GUITables[index];

			table.Width += change;

			if (change < 0.0f && table.Width <= OdinLocalizationConstants.MIN_COLUMN_WIDTH)
			{
				int previousIndex = index - 1;

				while (previousIndex > -1)
				{
					OdinGUITable<TTable> previousTable = this.GUITables[previousIndex];

					if (previousTable.IsPinned != table.IsPinned)
					{
						break;
					}

					previousTable.Width += change;

					if (previousTable.Width <= OdinLocalizationConstants.MIN_COLUMN_WIDTH)
					{
						previousIndex--;
						continue;
					}

					break;
				}

				if (previousIndex == -1)
				{
					previousIndex = 0;
				}

				if (index != lastIndex && this.GUITables[previousIndex].Width > OdinLocalizationConstants.MIN_COLUMN_WIDTH)
				{
					this.GUITables[index + 1].Width -= change;
				}
			}
			else if (index != lastIndex)
			{
				this.GUITables[index + 1].Width -= change;
			}
		}

		private void DrawDragHandles(Rect position, ref OdinGUIScrollView.VisibleItems visibleItems)
		{
			if (this.EntryScrollView.IsBeyondHorizontalBounds)
			{
				OdinGUIScrollView.ScrollBackground(position.AlignBottom(OdinGUIScrollView.SCROLL_BAR_SIZE), false);
			}
			
			Rect clipRect = this.EntryScrollView.GetClipRect();

			clipRect.x -= position.width;
			clipRect.width = position.width;

			this.EntryScrollView.BeginClip(clipRect, offset: new Vector2(0, OdinLocalizationConstants.COLUMN_HEIGHT), ignoreScrollX: true);
			{
				if (this.SharedEntries.IsSorted || this.SharedEntries.IsSearching) //(this.isDraggingNonHandle)
				{
					for (var i = 0; i < visibleItems.Length; i++)
					{
						Rect dragHandleRect = visibleItems.GetRect(i);

						dragHandleRect.width = OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

						dragHandleRect.x += 2;
						dragHandleRect.width -= 4;

						if (EditorGUIUtility.isProSkin)
						{
							SdfIcons.DrawIcon(dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical, new Color(0.35f, 0.35f, 0.35f, 1.0f));
						}
						else
						{
							SdfIcons.DrawIcon(dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical, new FancyColor(0.66f));
						}
					}
				}
				else
				{
					bool isDraggingAnything = this.IsDraggingAnything();
					
					for (var i = 0; i < visibleItems.Length; i++)
					{
						Rect dragHandleRect = visibleItems.GetRect(i);

						dragHandleRect.width = OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

						bool isMouseOver = Event.current.IsMouseOver(dragHandleRect);

						if (!isDraggingAnything)
						{
							var dragData = new DragInfo {Index = visibleItems.Offset + i};
							DragAndDropUtilities.DragZone(dragHandleRect, dragData, false, false);
						}

						dragHandleRect.x += 2;
						dragHandleRect.width -= 4;

						if (EditorGUIUtility.isProSkin)
						{
							SdfIcons.DrawIcon(dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical, new Color(1, 1, 1, isMouseOver ? 0.8f : 0.6f));
						}
						else
						{
							SdfIcons.DrawIcon(dragHandleRect.AlignMiddle(16), SdfIconType.GripVertical, new Color(0, 0, 0, isMouseOver ? 0.6f : 0.4f));
						}
					}
				}
			}
			this.EntryScrollView.EndClip();

			EditorGUI.DrawRect(position.TakeFromTop(OdinLocalizationConstants.COLUMN_HEIGHT), OdinLocalizationGUI.ColumnBackground);
		}

		// NOTE: for now we pass by index, since you can't drag stuff around when you're searching or sorting
		private void HandleDropZone(Rect position, int indexTo)
		{
			position.x += OdinLocalizationConstants.DRAG_HANDLE_WIDTH;

			float halfHeight = position.height * 0.5f;

			Rect topDropRect = position.AlignTop(halfHeight);
			Rect bottomDropRect = position.AlignBottom(halfHeight);

			int topId = this.DragDropIdHint + indexTo;
			
			DragInfo topValue = DragAndDropUtilities.DropZone(topDropRect, DragInfo.None, this.DragDropIdHint + indexTo);

			int bottomId = this.DragDropIdHint + indexTo + this.SharedEntries.Length;

			DragInfo bottomValue = DragAndDropUtilities.DropZone(bottomDropRect, DragInfo.None, bottomId);

			if (DragAndDropUtilities.IsDragging)
			{
				if (DragAndDropUtilities.HoveringAcceptedDropZone == topId)
				{
					if (EditorGUIUtility.isProSkin)
					{
						GUI.DrawTexture(topDropRect.AlignTop(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
											 OdinLocalizationGUITextures.TopToBottomFade,
											 ScaleMode.StretchToFill,
											 true,
											 1.0f,
											 new Color(0.16f, 0.7f, 1f, 0.25f),
											 Vector4.zero,
											 Vector4.zero);
					}
					else
					{
						GUI.DrawTexture(topDropRect.AlignTop(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
											 OdinLocalizationGUITextures.TopToBottomFade,
											 ScaleMode.StretchToFill,
											 true,
											 1.0f,
											 new Color(0.8f, 0.8f, 1, 0.7f),
											 Vector4.zero,
											 Vector4.zero);
					}
					//EditorGUI.DrawRect(topDropRect.AlignTop(1), new Color(0, 1, 1, 0.5f));
				}

				if (DragAndDropUtilities.HoveringAcceptedDropZone == bottomId)
				{
					if (EditorGUIUtility.isProSkin)
					{
						GUI.DrawTexture(bottomDropRect.AlignBottom(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
											 OdinLocalizationGUITextures.BottomToTopFade,
											 ScaleMode.StretchToFill,
											 true,
											 1.0f,
											 new Color(0.16f, 0.7f, 1f, 0.25f),
											 Vector4.zero,
											 Vector4.zero);
					}
					else
					{
						GUI.DrawTexture(bottomDropRect.AlignBottom(40.0f).SubXMin(OdinLocalizationConstants.DRAG_HANDLE_WIDTH),
											 OdinLocalizationGUITextures.BottomToTopFade,
											 ScaleMode.StretchToFill,
											 true,
											 1.0f,
											 new Color(0.8f, 0.8f, 1, 0.7f),
											 Vector4.zero,
											 Vector4.zero);
					}
					//EditorGUI.DrawRect(bottomDropRect.AlignBottom(1), new Color(0, 1, 1, 0.5f));
				}
			}

			if (!topValue.IsNone)
			{
				this.SharedEntries.MoveEntry(topValue.Index, indexTo);

				this.HasGUIChanged = true;
				
				return;
			}

			if (!bottomValue.IsNone)
			{
				this.SharedEntries.MoveEntry(bottomValue.Index, indexTo + 1);

				this.HasGUIChanged = true;
			}
		}

		protected void MoveScrollPositionToTable(OdinGUITable<TTable> table)
		{
			var x = 0.0f;

#if USING_WIDTH_NON_PERCENT
			for (var i = 0; i < this.GUITables.Count; i++)
			{
				if (this.GUITables[i] == table)
				{
					x += this.GUITables[i].Width * 0.5f;
					break;
				}

				if (!this.GUITables[i].IsVisible || this.GUITables[i].IsPinned)
				{
					continue;
				}

				x += this.GUITables[i].Width;
			}
#else
			for (var i = 0; i < this.GUITables.Count; i++)
			{
				if (this.GUITables[i] == table)
				{
					x += this.GUITables[i].Width * 0.5f;
					break;
				}

				if (!this.GUITables[i].IsVisible || this.GUITables[i].IsPinned)
				{
					continue;
				}

				x += this.GUITables[i].Width;
			}
#endif

			x -= this.EntryScrollView.Bounds.width * 0.5f;

			x += this.PinnedWidth * 0.5f;

			this.EntryScrollView.ScrollTo(1.0f / 0.35f, xPosition: x, easing: Easing.OutQuad);
		}

		private float rightMenuTopPanelHeight;
		private Rect topPanelRect = Rect.zero;
		private Rect bottomPanelRect = Rect.zero;

		private void DrawRightMenu(Rect position)
		{
			EditorGUI.DrawRect(position, OdinLocalizationGUI.WindowBackground);

			var topPanelMaxHeight = position.height - 32;
			this.topPanelRect = position.TakeFromTop(this.WindowState.RightMenuTopPanelHeight);
			var topSlideRect = this.topPanelRect.TakeFromBottom(14);
			this.bottomPanelRect = position;

			this.WindowState.RightMenuTopPanelHeight += this.HorizontalSlideRect(topSlideRect);
			// 183 is enough height to show exactly 3 collapsed entries.
			this.WindowState.RightMenuTopPanelHeight = Mathf.Clamp(this.WindowState.RightMenuTopPanelHeight, 183, topPanelMaxHeight);

			EditorGUI.DrawRect(this.topPanelRect, OdinLocalizationGUI.Panel);
			EditorGUI.DrawRect(this.bottomPanelRect, OdinLocalizationGUI.Panel);


			EditorGUI.DrawRect(this.topPanelRect.AlignTop(32), OdinLocalizationGUI.TabsBackground);
			EditorGUI.DrawRect(this.bottomPanelRect.AlignTop(32), OdinLocalizationGUI.TabsBackground);

			this.WindowState.CurrentTopTab = OdinLocalizationGUI.Tabs(this.topPanelRect.TakeFromTop(32), this.WindowState.CurrentTopTab, 115);

			this.WindowState.CurrentBottomTab = OdinLocalizationGUI.Tabs(this.bottomPanelRect.TakeFromTop(32), this.WindowState.CurrentBottomTab, 115);

			switch (this.WindowState.CurrentTopTab)
			{
				case OdinLocalizationEditorWindow.RightMenuTopTabs.Metadata:
					this.DrawTopTabMetadata(this.topPanelRect);
					break;

				case OdinLocalizationEditorWindow.RightMenuTopTabs.Settings:
					this.DrawTopTabSettings(this.topPanelRect);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			switch (this.WindowState.CurrentBottomTab)
			{
				case OdinLocalizationEditorWindow.RightMenuBottomTabs.Locale:
					this.DrawBottomTabLocale(this.bottomPanelRect);
					break;

#if false
				case OdinLocalizationEditorWindow.RightMenuBottomTabs.Template:
					this.DrawBottomTabTemplate(this.bottomPanelRect);
					break;
#endif

				case OdinLocalizationEditorWindow.RightMenuBottomTabs.Settings:
					this.DrawBottomTabSettings(this.bottomPanelRect);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				this.dragging = false;
				GUIHelper.RequestRepaint();
			}
		}

		private string metadataSearchTerm = string.Empty;
		private SearchField metadataSearchField = new SearchField();
		
		public InspectorProperty[] GetMetadataProperties()
		{
			InspectorProperty metadataCollection =
				this.WindowState?.MetadataTree?.RootProperty?.Children[
					OdinLocalizationReflectionValues.TABLE_ENTRY_DATA__METADATA__PATH];

			InspectorProperty items =
				metadataCollection?.Children[OdinLocalizationReflectionValues.METADATA_COLLECTION__ITEMS__PATH];

			return items?.Children.OrderBy(c => c.ValueEntry.TypeOfValue.Name).ToArray();
		}

		private LocalizationMetadata localizationMetadata;

		private void DrawTopTabMetadata(Rect rect)
		{
			if (Event.current.OnMouseDown(rect, 0, false))
			{
				GUIHelper.RemoveFocusControl();
			}

			if (this.SelectionType == OdinTableSelectionType.None)
			{
				return;
			}
			
			if (this.localizationMetadata == null)
			{
				this.localizationMetadata = new LocalizationMetadata(this.Collection, this.WindowState);
			}

			switch (this.SelectionType)
			{
				case OdinTableSelectionType.None:
					break;
				case OdinTableSelectionType.SharedEntry:
					this.localizationMetadata.Target = this.CurrentSelectedSharedEntry;
					break;
				case OdinTableSelectionType.SharedTable:
					this.localizationMetadata.Target = this.Collection;
					break;
				case OdinTableSelectionType.Table:
					this.localizationMetadata.Target = this.CurrentSelectedTable.Asset;
					break;
				case OdinTableSelectionType.TableEntry:
					this.localizationMetadata.Target = this.CurrentSelectedEntry;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			this.localizationMetadata.Draw(rect);
		}

		private void DrawTopTabSettings(Rect position)
		{
			position = position.Padding(4);

			GUILayout.BeginArea(position);
			{
				bool value;

				switch (this.SelectionType)
				{
					case OdinTableSelectionType.Table:
						EditorGUI.BeginChangeCheck();

						value = GUILayout.Toggle(LocalizationEditorSettings.GetPreloadTableFlag(this.CurrentSelectedTable.Asset), "Preload Table");

						if (EditorGUI.EndChangeCheck())
						{
							LocalizationEditorSettings.SetPreloadTableFlag(this.CurrentSelectedTable.Asset, value, true);
						}

						break;

					case OdinTableSelectionType.TableEntry:
						if (this.CurrentSelectedEntry is StringTableEntry stringTableEntry)
						{
							EditorGUI.BeginChangeCheck();

							value = GUILayout.Toggle(stringTableEntry.IsSmart, "Smart");

							if (EditorGUI.EndChangeCheck())
							{
								stringTableEntry.IsSmart = value;
								EditorUtility.SetDirty(stringTableEntry.Table);
							}

							break;
						}

						goto default;

					default:
						GUILayout.Label("No item with settings selected.", SirenixGUIStyles.LabelCentered);
						break;
				}
			}
			GUILayout.EndArea();
		}

		private Vector2 bottomTabLocaleScrollPosition = Vector2.zero;

		private List<Toggle> toggles;
		private static FancyColor ThumbColorGrayscale = FancyColor.White;
		private static FancyColor BackgroundColorGrayscale = FancyColor.Gray;
		private static FancyColor BorderColorGrayscale = new FancyColor(0.91f, 0.91f, 0.91f);
		private static readonly FancyColor EnabledColor = new FancyColor(EditorGUIUtility.isProSkin ? 0.66f : 0.86f);
		private static readonly FancyColor DisabledColor = new FancyColor(EditorGUIUtility.isProSkin ? 0.46f : 0.66f);
		private bool dragging;
		private Toggle lastChangedToggle;
		private bool newValue;
		private Vector2 localeTabScrollPosition;
		private void DrawBottomTabLocale(Rect position)
		{
			position = position.Padding(4);

			GUILayout.BeginArea(position);
			{
				ReadOnlyCollection<Locale> projectLocales = LocalizationEditorSettings.GetLocales();

				if (this.toggles == null)
				{
					this.toggles = new List<Toggle>
					{
						new Toggle
						{
							Label = "Key",
							Toggled = this.KeyTable.IsVisible,
						}
					};
					this.toggles.AddRange(projectLocales.Select(locale =>
					{
						this.LocaleTableMap.TryGetValue(locale, out var table);

						return new Toggle
						{
							Label = locale.LocaleName,
							Toggled = table?.IsVisible ?? false,
						};
					}));
				}

				bool hasAllLocales = projectLocales.Count == this.LocaleTableMap.Count;

				if (!hasAllLocales)
				{
					if (SirenixEditorGUI.Button("Add Missing Locales", ButtonSizes.Large))
					{
						for (var i = 0; i < projectLocales.Count; i++)
						{
							Locale locale = projectLocales[i];

							if (this.LocaleTableMap.ContainsKey(locale))
							{
								continue;
							}

							LocalizationTable table = this.Collection.GetTable(locale.Identifier);

							if (table != null)
							{
								this.Collection.AddTable(table, true);
							}
							else
							{
								this.Collection.AddNewTable(locale.Identifier);
							}
						}
					}

					GUILayout.Space(4);
					SirenixEditorGUI.HorizontalLineSeparator();
					GUILayout.Space(4);
				}

				const float LINE_HEIGHT = 20.0f;
				const float LOCALE_SPACING = 2.0f;

				this.localeTabScrollPosition = GUILayout.BeginScrollView(this.localeTabScrollPosition /*, GUILayoutOptions.MaxHeight(MAX_HEIGHT)*/);
				{
					var keyRect = GUILayoutUtility.GetRect(0, LINE_HEIGHT, GUILayoutOptions.ExpandWidth());
					this.KeyTable.IsVisible = this.DrawLocaleToggle(ref keyRect, this.toggles[0], this.KeyTable);

					if (this.EntryScrollView.IsBeyondHorizontalBounds && this.KeyTable.IsVisible &&
					    !this.KeyTable.IsPinned)
					{
						if (Event.current.OnMouseDown(keyRect, 0))
						{
							this.MoveScrollPositionToTable(this.KeyTable);
						}
					}

					var lastLocaleIndex = projectLocales.Count - 1;

					for (var i = 0; i < projectLocales.Count; i++)
					{
						var locale = projectLocales[i];
						var toggle = this.toggles[i + 1];

						var totalRect = GUILayoutUtility.GetRect(
							width: 0,
							height: LINE_HEIGHT,
							options: GUILayoutOptions.ExpandWidth().ExpandHeight(false));

						if (locale != null && !this.LocaleTableMap.ContainsKey(locale))
						{
							GUIHelper.PushGUIEnabled(false);
							{
								this.DrawLocaleToggle(ref totalRect, toggle, null);
							}
							GUIHelper.PopGUIEnabled();

							LocalizationTable looseTable = null;

							foreach (LocalizationTable localizationTable in this.LooseTables)
							{
								if (localizationTable.LocaleIdentifier == locale.Identifier)
								{
									looseTable = localizationTable;
									break;
								}
							}

							var buttonRect = totalRect.TakeFromRight(80);

							if (looseTable != null)
							{
								if (GUI.Button(buttonRect, "Add"))
								{
									this.Collection.AddTable(looseTable, createUndo: false);
									Undo.ClearUndo(looseTable);
									Undo.ClearUndo(this.Collection);
									FancyColor.PopBlend();
									GUIHelper.ExitGUI(false);
								}
							}
							else
							{
								if (GUI.Button(buttonRect, "Create"))
								{
									this.Collection.AddNewTable(locale.Identifier);
									Undo.ClearUndo(this.Collection);
									FancyColor.PopBlend();
									GUIHelper.ExitGUI(false);
								}
							}

							if (i != lastLocaleIndex)
							{
								GUILayout.Space(LOCALE_SPACING);
							}

							continue;
						}


						OdinGUITable<TTable> table = locale == null ? this.KeyTable : this.LocaleTableMap[locale];

						table.IsVisible = this.DrawLocaleToggle(ref totalRect, toggle, table);

						if (table.Type != OdinGUITable<TTable>.GUITableType.Key)
						{
							Rect removeLocaleRect = totalRect.TakeFromRight(80);

							if (GUI.Button(removeLocaleRect, "Remove"))
							{
								if (EditorUtility.DisplayDialog("Odin Localization Editor",
																		  $"Are you sure you want to remove the locale '{locale.Identifier.CultureInfo.EnglishName}' from '{this.Collection.name}'?\n" +
																		  "This can have side effects that can't be undone.",
																		  "Yes",
																		  "No"))
								{
									this.Collection.RemoveTable(table.Asset, createUndo: false);
									Undo.ClearUndo(table.Asset);
									Undo.ClearUndo(this.Collection);
									FancyColor.PopBlend();
									GUIHelper.ExitGUI(false);
								}
							}
						}

						if (this.EntryScrollView.IsBeyondHorizontalBounds && table.IsVisible && !table.IsPinned)
						{
							if (Event.current.OnMouseDown(totalRect, 0))
							{
								this.MoveScrollPositionToTable(table);
							}
						}
				 
						if (i != lastLocaleIndex)
						{
							GUILayout.Space(LOCALE_SPACING);
						}
					}
				}
				GUILayout.EndScrollView();

				GUILayout.Space(4);
				SirenixEditorGUI.HorizontalLineSeparator();
				GUILayout.Space(4);

				if (SirenixEditorGUI.Button("Manage Locales", ButtonSizes.Medium))
				{
					try
					{
						TwoWaySerializationBinder.Default.BindToType("UnityEditor.Localization.UI.LocaleGeneratorWindow, Unity.Localization.Editor")?
							.GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
					}
					catch (NullReferenceException nullReferenceException)
					{
						Debug.LogError($"[Odin]: Failed to find LocaleGeneratorWindow.ShowWindow.\n{nullReferenceException.Message}");
					}
				}
			}
			GUILayout.EndArea();
		}

		private bool DrawLocaleToggle(ref Rect rect, Toggle toggle, OdinGUITable<TTable> table)
		{
			const int toggleWidth = 35;

			var toggleRect = rect.TakeFromLeft(toggleWidth).SubXMax(4).VerticalPadding(2).AddY(1);
			var color = GUI.enabled ? toggle.CurrentColor : new Color(0.35f, 0.35f, 0.35f);
			toggle.Enabled = GUI.enabled;

			// Draw toggle background
			GUI.DrawTexture(
				position: toggleRect,
				image: Texture2D.whiteTexture,
				scaleMode: ScaleMode.StretchToFill,
				alphaBlend: false,
				imageAspect: 1f,
				color: BackgroundColorGrayscale.Blend(color, FancyColor.BlendMode.Multiply),
				borderWidth: 0,
				borderRadius: float.MaxValue);

			// Draw toggle thumb
			GUI.DrawTexture(
				position: toggle.CurrentThumbRect,
				image: Texture2D.whiteTexture,
				scaleMode: ScaleMode.StretchToFill,
				alphaBlend: false,
				imageAspect: 1f,
				color: ThumbColorGrayscale.Blend(color, FancyColor.BlendMode.Multiply),
				borderWidth: 0,
				borderRadius: float.MaxValue);

			AnimateThumb(toggleRect, toggle);

			GUI.Label(rect, toggle.Label,
				GUI.enabled && Event.current.IsMouseOver(rect) ? SirenixGUIStyles.WhiteLabel : SirenixGUIStyles.Label);

			if (GUI.enabled && Event.current.OnMouseDown(toggleRect, 0))
			{
				this.dragging = true;
				this.lastChangedToggle = toggle;
				this.newValue = !toggle.Toggled;
				toggle.Toggled = this.newValue;

				switch (Event.current.modifiers)
				{
					case EventModifiers.Control:
						for (var i = 0; i < this.toggles.Count; i++)
						{
							if (!this.toggles[i].Enabled) continue;
							this.toggles[i].Toggled = this.newValue;
						}

						break;
					case EventModifiers.Shift:
						for (var i = 0; i < this.toggles.Count; i++)
						{
							if (!this.toggles[i].Enabled) continue;
							this.toggles[i].Toggled = table.IsVisible;
						}

						break;
					case EventModifiers.Alt:
						for (var i = 0; i < this.toggles.Count; i++)
						{
							if (!this.toggles[i].Enabled) continue;
							this.toggles[i].Toggled = this.toggles[i] == toggle;
						}

						break;
				}
			}

			if (GUI.enabled && this.dragging)
			{
				var mp = Event.current.mousePosition;
				if (toggle != this.lastChangedToggle && toggleRect.y < mp.y && toggleRect.yMax > mp.y)
				{
					this.lastChangedToggle = toggle;
					toggle.Toggled = this.newValue;
				}

				GUIHelper.RequestRepaint();
			}

			if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
			{
				this.dragging = false;
				GUIHelper.RequestRepaint();
			}

			return toggle.Toggled;
		}

		private static void AnimateThumb(Rect toggleRect, Toggle toggle)
		{
			const float thumbAnimationDurationInSeconds = 0.07f;
			const float thumbAnimationSpeed = 1f / (thumbAnimationDurationInSeconds / 2f); // divided by 2 since the animation is split into 2 phases.
			const float colorAnimationDurationInSeconds = 0.6f;
			const float colorAnimationSpeed = 1f / colorAnimationDurationInSeconds;

			var targetRect = toggle.Toggled
				? toggleRect.AlignRight(toggleRect.height).AlignCenterY(toggleRect.height).Padding(2)
				: toggleRect.AlignLeft(toggleRect.height).AlignCenterY(toggleRect.height).Padding(2);

			var targetColor = toggle.Toggled ? EnabledColor : DisabledColor;

			if (toggle.CurrentColor != (Color)targetColor)
			{
				if (toggle.T1.IsDone)
				{
					toggle.T1.Reset(0f);
				}

				toggle.T1.Move(colorAnimationSpeed, Easing.InOutExpo);
				toggle.CurrentColor = Color.Lerp(toggle.StartColor, targetColor, toggle.T1.GetValue());
			}

			if (Event.current.type == EventType.Repaint && toggle.CurrentThumbRect == Rect.zero)
			{
				toggle.CurrentThumbRect = targetRect;
				toggle.CurrentColor = targetColor;
				GUIHelper.RequestRepaint();
			}

			if (Event.current.type == EventType.Repaint && toggle.CurrentThumbRect != targetRect)
			{
				if (toggle.Toggled)
				{
					if (toggle.CurrentThumbRect.xMax < targetRect.xMax)
					{
						if (toggle.T2.IsDone)
						{
							toggle.T2.Reset(0f);
						}

						toggle.T2.Move(thumbAnimationSpeed);
						var xMax = Mathf.Lerp(toggle.StartXMax, targetRect.xMax, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMax = xMax;
					}
					else if (toggle.CurrentThumbRect.xMin < targetRect.xMin)
					{
						if (toggle.T2.IsDone)
						{
							toggle.T2.Reset(0f);
						}

						toggle.T2.Move(thumbAnimationSpeed);
						var xMin = Mathf.Lerp(toggle.StartXMin, targetRect.xMin, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMin = xMin;
					}
				}
				else
				{
					if (toggle.CurrentThumbRect.xMin > targetRect.xMin)
					{
						if (toggle.T2.IsDone)
						{
							toggle.T2.Reset(0f);
						}

						toggle.T2.Move(thumbAnimationSpeed);
						var xMin = Mathf.Lerp(toggle.StartXMin, targetRect.xMin, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMin = xMin;
					}
					else if (toggle.CurrentThumbRect.xMax > targetRect.xMax)
					{
						if (toggle.T2.IsDone)
						{
							toggle.T2.Reset(0f);
						}

						toggle.T2.Move(thumbAnimationSpeed);
						var xMax = Mathf.Lerp(toggle.StartXMax, targetRect.xMax, toggle.T2.GetValue());
						toggle.CurrentThumbRect.xMax = xMax;
					}
				}

				GUIHelper.RequestRepaint();
			}
		}

		private void LocaleToggle(Rect position, Locale locale)
		{
			if (locale != null && !this.LocaleTableMap.ContainsKey(locale))
			{
				Rect createLocaleRect = position.TakeFromRight(80);

				GUIHelper.PushGUIEnabled(false);
				{
					GUI.Toggle(position.TakeFromLeft(GUI.skin.toggle.padding.left), false, GUIContent.none);

					GUI.Label(position, locale.LocaleName, SirenixGUIStyles.Label);
				}
				GUIHelper.PopGUIEnabled();

				LocalizationTable looseTable = null;

				foreach (LocalizationTable localizationTable in this.LooseTables)
				{
					if (localizationTable.LocaleIdentifier == locale.Identifier)
					{
						looseTable = localizationTable;
						break;
					}
				}

				if (looseTable != null)
				{
					if (GUI.Button(createLocaleRect, "Add"))
					{
						this.Collection.AddTable(looseTable, createUndo: true);
						FancyColor.PopBlend();
						GUIHelper.ExitGUI(false);
					}
				}
				else
				{
					if (GUI.Button(createLocaleRect, "Create"))
					{
						this.Collection.AddNewTable(locale.Identifier);
						FancyColor.PopBlend();
						GUIHelper.ExitGUI(false);
					}
				}

				return;
			}

			OdinGUITable<TTable> table = locale == null ? this.KeyTable : this.LocaleTableMap[locale];

			EditorGUI.BeginChangeCheck();
			{
				table.IsVisible = GUI.Toggle(position.TakeFromLeft(GUI.skin.toggle.padding.left), table.IsVisible, GUIContent.none);
			}
			if (EditorGUI.EndChangeCheck())
			{
				switch (Event.current.modifiers)
				{
					case EventModifiers.Shift:
						for (var i = 0; i < this.GUITables.Count; i++)
						{
							this.GUITables[i].IsVisible = table.IsVisible;
						}

						break;

					case EventModifiers.Alt:
						for (var i = 0; i < this.GUITables.Count; i++)
						{
							this.GUITables[i].IsVisible = this.GUITables[i] == table;
						}

						break;
				}
			}

			if (table.Type != OdinGUITable<TTable>.GUITableType.Key)
			{
				Rect removeLocaleRect = position.TakeFromRight(80);

				if (GUI.Button(removeLocaleRect, "Remove"))
				{
					this.Collection.RemoveTable(table.Asset, createUndo: true);
					FancyColor.PopBlend();
					GUIHelper.ExitGUI(false);
				}
			}

			if (this.EntryScrollView.IsBeyondHorizontalBounds && table.IsVisible && !table.IsPinned)
			{
				bool isMouseOver = Event.current.IsMouseOver(position);

				GUI.Label(position, table.DisplayName, isMouseOver ? SirenixGUIStyles.WhiteLabel : SirenixGUIStyles.Label);

				if (Event.current.OnMouseDown(position, 0))
				{
					this.MoveScrollPositionToTable(table);
				}
			}
			else
			{
				GUI.Label(position, table.DisplayName, SirenixGUIStyles.Label);
			}
		}

		protected bool IsDraggingAnything()
		{
			if (this.EntryScrollView.IsDraggingMouse ||
				 this.EntryScrollView.IsDraggingHorizontalScrollBar ||
				 this.EntryScrollView.IsDraggingVerticalScrollBar)
			{
				return true;
			}

			for (var i = 0; i < this.GUITables.Count; i++)
			{
				if (this.GUITables[i].IsDraggingSlider)
				{
					return true;
				}
			}

			return false;
		}

#if false
		private void DrawBottomTabTemplate(Rect position)
		{
			if (!this.Collection.SharedData.Metadata.HasMetadata<OdinTemplateMetadata>())
			{
				this.Collection.SharedData.Metadata.AddMetadata(new OdinTemplateMetadata());
				EditorUtility.SetDirty(this.Collection.SharedData);
			}

			var templateMetadata = this.Collection.SharedData.Metadata.GetMetadata<OdinTemplateMetadata>();
			
			GUILayout.BeginArea(position);
			{
				GUILayout.BeginScrollView(Vector2.zero);

				int removedItemIndex = -1;

				for (var i = 0; i < templateMetadata.MetadataExpected.Count; i++)
				{
					if (OdinLocalizationStyles.Metadata(templateMetadata.MetadataExpected[i], i == 0))
					{
						removedItemIndex = i;
					}
				}

				GUILayout.EndScrollView();

				if (removedItemIndex != -1)
				{
					templateMetadata.MetadataExpected.RemoveAt(removedItemIndex);
					EditorUtility.SetDirty(this.Collection.SharedData);
				}

				Rect addMetadataRect = GUILayoutUtility.GetRect(0, (int) ButtonSizes.Large);

				if (GUI.Button(addMetadataRect, "Add Metadata"))
				{
					this.ShowAddMetadataTemplateSelector(addMetadataRect, templateMetadata);
				}

				GUILayoutUtility.GetRect(0, 5);
			}
			GUILayout.EndArea();
		}

		private void ShowAddMetadataTemplateSelector(Rect rect, OdinTemplateMetadata templateMetadata)
		{
			TypeSelector selector = this.MakeMetadataSelector();

			selector.SelectionConfirmed += types =>
			{
				foreach (Type type in types)
				{
					if (templateMetadata.MetadataExpected.Contains(type) && !OdinLocalizationMetadataRegistry.MetadataAllowsMultiple[type])
					{
						continue;
					}

					templateMetadata.MetadataExpected.Add(type);

					EditorUtility.SetDirty(this.Collection.SharedData);
				}
			};

			selector.ShowInPopup(rect);
		}

		private TypeSelector MakeMetadataSelector()
		{
			TypeSelector selector;

			switch (this.Collection)
			{
				case AssetTableCollection _:
					selector = new TypeSelector(OdinLocalizationMetadataRegistry.AssetEntryMetadataTypes, excludeInheritors: true);
					break;

				case StringTableCollection _:
					selector = new TypeSelector(OdinLocalizationMetadataRegistry.StringEntryMetadataTypes, excludeInheritors: true);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return selector;
		}

		protected bool HasMetadataAmountOfType(IList<IMetadata> metadata, Type metadataType, OdinTemplateMetadata templateMetadata)
		{
			int count = CountMetadataType(metadata, metadataType);

			var expectedCount = 0;

			for (var i = 0; i < templateMetadata.MetadataExpected.Count; i++)
			{
				if (templateMetadata.MetadataExpected[i] == metadataType)
				{
					expectedCount++;
				}
			}

			return count >= expectedCount;
		}

		protected static int CountMetadataType(IList<IMetadata> metadata, Type metadataType)
		{
			var result = 0;

			for (var i = 0; i < metadata.Count; i++)
			{
				if (metadata[i].GetType() == metadataType)
				{
					result++;
				}
			}

			return result;
		}
#endif


		private void DrawBottomTabSettings(Rect position)
		{
			position = position.Padding(6);

			GUILayout.BeginArea(position);
			{
				// Table Collection Name
				{
					Rect namePosition = EditorGUILayout.GetControlRect();

					GUI.Label(namePosition.TakeFromLeft(130), "Collection Name");

					EditorGUI.BeginChangeCheck();

					string value = SirenixEditorFields.DelayedTextField(namePosition, this.Collection.SharedData.TableCollectionName);

					if (EditorGUI.EndChangeCheck())
					{
						if (!string.IsNullOrEmpty(value) && OdinLocalizationEditorSettings.IsTableNameValid(this.Collection.GetType(), value))
						{
							this.Collection.SetTableCollectionName(value, true);
							this.MenuItem.Name = this.Collection.SharedData.TableCollectionName;
							this.MenuItem.Select();
						}
					}
				}

				// Preload All Tables
				{
					EditorGUI.BeginChangeCheck();

					bool value = GUILayout.Toggle(this.Collection.IsPreloadTableFlagSet(), "Preload All Tables");

					if (EditorGUI.EndChangeCheck())
					{
						this.Collection.SetPreloadTableFlag(value, true);
					}
				}

				GUILayout.Space(4);
				SirenixEditorGUI.HorizontalLineSeparator();
				GUILayout.Space(4);

				if (SirenixEditorGUI.Button("Manage Collection", ButtonSizes.Large))
				{
					GUIHelper.OpenInspectorWindow(this.Collection);
				}
			}
			GUILayout.EndArea();
		}

		private float VerticalSlideRect(Rect rect, bool connect)
		{
			var offset = SirenixEditorGUI.SlideRect(rect, MouseCursor.SplitResizeLeftRight).x;

			var slideThumbColor = Event.current.IsMouseOver(rect)
				? EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 1f, 1f)
				: this.WindowState.RightMenuWidth > 0
					? EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.8f, 0.8f, 0.8f)
					: EditorGUIUtility.isProSkin
						? new Color(0.4f, 0.4f, 0.4f)
						: new Color(1f, 1f, 1f);

			EditorGUI.DrawRect(rect, Event.current.IsMouseOver(rect)
				? EditorGUIUtility.isProSkin ? new Color(0.26f, 0.26f, 0.26f) : new Color(0.7f, 0.7f, 0.7f)
				: EditorGUIUtility.isProSkin
					? new Color(0.2f, 0.2f, 0.2f)
					: new Color(0.6f, 0.6f, 0.6f));

			var h2 = connect ? this.WindowState.RightMenuTopPanelHeight : rect.height / 2f - 40;

			EditorGUI.DrawRect(rect.AlignLeft(1), new Color(0, 0, 0, 0.4f));
			var left = new Rect(rect.center.x - 1, 0, 1, rect.height);
			EditorGUI.DrawRect(left, slideThumbColor);

			if (!connect)
			{
				var right = new Rect(rect.center.x + 1, rect.y, 1, rect.height);
				EditorGUI.DrawRect(right, slideThumbColor);
				EditorGUI.DrawRect(rect.AlignRight(1), new Color(0, 0, 0, 0.4f));
			}
			else if (this.WindowState.RightMenuWidth > 0)
			{
				var crossTop = new Rect(rect.AlignCenterX(1).AddX(2).x, this.WindowState.RightMenuTopPanelHeight - (14 / 2 + 1), 4, 1);
				var crossBottom = crossTop.AddY(2);
				var rightTop = new Rect(rect.center.x + 1, 0, 1, crossTop.y + 1);
				var rightBottom = new Rect(rect.center.x + 1, crossBottom.y, 1, rect.height - crossBottom.y);
				EditorGUI.DrawRect(crossTop, slideThumbColor);
				EditorGUI.DrawRect(crossBottom, slideThumbColor);
				EditorGUI.DrawRect(rightTop, slideThumbColor);
				EditorGUI.DrawRect(rightBottom, slideThumbColor);
				EditorGUI.DrawRect(rect.AlignRight(1).SetHeight(connect ? h2 - 13 : rect.height), new Color(0, 0, 0, 0.4f));
				EditorGUI.DrawRect(rect.AlignRight(1).AddY(connect ? h2 - 1 : 0), new Color(0, 0, 0, 0.4f));
			}
			else
			{
				var right = new Rect(rect.center.x + 1, 0, 1, rect.height);
				EditorGUI.DrawRect(right, slideThumbColor);
				EditorGUI.DrawRect(rect.AlignRight(1), new Color(0, 0, 0, 0.4f));
			}

			return offset;
		}

		private float HorizontalSlideRect(Rect rect)
		{
			var offset = SirenixEditorGUI.SlideRect(rect, MouseCursor.SplitResizeUpDown).y;

			var slideThumbColor = Event.current.IsMouseOver(rect)
				? EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 1f, 1f)
				: this.WindowState.RightMenuWidth > 0
					? EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.8f, 0.8f, 0.8f)
					: EditorGUIUtility.isProSkin
						? new Color(0.4f, 0.4f, 0.4f)
						: new Color(1f, 1f, 1f);

			EditorGUI.DrawRect(rect, Event.current.IsMouseOver(rect)
				? EditorGUIUtility.isProSkin ? new Color(0.26f, 0.26f, 0.26f) : new Color(0.7f, 0.7f, 0.7f)
				: EditorGUIUtility.isProSkin
					? new Color(0.2f, 0.2f, 0.2f)
					: new Color(0.6f, 0.6f, 0.6f));

			var top = new Rect(rect.x, rect.center.y - 1, rect.width, 1);
			var bottom = new Rect(rect.x, rect.center.y + 1, rect.width, 1);

			EditorGUI.DrawRect(top, slideThumbColor);
			EditorGUI.DrawRect(bottom, slideThumbColor);
			EditorGUI.DrawRect(rect.AlignTop(1), new Color(0, 0, 0, 0.4f));
			EditorGUI.DrawRect(rect.AlignBottom(1), new Color(0, 0, 0, 0.4f));

			return offset;
		}
	}
}