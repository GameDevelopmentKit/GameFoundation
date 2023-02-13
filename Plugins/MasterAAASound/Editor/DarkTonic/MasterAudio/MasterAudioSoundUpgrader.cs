using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    // ReSharper disable once CheckNamespace
    public class MasterAudioSoundUpgrader : EditorWindow
    {
        private Vector2 _scrollPos = Vector2.zero;
        private int _audioSources = -1;

        [MenuItem("Window/Master Audio/Master Audio Sound Upgrader")]
        // ReSharper disable once UnusedMember.Local
        static void Init()
        {
            var window = GetWindow<MasterAudioSoundUpgrader>(false, "Upgrader");
            var height = 310;

            window.minSize = new Vector2(600, height);
            window.maxSize = new Vector2(600, height);

            GetWindow(typeof(MasterAudioSoundUpgrader));
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        void OnGUI()
        {
            _scrollPos = GUI.BeginScrollView(
                    new Rect(0, 0, position.width, position.height),
                    _scrollPos,
                    new Rect(0, 0, 600, 310)
            );


            if (MasterAudioInspectorResources.LogoTexture != null)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            if (Application.isPlaying)
            {
                DTGUIHelper.ShowLargeBarAlert("This window can only be used in edit mode.");
            }
            else
            {
                DTGUIHelper.ShowColorWarning("This window will help you prepare a project that has existing audio for switching over to Master Audio.");
                DTGUIHelper.ShowColorWarning("All Audio Source components should be created by Master Audio only. Let's remove all your old ones.");
                DTGUIHelper.ShowLargeBarAlert("For each Scene, open the Scene, then go through the steps below to locate & delete items.");

                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Step 1", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button(new GUIContent("Find Audio Sources In Scene"), EditorStyles.toolbarButton, GUILayout.Width(200)))
                {
                    var audSources = GetNonMAAudioSources();
                    _audioSources = audSources.Count;

                    if (_audioSources > 0)
                    {
                        Selection.objects = audSources.ToArray();
                    }

                    if (_audioSources == 0)
                    {
                        DTGUIHelper.ShowAlert("You have zero AudioSources in your Scene. You are finished.");
                    }
                    else
                    {
                        DTGUIHelper.ShowAlert(audSources.Count + " AudioSource(s) found and selected in the Hierarchy. Please take note of what game objects these are, so you can add sound to them later with Master Audio.");
                    }
                }
                GUI.contentColor = Color.white;

                if (_audioSources < 0)
                {
                    GUI.contentColor = DTGUIHelper.BrightButtonColor;
                    GUILayout.Label("Click button to find Audio Sources.");
                }
                else if (_audioSources == 0)
                {
                    GUI.contentColor = DTGUIHelper.BrightButtonColor;
                    GUILayout.Label("No Audio Sources! You are finished.");
                }
                else
                {
                    GUI.contentColor = Color.red;
                    GUILayout.Label(_audioSources.ToString() + " Audio Source(s) selected. Take note of them and go to step 2.");
                }
                GUI.contentColor = Color.white;

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Step 2", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button(new GUIContent("Delete Audio Sources In Scene"), EditorStyles.toolbarButton, GUILayout.Width(200)))
                {
                    var audSources = GetNonMAAudioSources();
                    _audioSources = audSources.Count;

                    if (_audioSources == 0)
                    {
                        DTGUIHelper.ShowAlert("You have zero AudioSources in your Scene. You are finished.");
                        _audioSources = 0;
                    }
                    else
                    {
                        DeleteAudioSources();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.EndScrollView();
        }

        // ReSharper disable once InconsistentNaming
        private static List<GameObject> GetNonMAAudioSources()
        {
            var sources = FindObjectsOfType(typeof(AudioSource));

            var audSources = new List<GameObject>();
            foreach (var t in sources)
            {
                var src = (AudioSource)t;

                var plController = src.GetComponent<PlaylistController>();
                if (plController != null)
                {
                    continue;
                }

                var variation = src.GetComponent<SoundGroupVariation>();
                if (variation != null)
                {
                    continue;
                }

                var dynVariation = src.GetComponent<DynamicGroupVariation>();
                if (dynVariation != null)
                {
                    continue;
                }

                audSources.Add(src.gameObject);
            }

            return audSources;
        }

        private void DeleteAudioSources()
        {
            Selection.objects = new Object[] { };

            var sources = GetNonMAAudioSources();

            var destroyed = 0;
            foreach (var aud in sources)
            {
                DestroyImmediate(aud.GetComponent<AudioSource>());
                destroyed++;
            }

            DTGUIHelper.ShowAlert(destroyed + " Audio Source(s) destroyed.");
            _audioSources = 0;
        }
    }
}