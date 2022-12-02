using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomPropertyDrawer(typeof(MasterCustomEventAttribute))]
    // ReSharper disable once CheckNamespace
    public class MasterCustomEventPropertyDrawer : PropertyDrawer
    {
        // ReSharper disable once InconsistentNaming
        public int index;
        // ReSharper disable once InconsistentNaming
        public bool typeIn;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!typeIn)
            {
                return base.GetPropertyHeight(property, label);
            }
            return base.GetPropertyHeight(property, label) + 16;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var ma = MasterAudio.SafeInstance;
            // ReSharper disable once RedundantAssignment
            var groupName = "[Type In]";

            var eventNames = new List<string>();

            var labelText = label.text;
            if (ma != null)
            {
                eventNames.AddRange(ma.CustomEventNames);
            }
            else
            {
                eventNames.AddRange(MasterAudio.CustomEventHardCodedNames);
                labelText += " (MA not in Scene)";
            }

            var creators = Object.FindObjectsOfType(typeof(DynamicSoundGroupCreator)) as DynamicSoundGroupCreator[];
            // ReSharper disable once PossibleNullReferenceException
            foreach (var dsgc in creators)
            {
                foreach (var custom in dsgc.customEventsToCreate)
                {
                    eventNames.Add(custom.EventName);
                }
            }

            eventNames.Sort();
            if (eventNames.Count > 1)
            { // "type in" back to index 0 (sort puts it at #1)
                eventNames.Insert(0, eventNames[1]);
            }

            if (eventNames.Count == 0)
            {
                index = -1;
                typeIn = false;
                property.stringValue = EditorGUI.TextField(position, labelText, property.stringValue);
                return;
            }

            index = eventNames.IndexOf(property.stringValue);

            if (typeIn || index == -1)
            {
                index = 0;
                typeIn = true;
                position.height -= 16;
            }

            index = EditorGUI.Popup(position, labelText, index, eventNames.ToArray());
            groupName = eventNames[index];

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
                default:
                    typeIn = false;
                    property.stringValue = groupName;
                    break;
            }
        }
    }
}