using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_XBOXONE
    using PlayerPrefs = DarkTonic.MasterAudio.FilePlayerPrefs;
#endif

namespace DarkTonic.MasterAudio.EditorScripts
{
    // ReSharper disable once CheckNamespace
    public class MasterAudioManager : EditorWindow
    {
        private Vector2 _scrollPos = Vector2.zero;

        [MenuItem("Window/Master Audio/Master Audio Manager")]
        // ReSharper disable once UnusedMember.Local
        static void Init()
        {
            var window = GetWindow<MasterAudioManager>(false, "Manager");
            var height = 556;

            window.minSize = new Vector2(560, height);
            window.maxSize = new Vector2(560, height);

            GetWindow(typeof(MasterAudioManager));
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        void OnGUI()
        {
            _scrollPos = GUI.BeginScrollView(
                    new Rect(0, 0, position.width, position.height),
                    _scrollPos,
                    new Rect(0, 0, 550, 530)
            );

            PlaylistController.Instances = null;
            var pcs = PlaylistController.Instances;
            // ReSharper disable once PossibleNullReferenceException
            var plControllerInScene = pcs.Count > 0;

            if (MasterAudioInspectorResources.LogoTexture != null)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            if (Application.isPlaying)
            {
                DTGUIHelper.ShowLargeBarAlert("This screen cannot be used during play.");
                GUI.EndScrollView();
                return;
            }

            DTGUIHelper.HelpHeader("https://www.dtdevtools.com/docs/masteraudio/MasterAudioManager.htm");

            var settings = MasterAudioInspectorResources.GearTexture;

            MasterAudio.Instance = null;
            var ma = MasterAudio.Instance;
            var maInScene = ma != null;

            var organizer = FindObjectOfType(typeof(SoundGroupOrganizer));
            var hasOrganizer = organizer != null;

            DTGUIHelper.ShowColorWarning("The Master Audio prefab holds sound FX group and mixer controls. Add this first (only one per scene).");
            EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);

            EditorGUILayout.LabelField("Master Audio prefab", GUILayout.Width(300));
            if (!maInScene)
            {
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button(new GUIContent("Create", "Create Master Audio prefab"), EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    MasterAudioSettings.Instance.UseDbScale = true;
                    EditorUtility.SetDirty(MasterAudioSettings.Instance);
                    var go = MasterAudio.CreateMasterAudio();
                    AudioUndoHelper.CreateObjectForUndo(go, "Create Master Audio prefab");
                }
                GUI.contentColor = Color.white;
            }
            else
            {
                if (settings != null)
                {
                    if (GUILayout.Button(new GUIContent(settings, "Master Audio Settings"), EditorStyles.toolbarButton))
                    {
                        Selection.activeObject = ma.transform;
                    }
                }
                GUILayout.Label("Exists in scene", EditorStyles.boldLabel);
            }

            GUILayout.FlexibleSpace();
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/MasterAudioManager.htm#MAGO");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            // Playlist Controller
            DTGUIHelper.ShowColorWarning("The Playlist Controller prefab controls sets of songs (or other audio) and ducking. No limit per scene.");
            EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
            EditorGUILayout.LabelField("Playlist Controller prefab", GUILayout.Width(300));

            GUI.contentColor = DTGUIHelper.BrightButtonColor;
            if (GUILayout.Button(new GUIContent("Create", "Place a Playlist Controller prefab in the current scene."), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                var go = MasterAudio.CreatePlaylistController();
                AudioUndoHelper.CreateObjectForUndo(go, "Create Playlist Controller prefab");
            }
            GUI.contentColor = Color.white;

            GUILayout.FlexibleSpace();
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/MasterAudioManager.htm#PCGO");
            EditorGUILayout.EndHorizontal();
            if (!plControllerInScene)
            {
                DTGUIHelper.ShowLargeBarAlert("There is no Playlist Controller in the scene. Music will not play.");
            }

            EditorGUILayout.Separator();
            // Dynamic Sound Group Creators
            DTGUIHelper.ShowColorWarning("The Dynamic Sound Group Creator prefab can per-Scene Sound Groups and other audio. No limit per scene.");
            EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
            EditorGUILayout.LabelField("Dynamic Sound Group Creator prefab", GUILayout.Width(300));

            GUI.contentColor = DTGUIHelper.BrightButtonColor;
            if (GUILayout.Button(new GUIContent("Create", "Place a Dynamic Sound Group prefab in the current scene."), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                var go = MasterAudio.CreateDynamicSoundGroupCreator();
                AudioUndoHelper.CreateObjectForUndo(go, "Create Dynamic Sound Group Creator prefab");
            }

            GUI.contentColor = Color.white;

            GUILayout.FlexibleSpace();
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/MasterAudioManager.htm#DSGC");
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Separator();
            // Sound Group Organizer
            DTGUIHelper.ShowColorWarning("The Sound Group Organizer prefab can import/export Groups to/from MA and Dynamic SGC prefabs.");
            EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
            EditorGUILayout.LabelField("Sound Group Organizer prefab", GUILayout.Width(300));

            if (hasOrganizer)
            {
                if (settings != null)
                {
                    if (GUILayout.Button(new GUIContent(settings, "Sound Group Organizer Settings"), EditorStyles.toolbarButton))
                    {
                        Selection.activeObject = organizer;
                    }
                }
                GUILayout.Label("Exists in scene", EditorStyles.boldLabel);
            }
            else
            {
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button(new GUIContent("Create", "Place a Sound Group Organizer prefab in the current scene."), EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    var go = MasterAudio.CreateSoundGroupOrganizer();
                    AudioUndoHelper.CreateObjectForUndo(go, "Create Dynamic Sound Group Creator prefab");
                }
            }

            GUI.contentColor = Color.white;

            GUILayout.FlexibleSpace();
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/MasterAudioManager.htm#SGO");
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Separator();

            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
                GUILayout.Label("Global Settings");
                DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/MasterAudioManager.htm#GlobalSettings");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                var newVol = GUILayout.Toggle(MasterAudio.UseDbScaleForVolume, " Display dB For Volumes");
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (newVol != MasterAudio.UseDbScaleForVolume)
                {
                    MasterAudio.UseDbScaleForVolume = newVol;
                }

                GUILayout.Space(30);

                var newCents = GUILayout.Toggle(MasterAudio.UseCentsForPitch, " Display Semitones for Pitches");
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (newCents != MasterAudio.UseCentsForPitch)
                {
                    MasterAudio.UseCentsForPitch = newCents;
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                var useLogo = GUILayout.Toggle(MasterAudio.HideLogoNav, " Hide Logo Nav. in Inspectors");
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (useLogo != MasterAudio.HideLogoNav)
                {
                    MasterAudio.HideLogoNav = useLogo;
                    EditorUtility.SetDirty(MasterAudioSettings.Instance);
                }

                GUILayout.Space(2);

                var removeVar = GUILayout.Toggle(MasterAudio.RemoveUnplayedVariationDueToProbability, new GUIContent(" Remove Probability-Unplayed Variations", "Remove Variations that were not played due to failing the Probability to Play Field"));
                // ReSharper disable once RedundantCheckBeforeAssignment
                if (removeVar != MasterAudio.RemoveUnplayedVariationDueToProbability)
                {
                    MasterAudio.RemoveUnplayedVariationDueToProbability = removeVar;
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();

                if (!Application.isPlaying)
                {
                    EditorGUILayout.BeginHorizontal();
                    var oldEdit = MasterAudioSettings.Instance.EditMAFolder;
                    MasterAudioSettings.Instance.EditMAFolder = GUILayout.Toggle(MasterAudioSettings.Instance.EditMAFolder, " Edit Installation Path");

                    if (oldEdit != MasterAudioSettings.Instance.EditMAFolder)
                    {
                        EditorUtility.SetDirty(MasterAudioSettings.Instance);
                    }

                    if (MasterAudioSettings.Instance.EditMAFolder)
                    {
                        var path = EditorGUILayout.TextField("", MasterAudio.ProspectiveMAPath);
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (!string.IsNullOrEmpty(path))
                        {
                            MasterAudio.ProspectiveMAPath = path;
                        }
                        else
                        {
                            MasterAudio.ProspectiveMAPath = MasterAudio.MasterAudioFolderPath;
                        }
                        GUI.contentColor = DTGUIHelper.BrightButtonColor;
                        if (
                            GUILayout.Button(
                                new GUIContent("Update",
                                    "This will update the installation folder path with the value to the left."),
                                EditorStyles.toolbarButton, GUILayout.Width(60)))
                        {
                            MasterAudio.MasterAudioFolderPath = MasterAudio.ProspectiveMAPath;
                            DTGUIHelper.ShowAlert("Installation Path updated!");
                        }
                        GUILayout.Space(4);
                        if (GUILayout.Button(new GUIContent("Revert", "Revert to default settings"),
                            EditorStyles.toolbarButton, GUILayout.Width(60)))
                        {
                            MasterAudio.MasterAudioFolderPath = MasterAudio.MasterAudioDefaultFolder;
                            MasterAudio.ProspectiveMAPath = MasterAudio.MasterAudioDefaultFolder;
                            DTGUIHelper.ShowAlert("Installation Path reverted!");
                        }
                        GUI.contentColor = Color.white;
                        GUILayout.Space(10);
                        DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/InstallationFolder.htm");
                    }
                    else
                    {
                        DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/InstallationFolder.htm");
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Separator();

                EditorGUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
                GUILayout.Label("Utility Functions");
                DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/MasterAudioManager.htm#UtilityFunctions");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button(new GUIContent("Delete all unused Filter FX", "This will delete all unused Unity Audio Filter FX components in the entire MasterAudio prefab and all Sound Groups within."), EditorStyles.toolbarButton, GUILayout.Width(160)))
                {
                    DeleteAllUnusedFilterFx();
                }

                GUILayout.Space(10);

                if (GUILayout.Button(new GUIContent("Reset Prefs / Settings", "This will delete all Master Audio's Persistent Settings and global preferences (back to installation default). None of your prefabs will be deleted."), EditorStyles.toolbarButton, GUILayout.Width(160)))
                {
                    ResetPrefs();
                }

                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            GUI.EndScrollView();
        }

        private static void ResetPrefs()
        {
            PlayerPrefs.DeleteKey(MasterAudio.StoredLanguageNameKey);

            MasterAudio.UseDbScaleForVolume = false;
            MasterAudio.UseCentsForPitch = false;
            MasterAudio.HideLogoNav = false;
            MasterAudioSettings.Instance.InstallationFolderPath = MasterAudio.MasterAudioDefaultFolder;
            MasterAudioSettings.Instance.MixerWidthSetting = MasterAudio.MixerWidthMode.Narrow;
            MasterAudioSettings.Instance.EditMAFolder = false;
            MasterAudioSettings.Instance.BusesShownInNarrow = true;
            MasterAudioSettings.Instance.ShowWelcomeWindowOnStart = true;

            EditorUtility.SetDirty(MasterAudioSettings.Instance);

            PlayerPrefs.DeleteKey(PersistentAudioSettings.SfxVolKey);
            PlayerPrefs.DeleteKey(PersistentAudioSettings.MusicVolKey);
            PlayerPrefs.DeleteKey(PersistentAudioSettings.SfxMuteKey);
            PlayerPrefs.DeleteKey(PersistentAudioSettings.MusicMuteKey);

            // delete group persistent settings
            var groups = PersistentAudioSettings.GroupsUpdatedKeys.Split(new[] { PersistentAudioSettings.Separator }, StringSplitOptions.RemoveEmptyEntries);
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < groups.Length; i++)
            {
                var aGrp = groups[i];
                var key = PersistentAudioSettings.GetGroupKey(aGrp);
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.DeleteKey(PersistentAudioSettings.GroupKeysKey);

            // bus persistent settings
            var buses = PersistentAudioSettings.BusesUpdatedKeys.Split(new[] { PersistentAudioSettings.Separator }, StringSplitOptions.RemoveEmptyEntries);
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < buses.Length; i++)
            {
                var aBus = buses[i];
                var key = PersistentAudioSettings.MakeBusKey(aBus);
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.DeleteKey(PersistentAudioSettings.BusKeysKey);
        }

        private static void DeleteAllUnusedFilterFx()
        {
            var ma = MasterAudio.Instance;

            if (ma == null)
            {
                DTGUIHelper.ShowAlert("There is no MasterAudio prefab in this scene. Try pressing this button on a different Scene.");
                return;
            }

            var affectedVariations = new List<SoundGroupVariation>();
            var filtersToDelete = new List<Object>();

            for (var g = 0; g < ma.transform.childCount; g++)
            {
                var sGroup = ma.transform.GetChild(g);
                for (var v = 0; v < sGroup.childCount; v++)
                {
                    var variation = sGroup.GetChild(v);
                    var grpVar = variation.GetComponent<SoundGroupVariation>();
                    if (grpVar == null)
                    {
                        continue;
                    }

                    if (grpVar.LowPassFilter != null && !grpVar.LowPassFilter.enabled)
                    {
                        if (!filtersToDelete.Contains(grpVar.LowPassFilter))
                        {
                            filtersToDelete.Add(grpVar.LowPassFilter);
                        }

                        if (!affectedVariations.Contains(grpVar))
                        {
                            affectedVariations.Add(grpVar);
                        }
                    }

                    if (grpVar.HighPassFilter != null && !grpVar.HighPassFilter.enabled)
                    {
                        if (!filtersToDelete.Contains(grpVar.HighPassFilter))
                        {
                            filtersToDelete.Add(grpVar.HighPassFilter);
                        }

                        if (!affectedVariations.Contains(grpVar))
                        {
                            affectedVariations.Add(grpVar);
                        }
                    }

                    if (grpVar.ChorusFilter != null && !grpVar.ChorusFilter.enabled)
                    {
                        if (!filtersToDelete.Contains(grpVar.ChorusFilter))
                        {
                            filtersToDelete.Add(grpVar.ChorusFilter);
                        }

                        if (!affectedVariations.Contains(grpVar))
                        {
                            affectedVariations.Add(grpVar);
                        }
                    }

                    if (grpVar.DistortionFilter != null && !grpVar.DistortionFilter.enabled)
                    {
                        if (!filtersToDelete.Contains(grpVar.DistortionFilter))
                        {
                            filtersToDelete.Add(grpVar.DistortionFilter);
                        }

                        if (!affectedVariations.Contains(grpVar))
                        {
                            affectedVariations.Add(grpVar);
                        }
                    }

                    if (grpVar.EchoFilter != null && !grpVar.EchoFilter.enabled)
                    {
                        if (!filtersToDelete.Contains(grpVar.EchoFilter))
                        {
                            filtersToDelete.Add(grpVar.EchoFilter);
                        }

                        if (!affectedVariations.Contains(grpVar))
                        {
                            affectedVariations.Add(grpVar);
                        }
                    }

                    if (grpVar.ReverbFilter == null || grpVar.ReverbFilter.enabled)
                    {
                        continue;
                    }

                    if (!filtersToDelete.Contains(grpVar.ReverbFilter))
                    {
                        filtersToDelete.Add(grpVar.ReverbFilter);
                    }

                    if (!affectedVariations.Contains(grpVar))
                    {
                        affectedVariations.Add(grpVar);
                    }
                }
            }

            AudioUndoHelper.RecordObjectsForUndo(affectedVariations.ToArray(), "delete all unused Filter FX Components");

            foreach (var t in filtersToDelete)
            {
                DestroyImmediate(t);
            }

            DTGUIHelper.ShowAlert(string.Format("{0} Filter FX Components deleted.", filtersToDelete.Count));
        }
    }
}