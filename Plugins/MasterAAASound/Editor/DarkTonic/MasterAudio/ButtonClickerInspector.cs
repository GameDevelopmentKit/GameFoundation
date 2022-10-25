using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(ButtonClicker))]
    // ReSharper disable once CheckNamespace
    public class ButtonClickerInspector : Editor
    {
        private List<string> _groupNames;
        private bool _maInScene;
        private bool _isDirty;

        protected virtual void PopulateGroupNames(List<string> groups)
        {
            if (groups != null)
            {
                var creators = FindObjectsOfType(typeof(DynamicSoundGroupCreator)) as DynamicSoundGroupCreator[];
                // ReSharper disable once PossibleNullReferenceException
                foreach (var dsgc in creators)
                {
                    var trans = dsgc.transform;
                    for (var i = 0; i < trans.childCount; ++i)
                    {
                        var group = trans.GetChild(i).GetComponent<DynamicSoundGroup>();
                        if (group != null)
                        {
                            groups.Add(group.name);
                        }
                    }
                }
                groups.Sort();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel = 0;

            var ma = MasterAudio.Instance;
            _maInScene = ma != null;

            if (_maInScene)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            var sounds = (ButtonClicker)target;

            if (_maInScene)
            {
                // ReSharper disable once PossibleNullReferenceException
                _groupNames = ma.GroupNames;
            }
            PopulateGroupNames(_groupNames);

            var resizeOnClick = EditorGUILayout.Toggle("Resize On Click", sounds.resizeOnClick);

            if (resizeOnClick != sounds.resizeOnClick)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, sounds, "change Resize On Click");
                sounds.resizeOnClick = resizeOnClick;
            }

            if (sounds.resizeOnClick)
            {
                EditorGUI.indentLevel = 1;
                var newResize = EditorGUILayout.Toggle("Resize All Siblings", sounds.resizeClickAllSiblings);
                if (newResize != sounds.resizeClickAllSiblings)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, sounds, "Toggle Resize All Siblings");
                    sounds.resizeClickAllSiblings = newResize;
                }
            }

            EditorGUI.indentLevel = 0;
            var resizeOnHover = EditorGUILayout.Toggle("Resize On Hover", sounds.resizeOnHover);

            if (resizeOnHover != sounds.resizeOnHover)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, sounds, "change Resize On Hover");
                sounds.resizeOnHover = resizeOnHover;
            }

            if (sounds.resizeOnHover)
            {
                EditorGUI.indentLevel = 1;
                var newResize = EditorGUILayout.Toggle("Resize All Siblings", sounds.resizeHoverAllSiblings);
                if (newResize != sounds.resizeHoverAllSiblings)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, sounds, "Toggle Resize All Siblings");
                    sounds.resizeHoverAllSiblings = newResize;
                }
            }

            EditorGUI.indentLevel = 0;

            EditSoundGroup(sounds, ref sounds.mouseDownSound, "Mouse Down Sound");
            EditSoundGroup(sounds, ref sounds.mouseUpSound, "Mouse Up Sound");
            EditSoundGroup(sounds, ref sounds.mouseClickSound, "Mouse Click Sound");
            EditSoundGroup(sounds, ref sounds.mouseOverSound, "Mouse Over Sound");
            EditSoundGroup(sounds, ref sounds.mouseOutSound, "Mouse Out Sound");

            if (GUI.changed || _isDirty)
            {
                EditorUtility.SetDirty(target);
            }

            //DrawDefaultInspector();
        }

        protected void EditSoundGroup(ButtonClicker sounds, ref string soundGroup, string label)
        {
            DTGUIHelper.StartGroupHeader();
            if (_maInScene)
            {
                var existingIndex = _groupNames.IndexOf(soundGroup);

                int? groupIndex = null;

                var noMatch = false;

                if (existingIndex >= 1)
                {
                    groupIndex = EditorGUILayout.Popup(label, existingIndex, _groupNames.ToArray());
                }
                else if (existingIndex == -1 && soundGroup == MasterAudio.NoGroupName)
                {
                    groupIndex = EditorGUILayout.Popup(label, existingIndex, _groupNames.ToArray());
                }
                else
                { // non-match
                    noMatch = true;

                    var newGroup = EditorGUILayout.TextField(label, soundGroup);
                    if (newGroup != soundGroup)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, sounds, "change Sound Group");
                        soundGroup = newGroup;
                    }
                    var newIndex = EditorGUILayout.Popup("All Sound Types", -1, _groupNames.ToArray());
                    if (newIndex >= 0)
                    {
                        groupIndex = newIndex;
                    }
                }

                if (noMatch)
                {
                    DTGUIHelper.ShowRedError("Sound Type found no match. Choose one from 'All Sound Types'.");
                }

                if (!groupIndex.HasValue)
                {
                    DTGUIHelper.EndGroupHeader();
                    return;
                }

                if (existingIndex != groupIndex.Value)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, sounds, "change Sound Group");
                }
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (groupIndex.Value == -1)
                {
                    soundGroup = MasterAudio.NoGroupName;
                }
                else
                {
                    soundGroup = _groupNames[groupIndex.Value];
                }
            }
            else
            {
                var newGroup = EditorGUILayout.TextField(label, soundGroup);
                if (newGroup == soundGroup)
                {
                    DTGUIHelper.EndGroupHeader();
                    return;
                }

                soundGroup = newGroup;
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, sounds, "change Sound Group");
            }
            DTGUIHelper.EndGroupHeader();
        }
    }
}