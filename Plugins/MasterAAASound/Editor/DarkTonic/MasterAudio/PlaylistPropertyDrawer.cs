using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomPropertyDrawer(typeof(PlaylistAttribute))]
    // ReSharper disable once CheckNamespace
    public class PlaylistPropertyDrawer : PropertyDrawer
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
            var playlistName = MasterAudio.NoGroupName;

            var playlistNames = new List<string>();

            var labelText = label.text;
            if (ma != null)
            {
                playlistNames.AddRange(ma.PlaylistNames);
            } else {
                labelText += " (MA not in Scene)";
            }

            var creators = Object.FindObjectsOfType(typeof(DynamicSoundGroupCreator)) as DynamicSoundGroupCreator[];
            // ReSharper disable once PossibleNullReferenceException
            foreach (var dsgc in creators)
            {
                foreach (var playlist in dsgc.musicPlaylists)
                {
                    playlistNames.Add(playlist.playlistName);
                }
            }

            playlistNames.Sort();
            if (playlistNames.Count > 1)
            { // "type in" back to index 0 (sort puts it at #1)
                playlistNames.Insert(0, playlistNames[1]);
            }

            if (playlistNames.Count == 0)
            {
                index = -1;
                typeIn = false;
                property.stringValue = EditorGUI.TextField(position, labelText, property.stringValue);
                return;
            }

            index = playlistNames.IndexOf(property.stringValue);

            if (typeIn || index == -1)
            {
                index = 0;
                typeIn = true;
                position.height -= 16;
            }

            position.width -= 30;
            index = EditorGUI.Popup(position, labelText, index, playlistNames.ToArray());
            playlistName = playlistNames[index];

            switch (playlistName)
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
                    property.stringValue = playlistName;
                    break;
            }

            if (typeIn || property.stringValue == MasterAudio.NoGroupName)
            {
                return;
            }

            var settingsIcon = MasterAudioInspectorResources.GearTexture;
            var buttonRect = new Rect(position.xMax + 4, position.y, 24, 16);

            if (GUI.Button(buttonRect, new GUIContent("", settingsIcon)))
            {
                Selection.activeObject = ma;
            }
        }
    }
}