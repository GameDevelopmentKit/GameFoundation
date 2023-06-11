using UnityEngine;
using UnityEditor;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(AudioTransformTracker))]
    // ReSharper disable once CheckNamespace
    public class AudioTransformTrackerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var tracker = (AudioTransformTracker)target;

            MasterAudio.Instance = null;

            var ma = MasterAudio.SafeInstance;
            var maInScene = ma != null;

            if (maInScene)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }
            else
            {
                return;
            }

            if (!Application.isPlaying)
            {
                DTGUIHelper.ShowLargeBarAlert("This script only works at runtime.");
                return;
            }

            var allVars = MasterAudio.GetAllPlayingVariationsOfTransform(tracker.Trans);

            DTGUIHelper.ShowColorWarning("Sounds made by this Transform: " + allVars.Count);

            if (allVars.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Stop All Sounds", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    MasterAudio.StopAllSoundsOfTransform(tracker.Trans);
                }
                GUILayout.Space(4);
                if (GUILayout.Button("Pause All Sounds", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    MasterAudio.PauseAllSoundsOfTransform(tracker.Trans);
                }
                GUILayout.Space(4);
                if (GUILayout.Button("Unpause All Sounds", EditorStyles.toolbarButton, GUILayout.Width(120)))
                {
                    MasterAudio.UnpauseAllSoundsOfTransform(tracker.Trans);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Separator();
            }

            GUI.color = Color.white;
            var lastGrpName = string.Empty;
            var groupCount = 0;

            foreach (var variation in allVars)
            {
                var grpName = variation.ParentGroup.GameObjectName;

                if (grpName != lastGrpName)
                {
                    groupCount++;
                    if (groupCount > 1)
                    {
                        EditorGUILayout.Separator();
                    }

                    GUI.color = DTGUIHelper.ActiveHeaderColor;
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    GUILayout.Label(grpName);

                    GUILayout.FlexibleSpace();
                    if (DTGUIHelper.AddSettingsButton("Group", true) == DTGUIHelper.DTFunctionButtons.Go)
                    {
                        Selection.activeObject = variation.ParentGroup.gameObject;
                    }
                    var groupButtonPressed = DTGUIHelper.Add2WayTrackerButtons();

                    switch (groupButtonPressed)
                    {
                        case DTGUIHelper.DTFunctionButtons.Stop:
                            tracker.Trans.StopSoundGroupOfTransform(grpName);
                            break;
                        case DTGUIHelper.DTFunctionButtons.Pause:
                            tracker.Trans.PauseSoundGroupOfTransform(grpName);
                            break;
                        case DTGUIHelper.DTFunctionButtons.Play:
                            tracker.Trans.UnpauseSoundGroupOfTransform(grpName);
                            break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUI.color = Color.white;
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Space(10);

                var varName = variation.GameObjectName;
                GUILayout.Label(varName);

                GUILayout.FlexibleSpace();
                GUI.color = Color.green;

                var label = "Playing ({0}%)";

                if (variation.IsPaused)
                {
                    GUI.color = Color.yellow;
                    label = "Paused ({0}%)";
                }

                var percentagePlayed = 0;
                if (variation.VarAudio.clip != null)
                {
                    percentagePlayed = (int)(variation.VarAudio.time / variation.VarAudio.clip.length * 100);
                }

                EditorGUILayout.LabelField(string.Format(label, percentagePlayed), EditorStyles.miniButtonMid, GUILayout.Height(16));

                GUI.color = Color.white;

                if (DTGUIHelper.AddSettingsButton("Variation") == DTGUIHelper.DTFunctionButtons.Go)
                {
                    Selection.activeObject = variation.gameObject;
                }
                var buttonPressed = DTGUIHelper.Add2WayTrackerButtons();

                switch (buttonPressed)
                {
                    case DTGUIHelper.DTFunctionButtons.Stop:
                        variation.Stop();
                        break;
                    case DTGUIHelper.DTFunctionButtons.Pause:
                        variation.Pause();
                        break;
                    case DTGUIHelper.DTFunctionButtons.Play:
                        variation.VarAudio.Play();
                        break;
                }

                EditorGUILayout.EndHorizontal();

                lastGrpName = grpName;
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }

            //DrawDefaultInspector();
        }
    }
}