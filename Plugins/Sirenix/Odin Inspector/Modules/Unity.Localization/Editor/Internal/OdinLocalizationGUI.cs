//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationGUI.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Internal;
using Sirenix.Reflection.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
	// TODO: update name
	public static class OdinLocalizationGUI
	{
		public static GUIStyle RichLabelTextCenteredMultiLine
		{
			get
			{
				if (_richLabelTextCenteredMultiLine == null)
				{
					_richLabelTextCenteredMultiLine = new GUIStyle(SirenixGUIStyles.MultiLineCenteredLabel)
					{
						richText = true
					};
				}

				return _richLabelTextCenteredMultiLine;
			}
		}

		public static GUIStyle CardTitleWhite
		{
			get
			{
				if (_cardTitleWhite == null)
				{
					_cardTitleWhite = new GUIStyle(SirenixGUIStyles.MultiLineWhiteLabel)
					{
						fontSize = 12,
						fontStyle = FontStyle.Bold
					};
				}

				return _cardTitleWhite;
			}
		}

		public static FancyColor Background => EditorGUIUtility.isProSkin ? Color.black : Color.white;

		// TODO: LIGHT MODE
		public static FancyColor MenuBackground => EditorGUIUtility.isProSkin ? DarkMenuBackground : LightMenuBackground;
		public static FancyColor WindowBackground => EditorGUIUtility.isProSkin ? DarkWindowBackground : LightWindowBackground;
		public static FancyColor Panel => EditorGUIUtility.isProSkin ? DarkPanel : LightPanel;
		public static FancyColor PanelGap => EditorGUIUtility.isProSkin ? DarkPanelGap : LightPanelGap;

		public static FancyColor Selected => EditorGUIUtility.isProSkin ? DarkSelected : LightSelected;

		public static FancyColor TabsBackground => EditorGUIUtility.isProSkin ? DarkTabsBackground : LightTabsBackground;

		public static FancyColor ColumnBackground => EditorGUIUtility.isProSkin ? DarkColumnBackground : LightColumnBackground;

		public static FancyColor RowEvenBackground => EditorGUIUtility.isProSkin ? DarkRowEvenBackground : LightRowEvenBackground;
		public static FancyColor RowEvenBackground2 => EditorGUIUtility.isProSkin ? DarkRowEvenBackground2 : LightRowEvenBackground2;
		public static FancyColor RowOddBackground => EditorGUIUtility.isProSkin ? DarkRowOddBackground : LightRowOddBackground;
		public static FancyColor RowOddBackground2 => EditorGUIUtility.isProSkin ? DarkRowOddBackground2 : LightRowOddBackground2;
		public static FancyColor RowBorder => EditorGUIUtility.isProSkin ? DarkRowBorder : LightRowBorder;
		public static FancyColor RowBorderHover => EditorGUIUtility.isProSkin ? DarkRowBorderHover : LightRowBorderHover;

		public static FancyColor Tab => EditorGUIUtility.isProSkin ? DarkTab : LightTab;
		public static FancyColor TabHover => EditorGUIUtility.isProSkin ? DarkTabHover : LightTabHover;
		public static FancyColor TabSelected => EditorGUIUtility.isProSkin ? DarkTabSelected : LightTabSelected;

		public static FancyColor Checkerboard => DarkCheckerboard;
		public static FancyColor CheckerboardBorder => DarkCheckerboardBorder;

		public static FancyColor DarkMenuBackground;

		public static FancyColor DarkWindowBackground;
		public static FancyColor DarkPanel;
		public static FancyColor DarkPanelGap;

		public static FancyColor DarkSelected;

		public static FancyColor DarkTabsBackground;

		public static FancyColor DarkColumnBackground;

		public static FancyColor DarkRowEvenBackground;
		public static FancyColor DarkRowEvenBackground2;
		public static FancyColor DarkRowOddBackground;
		public static FancyColor DarkRowOddBackground2;
		public static FancyColor DarkRowBorder;
		public static FancyColor DarkRowBorderHover;

		public static FancyColor DarkTab;
		public static FancyColor DarkTabHover;
		public static FancyColor DarkTabSelected;

		public static FancyColor LightMenuBackground;

		public static FancyColor LightWindowBackground;
		public static FancyColor LightPanel;
		public static FancyColor LightPanelGap;

		public static FancyColor LightSelected;

		public static FancyColor LightTabsBackground;

		public static FancyColor LightColumnBackground;

		public static FancyColor LightRowEvenBackground;
		public static FancyColor LightRowEvenBackground2;
		public static FancyColor LightRowOddBackground;
		public static FancyColor LightRowOddBackground2;
		public static FancyColor LightRowBorder;
		public static FancyColor LightRowBorderHover;

		public static FancyColor LightTab;
		public static FancyColor LightTabHover;
		public static FancyColor LightTabSelected;


		public static FancyColor DarkCheckerboard;
		public static FancyColor DarkCheckerboardBorder;

		private static GUIStyle DefaultTextStyle;

		private static GUIStyle _richLabelTextCenteredMultiLine;
		private static GUIStyle _cardTitleWhite;

		static OdinLocalizationGUI()
		{
			DarkWindowBackground = new Color(0.15f, 0.15f, 0.15f, 1.0f);
			LightWindowBackground = new Color(0.358f, 0.358f, 0.358f, 1.0f);

			DarkPanel = new Color(0.2156863f, 0.2156863f, 0.2156863f, 1.0f);
			DarkPanelGap = new Color(0.0856863f, 0.0856863f, 0.0856863f, 1.0f);

			LightPanel = new Color(0.761f, 0.761f, 0.761f, 1.0f);
			LightPanelGap = new Color(0.532f, 0.532f, 0.532f, 1.0f);

			DarkSelected = new Color(0.7076807f, 0.8213042f, 0.8773585f, 1.0f);
			LightSelected = new Color(0.358f, 0.446f, 0.509f, 1.0f);

			DarkTabsBackground = new Color(0.1886792f, 0.1886792f, 0.1886792f, 1.0f);
			LightTabsBackground = new Color(0.538f, 0.538f, 0.538f, 1.0f);

			DarkColumnBackground = new Color(0.099f, 0.099f, 0.099f, 1.0f);
			LightColumnBackground = new FancyColor(0.84f);

			var rowBaseBlend = new FancyColor(0.42f);

#region DARK_ROW

			var darkRowEvenBackground = new FancyColor(0.2f);
			var darkRowOddBackground = new FancyColor(0.16f);

			DarkRowEvenBackground = darkRowEvenBackground;
			DarkRowOddBackground = darkRowOddBackground;

			DarkRowEvenBackground2 = darkRowEvenBackground.Blend(rowBaseBlend, FancyColor.BlendMode.Overlay);
			DarkRowOddBackground2 = darkRowOddBackground.Blend(rowBaseBlend, FancyColor.BlendMode.Overlay);

			DarkRowBorder = new Color(0, 0, 0, 0.3607843f);
			DarkRowBorderHover = new Color(1, 1, 1, 0.2470588f);

#endregion

#region LIGHT_ROW

			var lightRowEvenBackground = new FancyColor(0.711f);
			var lightRowOddBackground = new FancyColor(0.611f);

			LightRowEvenBackground = lightRowEvenBackground;
			LightRowOddBackground = lightRowOddBackground;

			LightRowEvenBackground2 = lightRowEvenBackground.Blend(rowBaseBlend, FancyColor.BlendMode.Overlay);
			LightRowOddBackground2 = lightRowOddBackground.Blend(rowBaseBlend, FancyColor.BlendMode.Overlay);

			LightRowBorder = new Color(0, 0, 0, 0.2f);
			LightRowBorderHover = new Color(1, 1, 1, 0.4f);

#endregion

			DarkTab = new Color(0.254717f, 0.254717f, 0.254717f, 1.0f);
			DarkTabHover = new Color(0.277f, 0.277f, 0.277f, 1.0f);
			DarkTabSelected = new Color(0.312f, 0.312f, 0.312f, 1.0f);

			float scalar = 0.925f;
			LightTab = new FancyColor(0.85f * scalar);
			LightTabHover = new FancyColor(0.915f * scalar);
			LightTabSelected = new FancyColor(0.95f * scalar);

			DarkCheckerboard = new Color(0.298f, 0.298f, 0.298f, 1.0f);
			DarkCheckerboardBorder = new Color(0, 0, 0, 0.5529412f);

			DefaultTextStyle = new GUIStyle(SirenixGUIStyles.MultiLineCenteredLabel);
			DefaultTextStyle.focused.textColor = DefaultTextStyle.normal.textColor;

			DarkMenuBackground = DarkPanel;

			LightMenuBackground = new FancyColor(0.84f);
		}

		public static string TextField(Rect position, string text, out bool changed, int id)
		{
			if (id == 0)
			{
				GUI.Label(position, text, SirenixGUIStyles.MultiLineCenteredLabel);
				changed = false;
				return text;
			}

			Color lastCursorColor = GUI.skin.settings.cursorColor;

			bool isMouseDown = Event.current.type == EventType.MouseDown;

			if (isMouseDown)
			{
				GUI.skin.settings.cursorColor = Color.clear;
			}

			text = EditorGUI_Internals.DoTextField(id, position, text, DefaultTextStyle, null, out changed, false, true, false);

			if (isMouseDown)
			{
				GUI.skin.settings.cursorColor = lastCursorColor;
			}

			return text;
		}

		public static string TextFieldSyntaxHighlighted(Rect position, string text, string syntaxRichText, out bool changed, int id)
		{
			if (string.IsNullOrEmpty(syntaxRichText))
			{
				return TextField(position, text, out changed, id);
			}

			GUIHelper.PushContentColor(Color.clear);
			text = EditorGUI_Internals.DoTextField(id, position, text, SirenixGUIStyles.MultiLineCenteredLabel, null, out changed, false, true, false);
			GUIHelper.PopContentColor();

			GUI.Label(position, syntaxRichText, RichLabelTextCenteredMultiLine);

			return text;
		}

		public static bool ObjectPickerButton(Rect position)
		{
			bool isMouseOver = Event.current.IsMouseOver(position);

			var c1 = EditorGUIUtility.isProSkin
							? new Color(1, 1, 1, isMouseOver ? 0.8f : 0.3f)
							: new Color(0, 0, 0, isMouseOver ? 0.8f : 0.3f);

			SdfIcons.DrawIcon(position, SdfIconType.StopCircle, c1);

			return Event.current.OnMouseDown(position, 0);
		}

		public static TEnum Tabs<TEnum>(Rect rect, TEnum value, float width) where TEnum : Enum
		{
			EnumTypeUtilities<TEnum>.EnumMember[] infos = EnumTypeUtilities<TEnum>.VisibleEnumMemberInfos;

			for (var i = 0; i < infos.Length; i++)
			{
				Rect itemRect = rect.TakeFromLeft(width);

				bool isMouseOver = Event.current.IsMouseOver(itemRect);
				bool isSelected = value.Equals(infos[i].Value);
				bool isPressed = Event.current.OnMouseDown(itemRect, 0);

				Color tabColor;

				if (isSelected)
				{
					tabColor = TabSelected;
				}
				else if (isMouseOver)
				{
					tabColor = TabHover;
				}
				else
				{
					tabColor = Tab;
				}

				SirenixEditorGUI.DrawSolidRect(itemRect, tabColor);

				Vector2 textSize = EditorStyles.label.CalcSize(GUIHelper.TempContent(infos[i].NiceName));
				float iconSize = textSize.y;
				float size = iconSize + 2.0f + textSize.x + 1.0f;

				if (size > itemRect.width)
				{
					if (iconSize > itemRect.width)
					{
						continue;
					}

					SdfIcons.DrawIcon(itemRect.AlignCenter(iconSize), infos[i].Icon);
				}
				else
				{
					Rect contentRect = itemRect.AlignCenter(size);

					Rect iconRect = contentRect.TakeFromLeft(iconSize).AlignMiddle(iconSize);
					SdfIcons.DrawIcon(iconRect, infos[i].Icon);

					contentRect.TakeFromLeft(2);

					GUI.Label(contentRect, infos[i].NiceName);
				}

				if (isPressed)
				{
					value = infos[i].Value;
				}
			}

			return value;
		}

		public static bool OverlaidButton(Rect position, string text, SdfIconType icon = SdfIconType.None, GUIStyle labelStyle = null, bool invert = false)
		{
			const float ICON_SIZE = 18;
			const float SPACE = 4;

			bool isMouseOver = Event.current.IsMouseOver(position);

			ref SirenixAnimationUtility.InterpolatedFloat t = ref SirenixAnimationUtility.GetTemporaryFloat(position, isMouseOver ? 1.0f : 0.0f);

			t.ChangeDestination(isMouseOver ? 1.0f : 0.0f);

			t.Move(1.0f / 0.15f, Easing.OutQuad);

			float value = t.GetValue();
			float inverseValue = 1.0f - value;

			if (invert)
			{
				SirenixEditorGUI.DrawRoundRect(position, new Color(0, 0, 0, 0.2f * inverseValue), 5.0f, new Color(0, 0, 0, 0.05f * inverseValue), 1);
				SirenixEditorGUI.DrawRoundRect(position, new Color(0, 0, 0, 0.4f * value), 5.0f, new Color(1.0f, 1.0f, 1.0f, 0.05f * value), 1);
			}
			else
			{
				SirenixEditorGUI.DrawRoundRect(position, new Color(1, 1, 1, 0.2f * inverseValue), 5.0f, new Color(0, 0, 0, 0.05f * inverseValue), 1);
				SirenixEditorGUI.DrawRoundRect(position, new Color(1, 1, 1, 0.4f * value), 5.0f, new Color(1.0f, 1.0f, 1.0f, 0.05f * value), 1);
			}

			labelStyle = labelStyle ?? SirenixGUIStyles.WhiteLabelCentered;

			if (icon == SdfIconType.None)
			{
				GUI.Label(position, text, labelStyle);
			}
			else
			{
				float textWidth = SirenixGUIStyles.WhiteLabelCentered.CalcWidth(text);

				Rect contentPosition = position.AlignCenter(ICON_SIZE + SPACE + textWidth);

				SdfIcons.DrawIcon(contentPosition.AlignLeft(ICON_SIZE), icon, Color.white);

				GUI.Label(contentPosition.AlignRight(textWidth), text, labelStyle);
			}

			return Event.current.OnMouseDown(position, 0);
		}

		public static bool Metadata(Type metadataType, bool isFirst)
		{
			var isRemoved = false;

			if (isFirst)
			{
				EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), new FancyColor(0.06603771f));
			}

			Vector2 padding = new Vector2(4, 4);
			Rect rect = GUILayoutUtility.GetRect(0, 24);

			if (Event.current.IsMouseOver(rect))
			{
				EditorGUI.DrawRect(rect, new FancyColor(0.2784314f));
			}
			else
			{
				EditorGUI.DrawRect(rect, new FancyColor(0.2431373f));
			}


			EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1), new FancyColor(0.06603771f));

			var iconWidth = 16.0f;
			var iconPadding = new Vector2(2, 2);

			SdfIcons.DrawIcon(rect.TakeFromLeft(iconWidth).Padding(iconPadding.x, iconPadding.y), SdfIconType.GripVertical);

			rect = rect.Padding(padding.x, 0);

			SdfIcons.DrawIcon(rect.TakeFromLeft(iconWidth).Padding(iconPadding.x, iconPadding.y), SdfIconType.Braces);

			rect.TakeFromLeft(5);
			GUI.Label(rect, metadataType.GetNiceName(), SirenixGUIStyles.BoldLabel);

			var removeRect = rect.TakeFromRight(iconWidth).Padding(iconPadding.x, iconPadding.y);

			SdfIcons.DrawIcon(removeRect, SdfIconType.X);

			if (GUI.Button(removeRect, GUIContent.none, GUIStyle.none))
			{
				isRemoved = true;
			}

			return isRemoved;
		}

		public static void DrawRoundBlur20(Rect position, Color color)
		{
			position = position.Expand(63);
			SirenixEditorGUI.DrawTextureSliced(position, OdinLocalizationGUITextures.RoundBlur20, color, 63);
		}

		public static void DrawRoundBlur6(Rect position, Color color)
		{
			position = position.Expand(10);
			SirenixEditorGUI.DrawTextureSliced(position, OdinLocalizationGUITextures.RoundBlur6, color, 10);
		}
	}
}