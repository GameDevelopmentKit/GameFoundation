//-----------------------------------------------------------------------
// <copyright file="VectorIntDrawers.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER

namespace Sirenix.OdinInspector.Editor.Drawers
{
    using Utilities.Editor;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Vector2Int proprety drawer.
    /// </summary>
    public sealed class Vector2IntDrawer : OdinValueDrawer<Vector2Int>, IDefinesGenericMenuItems
    {
        /// <summary>
        /// Draws the property.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Rect labelRect;
            var  contentRect = SirenixEditorGUI.BeginHorizontalPropertyLayout(label, out labelRect);
            {
                EditorGUI.BeginChangeCheck();
                var val                                                    = SirenixEditorFields.VectorPrefixSlideRect(labelRect, (Vector2)this.ValueEntry.SmartValue);
                if (EditorGUI.EndChangeCheck()) this.ValueEntry.SmartValue = new((int)val.x, (int)val.y);

                var showLabels = SirenixEditorFields.ResponsiveVectorComponentFields && contentRect.width >= 185;
                GUIHelper.PushLabelWidth(SirenixEditorFields.SingleLetterStructLabelWidth);
                this.ValueEntry.Property.Children[0].Draw(showLabels ? GUIHelper.TempContent("X") : null);
                this.ValueEntry.Property.Children[1].Draw(showLabels ? GUIHelper.TempContent("Y") : null);
                GUIHelper.PopLabelWidth();
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        /// <summary>
        /// Populates the generic menu for the property.
        /// </summary>
        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
        {
            var value = (Vector2Int)property.ValueEntry.WeakSmartValue;

            if (genericMenu.GetItemCount() > 0) genericMenu.AddSeparator("");
            genericMenu.AddItem(new("Zero", "Set the vector to (0, 0)"), value == Vector2Int.zero, () => this.SetVector(property, Vector2Int.zero));
            genericMenu.AddItem(new("One", "Set the vector to (1, 1)"), value == Vector2Int.one, () => this.SetVector(property, Vector2Int.one));
            genericMenu.AddSeparator("");
            genericMenu.AddItem(new("Right", "Set the vector to (1, 0)"), value == Vector2Int.right, () => this.SetVector(property, Vector2Int.right));
            genericMenu.AddItem(new("Left", "Set the vector to (-1, 0)"), value == Vector2Int.left, () => this.SetVector(property, Vector2Int.left));
            genericMenu.AddItem(new("Up", "Set the vector to (0, 1)"), value == Vector2Int.up, () => this.SetVector(property, Vector2Int.up));
            genericMenu.AddItem(new("Down", "Set the vector to (0, -1)"), value == Vector2Int.down, () => this.SetVector(property, Vector2Int.down));
        }

        private void SetVector(InspectorProperty property, Vector2Int value)
        {
            property.Tree.DelayActionUntilRepaint(() =>
            {
                for (var i = 0; i < property.ValueEntry.ValueCount; i++) property.ValueEntry.WeakValues[i] = value;
            });
        }
    }

    /// <summary>
    /// Vector3Int property drawer.
    /// </summary>
    public sealed class Vector3IntDrawer : OdinValueDrawer<Vector3Int>, IDefinesGenericMenuItems
    {
        /// <summary>
        /// Draws the property.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Rect labelRect;
            var  contentRect = SirenixEditorGUI.BeginHorizontalPropertyLayout(label, out labelRect);
            {
                EditorGUI.BeginChangeCheck();
                var val                                                    = SirenixEditorFields.VectorPrefixSlideRect(labelRect, (Vector3)this.ValueEntry.SmartValue);
                if (EditorGUI.EndChangeCheck()) this.ValueEntry.SmartValue = new((int)val.x, (int)val.y, (int)val.z);

                var showLabels = SirenixEditorFields.ResponsiveVectorComponentFields && contentRect.width >= 185;
                GUIHelper.PushLabelWidth(SirenixEditorFields.SingleLetterStructLabelWidth);
                this.ValueEntry.Property.Children[0].Draw(showLabels ? GUIHelper.TempContent("X") : null);
                this.ValueEntry.Property.Children[1].Draw(showLabels ? GUIHelper.TempContent("Y") : null);
                this.ValueEntry.Property.Children[2].Draw(showLabels ? GUIHelper.TempContent("Z") : null);
                GUIHelper.PopLabelWidth();
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        /// <summary>
        /// Populates the generic menu for the property.
        /// </summary>
        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
        {
            var value = (Vector3Int)property.ValueEntry.WeakSmartValue;

            if (genericMenu.GetItemCount() > 0) genericMenu.AddSeparator("");

            genericMenu.AddItem(new("Zero", "Set the vector to (0, 0, 0)"), value == Vector3Int.zero, () => this.SetVector(property, Vector3Int.zero));
            genericMenu.AddItem(new("One", "Set the vector to (1, 1, 1)"), value == Vector3Int.one, () => this.SetVector(property, Vector3Int.one));
            genericMenu.AddSeparator("");
            genericMenu.AddItem(new("Right", "Set the vector to (1, 0, 0)"), value == Vector3Int.right, () => this.SetVector(property, Vector3Int.right));
            genericMenu.AddItem(new("Left", "Set the vector to (-1, 0, 0)"), value == Vector3Int.left, () => this.SetVector(property, Vector3Int.left));
            genericMenu.AddItem(new("Up", "Set the vector to (0, 1, 0)"), value == Vector3Int.up, () => this.SetVector(property, Vector3Int.up));
            genericMenu.AddItem(new("Down", "Set the vector to (0, -1, 0)"), value == Vector3Int.down, () => this.SetVector(property, Vector3Int.down));
            genericMenu.AddItem(new("Forward", "Set the vector property to (0, 0, 1)"), value == new Vector3Int(0, 0, 1), () => this.SetVector(property, new(0, 0, 1)));
            genericMenu.AddItem(new("Back", "Set the vector property to (0, 0, -1)"), value == new Vector3Int(0, 0, -1), () => this.SetVector(property, new(0, 0, -1)));
        }

        private void SetVector(InspectorProperty property, Vector3Int value)
        {
            property.Tree.DelayActionUntilRepaint(() =>
            {
                property.ValueEntry.WeakSmartValue = value;
            });
        }
    }
}

#endif // UNITY_EDITOR && UNITY_2017_2_OR_NEWER