//-----------------------------------------------------------------------
// <copyright file="OdinGUITableCollection.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define USING_WIDTH_NON_PERCENT
//#undef USING_WIDTH_NON_PERCENT

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor.Internal;
using UnityEditor;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinGUITableCollection<TTable> : List<OdinGUITable<TTable>> where TTable : LocalizationTable
	{
		public readonly struct VisibleTables
		{
			public readonly int Offset;
			public readonly int Length;
			public readonly int PinnedLength;
			public readonly float StartOffset;

			private readonly OdinGUITableCollection<TTable> collection;

			public VisibleTables(OdinGUITableCollection<TTable> collection, int offset, int length, int pinnedLength, float startOffset)
			{
				this.collection = collection;
				this.Offset = offset;
				this.Length = length + pinnedLength;
				this.PinnedLength = pinnedLength;
				this.StartOffset = startOffset;
			}

			public OdinGUITable<TTable> this[int index] =>
				index >= this.PinnedLength ? this.collection[this.Offset + index - this.PinnedLength] : this.collection[index];
		}

		//public VisibleTables CurrentVisibleTables;

		public HashSet<OdinGUITable<TTable>> TablesWithinVisibleBounds = new HashSet<OdinGUITable<TTable>>();
		
		public OdinGUITableCollection(int capacity) : base(capacity) { }

#if USING_WIDTH_NON_PERCENT
		public void AddKeyTable()
		{
			for (var i = 0; i < this.Count; i++)
			{
				if (this[i].Type == OdinGUITable<TTable>.GUITableType.Key)
				{
					return;
				}
			}

			this.Add(OdinGUITable<TTable>.CreateKeyTable());
		}
#else
		public void AddKeyTable(float widthPercent)
		{
			for (var i = 0; i < this.Count; i++)
			{
				if (this[i].Type == OdinGUITable<TTable>.GUITableType.Key)
				{
					return;
				}
			}

			this.Add(OdinGUITable<TTable>.CreateKeyTable(widthPercent));
		}
#endif

#if USING_WIDTH_NON_PERCENT
		public void UpdateVisibleTables(OdinGUIScrollView view, float pinnedWidth)
		{
#if true
			this.TablesWithinVisibleBounds.Clear();

			var offset = 0;
			var currentVisibleWidth = 0.0f;
			var xMin = 0.0f;
			var xMax = 0.0f;

			for (var i = 0; i < this.Count; i++)
			{
				OdinGUITable<TTable> table = this[i];

				if (!table.IsVisible)
				{
					continue;
				}

				if (table.IsPinned)
				{
					this.TablesWithinVisibleBounds.Add(table);
					continue;
				}

				xMax += table.Width;

				bool isVisible = view.Position.x >= xMin && view.Position.x <= xMax;

				if (!isVisible)
				{
					xMin = xMax;
					continue;
				}

				offset = i;

				currentVisibleWidth = xMax - view.Position.x;
				this.TablesWithinVisibleBounds.Add(table);
				break;
			}

			float width = view.Bounds.width - pinnedWidth;

			for (int i = offset + 1; i < this.Count; i++)
			{
				OdinGUITable<TTable> table = this[i];

				if (!table.IsVisible)
				{
					break;
				}

				if (currentVisibleWidth >= width)
				{
					this.TablesWithinVisibleBounds.Add(table);
					break;
				}

				this.TablesWithinVisibleBounds.Add(table);
				currentVisibleWidth += this[i].Width;
			}
#else
			if (this.GetVisibleCount() < 1)
			{
				this.CurrentVisibleTables = new VisibleTables(this, 0, 0, 0, 0.0f);
				return;
			}

			const int LENGTH_NOT_SET = -1;

			var offset = 0;
			int length = LENGTH_NOT_SET;

			var currentVisibleWidth = 0.0f;

			float xMin = 0.0f, xMax = 0.0f;

			float positionX = view.Position.x;
			float width = view.Bounds.width - pinnedWidth;

			for (var i = 0; i < this.Count; i++)
			{
				OdinGUITable<TTable> table = this[i];

				if (table.IsPinned)
				{
					continue;
				}

				if (!table.IsVisible)
				{
					break;
				}

				xMax += table.VisualWidth;

				bool isVisible = positionX >= xMin && positionX <= xMax;

				if (!isVisible)
				{
					xMin = xMax;
					continue;
				}

				offset = i;

				currentVisibleWidth = xMax - positionX;
				break;
			}

			float startOffset = xMin;

			for (int i = offset + 1; i < this.Count; i++)
			{
				if (!this[i].IsVisible)
				{
					length = i - offset;
					break;
				}

				if (currentVisibleWidth >= width)
				{
					length = i - offset + 1;
					break;
				}

				currentVisibleWidth += this[i].VisualWidth;
			}

			int pinnedCount = this.GetPinnedCount();

			if (length == LENGTH_NOT_SET)
			{
				length = this.Count - offset;
			}

			this.CurrentVisibleTables = new VisibleTables(this, offset, length, pinnedCount, startOffset);
#endif
		}
		
		public float GetVisibleWidth()
		{
			var result = 0.0f;

			for (var i = 0; i < this.Count; i++)
			{
				if (this[i].IsVisible)
				{
					result += this[i].Width;
				}
			}

			return result;
		}

		public void ResizeBy(float factor)
		{
			for (var i = 0; i < this.Count; i++)
			{
				if (this[i].IsVisible)
				{
					this[i].Width *= factor;
				}
			}
		}
#else
		/// <summary> Calculates the <see cref="GUITable.Width"/> field on <see cref="GUITable"/> beforehand. </summary>
		/// <param name="scrollView">The <see cref="OdinGUIScrollView"/> to perform calculations on.</param>
		public void CalcWidths(OdinGUIScrollView scrollView)
		{
			float viewWidth = scrollView.ViewRect.width;

			float visibleFactor = this.GetVisibleFactor();

			for (var i = 0; i < this.Count; i++)
			{
				this[i].Width = viewWidth * (this[i].WidthPercentage * visibleFactor);
			}
		}
#endif

		public int GetLastVisibleIndex()
		{
			for (int i = this.Count - 1; i >= 0; i--)
			{
				if (this[i].IsVisible)
				{
					return i;
				}
			}

			return 0;
		}

		public int GetLastVisiblePinnedIndex()
		{
			for (int i = this.Count - 1; i >= 0; i--)
			{
				if (this[i].IsVisible && this[i].IsPinned)
				{
					return i;
				}
			}

			return 0;
		}

		public int GetVisibleCount()
		{
			var result = 0;

			for (var i = 0; i < this.Count; i++)
			{
				if (this[i].IsVisible)
				{
					result++;
				}
			}

			return result;
		}

		public int GetHiddenCount()
		{
			var result = 0;

			for (var i = 0; i < this.Count; i++)
			{
				if (!this[i].IsVisible)
				{
					result++;
				}
			}

			return result;
		}

		public int GetPinnedCount()
		{
			var result = 0;

			for (var i = 0; i < this.Count; i++)
			{
				if (this[i].IsVisible && this[i].IsPinned)
				{
					result++;
				}
			}

			return result;
		}

		public OdinGUITable<TTable> GetNextVisible(int index)
		{
			for (int i = index + 1; i < this.Count; i++)
			{
				if (this[i].IsVisible)
				{
					return this[i];
				}
			}

			return null;
		}

		public void UndoRecordCollection(SharedTableData sharedTableData, string name)
		{
			for (var i = 0; i < this.Count; i++)
			{
				switch (this[i].Type)
				{
					case OdinGUITable<TTable>.GUITableType.Default:
						Undo.RecordObject(this[i].Asset, name);
						break;

					case OdinGUITable<TTable>.GUITableType.Key:
						if (sharedTableData != null)
						{
							Undo.RecordObject(sharedTableData, name);
						}

						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public void SetDirty(SharedTableData sharedTableData)
		{
			for (var i = 0; i < this.Count; i++)
			{
				switch (this[i].Type)
				{
					case OdinGUITable<TTable>.GUITableType.Default:
						EditorUtility.SetDirty(this[i].Asset);
						break;

					case OdinGUITable<TTable>.GUITableType.Key:
						if (sharedTableData != null)
						{
							EditorUtility.SetDirty(sharedTableData);
						}

						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

#if !USING_WIDTH_NON_PERCENT
		private float GetVisibleFactor()
		{
			var visibleCount = 0;
			var sum = 0.0f;

			for (var i = 0; i < this.Count; i++)
			{
				if (!this[i].IsVisible)
				{
					continue;
				}

				sum += this[i].WidthPercentage;
				visibleCount++;
			}

			float result = (float) this.Count / visibleCount;

			sum *= result;

			result *= 1.0f / sum;

			return result;
		}
#endif
		public void ResizeToFit(float targetWidth)
		{
			var count = 0;

			for (var i = 0; i < this.Count; i++)
			{
				if (this[i].IsPinned || !this[i].IsVisible)
				{
					continue;
				}

				count++;
			}

			if (count < 1)
			{
				return;
			}

			int averageSize = OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH * count;

			float scaleFactor = targetWidth / averageSize;

			for (var i = 0; i < this.Count; i++)
			{
				if (!this[i].IsVisible || this[i].IsPinned)
				{
					continue;
				}

				this[i].Width = OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH * scaleFactor;
			}
		}

		public void ResizePinnedToFit(float targetWidth)
		{
			var currentTotalWidth = 0.0f;

			for (var i = 0; i < this.Count; i++)
			{
				if (!this[i].IsPinned || !this[i].IsVisible)
				{
					continue;
				}

				currentTotalWidth += this[i].Width;
			}

			if (currentTotalWidth < 1.0f)
			{
				return;
			}

			float scaleFactor = targetWidth / currentTotalWidth;

			for (var i = 0; i < this.Count; i++)
			{
				if (!this[i].IsVisible || !this[i].IsPinned)
				{
					continue;
				}

				this[i].Width *= scaleFactor;
			}
		}
	}
}