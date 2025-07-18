//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationConfig.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Reflection.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Sirenix.OdinInspector.Modules.Localization.Editor.Configs
{
	[GlobalConfig("Plugins/Sirenix/Odin Inspector/Modules/Unity.Localization/Editor/Configs")]
	public class OdinLocalizationConfig : GlobalConfig<OdinLocalizationConfig>
	{
		public class ThemeColorDrawer : OdinValueDrawer<ThemeColor>
		{
			protected override void Initialize()
			{
				base.Initialize();
				this.Property.State.Expanded = false;
			}

			protected override void DrawPropertyLayout(GUIContent label)
			{
				SirenixEditorGUI.BeginBox(string.Empty);
				{
					SirenixEditorGUI.BeginBoxHeader();
					{
						GUILayout_Internal.BeginRow();
						{
							GUILayout_Internal.BeginColumn(LayoutSize.Pixels(EditorGUIUtility.labelWidth + 6.0f));
							{
								this.Property.State.Expanded = EditorGUILayout.Foldout(this.Property.State.Expanded,
																										 $"{label.text} ({(EditorGUIUtility.isProSkin ? "Dark" : "Light")})",
																										 true);
							}
							GUILayout_Internal.EndColumn();

							GUILayout_Internal.BeginColumn(LayoutSize.Auto);
							{
								this.Property.Children[nameof(ThemeColor.Color)].Draw(null);
							}
							GUILayout_Internal.EndColumn();
						}
						GUILayout_Internal.EndRow();
					}
					SirenixEditorGUI.EndBoxHeader();

					bool toggle = this.ValueEntry.ValueState != PropertyValueState.NullReference && this.Property.State.Expanded;

					if (SirenixEditorGUI.BeginFadeGroup(this, toggle))
					{
						GUILayout.BeginHorizontal();
						this.Property.Children[nameof(ThemeColor.lightColor)].Draw();
						GUILayout.Space(3.5f);
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						this.Property.Children[nameof(ThemeColor.darkColor)].Draw();
						GUILayout.Space(3.5f);
						GUILayout.EndHorizontal();
					}

					SirenixEditorGUI.EndFadeGroup();
				}
				SirenixEditorGUI.EndBox();

				if (this.Property.State.Expanded)
				{
					GUILayout.Space(4.0f);
				}
			}
		}

		[Serializable]
		public class ThemeColor
		{
			[ShowInInspector]
			public Color Color
			{
				get => EditorGUIUtility.isProSkin ? this.darkColor : this.lightColor;

				set
				{
					if (EditorGUIUtility.isProSkin)
					{
						this.darkColor = value;
					}
					else
					{
						this.lightColor = value;
					}
				}
			}

			public Color lightColor;
			public Color darkColor;

			public ThemeColor(Color lightColor, Color darkColor)
			{
				this.lightColor = lightColor;
				this.darkColor = darkColor;
			}

			public static implicit operator Color(ThemeColor color) => color.Color;
		}

		[ShowInInspector]
		[BoxGroup("User Interface")]
		[Range(96, 1024)]
		public int assetRowHeight = 128;

		[BoxGroup("Syntax Highlighting")]
		public bool useSyntaxHighlighter = true;

		[EnableIf(nameof(useSyntaxHighlighter))]
		[BoxGroup("Syntax Highlighting")]
		public ThemeColor placeholderColor = new ThemeColor(new Color(0.743147f, 0.9433962f, 0.9242815f), new Color(0, 0.5882353f, 0.5333334f));

		[EnableIf(nameof(useSyntaxHighlighter))]
		[BoxGroup("Syntax Highlighting")]
		public ThemeColor selectorColor = new ThemeColor(new Color(1.0f, 0.7727525f, 0.3632075f), new Color(1, 0.6470588f, 0));

		[EnableIf(nameof(useSyntaxHighlighter))]
		[BoxGroup("Syntax Highlighting")]
		public ThemeColor formatterColor = new ThemeColor(new Color(0.9921569f, 0.9855571f, 0.8823529f), new Color(0.9607843f, 0.9607843f, 0.8627451f));

		[BoxGroup("Navigation")]
		[Range(1, 1000.0f)]
		public float scrollSpeed = 24.0f;

		[BoxGroup("Navigation")]
		public bool invertMouseDragNavigation = true;

		[BoxGroup("Navigation")]
		[Range(0.5f, 5.0f)]
		public float mouseDragSpeed = 1.0f;

		[InfoBox("We couldn't find the necessary methods/classes to perform custom undo operations, therefore this option has been disabled and will be considered false even if true.",
					VisibleIf = "@!OdinLocalizationReflectionValues.HasAPIForCustomUndo")]
		[EnableIf("@OdinLocalizationReflectionValues.HasAPIForCustomUndo")]
		[BoxGroup("Undo")]
		public bool useCustomUndoHandlingForAssetCollections = true;

		[Button(ButtonSizes.Large)]
		public void Reset()
		{
			if (!EditorUtility.DisplayDialog("Odin Localization Config", "Are you certain you want to reset your Localization config?", "Yes", "No"))
			{
				return;
			}

			this.useCustomUndoHandlingForAssetCollections = OdinLocalizationReflectionValues.HasAPIForCustomUndo;

			this.assetRowHeight = 128;

			this.useSyntaxHighlighter = true;
			this.placeholderColor = new ThemeColor(new Color(0.743147f, 0.9433962f, 0.9242815f), new Color(0, 0.5882353f, 0.5333334f));
			this.selectorColor = new ThemeColor(new Color(1.0f, 0.7727525f, 0.3632075f), new Color(1, 0.6470588f, 0));
			this.formatterColor = new ThemeColor(new Color(0.9921569f, 0.9855571f, 0.8823529f), new Color(0.9607843f, 0.9607843f, 0.8627451f));

			this.scrollSpeed = 24.0f;
			this.invertMouseDragNavigation = true;
			this.mouseDragSpeed = 1.0f;
		}
	}
}