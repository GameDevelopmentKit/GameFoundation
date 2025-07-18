//-----------------------------------------------------------------------
// <copyright file="LocalizationMetadata.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
    public class LocalizationMetadata
    {
        private object lastTarget;
        public object Target;
        private LocalizationTableCollection localizationTableCollection;
        private SearchField searchField = new SearchField();
        private string searchTerm;
        private Vector2 scrollPosition;
        private IMetadata toBeAdded;
        private IMetadata toBeRemoved;
        private Type lastMetadataTypeUnderCursor;
        private StringBuilder stringBuilder = new StringBuilder();
        private OdinLocalizationEditorWindow.WindowState windowState;

        private List<Type> tags = new List<Type>
        {
            typeof(ExcludeEntryFromExport)
        };

        private GUIStyle _contentPadding;
        private GUIStyle ContentPadding =>
            this._contentPadding = this._contentPadding ?? new GUIStyle
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

        private GUIStyle _platformOverridePadding;

        private GUIStyle PlatformOverridePadding =>
            this._platformOverridePadding = this._platformOverridePadding ?? new GUIStyle
            {
                padding = new RectOffset(10, 20, 10, 10)
            };

        public LocalizationMetadata(LocalizationTableCollection localizationTableCollection, OdinLocalizationEditorWindow.WindowState windowState)
        {
            this.localizationTableCollection = localizationTableCollection;
            this.windowState = windowState;
        }

        /// <summary>
        /// Adds the given metadata to the shared data of the <see cref="LocalizationTableCollection"/>.
        /// </summary>
        public void Add(IMetadata metadata)
        {
            this.localizationTableCollection.SharedData.Metadata.AddMetadata(metadata);
        }

        /// <summary>
        /// Adds the given metadata to the given <see cref="LocalizationTable"/>.
        /// </summary>
        public void Add(LocalizationTable localizationTable, IMetadata metadata)
        {
            localizationTable.AddMetadata(metadata);
        }

        /// <summary>
        /// Adds the given metadata to the given <see cref="SharedTableData"/>.
        /// </summary>
        public void Add(SharedTableData.SharedTableEntry sharedTableEntry, IMetadata metadata)
        {
            sharedTableEntry.Metadata.AddMetadata(metadata);
        }

        /// <summary>
        /// Adds the given metadata to the given <see cref="TableEntry"/>.
        /// </summary>
        public void Add(TableEntry tableEntry, IMetadata metadata)
        {
            tableEntry.AddMetadata(metadata);
        }

        /// <summary>
        /// Removes the given metadata from the shared data of the <see cref="LocalizationTableCollection"/>.
        /// </summary>
        public void Remove(IMetadata metadata)
        {
            this.localizationTableCollection.SharedData.Metadata.RemoveMetadata(metadata);
        }

        /// <summary>
        /// Removes the given metadata from the given <see cref="LocalizationTable"/>.
        /// </summary>
        public void Remove(LocalizationTable localizationTable, IMetadata metadata)
        {
            localizationTable.RemoveMetadata(metadata);
        }

        /// <summary>
        /// Removes the given metadata from the given <see cref="SharedTableData"/>.
        /// </summary>
        public void Remove(SharedTableData.SharedTableEntry sharedTableEntry, IMetadata metadata)
        {
            sharedTableEntry.Metadata.RemoveMetadata(metadata);
        }

        /// <summary>
        /// Removes the given metadata from the given <see cref="TableEntry"/>.
        /// </summary>
        public void Remove(TableEntry tableEntry, IMetadata metadata)
        {
            tableEntry.RemoveMetadata(metadata);
        }

        /// <summary>
        /// Draws the metadata for the given <see cref="LocalizationTableCollection"/>
        /// </summary>
        public float Draw(Rect rect)
        {
            var minWidth = 0f;
            if (this.Target == null) return minWidth;

            if (Event.current.type != EventType.Repaint)
            {
                if (this.toBeAdded != null)
                {
                    switch (this.Target)
                    {
                        case LocalizationTable localizationTable:
                            Undo.RecordObject(this.localizationTableCollection, "Localization Tables Metadata Added");
                            this.Add(localizationTable, this.toBeAdded);
                            break;
                        case SharedTableData.SharedTableEntry sharedTableEntry:
                            Undo.RecordObject(this.localizationTableCollection.SharedData, "Localization Tables Metadata Added");
                            this.Add(sharedTableEntry, this.toBeAdded);
                            break;
                        case TableEntry tableEntry:
                            Undo.RecordObject(tableEntry.Table, "Localization Tables Metadata Added");
                            this.Add(tableEntry, this.toBeAdded);
                            break;
                        default:
                            Undo.RecordObject(this.localizationTableCollection.SharedData, "Localization Tables Metadata Added");
                            this.Add(this.toBeAdded);
                            break;
                    }

                    this.toBeAdded = null;
                    this.windowState.Dispose();
                }

                if (this.toBeRemoved != null)
                {
                    switch (this.Target)
                    {
                        case LocalizationTable localizationTable:
                            Undo.RecordObject(this.localizationTableCollection, "Localization Tables Metadata Removed");
                            this.Remove(localizationTable, this.toBeRemoved);
                            break;
                        case SharedTableData.SharedTableEntry sharedTableEntry:
                            Undo.RecordObject(this.localizationTableCollection.SharedData, "Localization Tables Metadata Removed");
                            this.Remove(sharedTableEntry, this.toBeRemoved);
                            break;
                        case TableEntry tableEntry:
                            Undo.RecordObject(tableEntry.Table, "Localization Tables Metadata Removed");
                            this.Remove(tableEntry, this.toBeRemoved);
                            break;
                        default:
                            Undo.RecordObject(this.localizationTableCollection.SharedData, "Localization Tables Metadata Removed");
                            this.Remove(this.toBeRemoved);
                            break;
                    }

                    this.toBeRemoved = null;
                    this.windowState.Dispose();
                }
            }

            if (this.windowState.MetadataTree == null || this.lastTarget != this.Target)
            {
                this.windowState.MetadataTree?.Dispose();
                
                switch (this.Target)
                {
                    case LocalizationTable localizationTable:
                        this.windowState.MetadataTree = PropertyTree.Create(localizationTable);
                        break;
                    case SharedTableData.SharedTableEntry sharedTableEntry:
                        this.windowState.MetadataTree = PropertyTree.Create(sharedTableEntry);
                        break;
                    case TableEntry tableEntry:
                    {
                        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                        var entryData = typeof(TableEntry).GetProperty("Data", bindingFlags).GetValue(tableEntry);
                        this.windowState.MetadataTree = PropertyTree.Create(entryData);
                        break;
                    }
                    case LocalizationTableCollection localizationTableCollection:
                        this.windowState.MetadataTree = PropertyTree.Create(localizationTableCollection.SharedData);
                        break;
                }

                this.lastTarget = this.Target;
            }

            var metadataCollection = this.windowState.MetadataTree.RootProperty.Children["m_Metadata"];
            var metadataEntries = metadataCollection.Children["m_Items"].Children.ToList();

            metadataEntries = metadataEntries
                .Where(entry => !entry.Attributes.HasAttribute<HideInInspector>())
                .OrderBy(entry => !this.tags.Contains(entry.ValueEntry.TypeOfValue))
                .ThenBy(entry => entry.Name)
                .ToList();

            this.windowState.MetadataTree.BeginDraw(false);
            switch (this.Target)
            {
                case LocalizationTable localizationTable:
                    minWidth = this.Draw(rect, localizationTable, "Table", metadataEntries, m => this.Add(localizationTable, m), m => this.Remove(localizationTable, m), () => this.OpenMetadataSelector(localizationTable));
                    break;
                case SharedTableData.SharedTableEntry sharedTableEntry:
                    minWidth = this.Draw(rect, this.localizationTableCollection.SharedData, "Shared Entry", metadataEntries, m => this.Add(sharedTableEntry, m), m => this.Remove(sharedTableEntry, m), () => this.OpenMetadataSelector(sharedTableEntry));
                    break;
                case TableEntry tableEntry:
                    minWidth = this.Draw(rect, tableEntry.Table, "Entry", metadataEntries, m => this.Add(tableEntry, m), m => this.Remove(tableEntry, m), () => this.OpenMetadataSelector(tableEntry));
                    break;
                default:
                    minWidth = this.Draw(rect, this.localizationTableCollection.SharedData, "Shared Table", metadataEntries, m => this.Add(m), m => this.Remove(m), () => this.OpenMetadataSelector(this.localizationTableCollection));
                    break;
            }
            this.windowState.MetadataTree.EndDraw();

            return minWidth;
        }

        private float Draw(Rect rect, UnityEngine.Object target, string title, List<InspectorProperty> metadataEntries, Action<IMetadata> add, Action<IMetadata> remove, Action openMetadataSelector)
        {
            const int headerHeight = 22;
            const int addButtonAreaHeight = 40;

            EditorGUIUtility.labelWidth = 0f;

            // We only want to process events if the user's mouse is currently
            // in the metadata panel so we grab some information for later.
            var metadataPanelHasFocus = Event.current.IsMouseOver(rect);
            var holdingControl = Event.current.control && metadataPanelHasFocus;
            var holdingAlt = Event.current.alt && metadataPanelHasFocus;

            if (Event.current.OnKeyUp(KeyCode.LeftAlt))
            {
                this.lastMetadataTypeUnderCursor = null;
            }

            var searchAreaRect = rect.TakeFromTop(32);
            EditorGUI.DrawRect(searchAreaRect.AlignBottom(1), SirenixGUIStyles.BorderColor);

            // Draw search field.
            if (searchAreaRect.width > 125)
            {
                var titleWidth = SirenixGUIStyles.LabelCentered.CalcWidth(title) + 13;
                var titleRect = searchAreaRect.AlignRight(titleWidth);
                EditorGUI.DrawRect(titleRect.TakeFromLeft(1), SirenixGUIStyles.BorderColor);
                this.searchTerm = this.searchField.Draw(searchAreaRect.SubXMax(titleWidth).Padding(6), this.searchTerm,
                    "Search for metadata...");
                GUI.Label(titleRect, title, SirenixGUIStyles.LabelCentered);
            }
            else
            {
                GUI.Label(searchAreaRect, title, SirenixGUIStyles.LabelCentered);
            }

            var filteredMetadataEntries = string.IsNullOrWhiteSpace(this.searchTerm)
                ? metadataEntries.ToList()
                : metadataEntries.Where(metadataEntry =>
                {
                    if (FuzzySearch.Contains(this.searchTerm, metadataEntry.Info.TypeOfOwner.Name))
                    {
                        return true;
                    }

                    if (FuzzySearch.Contains(this.searchTerm, metadataEntry.ValueEntry?.WeakSmartValue?.ToString()))
                    {
                        return true;
                    }

                    foreach (var child in metadataEntry.Children.Recurse())
                    {
                        if (FuzzySearch.Contains(this.searchTerm, child.Name))
                        {
                            return true;
                        }

                        if (FuzzySearch.Contains(this.searchTerm, child.ValueEntry?.WeakSmartValue?.ToString()))
                        {
                            return true;
                        }
                    }

                    return false;
                }).ToList();

            var metadataViewHeight = rect.height - addButtonAreaHeight;
            GUILayout.BeginArea(rect.AlignTop(metadataViewHeight));
            this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            GUIHelper.BeginLayoutMeasuring();
            {
                for (var i = 0; i < filteredMetadataEntries.Count; i++)
                {
                    var metadataEntry = filteredMetadataEntries[i];
                    var metadataHeaderRect = GUILayoutUtility.GetRect(0, headerHeight, GUILayoutOptions.ExpandWidth().ExpandHeight(false));
                    var unchangedMetadataHeaderRect = metadataHeaderRect;

                    var genericMenu = new GenericMenu();
                    genericMenu.AddItem(new GUIContent("Delete"), false, () =>
                    {
                        var metadata = (IMetadata)metadataEntry.ValueEntry.WeakSmartValue;
                        this.toBeRemoved = metadata;
                    });

                    var isTag = this.tags.Contains(metadataEntry.ValueEntry.TypeOfValue);

                    if (!isTag)
                    {
                        genericMenu.AddItem(new GUIContent("Copy"), false, () =>
                        {
                            var metadata = (IMetadata)metadataEntry.ValueEntry.WeakSmartValue;
                            Clipboard.Copy(metadata);
                        });

                        if (Clipboard.CanPaste<IMetadata>())
                            genericMenu.AddItem(new GUIContent("Paste"), false, () =>
                            {
                                var metadata = (IMetadata)Clipboard.Paste();
                                this.toBeAdded = metadata;
                            });
                        else
                            genericMenu.AddDisabledItem(new GUIContent("Paste"));

                        genericMenu.AddSeparator("");
                        genericMenu.AddItem(new GUIContent($"{(metadataEntry.State.Expanded ? "Collapse" : "Expand")} All   (Ctrl-Click)"), false, () =>
                        {
                            var newState = !metadataEntry.State.Expanded;
                            foreach (var entry in filteredMetadataEntries) entry.State.Expanded = newState;
                        });
                        genericMenu.AddItem(new GUIContent($"{(metadataEntry.State.Expanded ? "Collapse" : "Expand")} All Of Same Type   (Alt-Click)"), false, () =>
                        {
                            var newState = !metadataEntry.State.Expanded;
                            foreach (var entry in filteredMetadataEntries.Where(m => m.Info.TypeOfOwner == metadataEntry.Info.TypeOfOwner)) entry.State.Expanded = newState;
                        });
                    }

                    Color metadataHeaderBackground = new Color(0.243f, 0.243f, 0.243f);

                    if (!isTag && holdingControl)
                    {
                        metadataHeaderBackground = new Color(0.278f, 0.278f, 0.278f);

                        GUIHelper.RequestRepaint();
                    }
                    else if (!isTag && holdingAlt)
                    {
                        if (Event.current.IsMouseOver(unchangedMetadataHeaderRect))
                        {
                            this.lastMetadataTypeUnderCursor = metadataEntry.ValueEntry.TypeOfValue;
                            GUIHelper.RequestRepaint();
                        }

                        metadataHeaderBackground = this.lastMetadataTypeUnderCursor == metadataEntry.ValueEntry.TypeOfValue
                            ? new Color(0.278f, 0.278f, 0.278f)
                            : new Color(0.243f, 0.243f, 0.243f);
                    }

                    if (Event.current.IsMouseOver(unchangedMetadataHeaderRect))
                    {
                        metadataHeaderBackground = new Color(0.278f, 0.278f, 0.278f);
                    }

                    EditorGUI.DrawRect(unchangedMetadataHeaderRect, metadataHeaderBackground);

                    if (i != 0)
                    {
                        EditorGUI.DrawRect(unchangedMetadataHeaderRect.AlignTop(1), new Color(0.102f, 0.102f, 0.102f));
                    }

                    var foldoutTriangleRect = metadataHeaderRect.TakeFromLeft(headerHeight);

                    if (!isTag)
                        SdfIcons.DrawIcon(
                            foldoutTriangleRect.Padding(6),
                            metadataEntry.State.Expanded ? SdfIconType.CaretDownFill : SdfIconType.CaretRightFill,
                            new Color(0.443f, 0.443f, 0.443f));

                    var iconRect = metadataHeaderRect.TakeFromLeft(headerHeight);
                    SdfIcons.DrawIcon(
                        iconRect.Padding(0, 4, 4, 4),
                        isTag ? SdfIconType.TagFill : SdfIconType.GeoAltFill,
                        new Color(0.933f, 0.933f, 0.933f));

                    var contextMenuIconRect = metadataHeaderRect.TakeFromRight(headerHeight);

                    SdfIcons.DrawIcon(contextMenuIconRect.Padding(7), SdfIconType.ThreeDotsVertical);

                    var niceName = ObjectNames.NicifyVariableName(metadataEntry.ValueEntry.TypeOfValue.GetNiceName());

                    if (metadataEntry.Info.TypeOfOwner == typeof(Comment))
                    {
                        var headerLabelWidth = EditorStyles.label.CalcWidth(niceName) + 1;
                        var comment = ((string)metadataEntry.ValueEntry.WeakSmartValue).Split('\n')[0];
                        var hintRect = metadataHeaderRect.AlignRight(metadataHeaderRect.width - headerLabelWidth - 10);

                        if (SirenixGUIStyles.RightAlignedGreyMiniLabel.CalcWidth(comment) > hintRect.width)
                        {
                            if (hintRect.width < 30)
                            {
                                comment = "";
                            }
                            else
                            {
                                GUI.Label(hintRect.TakeFromLeft(11), "...", SirenixGUIStyles.RightAlignedGreyMiniLabel);
                            }
                        }

                        GUI.Label(hintRect, comment, SirenixGUIStyles.RightAlignedGreyMiniLabel);
                    }
                    else if (metadataEntry.Info.TypeOfOwner == typeof(PreloadAssetTableMetadata))
                    {
                        var headerLabelWidth = EditorStyles.label.CalcWidth(niceName) + 1;
                        var preloadAssetLabel = ObjectNames.NicifyVariableName(metadataEntry.ValueEntry.WeakSmartValue.ToString());
                        var hintRect = metadataHeaderRect.AlignRight(metadataHeaderRect.width - headerLabelWidth - 10);

                        if (SirenixGUIStyles.RightAlignedGreyMiniLabel.CalcWidth(preloadAssetLabel) > hintRect.width)
                        {
                            if (hintRect.width < 30)
                            {
                                preloadAssetLabel = "";
                            }
                            else
                            {
                                GUI.Label(hintRect.TakeFromLeft(11), "...", SirenixGUIStyles.RightAlignedGreyMiniLabel);
                            }
                        }

                        GUI.Label(hintRect, preloadAssetLabel, SirenixGUIStyles.RightAlignedGreyMiniLabel);
                    }
                    else if (metadataEntry.Info.TypeOfOwner == typeof(PlatformOverride))
                    {
                        var platformOverrides = metadataEntry.Children;
                        var headerLabelWidth = EditorStyles.label.CalcWidth(niceName) + 1;
                        var hintRect = metadataHeaderRect.AlignRight(metadataHeaderRect.width - headerLabelWidth - 10);

                        this.stringBuilder.Clear();
                        for (var j = 0; j < platformOverrides.Count; j++)
                        {
                            var platformOverrideInspectorProperty = platformOverrides[j];
                            var platform = (RuntimePlatform)platformOverrideInspectorProperty.Children[0].ValueEntry.WeakSmartValue;
                            this.stringBuilder.Append(platform);

                            if (j != platformOverrides.Count - 1)
                            {
                                this.stringBuilder.Append(", ");
                            }
                        }

                        var platforms = this.stringBuilder.ToString();

                        if (SirenixGUIStyles.RightAlignedGreyMiniLabel.CalcWidth(platforms) > hintRect.width)
                        {
                            if (hintRect.width < 30)
                            {
                                GUI.Label(hintRect, $"[{platformOverrides.Count}]", SirenixGUIStyles.RightAlignedGreyMiniLabel);
                                platforms = "";
                            }
                            else if (platformOverrides.Count > 1)
                            {
                                GUI.Label(hintRect.TakeFromLeft(11), "...", SirenixGUIStyles.RightAlignedGreyMiniLabel);
                            }
                        }

                        GUI.Label(hintRect, platforms, SirenixGUIStyles.RightAlignedGreyMiniLabel);
                    }

                    GUI.Label(metadataHeaderRect, niceName);

                    var evt = Event.current;
                    if (evt.OnMouseDown(contextMenuIconRect, 0, false))
                        genericMenu.ShowAsContext();
                    else if (evt.OnMouseDown(unchangedMetadataHeaderRect, 1, false)) genericMenu.ShowAsContext();

                    if (metadataEntry.ValueEntry.TypeOfValue == typeof(ExcludeEntryFromExport)) continue;

                    if (evt.OnMouseDown(unchangedMetadataHeaderRect, 0, false))
                    {
                        if (holdingControl)
                        {
                            var newState = !metadataEntry.State.Expanded;
                            foreach (var entry in filteredMetadataEntries)
                            {
                                entry.State.Expanded = newState;
                            }
                        }
                        else if (holdingAlt)
                        {
                            var newState = !metadataEntry.State.Expanded;
                            foreach (var entry in filteredMetadataEntries.Where(m => m.ValueEntry.TypeOfValue == this.lastMetadataTypeUnderCursor))
                            {
                                entry.State.Expanded = newState;
                            }
                        }
                        else
                        {
                            metadataEntry.State.Expanded = !metadataEntry.State.Expanded;
                        }

                        GUIHelper.RemoveFocusControl();
                    }

                    if (SirenixEditorGUI.BeginFadeGroup(metadataEntry, metadataEntry.State.Expanded))
                    {
                        EditorGUI.DrawRect(unchangedMetadataHeaderRect.AlignBottom(1),
                            new Color(0.188f, 0.188f, 0.188f));

                        var contentRect = EditorGUILayout.BeginVertical();

                        if (metadataEntry.ValueEntry.TypeOfValue == typeof(Comment))
                        {
                            EditorGUILayout.BeginVertical(this.ContentPadding);
                            foreach (var child in metadataEntry.Children)
                            {
                                child.Draw(null);
                            }
                            EditorGUILayout.EndVertical();
                        }
                        else if (metadataEntry.ValueEntry.TypeOfValue == typeof(PreloadAssetTableMetadata))
                        {
                            EditorGUILayout.BeginVertical(this.ContentPadding);

                            var preloadAssetTableMetadata = (PreloadAssetTableMetadata)metadataEntry.ValueEntry.WeakSmartValue;
                            var btnRect = EditorGUILayout.GetControlRect(false);
                            var leftRect = btnRect.Split(0, 2);
                            var mouseOverLeft = Event.current.IsMouseOver(leftRect);
                            var leftActive = mouseOverLeft && Event.current.type == EventType.MouseDown && Event.current.button == 0;
                            var leftOn = preloadAssetTableMetadata.Behaviour == PreloadAssetTableMetadata.PreloadBehaviour.NoPreload;

                            var rightRect = btnRect.Split(1, 2);
                            var mouseOverRight = Event.current.IsMouseOver(rightRect);
                            var rightActive = mouseOverRight && Event.current.type == EventType.MouseDown && Event.current.button == 0;
                            var rightOn = preloadAssetTableMetadata.Behaviour == PreloadAssetTableMetadata.PreloadBehaviour.PreloadAll;

                            if (Event.current.type == EventType.Repaint)
                            {
                                SirenixGUIStyles.ButtonLeft.Draw(leftRect, "No Preload", mouseOverLeft, leftActive, leftOn, false);
                                SirenixGUIStyles.ButtonRight.Draw(rightRect, "Preload All", mouseOverRight, rightActive, rightOn, false);
                            }

                            if (Event.current.OnMouseDown(leftRect, 0, false))
                            {
                                preloadAssetTableMetadata.Behaviour = PreloadAssetTableMetadata.PreloadBehaviour.NoPreload;
                            }
                            else if (Event.current.OnMouseDown(rightRect, 0, false))
                            {
                                preloadAssetTableMetadata.Behaviour = PreloadAssetTableMetadata.PreloadBehaviour.PreloadAll;
                            }

                            EditorGUILayout.EndVertical();
                        }
                        else if (metadataEntry.ValueEntry.TypeOfValue == typeof(PlatformOverride))
                        {
                            this.DrawPlatformOverride(metadataEntry);
                        }
                        else
                        {
                            EditorGUILayout.BeginVertical(this.ContentPadding);
                            foreach (var child in metadataEntry.Children)
                            {
                                child.Draw();
                            }
                            EditorGUILayout.EndVertical();
                        }

                        EditorGUILayout.EndVertical();
                    }

                    SirenixEditorGUI.EndFadeGroup();
                }
            }
            var measureRect = GUIHelper.EndLayoutMeasuring();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            var pinAddMetadataButton = measureRect.height > metadataViewHeight;
            rect.TakeFromTop(pinAddMetadataButton ? metadataViewHeight : measureRect.height);

            if (metadataEntries.Count > 1)
            {
                EditorGUI.DrawRect(rect.TakeFromTop(1), SirenixGUIStyles.BorderColor);
            }

            var addMetadataRect = rect.AlignTop(addButtonAreaHeight).AlignCenter(Mathf.Min(200, rect.width - 16)).VerticalPadding(8);

            if (GUI.Button(addMetadataRect, "Add Metadata"))
            {
                openMetadataSelector();
            }

            // Draw the shadow that appears under the search bar while scrolling through the metadata.
            GUI.DrawTexture(
                searchAreaRect.AlignBottom(10).AddY(10),
                OdinLocalizationGUITextures.TopToBottomFade,
                ScaleMode.StretchToFill,
                true,
                1.0f,
                new Color(0, 0, 0, Mathf.InverseLerp(0f, 30f, this.scrollPosition.y) * 0.6f),
                0,
                0);

            if (measureRect.height > metadataViewHeight)
            {
                var pos = this.scrollPosition.y + metadataViewHeight;
                // Draw the shadow that appears at the bottom of the metadata while scrolling through the metadata
                GUI.DrawTexture(
                    rect.AlignTop(10).SubY(10),
                    OdinLocalizationGUITextures.BottomToTopFade,
                    ScaleMode.StretchToFill,
                    true,
                    1.0f,
                    new Color(0, 0, 0, Mathf.InverseLerp(measureRect.height, measureRect.height - 30, pos) * 0.6f),
                    0,
                    0);
            }

            return measureRect.width;
        }

        private void DrawPlatformOverride(InspectorProperty metadataEntry)
        {
            var platformOverride = (PlatformOverride)metadataEntry.ValueEntry.WeakSmartValue;
            var platformOverrideDatas = (IList)metadataEntry.Children["m_PlatformOverrides"]?.ValueEntry.WeakSmartValue;
            var platformOverrideDataInspectorProperties = metadataEntry.Children["m_PlatformOverrides"]?.Children;

            if (platformOverrideDatas == null)
            {
                return;
            }

            if (platformOverrideDatas.Count == 0)
            {
                platformOverride.AddPlatformOverride(RuntimePlatform.WindowsPlayer, null, null, EntryOverrideType.None);
            }

            for (var i = 0; i < platformOverrideDataInspectorProperties.Count; i++)
            {
                var platformOverrideDataInspectorProperty = platformOverrideDataInspectorProperties[i];
                var backgroundRect = EditorGUILayout.BeginVertical(this.PlatformOverridePadding);
                var backgroundColor = i % 2 == 0 ? SirenixGUIStyles.ListItemColorEven : SirenixGUIStyles.ListItemColorOdd;
                EditorGUI.DrawRect(backgroundRect, backgroundColor);
                platformOverrideDataInspectorProperty.Children["platform"].Draw();
                platformOverrideDataInspectorProperty.Children["entryOverrideType"].Draw();

                var entryOverrideType = (EntryOverrideType)platformOverrideDataInspectorProperty.Children
                    .FirstOrDefault(c => c.Info.TypeOfValue == typeof(EntryOverrideType))?.ValueEntry.WeakSmartValue;

                var entryOverrideRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayoutOptions.ExpandWidth(true).ExpandHeight(false));

                switch (entryOverrideType)
                {
                    case EntryOverrideType.Table:
                    {
                        var tableReference = (TableReference)platformOverrideDataInspectorProperty.Children.FirstOrDefault(c => c.Info.TypeOfValue == typeof(TableReference))?.ValueEntry.WeakSmartValue;
                        this.DoTableGUI(entryOverrideRect, tableReference, platformOverrideDataInspectorProperty);
                        break;
                    }
                    case EntryOverrideType.Entry:
                    {
                        var tableReference = (TableReference)platformOverrideDataInspectorProperty.Children.FirstOrDefault(c => c.Info.TypeOfValue == typeof(TableReference))
                            ?.ValueEntry.WeakSmartValue;
                        var tableEntryReference = (TableEntryReference)platformOverrideDataInspectorProperty.Children.FirstOrDefault(c => c.Info.TypeOfValue == typeof(TableEntryReference))?.ValueEntry.WeakSmartValue;
                        this.DoTableAndEntryGUI(entryOverrideRect, tableReference, tableEntryReference, platformOverrideDataInspectorProperty, false);
                        break;
                    }

                    case EntryOverrideType.TableAndEntry:
                    {
                        var tableReference = (TableReference)platformOverrideDataInspectorProperty.Children.FirstOrDefault(c => c.Info.TypeOfValue == typeof(TableReference))?.ValueEntry.WeakSmartValue;
                        var tableEntryReference = (TableEntryReference)platformOverrideDataInspectorProperty.Children.FirstOrDefault(c => c.Info.TypeOfValue == typeof(TableEntryReference))?.ValueEntry.WeakSmartValue;
                        this.DoTableAndEntryGUI(entryOverrideRect, tableReference, tableEntryReference, platformOverrideDataInspectorProperty, true);
                        break;
                    }
                }

                EditorGUILayout.EndVertical();

                var xIconRect = backgroundRect.AlignRight(10).SubX(4).AlignCenterY(10);
                var mouseOverX = Event.current.IsMouseOver(xIconRect);
                SdfIcons.DrawIcon(xIconRect, SdfIconType.X, mouseOverX ? Color.white : EditorStyles.label.normal.textColor);

                if (Event.current.OnMouseDown(xIconRect, 0, false))
                {
                    var platform = (RuntimePlatform)platformOverrideDataInspectorProperty.Children[0].ValueEntry.WeakSmartValue;
                    platformOverride.RemovePlatformOverride(platform);
                }
            }

            var addButtonRect = GUILayoutUtility.GetRect(0, 40, GUILayoutOptions.ExpandWidth().ExpandHeight(false));
            EditorGUI.DrawRect(addButtonRect.AlignTop(1), new Color(0.188f, 0.188f, 0.188f));

            if (GUI.Button(addButtonRect.Padding(8), "Add Platform Override"))
            {
                var selector = new EnumSelector<RuntimePlatform>();
                selector.SelectionConfirmed += platforms =>
                {
                    var platform = platforms.FirstOrDefault();
                    platformOverride.AddPlatformOverride(platform, null, null, EntryOverrideType.None);
                };
                selector.ShowInPopup();
            }
        }

        private void OpenMetadataSelector(LocalizationTableCollection localizationTableCollection)
        {
            this.OpenMetadataSelector(localizationTableCollection.SharedData.Metadata.MetadataEntries, MetadataType.SharedTableData);
        }

        private void OpenMetadataSelector(LocalizationTable localizationTable)
        {
            switch (localizationTable)
            {
                case StringTable _:
                    this.OpenMetadataSelector(localizationTable.MetadataEntries, MetadataType.StringTable);
                    break;
                case AssetTable _:
                    this.OpenMetadataSelector(localizationTable.MetadataEntries, MetadataType.AssetTable);
                    break;
            }
        }

        private void OpenMetadataSelector(SharedTableData.SharedTableEntry sharedTableEntry)
        {
            switch (this.localizationTableCollection)
            {
                case StringTableCollection _:
                    this.OpenMetadataSelector(sharedTableEntry.Metadata.MetadataEntries, MetadataType.StringTableEntry, MetadataType.SharedStringTableEntry);
                    break;
                case AssetTableCollection _:
                    this.OpenMetadataSelector(sharedTableEntry.Metadata.MetadataEntries, MetadataType.AssetTableEntry, MetadataType.SharedAssetTableEntry);
                    break;
            }
        }

        private void OpenMetadataSelector(TableEntry tableEntry)
        {
            switch (tableEntry)
            {
                case StringTableEntry _:
                    this.OpenMetadataSelector(tableEntry.MetadataEntries, MetadataType.StringTableEntry);
                    break;
                case AssetTableEntry _:
                    this.OpenMetadataSelector(tableEntry.MetadataEntries, MetadataType.AssetTableEntry);
                    break;
            }
        }

        private void OpenMetadataSelector(IList<IMetadata> existingMetadata, params MetadataType[] allowedTypes)
        {
            var existingMetadataTypes = existingMetadata.Select(m => m?.GetType()).ToList();
            var metadataTypes = TypeCache.GetTypesDerivedFrom<IMetadata>();
            var validMetadataTypes = new List<Type>();

            foreach (var metadataType in metadataTypes)
            {
                var metadataAttribute = metadataType.GetCustomAttribute<MetadataAttribute>();
                if (metadataAttribute != null && allowedTypes.Any(allowedType => metadataAttribute.AllowedTypes.HasFlag(allowedType)))
                {
                    if (existingMetadataTypes.Contains(metadataType) && !OdinLocalizationMetadataRegistry.MetadataAllowsMultiple.ContainsKey(metadataType)) continue;

                    validMetadataTypes.Add(metadataType);
                }
            }

            var selector = new GenericSelector<Type>("", validMetadataTypes, false, type => ObjectNames.NicifyVariableName(type.Name));

            foreach (var item in selector.SelectionTree.MenuItems)
            {
                item.SdfIcon = SdfIconType.Braces;
            }

            selector.SelectionConfirmed += types =>
            {
                var selectedType = types.FirstOrDefault();

                if (selectedType == null)
                {
                    return;
                }

                var metadata = (IMetadata)Activator.CreateInstance(selectedType);
                this.toBeAdded = metadata;
            };
            selector.EnableSingleClickToSelect();
            selector.ShowInPopup();
        }

        public class TableReferenceSelector : OdinSelector<LocalizationTableCollection>
        {
            private Type tableType;

            public TableReferenceSelector(Type tableType)
            {
                this.tableType = tableType;
            }

            protected override void BuildSelectionTree(OdinMenuTree tree)
            {
                tree.Config.SelectMenuItemsOnMouseDown = true;
                tree.Selection.SupportsMultiSelect = false;

                var collectionGUIDs = AssetDatabase.FindAssets($"t:{nameof(LocalizationTableCollection)}");

                foreach (var guid in collectionGUIDs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var collection = AssetDatabase.LoadAssetAtPath<LocalizationTableCollection>(path);

                    if (collection is null) continue;
                    if (collection.Tables.Count < 1) continue;
                    if (collection.Tables[0].asset.GetType() != this.tableType) continue;

                    var collectionItem = new OdinMenuItem(tree, collection.name, collection)
                    {
                        SdfIcon = SdfIconType.Table
                    };

                    tree.MenuItems.Add(collectionItem);
                }
            }
        }

        public class TableEntryReferenceSelector : OdinSelector<TableEntryReferenceSelector.TableEntry>
        {
            private Type tableType;

            public struct TableEntry
            {
                public LocalizationTableCollection Collection;
                public SharedTableData.SharedTableEntry SharedEntry;
            }

            public TableEntryReferenceSelector(Type tableType)
            {
                this.tableType = tableType;
            }

            protected override void BuildSelectionTree(OdinMenuTree tree)
            {
                tree.Config.SelectMenuItemsOnMouseDown = true;
                tree.Selection.SupportsMultiSelect = false;


                var collectionGUIDs = AssetDatabase.FindAssets($"t:{nameof(LocalizationTableCollection)}");

                foreach (var guid in collectionGUIDs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var collection = AssetDatabase.LoadAssetAtPath<LocalizationTableCollection>(path);

                    if (collection is null) continue;
                    if (collection.Tables.Count < 1) continue;
                    if (collection.Tables[0].asset.GetType() != this.tableType) continue;

                    var collectionItem = new OdinMenuItem(tree, collection.name, collection)
                    {
                        SdfIcon = SdfIconType.Table
                    };

                    List<SharedTableData.SharedTableEntry> entries = collection.SharedData.Entries;

                    for (var j = 0; j < entries.Count; j++)
                    {
                        SharedTableData.SharedTableEntry entry = entries[j];

                        var tableEntry = new TableEntry {Collection = collection, SharedEntry = entry};

                        var entryItem = new OdinMenuItem(tree, entry.Key, tableEntry)
                        {
                            SdfIcon = SdfIconType.KeyFill
                        };

                        collectionItem.ChildMenuItems.Add(entryItem);
                    }

                    collectionItem.IsSelectable = false;
                    tree.MenuItems.Add(collectionItem);
                }
            }
        }

        private void DoTableGUI(Rect rect, TableReference tableReference, InspectorProperty platformOverrideData)
        {
            var dropDownPosition = EditorGUI.PrefixLabel(rect, new GUIContent("Table Collection"));
            var label = tableReference.TableCollectionName;

            if (EditorGUI.DropdownButton(dropDownPosition, new GUIContent(label), FocusType.Passive))
            {
                TableReferenceSelector tableReferenceSelector;

                tableReferenceSelector = this.localizationTableCollection.GetType() == typeof(AssetTableCollection)
                    ? new TableReferenceSelector(typeof(AssetTable))
                    : new TableReferenceSelector(typeof(StringTable));

                tableReferenceSelector.GetCurrentSelection();

                tableReferenceSelector.SelectionConfirmed += selections =>
                {
                    var selection = selections.FirstOrDefault();

                    var platformOverrideType = typeof(PlatformOverride);
                    var platformOverrideDataType = platformOverrideType.GetNestedType("PlatformOverrideData", BindingFlags.NonPublic);
                    
                    platformOverrideDataType
                        .GetField("tableReference")
                        .SetValue(platformOverrideData.ValueEntry.WeakSmartValue, selection.TableCollectionNameReference);

                    platformOverrideDataType
                        .GetField("tableEntryReference")
                        .SetValue(platformOverrideData.ValueEntry.WeakSmartValue, (TableEntryReference) string.Empty);
                };

                tableReferenceSelector.ShowInPopup();
            }
        }

        private void DoTableAndEntryGUI(Rect rect,
                                        TableReference tableReference,
                                        TableEntryReference tableEntryReference,
                                        InspectorProperty platformOverrideData,
                                        bool displayTableName)
        {
            var dropDownPosition = EditorGUI.PrefixLabel(rect, new GUIContent("Reference"));

            var entryLabel = tableEntryReference.ReferenceType != TableEntryReference.Type.Empty
                                 ? tableEntryReference.ResolveKeyName(this.localizationTableCollection.SharedData)
                                 : null;

            string label;
            if (displayTableName)
            {
                var tableLabel = tableReference.TableCollectionName ?? "";
                label = string.IsNullOrEmpty(tableLabel) || string.IsNullOrEmpty(entryLabel) ? "None" : $"{tableLabel}/{entryLabel}";
            }
            else
            {
                label = string.IsNullOrWhiteSpace(entryLabel) ? "None" : $"{entryLabel}";
            }

            if (EditorGUI.DropdownButton(dropDownPosition, new GUIContent(label), FocusType.Passive))
            {
                Type tableType = this.localizationTableCollection.GetType() == typeof(AssetTableCollection)
                    ? typeof(AssetTable)
                    : typeof(StringTable);

                TableEntryReferenceSelector tableEntryReferenceSelector = new TableEntryReferenceSelector(tableType);
                tableEntryReferenceSelector.SelectionConfirmed += selections =>
                {
                    var selection = selections.FirstOrDefault();
                    var platformOverrideType = typeof(PlatformOverride);
                    var platformOverrideDataType = platformOverrideType.GetNestedType("PlatformOverrideData", BindingFlags.NonPublic);

                    platformOverrideDataType
                        .GetField("tableReference")
                        .SetValue(platformOverrideData.ValueEntry.WeakSmartValue, selection.Collection.TableCollectionNameReference);
                    
                    platformOverrideDataType
                        .GetField("tableEntryReference")
                        .SetValue(platformOverrideData.ValueEntry.WeakSmartValue, (TableEntryReference) selection.SharedEntry.Key);
                };

                tableEntryReferenceSelector.ShowInPopup();
            }
        }
    }
}