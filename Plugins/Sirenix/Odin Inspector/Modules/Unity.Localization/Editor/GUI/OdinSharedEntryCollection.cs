//-----------------------------------------------------------------------
// <copyright file="OdinSharedEntryCollection.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinSharedEntryCollection
	{
		private class StringComparer : IComparer<string>
		{
			public bool IsAscending = true;

			public int Compare(string self, string other)
			{
				if (string.IsNullOrEmpty(self) && string.IsNullOrEmpty(other))
				{
					return 0;
				}

				if (string.IsNullOrEmpty(self))
				{
					return this.IsAscending ? 1 : -1;
				}

				if (string.IsNullOrEmpty(other))
				{
					return this.IsAscending ? -1 : 1;
				}

				return string.Compare(self, other, StringComparison.InvariantCulture);
			}
		}
		
		public enum SortOrderState
		{
			Unsorted,
			Ascending,
			Descending
		}

		public bool IsSearching;

		public bool IsSorted => this.CurrentSortOrderState != SortOrderState.Unsorted;

		public int Length => this.Entries.Count;

		private string _searchTerm = string.Empty;

		private StringComparer stringComparer = new StringComparer();

		public string SearchTerm
		{
			get => this._searchTerm;
			private set
			{
				this._searchTerm = value;

				this.IsSearching = !string.IsNullOrEmpty(value);
			}
		}

		public List<SharedTableData.SharedTableEntry> Entries => this.IsSorted ? this.SortedEntries : this.Collection.SharedData.Entries;

		public List<SharedTableData.SharedTableEntry> SortedEntries;
		public readonly HashSet<SharedTableData.SharedTableEntry> FilteredEntries;

		public readonly LocalizationTableCollection Collection;

		public SortOrderState CurrentSortOrderState = SortOrderState.Unsorted;

		public OdinSharedEntryCollection(LocalizationTableCollection collection)
		{
			this.Collection = collection;

			this.FilteredEntries = new HashSet<SharedTableData.SharedTableEntry>();
		}

		public SharedTableData.SharedTableEntry this[int index] => this.Entries[index];

		public bool IsVisible(SharedTableData.SharedTableEntry sharedEntry)
		{
			return !this.IsSearching || (this.IsSearching && this.FilteredEntries.Contains(sharedEntry));
		}

		public bool UpdateSearchTerm<TTable>(string value,
														 OdinGUITableCollection<TTable> tables,
														 LocalizationTableCollection collection,
														 bool forceUpdate = false) where TTable : LocalizationTable
		{
			if (this.SearchTerm == value && !forceUpdate)
			{
				return false;
			}
			
			this.SearchTerm = value;

			if (string.IsNullOrEmpty(this.SearchTerm))
			{
				return true;
			}

			this.FilteredEntries.Clear();

			for (var i = 0; i < tables.Count; i++)
			{
				OdinGUITable<TTable> table = tables[i];

				switch (table.Type)
				{
					case OdinGUITable<TTable>.GUITableType.Default:
						switch (table.Asset)
						{
							case AssetTable assetTable:
								var assetCollection = collection as AssetTableCollection;

								for (var j = 0; j < this.Length; j++)
								{
									SharedTableData.SharedTableEntry sharedEntry = this[j];

									Type assetType = assetCollection.GetEntryAssetType(sharedEntry.Id);

									UnityEngine.Object asset = OdinLocalizationAssetCache.Get(sharedEntry, assetTable, assetType);

									if (asset == null)
									{
										continue;
									}

									if (FuzzySearch.Contains(this.SearchTerm, asset.name))
									{
										this.FilteredEntries.Add(sharedEntry);
									}
								}

								break;

							case StringTable stringTable:
								for (var j = 0; j < this.Length; j++)
								{
									SharedTableData.SharedTableEntry sharedEntry = this[j];

									StringTableEntry entry = stringTable.GetEntry(sharedEntry.Id);

									if (entry is null || string.IsNullOrEmpty(entry.Value))
									{
										continue;
									}

									if (FuzzySearch.Contains(this.SearchTerm, entry.Value))
									{
										this.FilteredEntries.Add(sharedEntry);
									}
								}

								break;
						}

						break;

					case OdinGUITable<TTable>.GUITableType.Key:
						for (var j = 0; j < this.Entries.Count; j++)
						{
							if (FuzzySearch.Contains(this.SearchTerm, this.Entries[j].Key))
							{
								this.FilteredEntries.Add(this.Entries[j]);
							}
						}

						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return true;
		}

		public void SortByKeys(bool preserveCurrentOrder)
		{
			switch (this.CurrentSortOrderState)
			{
				case SortOrderState.Ascending:
					this.stringComparer.IsAscending = true;
						
					if (preserveCurrentOrder)
					{
						List<SharedTableData.SharedTableEntry> result = this.Collection.SharedData.Entries.OrderBy(entry => entry.Key, this.stringComparer)
																							 .ThenBy(this.GetOrderIndex)
																							 .ToList();

						this.SortedEntries = result;
					}
					else
					{
						this.SortedEntries = this.Collection.SharedData.Entries.OrderBy(entry => entry.Key, this.stringComparer).ToList();
					}

					return;

				case SortOrderState.Descending:
					this.stringComparer.IsAscending = false;
						
					if (preserveCurrentOrder)
					{
						List<SharedTableData.SharedTableEntry> result = this.Collection.SharedData.Entries.OrderByDescending(entry => entry.Key, this.stringComparer)
																							 .ThenBy(this.GetOrderIndex)
																							 .ToList();

						this.SortedEntries = result;
					}
					else
					{
						this.SortedEntries = this.Collection.SharedData.Entries.OrderByDescending(entry => entry.Key, this.stringComparer).ToList();
					}

					return;
			}
		}

		public void SortByAssetTable(AssetTableCollection collection, AssetTable table, bool preserveCurrentOrder)
		{
			switch (this.CurrentSortOrderState)
			{
				case SortOrderState.Ascending:
					this.stringComparer.IsAscending = true;
					
					if (preserveCurrentOrder)
					{
						List<SharedTableData.SharedTableEntry> result = this.Collection.SharedData.Entries
																							 .OrderBy(entry => GetAssetNameFromEntry(entry, table, collection), this.stringComparer)
																							 .ThenBy(this.GetOrderIndex)
																							 .ToList();

						this.SortedEntries = result;
					}
					else
					{
						this.SortedEntries = this.Collection.SharedData.Entries.OrderBy(entry => GetAssetNameFromEntry(entry, table, collection), this.stringComparer)
														 .ToList();
					}

					return;

				case SortOrderState.Descending:
					this.stringComparer.IsAscending = false;
					
					if (preserveCurrentOrder)
					{
						List<SharedTableData.SharedTableEntry> result = this.Collection.SharedData.Entries
																							 .OrderByDescending(entry => GetAssetNameFromEntry(entry, table, collection),
																													  this.stringComparer)
																							 .ThenBy(this.GetOrderIndex)
																							 .ToList();

						this.SortedEntries = result;
					}
					else
					{
						this.SortedEntries = this.Entries.OrderByDescending(entry => GetAssetNameFromEntry(entry, table, collection), this.stringComparer).ToList();
					}

					return;
			}
		}

		public void SortByStringTable(StringTable table, bool preserveCurrentOrder)
		{
			switch (this.CurrentSortOrderState)
			{
				case SortOrderState.Ascending:
					this.stringComparer.IsAscending = true;
					
					if (preserveCurrentOrder)
					{
						List<SharedTableData.SharedTableEntry> result = this.Collection.SharedData.Entries
																							 .OrderBy(entry => GetStringFromEntry(entry, table), this.stringComparer)
																							 .ThenBy(this.GetOrderIndex)
																							 .ToList();

						this.SortedEntries = result;
					}
					else
					{
						this.SortedEntries = this.Collection.SharedData.Entries.OrderBy(entry => GetStringFromEntry(entry, table), this.stringComparer).ToList();
					}

					return;

				case SortOrderState.Descending:
					this.stringComparer.IsAscending = false;
					
					if (preserveCurrentOrder)
					{
						List<SharedTableData.SharedTableEntry> result = this.Collection.SharedData.Entries
																							 .OrderByDescending(entry => GetStringFromEntry(entry, table), this.stringComparer)
																							 .ThenBy(this.GetOrderIndex)
																							 .ToList();

						this.SortedEntries = result;
					}
					else
					{
						this.SortedEntries = this.Entries.OrderByDescending(entry => GetStringFromEntry(entry, table), this.stringComparer).ToList();
					}

					return;
			}
		}

		private static string GetStringFromEntry(SharedTableData.SharedTableEntry sharedEntry, StringTable table)
		{
			StringTableEntry entry = table.GetEntry(sharedEntry.Id);

			return entry?.Value;
		}

		private static string GetAssetNameFromEntry(SharedTableData.SharedTableEntry sharedEntry, AssetTable table, AssetTableCollection collection)
		{
			AssetTableEntry entry = table.GetEntry(sharedEntry.Id);

			if (entry == null || entry.IsEmpty)
			{
				return null;
			}

			Type type = collection.GetEntryAssetType(sharedEntry.Id);

			UnityEngine.Object asset = OdinLocalizationAssetCache.Get(entry.Guid, type);

			return asset == null ? null : asset.name;
		}

		public void MoveEntry(int from, int to)
		{
			if (from < 0 || from >= this.Entries.Count)
			{
				return;
			}

			if (to < 0 || to > this.Entries.Count)
			{
				return;
			}

			if (from == to)
			{
				return;
			}

			SharedTableData.SharedTableEntry fromEntry = this.Collection.SharedData.Entries[from];

			if (to > from)
			{
				to -= 1;
			}

			//to = afterTo ? to + 1 : to;

			this.Collection.SharedData.Entries.RemoveAt(from);

			this.Collection.SharedData.Entries.Insert(to, fromEntry);

			OdinLocalizationEvents.RaiseTableEntryModified(this.Collection.SharedData.Entries[from]);
			OdinLocalizationEvents.RaiseTableEntryModified(this.Collection.SharedData.Entries[to]);

			EditorUtility.SetDirty(this.Collection.SharedData);
		}

		public void GotoNextSortOrderState()
		{
			switch (this.CurrentSortOrderState)
			{
				case SortOrderState.Unsorted:
					this.CurrentSortOrderState = SortOrderState.Ascending;
					break;

				case SortOrderState.Ascending:
					this.CurrentSortOrderState = SortOrderState.Descending;
					break;

				case SortOrderState.Descending:
					this.CurrentSortOrderState = SortOrderState.Unsorted;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public int GetIndex(SharedTableData.SharedTableEntry sharedEntry)
		{
			for (var i = 0; i < this.Length; i++)
			{
				if (this[i].Id == sharedEntry.Id)
				{
					return i;
				}
			}

			return -1;
		}

		public int GetOrderIndex(SharedTableData.SharedTableEntry sharedEntry)
		{
			if (this.IsSorted && this.SortedEntries.Count == this.Length)
			{
				for (var i = 0; i < this.SortedEntries.Count; i++)
				{
					if (this.SortedEntries[i].Id == sharedEntry.Id)
					{
						return i;
					}
				}

				return -1;
			}

			for (var i = 0; i < this.Length; i++)
			{
				if (this[i].Id == sharedEntry.Id)
				{
					return i;
				}
			}

			return -1;
		}
	}
}
