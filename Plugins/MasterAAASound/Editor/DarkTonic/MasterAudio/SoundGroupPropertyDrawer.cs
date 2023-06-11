using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomPropertyDrawer(typeof(SoundGroupAttribute))]
    // ReSharper disable once CheckNamespace
    public class SoundGroupPropertyDrawer : PropertyDrawer
    {
        // ReSharper disable once InconsistentNaming
        public int index;
        // ReSharper disable once InconsistentNaming
        public bool typeIn;
        public bool hasError;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (typeIn)
            {
                return base.GetPropertyHeight(property, label) + 16;
            }

            if (hasError)
            {
                return base.GetPropertyHeight(property, label) + 48;
            }

            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var ma = MasterAudio.SafeInstance;
            // ReSharper disable once RedundantAssignment
            var groupName = MasterAudio.NoGroupName;

            var groupNames = new List<string>();

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            var labelText = label.text;
            if (ma != null)
            {
                groupNames.AddRange(ma.GroupNames);
            }
            else
            {
                groupNames.AddRange(MasterAudio.SoundGroupHardCodedNames);
                labelText += " (MA not in Scene)";
            }

            var creators = Object.FindObjectsOfType(typeof(DynamicSoundGroupCreator)) as DynamicSoundGroupCreator[];
            // ReSharper disable once PossibleNullReferenceException
            foreach (var dsgc in creators)
            {
                var trans = dsgc.transform;
                for (var i = 0; i < trans.childCount; ++i)
                {
                    var group = trans.GetChild(i).GetComponent<DynamicSoundGroup>();
                    if (group != null)
                    {
                        groupNames.Add(group.name);
                    }
                }
            }

            groupNames.Sort();
            if (groupNames.Count > 1)
            { // "type in" back to index 0 (sort puts it at #1)
                groupNames.Insert(0, groupNames[1]);
            }

            if (groupNames.Count == 0)
            {
                index = -1;
                typeIn = false;
                property.stringValue = EditorGUI.TextField(position, labelText, property.stringValue);
                return;
            }

            index = groupNames.IndexOf(property.stringValue);

            if (typeIn || index == -1)
            {
                index = 0;
                typeIn = true;
                position.height -= 16;
            }

            position.width -= 82;
            index = EditorGUI.Popup(position, labelText, index, groupNames.ToArray());
            groupName = groupNames[index];

            switch (groupName)
            {
                case "[Type In]":
                    typeIn = true;
                    position.yMin += 16;
                    position.height += 16;
                    EditorGUI.BeginChangeCheck();
                    property.stringValue = EditorGUI.TextField(position, labelText, property.stringValue);
                    EditorGUI.EndChangeCheck();
                    break;
                case MasterAudio.VideoPlayerSoundGroupName:
                    property.stringValue = groupName;
                    hasError = true;
                    EditorGUI.HelpBox(new Rect(position.x, position.y + 20, position.xMax - position.x, 40), 
                        MasterAudio.VideoPlayersSoundGroupSelectedError, MessageType.Error);
                    break;
                default:
                    hasError = false;
                    typeIn = false;
                    property.stringValue = groupName;
                    break;
            }

            if (typeIn || property.stringValue == MasterAudio.NoGroupName)
            {
                return;
            }

            var sType = property.stringValue;
            var settingsIcon = MasterAudioInspectorResources.GearTexture;
            var buttonRect = new Rect(position.xMax + 4, position.y, 24, 16);

            if (GUI.Button(buttonRect, new GUIContent("", settingsIcon)))
            {
                var trs = MasterAudio.FindGroupTransform(property.stringValue);
                if (trs != null)
                {
                    Selection.activeObject = trs;
                }
            }

            buttonRect = new Rect(position.xMax + 30, position.y, 24, 16);
            if (GUI.Button(buttonRect, new GUIContent("", MasterAudioInspectorResources.PreviewTexture)))
            {
                DTGUIHelper.PreviewSoundGroup(sType);
            }

            buttonRect = new Rect(position.xMax + 56, position.y, 24, 16);
            if (GUI.Button(buttonRect, new GUIContent("", MasterAudioInspectorResources.StopTexture)))
            {
                DTGUIHelper.StopPreview(sType);
            }
        }
    }
}