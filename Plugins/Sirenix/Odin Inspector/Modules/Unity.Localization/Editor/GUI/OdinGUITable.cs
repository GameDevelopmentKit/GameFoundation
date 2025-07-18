//-----------------------------------------------------------------------
// <copyright file="OdinGUITable.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#define USING_WIDTH_NON_PERCENT
//#undef USING_WIDTH_NON_PERCENT

using System;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Serialization;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
	public class OdinGUITable<TTable> : IComparable<OdinGUITable<TTable>> where TTable : LocalizationTable
	{
		public const string KEY_DISPLAY_NAME = "Key";

		public enum GUITableType
		{
			Default,
			Key
		}

		public float Width
		{
			get => this._width;
			set
			{
				if (value < OdinLocalizationConstants.MIN_COLUMN_WIDTH)
				{
					this._width = OdinLocalizationConstants.MIN_COLUMN_WIDTH;
					return;
				}

				this._width = Mathf.Round(value);
			}
		}

		public bool IsDraggingSlider = false;
		public bool IsVisible;
		public string DisplayName;
		public GUITableType Type;

		[FormerlySerializedAs("Table")]
		public TTable Asset;
		public bool IsPinned;

		private float _width;

#if USING_WIDTH_NON_PERCENT
		//public float VisualWidth;
#else
		internal float WidthPercentage;
#endif

#if USING_WIDTH_NON_PERCENT
		public static OdinGUITable<TTable> CreateKeyTable()
#else
		public static OdinGUITable<TTable> CreateKeyTable(float widthPercent)
#endif
		{
			return new OdinGUITable<TTable>
			{
				IsVisible = true,
				DisplayName = KEY_DISPLAY_NAME,
				Type = GUITableType.Key,
				Asset = null,
				IsPinned = false,
				Width = OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH,
#if USING_WIDTH_NON_PERCENT
				//	VisualWidth = OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH
#else
				WidthPercentage = widthPercent
#endif
			};
		}

#if USING_WIDTH_NON_PERCENT
		public static OdinGUITable<TTable> CreateTable(TTable table, Locale locale)
#else
		public static OdinGUITable<TTable> CreateTable(TTable table, float widthPercent)
#endif
		{
			return new OdinGUITable<TTable>
			{
				IsVisible = true,
				DisplayName = locale.LocaleName,
				Type = GUITableType.Default,
				Asset = table,
				IsPinned = false,
				Width = OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH,
#if USING_WIDTH_NON_PERCENT
				//	VisualWidth = OdinLocalizationConstants.DEFAULT_COLUMN_WIDTH
#else
				WidthPercentage = widthPercent
#endif
			};
		}

		public int CompareTo(OdinGUITable<TTable> other)
		{
			if (other == null)
			{
				return -1;
			}

			if (this.IsVisible && !other.IsVisible)
			{
				return -1;
			}

			if (!this.IsVisible && other.IsVisible)
			{
				return 1;
			}

			if (this.IsPinned && !other.IsPinned)
			{
				return -1;
			}

			if (!this.IsPinned && other.IsPinned)
			{
				return 1;
			}

			if (this.DisplayName == KEY_DISPLAY_NAME && other.DisplayName == KEY_DISPLAY_NAME)
			{
				return 0;
			}

			if (other.DisplayName == KEY_DISPLAY_NAME)
			{
				return 1;
			}

			if (this.DisplayName == KEY_DISPLAY_NAME)
			{
				return -1;
			}

			return this.Asset.LocaleIdentifier.CompareTo(other.Asset.LocaleIdentifier);
		}

		public Vector2 HandleSlider(Rect position)
		{
			if (GUIUtility.hotControl == 0)
			{
				this.IsDraggingSlider = false;
			}
			
			if (!GUI.enabled)
			{
				return Vector2.zero;
			}

			EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);

			if (GUI.enabled && Event.current.OnMouseDown(position, 0))
			{
				this.IsDraggingSlider = true;
				SharedUniqueControlId.SetActive();
				EditorGUIUtility.SetWantsMouseJumping(1);
				Event.current.Use();
			}
			else if (SharedUniqueControlId.IsActive && this.IsDraggingSlider)
			{
				if (Event.current.type == EventType.MouseDrag)
				{
					Event.current.Use();
					GUI.changed = true;
					return Event.current.delta;
				}

				if (Event.current.type != EventType.MouseUp)
				{
					return Vector2.zero;
				}

				this.IsDraggingSlider = false;
				SharedUniqueControlId.SetInactive();
				EditorGUIUtility.SetWantsMouseJumping(0);
				Event.current.Use();
			}

			return Vector2.zero;
		}
	}
}