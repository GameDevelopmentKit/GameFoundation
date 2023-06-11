using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(DynamicSoundGroupCreator))]
    // ReSharper disable once CheckNamespace
    public class DynamicSoundGroupCreatorInspector : Editor
    {
        private const string ExistingBus = "[EXISTING BUS]";
        private const string ExistingNameName = "[EXISTING BUS NAME]";

        private DynamicSoundGroupCreator _creator;
        private List<DynamicSoundGroup> _groups;
        private bool _isDirty;
        private List<string> _customEventNames = new List<string>();
        private List<string> _audioSourceTemplateNames = new List<string>();

        private List<DynamicSoundGroup> ScanForGroups()
        {
            var groups = new List<DynamicSoundGroup>();

            for (var i = 0; i < _creator.transform.childCount; i++)
            {
                var aChild = _creator.transform.GetChild(i);

                var grp = aChild.GetComponent<DynamicSoundGroup>();
                if (grp == null)
                {
                    continue;
                }

                grp.groupVariations = VariationsForGroup(aChild.transform);

                groups.Add(grp);
            }

            if (_creator.groupByBus)
            {
                groups.Sort(delegate (DynamicSoundGroup g1, DynamicSoundGroup g2)
                {
                    // ReSharper disable once ConvertIfStatementToReturnStatement
                    if (g1.busIndex == g2.busIndex)
                    {
                        return g1.name.CompareTo(g2.name);
                    }

                    return g1.busIndex.CompareTo(g2.busIndex);
                });
            }
            else
            {
                groups.Sort(delegate (DynamicSoundGroup g1, DynamicSoundGroup g2)
                {
                    return g1.name.CompareTo(g2.name);
                });
            }

            return groups;
        }

        private static List<DynamicGroupVariation> VariationsForGroup(Transform groupTrans)
        {
            var variations = new List<DynamicGroupVariation>();

            for (var i = 0; i < groupTrans.childCount; i++)
            {
                var aVar = groupTrans.GetChild(i);

                var variation = aVar.GetComponent<DynamicGroupVariation>();
                variations.Add(variation);
            }

            return variations;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel = 1;
            _isDirty = false;

            _creator = (DynamicSoundGroupCreator)target;

            var isInProjectView = DTGUIHelper.IsPrefabInProjectView(_creator.gameObject);

            if (MasterAudioInspectorResources.LogoTexture != null)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            DTGUIHelper.HelpHeader("https://www.dtdevtools.com/docs/masteraudio/DynamicSoundGroupCreators.htm", "https://www.dtdevtools.com/API/masteraudio/class_dark_tonic_1_1_master_audio_1_1_dynamic_sound_group_creator.html");

            MasterAudio.Instance = null;
            var ma = MasterAudio.SafeInstance;
            var maInScene = ma != null;

            if (DTGUIHelper.IsLinkedToDarkTonicPrefabFolder(_creator))
            {
                DTGUIHelper.MakePrefabMessage();
                return;
            }

            _customEventNames.Clear();

            if (maInScene)
            {
                _customEventNames = ma.CustomEventNames;
            }
            else
            {
                _customEventNames = MasterAudio.CustomEventHardCodedNames;
            }

            var eventAdded = false;

            for (var i = 0; i < _creator.customEventsToCreate.Count; i++)
            {
                var evt = _creator.customEventsToCreate[i];
                if (_customEventNames.Contains(evt.EventName))
                {
                    continue;
                }

                eventAdded = true;
                _customEventNames.Add(evt.EventName);
            }

            if (eventAdded)
            {
                _customEventNames.Sort();
                if (_customEventNames.Count > 1)
                {
                    _customEventNames.Insert(0, _customEventNames[1]);
                }
            }

            var allowPreview = !DTGUIHelper.IsPrefabInProjectView(_creator.gameObject);

            EditorGUI.indentLevel = 0;  // Space will handle this for the header

            if (!allowPreview)
            {
                DTGUIHelper.ShowLargeBarAlert("You are in Project View and cannot edit this Game Object from here.");
                return;
            }

            AudioSource previewer;

            var sliderIndicatorChars = 6;
            var sliderWidth = 40;

            if (MasterAudio.UseDbScaleForVolume)
            {
                sliderIndicatorChars = 9;
                sliderWidth = 56;
            }

            var busVoiceLimitList = new List<string> { MasterAudio.NoVoiceLimitName };

            for (var i = 1; i <= 32; i++)
            {
                busVoiceLimitList.Add(i.ToString());
            }

            var busList = new List<string> { MasterAudioGroup.NoBus, MasterAudioInspector.NewBusName, ExistingBus };

            var maxChars = 12;

            foreach (var t in _creator.groupBuses)
            {
                var bus = t;
                busList.Add(bus.busName);

                if (bus.busName.Length > maxChars)
                {
                    maxChars = bus.busName.Length;
                }
            }
            var busListWidth = 9 * maxChars;

            EditorGUI.indentLevel = 0;  // Space will handle this for the header

            if (MasterAudio.SafeInstance == null)
            {
                var newLang = (SystemLanguage)EditorGUILayout.EnumPopup(new GUIContent("Preview Language", "This setting is only used (and visible) to choose the previewing language when there's no Master Audio Game Object in the Scene (language settings are grabbed from there normally). This should only happen when you're using a Master Audio Game Object from a previous Scene in persistent mode."), _creator.previewLanguage);
                if (newLang != _creator.previewLanguage)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Preview Language");
                    _creator.previewLanguage = newLang;
                }
            }

            EditorGUILayout.Separator();

            var newAllow = (DynamicSoundGroupCreator.CreateItemsWhen)EditorGUILayout.EnumPopup("Items Created When?", _creator.reUseMode);
            if (newAllow != _creator.reUseMode)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Items Created When?");
                _creator.reUseMode = newAllow;
            }

            var newIgnore = EditorGUILayout.Toggle("Error On Duplicate Items", _creator.errorOnDuplicates);
            if (newIgnore != _creator.errorOnDuplicates)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Error On Duplicate Items");
                _creator.errorOnDuplicates = newIgnore;
            }
            if (_creator.errorOnDuplicates)
            {
                DTGUIHelper.ShowColorWarning("An error will be logged if your Dynamic items already exist in the MA Game Object.");
            }
            else
            {
                DTGUIHelper.ShowLargeBarAlert("Dynamic items that already exist in the MA Game Object will be ignored and not created.");
            }


            var newAwake = EditorGUILayout.Toggle("Auto-create Items", _creator.createOnAwake);
            if (newAwake != _creator.createOnAwake)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Auto-create Items");
                _creator.createOnAwake = newAwake;
            }
            if (_creator.createOnAwake)
            {
                DTGUIHelper.ShowColorWarning("Items will be created as soon as this object is in the Scene.");
            }
            else
            {
                DTGUIHelper.ShowLargeBarAlert("You will need to call this object's CreateItems method manually to create the items.");
            }

            var newRemove = EditorGUILayout.Toggle("Auto-remove Items", _creator.removeGroupsOnSceneChange);
            if (newRemove != _creator.removeGroupsOnSceneChange)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Auto-remove Items");
                _creator.removeGroupsOnSceneChange = newRemove;
            }

            if (_creator.removeGroupsOnSceneChange)
            {
                DTGUIHelper.ShowColorWarning("Items will be deleted when this object is disabled or destroyed.");
            }
            else
            {
                DTGUIHelper.ShowLargeBarAlert("Items created by this will persist across Scenes if MasterAudio does.");
            }

            // custom event
            DTGUIHelper.StartGroupHeader();
            GUI.color = Color.white;
            var exp = EditorGUILayout.BeginToggleGroup("Fire 'Items Created' Event", _creator.itemsCreatedEventExpanded);
            if (exp != _creator.itemsCreatedEventExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Fire 'Items Created' Event");
                _creator.itemsCreatedEventExpanded = exp;
            }
            GUI.color = Color.white;
            DTGUIHelper.EndGroupHeader();

            if (_creator.itemsCreatedEventExpanded)
            {
                EditorGUI.indentLevel = 0;
                DTGUIHelper.ShowColorWarning("When items are created, fire Custom Event below.");

                var existingIndex = _customEventNames.IndexOf(_creator.itemsCreatedCustomEvent);

                int? customEventIndex = null;

                var noEvent = false;
                var noMatch = false;

                if (existingIndex >= 1)
                {
                    customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    if (existingIndex == 1)
                    {
                        noEvent = true;
                    }
                }
                else if (existingIndex == -1 && _creator.itemsCreatedCustomEvent == MasterAudio.NoGroupName)
                {
                    customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                }
                else
                { // non-match
                    noMatch = true;
                    var newEventName = EditorGUILayout.TextField("Custom Event Name", _creator.itemsCreatedCustomEvent);
                    if (newEventName != _creator.itemsCreatedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Custom Event Name");
                        _creator.itemsCreatedCustomEvent = newEventName;
                    }

                    var newIndex = EditorGUILayout.Popup("All Custom Events", -1, _customEventNames.ToArray());
                    if (newIndex >= 0)
                    {
                        customEventIndex = newIndex;
                    }
                }

                if (noEvent)
                {
                    DTGUIHelper.ShowRedError("No Custom Event specified. This section will do nothing.");
                }
                else if (noMatch)
                {
                    DTGUIHelper.ShowRedError("Custom Event found no match. Type in or choose one.");
                }

                if (customEventIndex.HasValue)
                {
                    if (existingIndex != customEventIndex.Value)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Custom Event");
                    }
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (customEventIndex.Value == -1)
                    {
                        _creator.itemsCreatedCustomEvent = MasterAudio.NoGroupName;
                    }
                    else
                    {
                        _creator.itemsCreatedCustomEvent = _customEventNames[customEventIndex.Value];
                    }
                }
            }
            EditorGUILayout.EndToggleGroup();

            _groups = ScanForGroups();
            var groupNameList = GroupNameList;

            EditorGUI.indentLevel = 0;

            var state = _creator.showMusicDucking;
            var text = "Dynamic Music Ducking";

            DTGUIHelper.ShowCollapsibleSection(ref state, text);

            GUILayout.Space(2f);



            if (state != _creator.showMusicDucking)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Dynamic Music Ducking");
                _creator.showMusicDucking = state;
            }

            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/DynamicSoundGroupCreators.htm#Ducking");

            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            if (_creator.showMusicDucking)
            {
                DTGUIHelper.BeginGroupedControls();
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                EditorGUILayout.BeginHorizontal();

                GUILayout.Space(4);

                if (GUILayout.Button(new GUIContent("Add Duck Group"), EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Add Duck Group");

                    var defaultBeginUnduck = 0.5f;
                    var defaultDuckedVolumeCut = MasterAudio.DefaultDuckVolCut;
                    if (maInScene)
                    {
                        defaultBeginUnduck = ma.defaultRiseVolStart;
                        defaultDuckedVolumeCut = ma.defaultDuckedVolumeCut;
                    }

                    _creator.musicDuckingSounds.Add(new DuckGroupInfo
                    {
                        soundType = MasterAudio.NoGroupName,
                        riseVolStart = defaultBeginUnduck,
                        duckedVolumeCut = defaultDuckedVolumeCut
                    });
                }

                EditorGUILayout.EndHorizontal();
                GUI.contentColor = Color.white;
                EditorGUILayout.Separator();

                if (_creator.musicDuckingSounds.Count == 0)
                {
                    DTGUIHelper.ShowLargeBarAlert("You currently have no ducking sounds set up.");
                }
                else
                {
                    int? duckSoundToRemove = null;

                    if (_creator.musicDuckingSounds.Count > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Sound Group", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(new GUIContent("Vol. Cut (dB)", "Amount to duck the music volume."), EditorStyles.boldLabel);
                        GUILayout.Space(9);
                        GUILayout.Label(new GUIContent("Beg. Unduck", "Begin Unducking after this amount of the sound has been played."), EditorStyles.boldLabel);
                        GUILayout.Space(11);
                        GUILayout.Label(new GUIContent("Unduck Time", "Unducking will take X seconds."), EditorStyles.boldLabel);
                        GUILayout.Space(54);
                        EditorGUILayout.EndHorizontal();
                    }

                    for (var i = 0; i < _creator.musicDuckingSounds.Count; i++)
                    {
                        var duckSound = _creator.musicDuckingSounds[i];
                        var index = groupNameList.IndexOf(duckSound.soundType);
                        if (index == -1)
                        {
                            index = 0;
                        }

                        DTGUIHelper.StartGroupHeader(2);

                        EditorGUILayout.BeginHorizontal();
                        var newIndex = EditorGUILayout.Popup(index, groupNameList.ToArray(), GUILayout.MaxWidth(200));
                        if (newIndex >= 0)
                        {
                            if (index != newIndex)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Duck Group");
                            }
                            duckSound.soundType = groupNameList[newIndex];
                        }

                        GUILayout.FlexibleSpace();

                        GUI.contentColor = DTGUIHelper.BrightButtonColor;
                        GUILayout.TextField(duckSound.duckedVolumeCut.ToString("N1"), 20, EditorStyles.miniLabel);

                        var newDuckMult = GUILayout.HorizontalSlider(duckSound.duckedVolumeCut, DTGUIHelper.MinDb, DTGUIHelper.MaxDb, GUILayout.Width(60));
                        if (newDuckMult != duckSound.duckedVolumeCut)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Ducked Vol Cut");
                            duckSound.duckedVolumeCut = newDuckMult;
                        }
                        GUI.contentColor = Color.white;

                        GUI.contentColor = DTGUIHelper.BrightButtonColor;
                        GUILayout.TextField(duckSound.riseVolStart.ToString("N2"), 20, EditorStyles.miniLabel);

                        var newUnduck = GUILayout.HorizontalSlider(duckSound.riseVolStart, 0f, 1f, GUILayout.Width(60));
                        if (newUnduck != duckSound.riseVolStart)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Begin Unduck");
                            duckSound.riseVolStart = newUnduck;
                        }
                        GUI.contentColor = Color.white;

                        GUILayout.Space(4);
                        GUILayout.TextField(duckSound.unduckTime.ToString("N2"), 20, EditorStyles.miniLabel);
                        var newTime = GUILayout.HorizontalSlider(duckSound.unduckTime, 0f, 5f, GUILayout.Width(60));
                        if (newTime != duckSound.unduckTime)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Unduck Time");
                            duckSound.unduckTime = newTime;
                        }

                        GUILayout.Space(10);
                        if (DTGUIHelper.AddDeleteIcon("Duck Sound"))
                        {
                            duckSoundToRemove = i;
                        }

                        EditorGUILayout.EndHorizontal();
                        DTGUIHelper.EndGroupHeader();
                    }

                    if (duckSoundToRemove.HasValue)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Duck Group");
                        _creator.musicDuckingSounds.RemoveAt(duckSoundToRemove.Value);
                    }
                }

                DTGUIHelper.EndGroupedControls();
            }

            DTGUIHelper.ResetColors();


            DTGUIHelper.VerticalSpace(3);

            state = _creator.soundGroupsAreExpanded;
            text = "Dynamic Group Mixer";

            DTGUIHelper.ShowCollapsibleSection(ref state, text);

            GUILayout.Space(2f);


            if (state != _creator.soundGroupsAreExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Dynamic Group Mixer");
                _creator.soundGroupsAreExpanded = state;
            }

            var applyTemplateToAll = false;
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/DynamicSoundGroupCreators.htm#Mixer");

            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            if (_creator.soundGroupsAreExpanded)
            {
                DTGUIHelper.BeginGroupedControls();

                DTGUIHelper.StartGroupHeader();
                EditorGUILayout.LabelField("Group Control");

                EditorGUILayout.EndVertical();

                _audioSourceTemplateNames = new List<string>();

                foreach (var temp in _creator.audioSourceTemplates)
                {
                    if (temp == null)
                    {
                        continue;
                    }
                    _audioSourceTemplateNames.Add(temp.name);
                }

                var audTemplatesMissing = false;

                if (Directory.Exists(MasterAudio.AudioSourceTemplateFolder))
                {
                    var audioSrcTemplates = Directory.GetFiles(MasterAudio.AudioSourceTemplateFolder, "*.prefab").Length;
                    if (audioSrcTemplates > _creator.audioSourceTemplates.Count)
                    {
                        audTemplatesMissing = true;
                        DTGUIHelper.ShowLargeBarAlert("There's " + (audioSrcTemplates - _creator.audioSourceTemplates.Count) + " Audio Source Template(s) that aren't set up in this MA Game Object. Locate them in Plugins/DarkTonic/MasterAudio/Sources/Prefabs/AudioSourceTemplates and drag them in below.");
                    }
                }

                Event aEvent;
                if (audTemplatesMissing)
                {
                    // create groups start
                    EditorGUILayout.BeginVertical();
                    aEvent = Event.current;

                    GUI.color = DTGUIHelper.DragAreaColor;

                    var dragArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
                    GUI.Box(dragArea, "Drag prefabs here from Project View to create Audio Source Templates!");

                    GUI.color = Color.white;

                    switch (aEvent.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            if (!dragArea.Contains(aEvent.mousePosition))
                            {
                                break;
                            }

                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if (aEvent.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();

                                foreach (var dragged in DragAndDrop.objectReferences)
                                {
                                    var temp = dragged as GameObject;
                                    if (temp == null)
                                    {
                                        continue;
                                    }

                                    AddAudioSourceTemplate(temp);
                                }
                            }
                            Event.current.Use();
                            break;
                    }
                    EditorGUILayout.EndVertical();
                    // create groups end
                }

                if (_audioSourceTemplateNames.Count == 0)
                {
                    DTGUIHelper.ShowRedError("You have no Audio Source Templates. Drag them in to create them.");
                }
                else
                {
                    var audSrcTemplateIndex = _audioSourceTemplateNames.IndexOf(_creator.audioSourceTemplateName);
                    if (audSrcTemplateIndex < 0)
                    {
                        audSrcTemplateIndex = 0;
                        _creator.audioSourceTemplateName = _audioSourceTemplateNames[0];
                    }

                    var newIndex = EditorGUILayout.Popup("Audio Source Template", audSrcTemplateIndex, _audioSourceTemplateNames.ToArray());
                    if (newIndex != audSrcTemplateIndex)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Audio Source Template");
                        _creator.audioSourceTemplateName = _audioSourceTemplateNames[newIndex];
                    }
                }

                var newDragMode = (MasterAudio.DragGroupMode)EditorGUILayout.EnumPopup("Bulk Creation Mode", _creator.curDragGroupMode);
                if (newDragMode != _creator.curDragGroupMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Bulk Creation Mode");
                    _creator.curDragGroupMode = newDragMode;
                }

                var bulkMode = (MasterAudio.AudioLocation)EditorGUILayout.EnumPopup("Variation Create Mode", _creator.bulkVariationMode);
                if (bulkMode != _creator.bulkVariationMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Variation Mode");
                    _creator.bulkVariationMode = bulkMode;
                }

                if (_creator.groupBuses.Count > 0)
                {
                    var newGroupByBus = EditorGUILayout.Toggle("Group by Bus", _creator.groupByBus);
                    if (newGroupByBus != _creator.groupByBus)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Group by Bus");
                        _creator.groupByBus = newGroupByBus;
                    }
                }

                // create groups start
                EditorGUILayout.BeginVertical();
                aEvent = Event.current;

                if (isInProjectView)
                {
                    DTGUIHelper.ShowLargeBarAlert("You are in Project View and cannot create Groups.");
                } else if (DTGUIHelper.IsInPrefabMode(_creator.gameObject)) {
                    DTGUIHelper.ShowLargeBarAlert("You are in Prefab Mode and cannot create Groups.");
                } else if (Application.isPlaying) {
                    DTGUIHelper.ShowLargeBarAlert("You are running and cannot create Groups.");
                }
                else
                {
                    GUI.color = DTGUIHelper.DragAreaColor;

                    var dragAreaGroup = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
                    GUI.Box(dragAreaGroup, MasterAudio.DragAudioTip + " to create groups!");

                    GUI.color = Color.white;

                    switch (aEvent.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            if (!dragAreaGroup.Contains(aEvent.mousePosition))
                            {
                                break;
                            }

                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if (aEvent.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();

                                Transform groupInfo = null;

                                var clips = new List<AudioClip>();

                                foreach (var dragged in DragAndDrop.objectReferences)
                                {
                                    if (dragged is DefaultAsset)
                                    {
                                        var assetPaths = AssetDatabase.FindAssets("t:AudioClip", DragAndDrop.paths);
                                        foreach (var assetPath in assetPaths)
                                        {
                                            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(assetPath));
                                            if (clip == null)
                                            {
                                                continue;
                                            }

                                            clips.Add(clip);
                                        }

                                        continue;
                                    }

                                    var aClip = dragged as AudioClip;
                                    if (aClip == null)
                                    {
                                        continue;
                                    }

                                    clips.Add(aClip);
                                }

                                clips.Sort(delegate (AudioClip x, AudioClip y)
                                {
                                    return x.name.CompareTo(y.name);
                                });

                                foreach (var aClip in clips)
                                {
                                    if (_creator.curDragGroupMode == MasterAudio.DragGroupMode.OneGroupPerClip)
                                    {
                                        CreateGroup(aClip);
                                    }
                                    else
                                    {
                                        if (groupInfo == null)
                                        { // one group with variations
                                            groupInfo = CreateGroup(aClip);
                                        }
                                        else
                                        {
                                            CreateVariation(groupInfo, aClip);
                                        }
                                    }

                                    _isDirty = true;
                                }
                            }
                            Event.current.Use();
                            break;
                    }
                }
                EditorGUILayout.EndVertical();
                // create groups end

                if (_groups.Count == 0)
                {
                    DTGUIHelper.ShowLargeBarAlert("You currently have no Dynamic Sound Groups created.");
                }

                int? indexToDelete = null;
                DTGUIHelper.ResetColors();

                GUI.color = Color.white;
                int? busToCreate = null;
                var isExistingBus = false;

                for (var i = 0; i < _groups.Count; i++)
                {
                    var aGroup = _groups[i];

                    var groupDirty = false;

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    
                    var oldColor2 = GUI.color;
                    GUI.color = DTGUIHelper.BrightButtonColor;

                    var newImportance = EditorGUILayout.Popup("", aGroup.importance,
                        MasterAudio.ImportanceChoices.ToArray(), GUILayout.Width(32));
                    if (newImportance != aGroup.importance)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref groupDirty, aGroup,
                            "change Importance");
                        aGroup.importance = newImportance;
                    }

                    GUI.color = oldColor2;

                    GUILayout.Label(aGroup.name, GUILayout.MinWidth(100));

                    GUILayout.FlexibleSpace();

                    // find bus.
                    var selectedBusIndex = aGroup.busIndex == -1 ? 0 : aGroup.busIndex;

                    GUI.contentColor = Color.white;
                    GUI.color = DTGUIHelper.BrightButtonColor;

                    var busIndex = EditorGUILayout.Popup("", selectedBusIndex, busList.ToArray(), GUILayout.Width(busListWidth));
                    if (busIndex == -1)
                    {
                        busIndex = 0;
                    }

                    if (aGroup.busIndex != busIndex && busIndex != 1)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref groupDirty, aGroup, "change Group Bus");
                    }

                    if (busIndex != 1)
                    { // don't change the index, so undo will work.
                        aGroup.busIndex = busIndex;
                    }

                    GUI.color = Color.white;

                    if (selectedBusIndex != busIndex)
                    {
                        if (busIndex == 1 || busIndex == 2)
                        {
                            busToCreate = i;

                            isExistingBus = busIndex == 2;
                        }
                        else if (busIndex >= DynamicSoundGroupCreator.HardCodedBusOptions)
                        {
                            //GroupBus newBus = _creator.groupBuses[busIndex - MasterAudio.HARD_CODED_BUS_OPTIONS];
                            // do nothing unless we add muting and soloing here.
                        }
                    }

                    GUI.contentColor = DTGUIHelper.BrightTextColor;
                    GUILayout.TextField(DTGUIHelper.DisplayVolumeNumber(aGroup.groupMasterVolume, sliderIndicatorChars), sliderIndicatorChars, EditorStyles.miniLabel, GUILayout.Width(sliderWidth));

                    var newVol = DTGUIHelper.DisplayVolumeField(aGroup.groupMasterVolume, DTGUIHelper.VolumeFieldType.DynamicMixerGroup, MasterAudio.MixerWidthMode.Normal);
                    if (newVol != aGroup.groupMasterVolume)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref groupDirty, aGroup, "change Group Volume");
                        aGroup.groupMasterVolume = newVol;
                    }

                    GUI.contentColor = Color.white;

                    var buttonPressed = DTGUIHelper.AddDynamicGroupButtons(_creator.gameObject);
                    EditorGUILayout.EndHorizontal();

                    switch (buttonPressed)
                    {
                        case DTGUIHelper.DTFunctionButtons.Go:
                            Selection.activeGameObject = aGroup.gameObject;
                            break;
                        case DTGUIHelper.DTFunctionButtons.Remove:
                            indexToDelete = i;
                            break;
                        case DTGUIHelper.DTFunctionButtons.Play:
                            PreviewGroup(aGroup);
                            break;
                        case DTGUIHelper.DTFunctionButtons.Stop:
                            MasterAudioInspector.StopPreviewer();
                            break;
                    }

                    if (groupDirty)
                    {
                        EditorUtility.SetDirty(aGroup);
                    }
                }

                if (busToCreate.HasValue)
                {
                    CreateBus(busToCreate.Value, isExistingBus);
                }

                if (indexToDelete.HasValue)
                {
                    var groupToDelete = _groups[indexToDelete.Value];

                    var wasDestroyed = false;

                    if (PrefabUtility.IsPartOfPrefabInstance(_creator)) {
                        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_creator);
                        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                        var deadTrans = prefabRoot.transform.Find(groupToDelete.name);

                        if (deadTrans != null) {
                            // Destroy child objects or components on rootGO
                            DestroyImmediate(deadTrans.gameObject); // can't undo
                            wasDestroyed = true;
                        } 

                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    } 
                
                    if (!wasDestroyed) { 
                        AudioUndoHelper.DestroyForUndo(groupToDelete.gameObject);
                    }
                }

                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal();

                GUILayout.Space(2);

                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (_groups.Count > 0)
                {
                    if (GUILayout.Button(new GUIContent("Max Group Volumes", "Reset all group volumes to full"),
                        EditorStyles.toolbarButton, GUILayout.Width(120)))
                    {
                        AudioUndoHelper.RecordObjectsForUndo(_groups.ToArray(), "Max Group Volumes");

                        foreach (var aGroup in _groups)
                        {
                            aGroup.groupMasterVolume = 1f;
                        }
                    }
                }

                if (_creator.audioSourceTemplates.Count > 0 && !Application.isPlaying && _creator.transform.childCount > 0)
                {
                    if (_groups.Count > 0)
                    {
                        GUILayout.Space(10);
                    }

                    GUI.contentColor = DTGUIHelper.BrightButtonColor;

                    if (GUILayout.Button("Apply Audio Source Template to All", EditorStyles.toolbarButton, GUILayout.Width(210)))
                    {
                        applyTemplateToAll = true;
                    }

                }
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                //buses
                if (_creator.groupBuses.Count > 0)
                {
                    DTGUIHelper.VerticalSpace(3);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Bus Control", GUILayout.Width(100));

                    EditorGUILayout.EndHorizontal();

                    int? busToDelete = null;

                    var showOcclusion = !maInScene || (MasterAudio.Instance.useOcclusion && MasterAudio.Instance.occlusionSelectType == MasterAudio.OcclusionSelectionType.TurnOnPerBusOrGroup);

                    for (var i = 0; i < _creator.groupBuses.Count; i++)
                    {
                        DTGUIHelper.StartGroupHeader(1, false);
                        var aBus = _creator.groupBuses[i];

                        var showingMixer = _creator.ShouldShowUnityAudioMixerGroupAssignments && !aBus.isExisting;

                        if (showingMixer)
                        {
                            EditorGUILayout.BeginVertical();
                            EditorGUILayout.BeginHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.BeginHorizontal();
                        }

                        var newBusName = EditorGUILayout.TextField("", aBus.busName, GUILayout.MaxWidth(170));
                        if (newBusName != aBus.busName)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Bus Name");
                            aBus.busName = newBusName;
                        }

                        GUILayout.FlexibleSpace();

                        if (!aBus.isExisting)
                        {
                            GUI.color = Color.white;
                            DTGUIHelper.WhiteLabel("Voices");
                            GUI.color = DTGUIHelper.BrightButtonColor;

                            var oldLimitIndex = busVoiceLimitList.IndexOf(aBus.voiceLimit.ToString());
                            if (oldLimitIndex == -1)
                            {
                                oldLimitIndex = 0;
                            }
                            var busVoiceLimitIndex = EditorGUILayout.Popup("", oldLimitIndex, busVoiceLimitList.ToArray(), GUILayout.MaxWidth(70));
                            if (busVoiceLimitIndex != oldLimitIndex)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Bus Voice Limit");
                                aBus.voiceLimit = busVoiceLimitIndex <= 0 ? -1 : busVoiceLimitIndex;
                            }

                            GUI.color = DTGUIHelper.BrightTextColor;

                            GUILayout.TextField(DTGUIHelper.DisplayVolumeNumber(aBus.volume, sliderIndicatorChars), sliderIndicatorChars, EditorStyles.miniLabel, GUILayout.Width(sliderWidth));

                            GUI.color = Color.white;
                            var newBusVol = DTGUIHelper.DisplayVolumeField(aBus.volume, DTGUIHelper.VolumeFieldType.Bus, MasterAudio.MixerWidthMode.Normal);
                            if (newBusVol != aBus.volume)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Bus Volume");
                                aBus.volume = newBusVol;
                            }

                            GUI.contentColor = Color.white;
                        }
                        else
                        {
                            EditorGUILayout.LabelField("Existing bus. No control.");
                        }

                        if (DTGUIHelper.AddDeleteIcon("Bus"))
                        {
                            busToDelete = i;
                        }

                        EditorGUILayout.EndHorizontal();

                        if (aBus.voiceLimit >= 0)
                        {
                            GUI.color = DTGUIHelper.BrightButtonColor;
                            var newVoiceLimitExceededMode = (MasterAudio.BusVoiceLimitExceededMode)EditorGUILayout.EnumPopup(
                                new GUIContent("Voices Exceeded Behavior", "This controls what happens when the Bus voice limit is already reached and you play a sound"),
                                aBus.busVoiceLimitExceededMode);
                            if (newVoiceLimitExceededMode != aBus.busVoiceLimitExceededMode)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Voices Exceeded Behavior");
                                aBus.busVoiceLimitExceededMode = newVoiceLimitExceededMode;
                            }
                            GUI.color = Color.white;
                        }

                        if (showingMixer)
                        {
                            var newChan = (AudioMixerGroup)EditorGUILayout.ObjectField(aBus.mixerChannel, typeof(AudioMixerGroup), false);
                            if (newChan != aBus.mixerChannel)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Bus Mixer Group");
                                aBus.mixerChannel = newChan;
                            }
                            EditorGUILayout.EndVertical();
                        }

                        GUI.backgroundColor = Color.white;
                        if (!aBus.isExisting)
                        {
                            EditorGUILayout.BeginHorizontal();
                            var new2D = GUILayout.Toggle(aBus.forceTo2D, "Force to 2D");
                            if (new2D != aBus.forceTo2D)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Force to 2D");
                                aBus.forceTo2D = new2D;
                            }


                            if (!aBus.forceTo2D)
                            {
                                var newOcc = GUILayout.Toggle(aBus.isUsingOcclusion, "Use Occlusion");
                                if (newOcc != aBus.isUsingOcclusion)
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator,
                                        "toggle Use Occlusion");
                                    aBus.isUsingOcclusion = newOcc;
                                }
                            }

                            GUILayout.Label("Color");
                            var newBusColor = EditorGUILayout.ColorField(aBus.busColor);
                            if (newBusColor != aBus.busColor)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Bus Color");
                                aBus.busColor = newBusColor;
                            }

                            GUILayout.FlexibleSpace();

                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndVertical();
                    }

                    if (showOcclusion && _creator.groupBuses.Count > 0 && !maInScene)
                    {
                        DTGUIHelper.ShowLargeBarAlert("The Occlusion setting on Buses will only be used if the Master Audio Game Object is set to allow Occlusion.");
                    }

                    if (busToDelete.HasValue)
                    {
                        DeleteBus(busToDelete.Value);
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(6);

                    GUI.contentColor = DTGUIHelper.BrightButtonColor;
                    var buttonText = "Show Unity Mixer Groups";
                    if (_creator.showUnityMixerGroupAssignment)
                    {
                        buttonText = "Hide Unity Mixer Groups";
                    }
                    if (GUILayout.Button(new GUIContent(buttonText), EditorStyles.toolbarButton, GUILayout.Width(150)))
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, buttonText);
                        _creator.showUnityMixerGroupAssignment = !_creator.showUnityMixerGroupAssignment;
                    }

                    GUI.contentColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }

                DTGUIHelper.EndGroupedControls();
            }

            if (applyTemplateToAll)
            {
                AudioUndoHelper.RecordObjectsForUndo(_groups.ToArray(), "Apply Audio Source Template to All");

                foreach (var myGroup in _groups)
                {
                    for (var v = 0; v < myGroup.transform.childCount; v++)
                    {
                        var aVar = myGroup.transform.GetChild(v);
                        var oldAudio = aVar.GetComponent<AudioSource>();
                        CopyFromAudioSourceTemplate(_creator, oldAudio, true);
                    }
                }
            }

            DTGUIHelper.VerticalSpace(3);
            DTGUIHelper.ResetColors();

            // Music playlist Start		
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel = 0;  // Space will handle this for the header

            state = _creator.playListExpanded;
            text = "Dynamic Playlist Settings";

            DTGUIHelper.ShowCollapsibleSection(ref state, text);

            GUILayout.Space(2f);


            if (state != _creator.playListExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Dynamic Playlist Settings");
                _creator.playListExpanded = state;
            }

            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/DynamicSoundGroupCreators.htm#Playlists");

            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            if (_creator.playListExpanded)
            {
                DTGUIHelper.BeginGroupedControls();
                EditorGUI.indentLevel = 0;  // Space will handle this for the header

                if (_creator.musicPlaylists.Count == 0)
                {
                    DTGUIHelper.ShowLargeBarAlert("You currently have no Playlists set up.");
                }

                EditorGUI.indentLevel = 1;
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                var oldPlayExpanded = DTGUIHelper.Foldout(_creator.playlistEditorExp, string.Format("Playlists ({0})", _creator.musicPlaylists.Count));
                if (oldPlayExpanded != _creator.playlistEditorExp)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Playlists");
                    _creator.playlistEditorExp = oldPlayExpanded;
                }

                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(100));

                const string buttonText = "Click to add new Playlist at the end";

                // Add button - Process presses later
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                var addPressed = GUILayout.Button(new GUIContent("Add", buttonText),
                    EditorStyles.toolbarButton);

                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                var content = new GUIContent("Collapse", "Click to collapse all");
                var masterCollapse = GUILayout.Button(content, EditorStyles.toolbarButton);

                content = new GUIContent("Expand", "Click to expand all");
                var masterExpand = GUILayout.Button(content, EditorStyles.toolbarButton);
                if (masterExpand)
                {
                    ExpandCollapseAllPlaylists(true);
                }
                if (masterCollapse)
                {
                    ExpandCollapseAllPlaylists(false);
                }
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();

                if (_creator.playlistEditorExp)
                {
                    int? playlistToRemove = null;
                    int? playlistToInsertAt = null;
                    int? playlistToMoveUp = null;
                    int? playlistToMoveDown = null;

                    for (var i = 0; i < _creator.musicPlaylists.Count; i++)
                    {
                        DTGUIHelper.StartGroupHeader();

                        var aList = _creator.musicPlaylists[i];

                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.BeginHorizontal();
                        aList.isExpanded = DTGUIHelper.Foldout(aList.isExpanded, "Playlist: " + aList.playlistName);

                        var playlistButtonPressed = DTGUIHelper.AddFoldOutListItemButtonItems(i, _creator.musicPlaylists.Count, "playlist", false, false, true);

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();

                        if (aList.isExpanded)
                        {
                            DTGUIHelper.StartGroupHeader(2);
                            EditorGUI.indentLevel = 0;
                            var exp2 = EditorGUILayout.BeginToggleGroup(" Show Song Metadata", aList.showMetadata);
                            if (exp2 != aList.showMetadata)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Show Song Metadata");
                                aList.showMetadata = exp2;
                            }
                            DTGUIHelper.EndGroupHeader();

                            if (aList.showMetadata)
                            {
                                if (!Application.isPlaying)
                                {
                                    var newPropName = EditorGUILayout.TextField("Property Name", aList.newMetadataPropName);
                                    if (newPropName != aList.newMetadataPropName)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property Name");
                                        aList.newMetadataPropName = newPropName;
                                    }

                                    var newPropType = (SongMetadataProperty.MetadataPropertyType)EditorGUILayout.EnumPopup("Property Type", aList.newMetadataPropType);
                                    if (newPropType != aList.newMetadataPropType)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property Name");
                                        aList.newMetadataPropType = newPropType;
                                    }

                                    var newPropRequired = EditorGUILayout.Toggle("Is Required", aList.newMetadataPropRequired);
                                    if (newPropRequired != aList.newMetadataPropRequired)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Is Required");
                                        aList.newMetadataPropRequired = newPropRequired;
                                    }

                                    var newPropMult = EditorGUILayout.Toggle("Song Can Have Multiple", aList.newMetadataPropCanHaveMult);
                                    if (newPropMult != aList.newMetadataPropCanHaveMult)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Song Can Have Multiple");
                                        aList.newMetadataPropCanHaveMult = newPropMult;
                                    }

                                    GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                    if (GUILayout.Button(new GUIContent("Create Metadata Property"), EditorStyles.toolbarButton, GUILayout.Width(160)))
                                    {
                                        CreateMetadataProperty(aList, aList.newMetadataPropName, aList.newMetadataPropType, aList.newMetadataPropRequired, aList.newMetadataPropCanHaveMult);
                                    }
                                }
                                GUI.contentColor = Color.white;

                                var metaLabelText = "Metadata Properties";
                                if (aList.songMetadataProps.Count > 0)
                                {
                                    metaLabelText += "(" + aList.songMetadataProps.Count + ")";
                                }

                                GUILayout.Label(metaLabelText, EditorStyles.boldLabel);

                                if (aList.songMetadataProps.Count == 0)
                                {
                                    DTGUIHelper.ShowColorWarning("You have no Metadata. Add some properties above.");
                                }

                                int? propIndexToDelete = null;

                                for (var s = 0; s < aList.songMetadataProps.Count; s++)
                                {
                                    var property = aList.songMetadataProps[s];

                                    GUI.backgroundColor = Color.white;
                                    DTGUIHelper.StartGroupHeader(0, false);

                                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                                    if (property.IsEditing)
                                    {
                                        GUI.backgroundColor = DTGUIHelper.BrightTextColor;
                                        var propName = EditorGUILayout.TextField("", property.ProspectiveName);
                                        if (propName != property.ProspectiveName)
                                        {
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property Name");
                                            property.ProspectiveName = propName;
                                        }

                                        var buttonPressed = DTGUIHelper.AddCancelSaveButtons("Song Metadata Property");

                                        switch (buttonPressed)
                                        {
                                            case DTGUIHelper.DTFunctionButtons.Cancel:
                                                property.IsEditing = false;
                                                property.ProspectiveName = property.PropertyName;
                                                _isDirty = true;
                                                break;
                                            case DTGUIHelper.DTFunctionButtons.Save:
                                                if (propName != null)
                                                {
                                                    propName = propName.Replace(" ", "");
                                                }
                                                if (string.IsNullOrEmpty(propName))
                                                {
                                                    DTGUIHelper.ShowAlert("You must give a name to your new Meta Property.");
                                                    break;
                                                }

                                                var match = aList.songMetadataProps.Find(delegate (SongMetadataProperty p)
                                                {
                                                    return p.PropertyName == propName;
                                                });

                                                if (match != null)
                                                {
                                                    DTGUIHelper.ShowAlert("You already have a Metadata Property named '" + propName + "'. Names must be unique.");
                                                    break;
                                                }

                                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property Name");

                                                var newName = property.ProspectiveName;
                                                var oldName = property.PropertyName;

                                                // update all songs to have the new Property Name
                                                for (var t = 0; t < aList.MusicSettings.Count; t++)
                                                {
                                                    var aSong = aList.MusicSettings[t];


                                                    switch (property.PropertyType)
                                                    {
                                                        case SongMetadataProperty.MetadataPropertyType.String:
                                                            var sMatches = aSong.metadataStringValues.FindAll(delegate (SongMetadataStringValue v)
                                                            {
                                                                return v.PropertyName == oldName;
                                                            });

                                                            for (var loop = 0; loop < sMatches.Count; loop++)
                                                            {
                                                                sMatches[loop].PropertyName = newName;
                                                            }
                                                            break;
                                                        case SongMetadataProperty.MetadataPropertyType.Boolean:
                                                            var bMatches = aSong.metadataBoolValues.FindAll(delegate (SongMetadataBoolValue v)
                                                            {
                                                                return v.PropertyName == oldName;
                                                            });

                                                            for (var loop = 0; loop < bMatches.Count; loop++)
                                                            {
                                                                bMatches[loop].PropertyName = newName;
                                                            }
                                                            break;
                                                        case SongMetadataProperty.MetadataPropertyType.Integer:
                                                            var iMatches = aSong.metadataIntValues.FindAll(delegate (SongMetadataIntValue v)
                                                            {
                                                                return v.PropertyName == oldName;
                                                            });

                                                            for (var loop = 0; loop < iMatches.Count; loop++)
                                                            {
                                                                iMatches[loop].PropertyName = newName;
                                                            }
                                                            break;
                                                        case SongMetadataProperty.MetadataPropertyType.Float:
                                                            var fMatches = aSong.metadataFloatValues.FindAll(delegate (SongMetadataFloatValue v)
                                                            {
                                                                return v.PropertyName == oldName;
                                                            });

                                                            for (var loop = 0; loop < fMatches.Count; loop++)
                                                            {
                                                                fMatches[loop].PropertyName = newName;
                                                            }
                                                            break;
                                                    }
                                                }

                                                property.PropertyName = newName;
                                                property.IsEditing = false;
                                                break;
                                        }

                                    }
                                    else
                                    {
                                        EditorGUI.indentLevel = 1;
                                        GUI.backgroundColor = Color.white;

                                        var newEditing = DTGUIHelper.Foldout(property.PropertyExpanded, property.PropertyName + " (" + property.PropertyType.ToString() + ")");
                                        if (newEditing != property.PropertyExpanded)
                                        {
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Property");
                                            property.PropertyExpanded = newEditing;
                                        }

                                        GUILayout.FlexibleSpace();
                                        var settingsIcon = new GUIContent(MasterAudioInspectorResources.GearTexture, "Click to edit Property");

                                        if (!Application.isPlaying)
                                        {
                                            GUI.backgroundColor = Color.white;
                                            if (GUILayout.Button(settingsIcon, EditorStyles.toolbarButton, GUILayout.Width(24), GUILayout.Height(16)))
                                            {
                                                property.IsEditing = true;
                                            }
                                            if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete Property"), EditorStyles.toolbarButton, GUILayout.MaxWidth(36)))
                                            {
                                                propIndexToDelete = s;
                                            }
                                        }
                                    }

                                    GUI.backgroundColor = Color.white;

                                    EditorGUILayout.EndHorizontal();

                                    if (property.PropertyExpanded)
                                    {
                                        EditorGUI.indentLevel = 0;

                                        if (Application.isPlaying)
                                        {
                                            DTGUIHelper.ShowColorWarning("Can't edit at runtime.");
                                        }
                                        else
                                        {
                                            var newRequired = EditorGUILayout.Toggle("Is Required", property.AllSongsMustContain);
                                            if (newRequired != property.AllSongsMustContain)
                                            {
                                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Is Required");
                                                property.AllSongsMustContain = newRequired;
                                                if (newRequired)
                                                {
                                                    MakePropertyRequired(property, aList);
                                                }
                                            }
                                            var newMult = EditorGUILayout.Toggle("Song Can Have Multiple", property.CanSongHaveMultiple);
                                            if (newMult != property.CanSongHaveMultiple)
                                            {
                                                var sbSongsWithMult = new StringBuilder();
                                                if (!newMult)
                                                { // turned off the checkbox
                                                    for (var x = 0; x < aList.MusicSettings.Count; x++)
                                                    {
                                                        var aSong = aList.MusicSettings[x];

                                                        switch (property.PropertyType)
                                                        {
                                                            case SongMetadataProperty.MetadataPropertyType.String:
                                                                if (aSong.metadataStringValues.FindAll(delegate (SongMetadataStringValue v)
                                                                {
                                                                    return v.PropertyName == property.PropertyName;
                                                                }).Count > 1)
                                                                {
                                                                    if (sbSongsWithMult.Length > 0)
                                                                    {
                                                                        sbSongsWithMult.Append(",");
                                                                    }
                                                                    sbSongsWithMult.Append(aSong.songName);
                                                                }
                                                                break;
                                                            case SongMetadataProperty.MetadataPropertyType.Boolean:
                                                                if (aSong.metadataBoolValues.FindAll(delegate (SongMetadataBoolValue v)
                                                                {
                                                                    return v.PropertyName == property.PropertyName;
                                                                }).Count > 1)
                                                                {
                                                                    if (sbSongsWithMult.Length > 0)
                                                                    {
                                                                        sbSongsWithMult.Append(",");
                                                                    }
                                                                    sbSongsWithMult.Append(aSong.songName);
                                                                }
                                                                break;
                                                            case SongMetadataProperty.MetadataPropertyType.Integer:
                                                                if (aSong.metadataIntValues.FindAll(delegate (SongMetadataIntValue v)
                                                                {
                                                                    return v.PropertyName == property.PropertyName;
                                                                }).Count > 1)
                                                                {
                                                                    if (sbSongsWithMult.Length > 0)
                                                                    {
                                                                        sbSongsWithMult.Append(",");
                                                                    }
                                                                    sbSongsWithMult.Append(aSong.songName);
                                                                }
                                                                break;
                                                            case SongMetadataProperty.MetadataPropertyType.Float:
                                                                if (aSong.metadataFloatValues.FindAll(delegate (SongMetadataFloatValue v)
                                                                {
                                                                    return v.PropertyName == property.PropertyName;
                                                                }).Count > 1)
                                                                {
                                                                    if (sbSongsWithMult.Length > 0)
                                                                    {
                                                                        sbSongsWithMult.Append(",");
                                                                    }
                                                                    sbSongsWithMult.Append(aSong.songName);
                                                                }
                                                                break;
                                                        }
                                                    }
                                                }

                                                if (sbSongsWithMult.Length > 0)
                                                {
                                                    DTGUIHelper.ShowAlert("Cannot turn off 'Song Can Have Multiple' because you have song(s) with multiple values for this property: " + sbSongsWithMult.ToString() + ". We cannot choose which value to keep, so you must make sure there is no more than one before turning off this checkbox.");
                                                }
                                                else
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Song Can Have Multiple");
                                                    property.CanSongHaveMultiple = newMult;
                                                }
                                            }
                                        }
                                    }

                                    EditorGUILayout.EndVertical();
                                }

                                if (propIndexToDelete.HasValue)
                                {
                                    DeleteMetadataProperty(aList, propIndexToDelete.Value);
                                }
                            }
                            EditorGUILayout.EndToggleGroup();
                            GUI.backgroundColor = Color.white;
                            EditorGUI.indentLevel = 0;

                            var newPlaylist = EditorGUILayout.TextField("Name", aList.playlistName);
                            if (newPlaylist != aList.playlistName)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Name");
                                aList.playlistName = newPlaylist;
                            }

                            var crossfadeMode = (MasterAudio.Playlist.CrossfadeTimeMode)EditorGUILayout.EnumPopup("Crossfade Mode", aList.crossfadeMode);
                            if (crossfadeMode != aList.crossfadeMode)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Crossfade Mode");
                                aList.crossfadeMode = crossfadeMode;
                            }
                            if (aList.crossfadeMode == MasterAudio.Playlist.CrossfadeTimeMode.Override)
                            {
                                var newCf = EditorGUILayout.Slider("Crossfade time (sec)", aList.crossFadeTime, 0f, MasterAudio.MaxCrossFadeTimeSeconds);
                                // ReSharper disable once CompareOfFloatsByEqualityOperator
                                if (newCf != aList.crossFadeTime)
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Crossfade time (sec)");
                                    aList.crossFadeTime = newCf;
                                }
                            }

                            var newFadeIn = EditorGUILayout.Toggle("Fade In First Song", aList.fadeInFirstSong);
                            if (newFadeIn != aList.fadeInFirstSong)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Fade In First Song");
                                aList.fadeInFirstSong = newFadeIn;
                            }

                            var newFadeOut = EditorGUILayout.Toggle("Fade Out Last Song", aList.fadeOutLastSong);
                            if (newFadeOut != aList.fadeOutLastSong)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Fade Out Last Song");
                                aList.fadeOutLastSong = newFadeOut;
                            }

                            var newTransType = (MasterAudio.SongFadeInPosition)EditorGUILayout.EnumPopup(new GUIContent("Song Transition Type", "If you choose 'New Clip From Beginning', then 'Begin Song Time Mode' for each Song will be used."), aList.songTransitionType);
                            if (newTransType != aList.songTransitionType)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Song Transition Type");
                                aList.songTransitionType = newTransType;
                            }
                            if (aList.songTransitionType == MasterAudio.SongFadeInPosition.SynchronizeClips)
                            {
                                DTGUIHelper.ShowColorWarning("All clips must be of exactly the same length in this mode.");
                            }

                            EditorGUI.indentLevel = 0;
                            var newBulkMode = (MasterAudio.AudioLocation)EditorGUILayout.EnumPopup("Clip Create Mode", aList.bulkLocationMode);
                            if (newBulkMode != aList.bulkLocationMode)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Bulk Clip Mode");
                                aList.bulkLocationMode = newBulkMode;
                            }

                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(2);
                            GUI.contentColor = DTGUIHelper.BrightButtonColor;
                            if (GUILayout.Button(new GUIContent("Eq. Song Volumes"), EditorStyles.toolbarButton, GUILayout.Width(110)))
                            {
                                EqualizePlaylistVolumes(aList.MusicSettings);
                            }

                            var hasExpanded = false;
                            foreach (var t in aList.MusicSettings)
                            {
                                if (!t.isExpanded)
                                {
                                    continue;
                                }
                                hasExpanded = true;
                                break;
                            }

                            var theButtonText = hasExpanded ? "Collapse All" : "Expand All";

                            GUILayout.Space(10);
                            GUI.contentColor = DTGUIHelper.BrightButtonColor;
                            if (GUILayout.Button(new GUIContent(theButtonText), EditorStyles.toolbarButton, GUILayout.Width(100)))
                            {
                                ExpandCollapseSongs(aList, !hasExpanded);
                            }
                            GUILayout.Space(10);
                            if (GUILayout.Button(new GUIContent("Sort Alpha"), EditorStyles.toolbarButton, GUILayout.Width(100)))
                            {
                                SortSongsAlpha(aList);
                            }

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            GUI.contentColor = Color.white;
                            EditorGUILayout.Separator();

                            EditorGUILayout.BeginVertical();
                            var anEvent = Event.current;

                            GUI.color = DTGUIHelper.DragAreaColor;

                            var dragArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
                            GUI.Box(dragArea, MasterAudio.DragAudioTip + " to add to playlist!");

                            GUI.color = Color.white;

                            switch (anEvent.type)
                            {
                                case EventType.DragUpdated:
                                case EventType.DragPerform:
                                    if (!dragArea.Contains(anEvent.mousePosition))
                                    {
                                        break;
                                    }

                                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                                    if (anEvent.type == EventType.DragPerform)
                                    {
                                        DragAndDrop.AcceptDrag();

                                        foreach (var dragged in DragAndDrop.objectReferences)
                                        {
                                            if (dragged is DefaultAsset)
                                            {
                                                var assetPaths = AssetDatabase.FindAssets("t:AudioClip", DragAndDrop.paths);

                                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Add Playlist Songs From Folder");

                                                foreach (var assetPath in assetPaths)
                                                {
                                                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(assetPath));
                                                    if (clip == null)
                                                    {
                                                        continue;
                                                    }

                                                    AddSongToPlaylist(aList, clip);
                                                }

                                                continue;
                                            }

                                            var aClip = dragged as AudioClip;
                                            if (aClip == null)
                                            {
                                                continue;
                                            }

                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Add Playlist Song(s)");
                                            AddSongToPlaylist(aList, aClip);
                                        }
                                    }
                                    Event.current.Use();
                                    break;
                            }
                            EditorGUILayout.EndVertical();

                            EditorGUI.indentLevel = 2;

                            int? addIndex = null;
                            int? removeIndex = null;
                            int? moveUpIndex = null;
                            int? moveDownIndex = null;
                            int? indexToClone = null;

                            if (aList.MusicSettings.Count == 0)
                            {
                                EditorGUI.indentLevel = 0;
                                DTGUIHelper.ShowLargeBarAlert("You currently have no songs in this Playlist.");
                            }

                            EditorGUI.indentLevel = 0;

                            for (var j = 0; j < aList.MusicSettings.Count; j++)
                            {
                                DTGUIHelper.StartGroupHeader(1);
                                var aSong = aList.MusicSettings[j];
                                var clipName = "Empty";
                                switch (aSong.audLocation)
                                {
                                    case MasterAudio.AudioLocation.Clip:
                                        if (aSong.clip != null)
                                        {
                                            clipName = aSong.clip.CachedName();
                                        }
                                        break;
                                    case MasterAudio.AudioLocation.ResourceFile:
                                        if (!string.IsNullOrEmpty(aSong.resourceFileName))
                                        {
                                            clipName = aSong.resourceFileName;
                                        }
                                        break;
#if ADDRESSABLES_ENABLED
                                case MasterAudio.AudioLocation.Addressable:
                                    clipName = AddressableEditorHelper.EditTimeAddressableName(aSong.audioClipAddressable);
                                    break;
#endif
                                }
                                EditorGUILayout.BeginHorizontal();
                                EditorGUI.indentLevel = 1;

                                aSong.songName = aSong.alias;
                                if (!string.IsNullOrEmpty(clipName) && string.IsNullOrEmpty(aSong.songName))
                                {
                                    aSong.songName = clipName;
                                }

                                GUI.backgroundColor = Color.white;

                                var newSongExpanded = DTGUIHelper.Foldout(aSong.isExpanded, aSong.songName);
                                if (newSongExpanded != aSong.isExpanded)
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Song expand");
                                    aSong.isExpanded = newSongExpanded;
                                }

                                var songButtonPressed = DTGUIHelper.AddFoldOutListItemButtonItems(j, aList.MusicSettings.Count, "clip", false, true, true, allowPreview);
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndVertical();

                                if (aSong.isExpanded)
                                {
                                    EditorGUI.indentLevel = 0;

                                    if (aList.showMetadata)
                                    {
                                        var oldBG = GUI.backgroundColor;

                                        DTGUIHelper.StartGroupHeader(0, false);

                                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                                        GUI.backgroundColor = Color.white;
                                        EditorGUI.indentLevel = 1;
                                        var metaLabelText = "Song Metadata";

                                        if (aSong.HasMetadataProperties)
                                        {
                                            metaLabelText += " (" + aSong.MetadataPropertyCount + ")";
                                        }

                                        var newExp = DTGUIHelper.Foldout(aSong.metadataExpanded, metaLabelText);
                                        if (newExp != aSong.metadataExpanded)
                                        {
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Song Metadata");
                                            aSong.metadataExpanded = newExp;
                                        }

                                        EditorGUILayout.EndHorizontal();

                                        if (aSong.metadataExpanded)
                                        {
                                            EditorGUI.indentLevel = 0;

                                            var propertiesToAdd = new List<string>();
                                            propertiesToAdd.Add("[Select a Property]");
                                            for (var p = 0; p < aList.songMetadataProps.Count; p++)
                                            {
                                                var aProp = aList.songMetadataProps[p];
                                                if (aProp.CanSongHaveMultiple)
                                                {
                                                    propertiesToAdd.Add(aProp.PropertyName);
                                                    continue;
                                                }

                                                if (aProp.AllSongsMustContain)
                                                {
                                                    continue;
                                                }

                                                // not required. If you don't have one, you can add.
                                                switch (aProp.PropertyType)
                                                {
                                                    case SongMetadataProperty.MetadataPropertyType.Boolean:
                                                        if (aSong.metadataBoolValues.FindAll(delegate (SongMetadataBoolValue v)
                                                        {
                                                            return v.PropertyName == aProp.PropertyName;
                                                        }).Count == 0)
                                                        {
                                                            propertiesToAdd.Add(aProp.PropertyName);
                                                        }
                                                        break;
                                                    case SongMetadataProperty.MetadataPropertyType.Integer:
                                                        if (aSong.metadataIntValues.FindAll(delegate (SongMetadataIntValue v)
                                                        {
                                                            return v.PropertyName == aProp.PropertyName;
                                                        }).Count == 0)
                                                        {
                                                            propertiesToAdd.Add(aProp.PropertyName);
                                                        }
                                                        break;
                                                    case SongMetadataProperty.MetadataPropertyType.Float:
                                                        if (aSong.metadataFloatValues.FindAll(delegate (SongMetadataFloatValue v)
                                                        {
                                                            return v.PropertyName == aProp.PropertyName;
                                                        }).Count == 0)
                                                        {
                                                            propertiesToAdd.Add(aProp.PropertyName);
                                                        }
                                                        break;
                                                    case SongMetadataProperty.MetadataPropertyType.String:
                                                        if (aSong.metadataStringValues.FindAll(delegate (SongMetadataStringValue v)
                                                        {
                                                            return v.PropertyName == aProp.PropertyName;
                                                        }).Count == 0)
                                                        {
                                                            propertiesToAdd.Add(aProp.PropertyName);
                                                        }
                                                        break;
                                                }
                                            }

                                            var oldBG2 = GUI.backgroundColor;

                                            if (!Application.isPlaying)
                                            {
                                                var propIndex = EditorGUILayout.Popup("Add Metadata Property", 0, propertiesToAdd.ToArray());
                                                if (propIndex != 0)
                                                {
                                                    var propName = propertiesToAdd[propIndex];
                                                    var propToAdd = aList.songMetadataProps.Find(delegate (SongMetadataProperty p)
                                                    {
                                                        return p.PropertyName == propName;
                                                    });

                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "add Property");

                                                    switch (propToAdd.PropertyType)
                                                    {
                                                        case SongMetadataProperty.MetadataPropertyType.Boolean:
                                                            aSong.metadataBoolValues.Add(new SongMetadataBoolValue(propToAdd));
                                                            break;
                                                        case SongMetadataProperty.MetadataPropertyType.String:
                                                            aSong.metadataStringValues.Add(new SongMetadataStringValue(propToAdd));
                                                            break;
                                                        case SongMetadataProperty.MetadataPropertyType.Integer:
                                                            aSong.metadataIntValues.Add(new SongMetadataIntValue(propToAdd));
                                                            break;
                                                        case SongMetadataProperty.MetadataPropertyType.Float:
                                                            aSong.metadataFloatValues.Add(new SongMetadataFloatValue(propToAdd));
                                                            break;
                                                    }
                                                }
                                            }

                                            if (!aSong.HasMetadataProperties)
                                            {
                                                DTGUIHelper.ShowColorWarning("This song has no metadata.");
                                            }

                                            int? sIndexToDelete = null;
                                            for (var m = 0; m < aSong.metadataStringValues.Count; m++)
                                            {
                                                GUI.backgroundColor = oldBG2;
                                                var aVal = aSong.metadataStringValues[m];

                                                EditorGUILayout.BeginHorizontal();

                                                var newVal = EditorGUILayout.TextField(aVal.PropertyName, aVal.Value);
                                                if (newVal != aVal.Value)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property value");
                                                    aVal.Value = newVal;
                                                }

                                                if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete Property"), EditorStyles.toolbarButton, GUILayout.MaxWidth(36)))
                                                {
                                                    sIndexToDelete = m;
                                                }

                                                EditorGUILayout.EndHorizontal();
                                            }

                                            GUI.backgroundColor = oldBG2;
                                            int? bIndexToDelete = null;
                                            for (var m = 0; m < aSong.metadataBoolValues.Count; m++)
                                            {
                                                var aVal = aSong.metadataBoolValues[m];
                                                EditorGUILayout.BeginHorizontal();

                                                var newVal = EditorGUILayout.Toggle(aVal.PropertyName, aVal.Value);
                                                if (newVal != aVal.Value)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property value");
                                                    aVal.Value = newVal;
                                                }

                                                if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete Property"), EditorStyles.toolbarButton, GUILayout.MaxWidth(36)))
                                                {
                                                    bIndexToDelete = m;
                                                }

                                                EditorGUILayout.EndHorizontal();
                                            }

                                            GUI.backgroundColor = oldBG2;
                                            int? iIndexToDelete = null;
                                            for (var m = 0; m < aSong.metadataIntValues.Count; m++)
                                            {
                                                GUI.backgroundColor = oldBG2;
                                                var aVal = aSong.metadataIntValues[m];
                                                EditorGUILayout.BeginHorizontal();

                                                var newVal = EditorGUILayout.IntField(aVal.PropertyName, aVal.Value);
                                                if (newVal != aVal.Value)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property value");
                                                    aVal.Value = newVal;
                                                }

                                                if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete Property"), EditorStyles.toolbarButton, GUILayout.MaxWidth(36)))
                                                {
                                                    iIndexToDelete = m;
                                                }

                                                EditorGUILayout.EndHorizontal();
                                            }

                                            GUI.backgroundColor = oldBG2;
                                            int? fIndexToDelete = null;
                                            for (var m = 0; m < aSong.metadataFloatValues.Count; m++)
                                            {
                                                GUI.backgroundColor = oldBG2;
                                                var aVal = aSong.metadataFloatValues[m];
                                                EditorGUILayout.BeginHorizontal();

                                                var newVal = EditorGUILayout.FloatField(aVal.PropertyName, aVal.Value);
                                                if (newVal != aVal.Value)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Property value");
                                                    aVal.Value = newVal;
                                                }

                                                if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete Property"), EditorStyles.toolbarButton, GUILayout.MaxWidth(36)))
                                                {
                                                    fIndexToDelete = m;
                                                }

                                                EditorGUILayout.EndHorizontal();
                                            }

                                            if (sIndexToDelete.HasValue)
                                            {
                                                var deadProp = aSong.metadataStringValues[sIndexToDelete.Value];
                                                var srcProp = aList.songMetadataProps.Find(delegate (SongMetadataProperty p)
                                                {
                                                    return p.PropertyName == deadProp.PropertyName;
                                                });
                                                if (srcProp == null || !srcProp.AllSongsMustContain)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                    aSong.metadataStringValues.RemoveAt(sIndexToDelete.Value);
                                                }
                                                else
                                                {
                                                    var propMatches = aSong.metadataStringValues.FindAll(delegate (SongMetadataStringValue s)
                                                    {
                                                        return s.PropertyName == deadProp.PropertyName;
                                                    });
                                                    if (propMatches.Count <= 1)
                                                    {
                                                        DTGUIHelper.ShowAlert("This Property is required. You cannot delete all instances of it from a song.");
                                                    }
                                                    else
                                                    {
                                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                        aSong.metadataStringValues.RemoveAt(sIndexToDelete.Value);
                                                    }
                                                }
                                            }
                                            if (bIndexToDelete.HasValue)
                                            {
                                                var deadProp = aSong.metadataBoolValues[bIndexToDelete.Value];
                                                var srcProp = aList.songMetadataProps.Find(delegate (SongMetadataProperty p)
                                                {
                                                    return p.PropertyName == deadProp.PropertyName;
                                                });
                                                if (srcProp == null || !srcProp.AllSongsMustContain)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                    aSong.metadataBoolValues.RemoveAt(bIndexToDelete.Value);
                                                }
                                                else
                                                {
                                                    var propMatches = aSong.metadataBoolValues.FindAll(delegate (SongMetadataBoolValue s)
                                                    {
                                                        return s.PropertyName == deadProp.PropertyName;
                                                    });
                                                    if (propMatches.Count <= 1)
                                                    {
                                                        DTGUIHelper.ShowAlert("This Property is required. You cannot delete all instances of it from a song.");
                                                    }
                                                    else
                                                    {
                                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                        aSong.metadataBoolValues.RemoveAt(bIndexToDelete.Value);
                                                    }
                                                }
                                            }
                                            if (iIndexToDelete.HasValue)
                                            {
                                                var deadProp = aSong.metadataIntValues[iIndexToDelete.Value];
                                                var srcProp = aList.songMetadataProps.Find(delegate (SongMetadataProperty p)
                                                {
                                                    return p.PropertyName == deadProp.PropertyName;
                                                });
                                                if (srcProp == null || !srcProp.AllSongsMustContain)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                    aSong.metadataIntValues.RemoveAt(iIndexToDelete.Value);
                                                }
                                                else
                                                {
                                                    var propMatches = aSong.metadataIntValues.FindAll(delegate (SongMetadataIntValue s)
                                                    {
                                                        return s.PropertyName == deadProp.PropertyName;
                                                    });
                                                    if (propMatches.Count <= 1)
                                                    {
                                                        DTGUIHelper.ShowAlert("This Property is required. You cannot delete all instances of it from a song.");
                                                    }
                                                    else
                                                    {
                                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                        aSong.metadataIntValues.RemoveAt(iIndexToDelete.Value);
                                                    }
                                                }
                                            }
                                            if (fIndexToDelete.HasValue)
                                            {
                                                var deadProp = aSong.metadataFloatValues[fIndexToDelete.Value];
                                                var srcProp = aList.songMetadataProps.Find(delegate (SongMetadataProperty p)
                                                {
                                                    return p.PropertyName == deadProp.PropertyName;
                                                });
                                                if (srcProp == null || !srcProp.AllSongsMustContain)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                    aSong.metadataFloatValues.RemoveAt(fIndexToDelete.Value);
                                                }
                                                else
                                                {
                                                    var propMatches = aSong.metadataFloatValues.FindAll(delegate (SongMetadataFloatValue s)
                                                    {
                                                        return s.PropertyName == deadProp.PropertyName;
                                                    });
                                                    if (propMatches.Count <= 1)
                                                    {
                                                        DTGUIHelper.ShowAlert("This Property is required. You cannot delete all instances of it from a song.");
                                                    }
                                                    else
                                                    {
                                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Property");
                                                        aSong.metadataFloatValues.RemoveAt(fIndexToDelete.Value);
                                                    }
                                                }
                                            }
                                        }

                                        EditorGUILayout.EndVertical();
                                        GUI.backgroundColor = oldBG;
                                    }

                                    EditorGUI.indentLevel = 0;
                                    var newName = EditorGUILayout.TextField(new GUIContent("Song Id (optional)", "When you 'Play song by name', Song Id's will be searched first before audio file name. You should add an alias for each Addressable song if you want to play it by name so you don't have specify the entire folder path."), aSong.alias);
                                    if (newName != aSong.alias)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Song Id");
                                        aSong.alias = newName;
                                    }

                                    var oldLocation = aSong.audLocation;
                                    var newClipSource = (MasterAudio.AudioLocation)EditorGUILayout.EnumPopup("Audio Origin", aSong.audLocation);
                                    if (newClipSource != aSong.audLocation)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Audio Origin");
                                        aSong.audLocation = newClipSource;
                                    }

                                    if (oldLocation != aSong.audLocation && oldLocation == MasterAudio.AudioLocation.Clip)
                                    {
                                        if (aSong.clip != null)
                                        {
                                            Debug.Log("Audio clip removed to prevent unnecessary memory usage.");
                                        }
                                        aSong.clip = null;
                                        aSong.songName = string.Empty;
                                    }

                                    switch (aSong.audLocation)
                                    {
                                        case MasterAudio.AudioLocation.Clip:
                                            var newClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", aSong.clip, typeof(AudioClip), true);
                                            if (newClip != aSong.clip)
                                            {
                                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Clip");
                                                aSong.clip = newClip;
                                                var cName = newClip == null ? "Empty" : newClip.CachedName();
                                                aSong.songName = cName;
                                            }
                                            break;
#if ADDRESSABLES_ENABLED
                                    case MasterAudio.AudioLocation.Addressable:
                                        var varSerialized = new SerializedObject(_creator);
                                        varSerialized.Update();

                                        var propertyPlaylists = serializedObject.FindProperty(nameof(_creator.musicPlaylists));
                                        if (propertyPlaylists.arraySize <= i) {
                                            break;
                                        }
                                        var propertySongs = propertyPlaylists.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(MasterAudio.Playlist.MusicSettings));
                                        if (propertySongs.arraySize <= j) {
                                            break;
                                        }
                                        var property = propertySongs.GetArrayElementAtIndex(j).FindPropertyRelative(nameof(MusicSetting.audioClipAddressable));

                                        if (property != null) {
                                            EditorGUILayout.PropertyField(property, new GUIContent("Audio Clip Addressable", "Select your Addressable Audio Clip"));
                                        }
                                        varSerialized.ApplyModifiedProperties();

                                        if (!DTGUIHelper.IsAddressableTypeValid(aSong.audioClipAddressable, aList.playlistName)) {
                                            aSong.audioClipAddressable = null;
                                            _isDirty = true;
                                        }
                                        break;
#endif
                                        case MasterAudio.AudioLocation.ResourceFile:
                                            EditorGUILayout.BeginVertical();
                                            anEvent = Event.current;

                                            GUI.color = DTGUIHelper.DragAreaColor;
                                            dragArea = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
                                            GUI.Box(dragArea, "Drag Resource Audio clip here to use its name!");
                                            GUI.color = Color.white;

                                            switch (anEvent.type)
                                            {
                                                case EventType.DragUpdated:
                                                case EventType.DragPerform:
                                                    if (!dragArea.Contains(anEvent.mousePosition))
                                                    {
                                                        break;
                                                    }

                                                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                                                    if (anEvent.type == EventType.DragPerform)
                                                    {
                                                        DragAndDrop.AcceptDrag();

                                                        foreach (var dragged in DragAndDrop.objectReferences)
                                                        {
                                                            var aClip = dragged as AudioClip;
                                                            if (aClip == null)
                                                            {
                                                                continue;
                                                            }

                                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Resource Filename");

                                                            var unused = false;
                                                            var resourceFileName = DTGUIHelper.GetResourcePath(aClip, ref unused, true);
                                                            if (string.IsNullOrEmpty(resourceFileName))
                                                            {
                                                                resourceFileName = aClip.CachedName();
                                                            }

                                                            aSong.resourceFileName = resourceFileName;
                                                            aSong.songName = aClip.CachedName();
                                                        }
                                                    }
                                                    Event.current.Use();
                                                    break;
                                            }
                                            EditorGUILayout.EndVertical();

                                            var newFilename = EditorGUILayout.TextField("Resource Filename", aSong.resourceFileName);
                                            if (newFilename != aSong.resourceFileName)
                                            {
                                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Resource Filename");
                                                aSong.resourceFileName = newFilename;
                                            }

                                            break;
                                    }

                                    var newVol = DTGUIHelper.DisplayVolumeField(aSong.volume, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, 0f, true);
                                    if (newVol != aSong.volume)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Volume");
                                        aSong.volume = newVol;
                                    }

                                    var newPitch = DTGUIHelper.DisplayPitchField(aSong.pitch);
                                    if (newPitch != aSong.pitch)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Pitch");
                                        aSong.pitch = newPitch;
                                    }

                                    if (aList.songTransitionType == MasterAudio.SongFadeInPosition.SynchronizeClips)
                                    {
                                        DTGUIHelper.ShowLargeBarAlert("All songs must loop in Synchronized Playlists when crossfade time is not zero. Auto-advance is disabled.");
                                    }
                                    else
                                    {
                                        var newLoop = EditorGUILayout.Toggle("Loop Clip", aSong.isLoop);
                                        if (newLoop != aSong.isLoop)
                                        {
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Loop Clip");
                                            aSong.isLoop = newLoop;
                                        }
                                    }

                                    var useLastKnownPosition = aList.songTransitionType == MasterAudio.SongFadeInPosition.NewClipFromLastKnownPosition;

                                    if (aList.songTransitionType == MasterAudio.SongFadeInPosition.NewClipFromBeginning || useLastKnownPosition)
                                    {
                                        if (useLastKnownPosition && aSong.songStartTimeMode != MasterAudio.CustomSongStartTimeMode.Beginning)
                                        {
                                            DTGUIHelper.ShowLargeBarAlert("In this Song Transition Type, the Begin Song Time Mode will only be used the first time a song is played.");
                                        }

                                        var startTimeMode = (MasterAudio.CustomSongStartTimeMode)EditorGUILayout.EnumPopup("Begin Song Time Mode", aSong.songStartTimeMode);
                                        if (startTimeMode != aSong.songStartTimeMode)
                                        {
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Begin Song Time Mode");
                                            aSong.songStartTimeMode = startTimeMode;
                                        }

                                        switch (aSong.songStartTimeMode)
                                        {
                                            case MasterAudio.CustomSongStartTimeMode.SpecificTime:
                                                var newStart = EditorGUILayout.FloatField("Start Time (seconds)", aSong.customStartTime, GUILayout.Width(300));
                                                if (newStart < 0)
                                                {
                                                    newStart = 0f;
                                                }
                                                if (newStart != aSong.customStartTime)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Start Time (seconds)");
                                                    aSong.customStartTime = newStart;
                                                }
                                                break;
                                            case MasterAudio.CustomSongStartTimeMode.RandomTime:
                                                var newStartMin = EditorGUILayout.FloatField("Start Time Min (seconds)", aSong.customStartTime, GUILayout.Width(300));
                                                if (newStartMin < 0)
                                                {
                                                    newStartMin = 0f;
                                                }
                                                else if (newStartMin > aSong.customStartTimeMax)
                                                {
                                                    newStartMin = aSong.customStartTimeMax;
                                                }
                                                if (newStartMin != aSong.customStartTime)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Start Time Min (seconds)");
                                                    aSong.customStartTime = newStartMin;
                                                }

                                                var newMaxStart = EditorGUILayout.FloatField("Start Time Max (seconds)", aSong.customStartTimeMax, GUILayout.Width(300));
                                                if (newMaxStart < 0)
                                                {
                                                    newMaxStart = 0f;
                                                }
                                                else if (newMaxStart < aSong.customStartTime)
                                                {
                                                    newMaxStart = aSong.customStartTime;
                                                }
                                                if (newMaxStart != aSong.customStartTimeMax)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Start Time Max (seconds)");
                                                    aSong.customStartTimeMax = newMaxStart;
                                                }
                                                break;
                                        }
                                    }

                                    GUI.backgroundColor = Color.white;

                                    EditorGUI.indentLevel = 0;
                                    exp = EditorGUILayout.BeginToggleGroup("Song Started Event", aSong.songStartedEventExpanded);
                                    if (exp != aSong.songStartedEventExpanded)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Song Started Event");
                                        aSong.songStartedEventExpanded = exp;
                                    }
                                    GUI.color = Color.white;

                                    if (aSong.songStartedEventExpanded)
                                    {
                                        EditorGUI.indentLevel = 1;
                                        DTGUIHelper.ShowColorWarning("When song starts, fire Custom Event below.");

                                        if (maInScene)
                                        {
                                            var existingIndex = _customEventNames.IndexOf(aSong.songStartedCustomEvent);

                                            int? customEventIndex = null;

                                            var noEvent = false;
                                            var noMatch = false;

                                            if (existingIndex >= 1)
                                            {
                                                customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                                                if (existingIndex == 1)
                                                {
                                                    noEvent = true;
                                                }
                                            }
                                            else if (existingIndex == -1 && aSong.songStartedCustomEvent == MasterAudio.NoGroupName)
                                            {
                                                customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                                            }
                                            else
                                            { // non-match
                                                noMatch = true;
                                                var newEventName = EditorGUILayout.TextField("Custom Event Name", aSong.songStartedCustomEvent);
                                                if (newEventName != aSong.songStartedCustomEvent)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Custom Event Name");
                                                    aSong.songStartedCustomEvent = newEventName;
                                                }

                                                var newIndex = EditorGUILayout.Popup("All Custom Events", -1, _customEventNames.ToArray());
                                                if (newIndex >= 0)
                                                {
                                                    customEventIndex = newIndex;
                                                }
                                            }

                                            if (noEvent)
                                            {
                                                DTGUIHelper.ShowRedError("No Custom Event specified. This section will do nothing.");
                                            }
                                            else if (noMatch)
                                            {
                                                DTGUIHelper.ShowRedError("Custom Event found no match. Type in or choose one.");
                                            }

                                            if (customEventIndex.HasValue)
                                            {
                                                if (existingIndex != customEventIndex.Value)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Custom Event");
                                                }
                                                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                                if (customEventIndex.Value == -1)
                                                {
                                                    aSong.songStartedCustomEvent = MasterAudio.NoGroupName;
                                                }
                                                else
                                                {
                                                    aSong.songStartedCustomEvent = _customEventNames[customEventIndex.Value];
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", aSong.songStartedCustomEvent);
                                            if (newCustomEvent != aSong.songStartedCustomEvent)
                                            {
                                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Custom Event Name");
                                                aSong.songStartedCustomEvent = newCustomEvent;
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndToggleGroup();

                                    EditorGUI.indentLevel = 0;
                                    exp = EditorGUILayout.BeginToggleGroup("Song Changed Event", aSong.songChangedEventExpanded);
                                    if (exp != aSong.songChangedEventExpanded)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Song Changed Event");
                                        aSong.songChangedEventExpanded = exp;
                                    }
                                    GUI.color = Color.white;

                                    if (aSong.songChangedEventExpanded)
                                    {
                                        EditorGUI.indentLevel = 1;
                                        DTGUIHelper.ShowColorWarning("When song changes to another, fire Custom Event below.");
                                        DTGUIHelper.ShowLargeBarAlert("If you are using gapless transitions, Song Changed Event cannot be used.");

                                        if (maInScene)
                                        {
                                            var existingIndex = _customEventNames.IndexOf(aSong.songChangedCustomEvent);

                                            int? customEventIndex = null;


                                            var noEvent = false;
                                            var noMatch = false;

                                            if (existingIndex >= 1)
                                            {
                                                customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                                                if (existingIndex == 1)
                                                {
                                                    noEvent = true;
                                                }
                                            }
                                            else if (existingIndex == -1 && aSong.songChangedCustomEvent == MasterAudio.NoGroupName)
                                            {
                                                customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                                            }
                                            else
                                            { // non-match
                                                noMatch = true;
                                                var newEventName = EditorGUILayout.TextField("Custom Event Name", aSong.songChangedCustomEvent);
                                                if (newEventName != aSong.songChangedCustomEvent)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Custom Event Name");
                                                    aSong.songChangedCustomEvent = newEventName;
                                                }

                                                var newIndex = EditorGUILayout.Popup("All Custom Events", -1, _customEventNames.ToArray());
                                                if (newIndex >= 0)
                                                {
                                                    customEventIndex = newIndex;
                                                }
                                            }

                                            if (noEvent)
                                            {
                                                DTGUIHelper.ShowRedError("No Custom Event specified. This section will do nothing.");
                                            }
                                            else if (noMatch)
                                            {
                                                DTGUIHelper.ShowRedError("Custom Event found no match. Type in or choose one.");
                                            }

                                            if (customEventIndex.HasValue)
                                            {
                                                if (existingIndex != customEventIndex.Value)
                                                {
                                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Custom Event");
                                                }
                                                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                                if (customEventIndex.Value == -1)
                                                {
                                                    aSong.songChangedCustomEvent = MasterAudio.NoGroupName;
                                                }
                                                else
                                                {
                                                    aSong.songChangedCustomEvent = _customEventNames[customEventIndex.Value];
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", aSong.songChangedCustomEvent);
                                            if (newCustomEvent != aSong.songChangedCustomEvent)
                                            {
                                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Custom Event Name");
                                                aSong.songChangedCustomEvent = newCustomEvent;
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndToggleGroup();
                                }

                                switch (songButtonPressed)
                                {
                                    case DTGUIHelper.DTFunctionButtons.Add:
                                        addIndex = j;
                                        break;
                                    case DTGUIHelper.DTFunctionButtons.Remove:
                                        removeIndex = j;
                                        break;
                                    case DTGUIHelper.DTFunctionButtons.Clone:
                                        indexToClone = j;
                                        break;
                                    case DTGUIHelper.DTFunctionButtons.ShiftUp:
                                        moveUpIndex = j;
                                        break;
                                    case DTGUIHelper.DTFunctionButtons.ShiftDown:
                                        moveDownIndex = j;
                                        break;
                                    case DTGUIHelper.DTFunctionButtons.Play:
                                        previewer = MasterAudioInspector.GetPreviewer();
                                        MasterAudioInspector.StopPreviewer();
                                        switch (aSong.audLocation)
                                        {
                                            case MasterAudio.AudioLocation.Clip:
                                                if (previewer != null)
                                                {
                                                    DTGUIHelper.PlaySilentWakeUpPreview(previewer, aSong.clip);
                                                    previewer.PlayOneShot(aSong.clip, aSong.volume);
                                                }
                                                break;
                                            case MasterAudio.AudioLocation.ResourceFile:
                                                if (previewer != null)
                                                {
                                                    var resClip = Resources.Load(aSong.resourceFileName) as AudioClip;
                                                    DTGUIHelper.PlaySilentWakeUpPreview(previewer, resClip);
                                                    previewer.PlayOneShot(resClip, aSong.volume);
                                                }
                                                break;
#if ADDRESSABLES_ENABLED
                                        case MasterAudio.AudioLocation.Addressable:
                                            DTGUIHelper.PreviewAddressable(aSong.audioClipAddressable, previewer, aSong.volume);
                                            break;
#endif
                                        }
                                        break;
                                    case DTGUIHelper.DTFunctionButtons.Stop:
                                        MasterAudioInspector.StopPreviewer();
                                        break;
                                }

                                EditorGUILayout.EndVertical();
                            }

                            if (addIndex.HasValue)
                            {
                                var mus = new MusicSetting();
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "add song");
                                aList.MusicSettings.Insert(addIndex.Value + 1, mus);
                            }
                            else if (removeIndex.HasValue)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete song");
                                aList.MusicSettings.RemoveAt(removeIndex.Value);
                            }
                            else if (moveUpIndex.HasValue)
                            {
                                var item = aList.MusicSettings[moveUpIndex.Value];

                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "shift up song");

                                aList.MusicSettings.Insert(moveUpIndex.Value - 1, item);
                                aList.MusicSettings.RemoveAt(moveUpIndex.Value + 1);
                            }
                            else if (moveDownIndex.HasValue)
                            {
                                var index = moveDownIndex.Value + 1;
                                var item = aList.MusicSettings[index];

                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "shift down song");

                                aList.MusicSettings.Insert(index - 1, item);
                                aList.MusicSettings.RemoveAt(index + 1);
                            }
                            else if (indexToClone.HasValue)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "clone song");
                                aList.MusicSettings.Insert(indexToClone.Value, MusicSetting.Clone(aList.MusicSettings[indexToClone.Value], aList));
                            }
                        }

                        switch (playlistButtonPressed)
                        {
                            case DTGUIHelper.DTFunctionButtons.Remove:
                                playlistToRemove = i;
                                break;
                            case DTGUIHelper.DTFunctionButtons.Add:
                                playlistToInsertAt = i;
                                break;
                            case DTGUIHelper.DTFunctionButtons.ShiftUp:
                                playlistToMoveUp = i;
                                break;
                            case DTGUIHelper.DTFunctionButtons.ShiftDown:
                                playlistToMoveDown = i;
                                break;
                        }

                        EditorGUILayout.EndVertical();
                    }


                    if (playlistToRemove.HasValue)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "delete Playlist");

                        _creator.musicPlaylists.RemoveAt(playlistToRemove.Value);
                    }
                    if (playlistToInsertAt.HasValue)
                    {
                        var pl = new MasterAudio.Playlist();
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "add Playlist");
                        _creator.musicPlaylists.Insert(playlistToInsertAt.Value + 1, pl);
                    }
                    if (playlistToMoveUp.HasValue)
                    {
                        var item = _creator.musicPlaylists[playlistToMoveUp.Value];
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "shift up Playlist");
                        _creator.musicPlaylists.Insert(playlistToMoveUp.Value - 1, item);
                        _creator.musicPlaylists.RemoveAt(playlistToMoveUp.Value + 1);
                    }
                    if (playlistToMoveDown.HasValue)
                    {
                        var index = playlistToMoveDown.Value + 1;
                        var item = _creator.musicPlaylists[index];

                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "shift down Playlist");

                        _creator.musicPlaylists.Insert(index - 1, item);
                        _creator.musicPlaylists.RemoveAt(index + 1);
                    }
                }

                if (addPressed)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "add Playlist");
                    _creator.musicPlaylists.Add(new MasterAudio.Playlist());
                }

                DTGUIHelper.EndGroupedControls();
            }
            // Music playlist End

            EditorGUI.indentLevel = 0;
            // Show Custom Events

            DTGUIHelper.VerticalSpace(3);
            DTGUIHelper.ResetColors();

            state = _creator.showCustomEvents;
            text = "Dynamic Custom Events";

            DTGUIHelper.ShowCollapsibleSection(ref state, text);

            GUILayout.Space(2f);

            if (_creator.showCustomEvents != state)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Dynamic Custom Events");
                _creator.showCustomEvents = state;
            }

            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/DynamicSoundGroupCreators.htm#CustomEvents");
            EditorGUILayout.EndHorizontal();
            GUI.color = Color.white;

            if (_creator.showCustomEvents)
            {
                var catNames = new List<string>(_creator.customEventCategories.Count);
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _creator.customEventCategories.Count; i++)
                {
                    catNames.Add(_creator.customEventCategories[i].CatName);
                }

                var selCatIndex = catNames.IndexOf(_creator.addToCustomEventCategoryName);

                if (selCatIndex == -1)
                {
                    selCatIndex = 0;
                    _isDirty = true;
                }

                var defaultCat = catNames[selCatIndex];

                DTGUIHelper.BeginGroupedControls();
                DTGUIHelper.StartGroupHeader(0, false);
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
                var newEvent = EditorGUILayout.TextField("New Event Name", _creator.newEventName);
                if (newEvent != _creator.newEventName)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change New Event Name");
                    _creator.newEventName = newEvent;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button("Create New Event", EditorStyles.toolbarButton, GUILayout.Width(115)))
                {
                    CreateCustomEvent(_creator.newEventName, defaultCat);
                }
                GUILayout.Space(10);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;

                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                DTGUIHelper.StartGroupHeader(0, false);
                DTGUIHelper.ResetColors();
                var newCat = EditorGUILayout.TextField("New Category Name", _creator.newCustomEventCategoryName);
                if (newCat != _creator.newCustomEventCategoryName)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change New Category Name");
                    _creator.newCustomEventCategoryName = newCat;
                }
                EditorGUILayout.BeginHorizontal();
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                GUILayout.Space(4);
                if (GUILayout.Button("Create New Category", EditorStyles.toolbarButton, GUILayout.Width(130)))
                {
                    CreateCategory();
                }
                GUI.contentColor = Color.white;

                EditorGUILayout.EndHorizontal();

                DTGUIHelper.ShowLargeBarAlert("You must Create all Categories you need here even if they exist in the MA game object.");

                EditorGUILayout.EndVertical();
                DTGUIHelper.ResetColors();

                GUI.backgroundColor = DTGUIHelper.BrightButtonColor;

                var newIndex = EditorGUILayout.Popup("Default Event Category", selCatIndex, catNames.ToArray());
                if (newIndex != selCatIndex)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Default Event Category");
                    _creator.addToCustomEventCategoryName = catNames[newIndex];
                }

                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);

                var hasExpanded = false;
                foreach (var t in _creator.customEventsToCreate)
                {
                    if (string.IsNullOrEmpty(t.categoryName))
                    {
                        t.categoryName = defaultCat;
                    }

                    if (!t.eventExpanded)
                    {
                        continue;
                    }
                    hasExpanded = true;
                    break;
                }

                var buttonText = hasExpanded ? "Collapse All" : "Expand All";

                if (GUILayout.Button(buttonText, EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    ExpandCollapseCustomEvents(!hasExpanded);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                int? indexToShiftUp = null;
                int? indexToShiftDown = null;
                CustomEventCategory catEditing = null;
                CustomEventCategory catRenaming = null;
                CustomEventCategory catToDelete = null;
                CustomEvent eventEditing = null;
                CustomEvent eventToDelete = null;
                CustomEvent eventRenaming = null;

                DTGUIHelper.StartGroupHeader(0);

                for (var c = 0; c < _creator.customEventCategories.Count; c++)
                {
                    var cat = _creator.customEventCategories[c];

                    EditorGUI.indentLevel = 0;

                    var matchingItems = new List<CustomEvent>();
                    matchingItems.AddRange(_creator.customEventsToCreate);
                    matchingItems.RemoveAll(delegate (CustomEvent x)
                    {
                        return x.categoryName != cat.CatName;
                    });

                    var hasItems = matchingItems.Count > 0;

                    if (!cat.IsEditing || Application.isPlaying)
                    {

                        var catName = cat.CatName;

                        catName += ": " + matchingItems.Count + " item" + ((matchingItems.Count != 1) ? "s" : "");

                        var state2 = cat.IsExpanded;
                        var text2 = catName;

                        DTGUIHelper.ShowCollapsibleSectionInline(ref state2, text2);

                        if (state2 != cat.IsExpanded)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Custom Event Category");
                            cat.IsExpanded = state2;
                        }

                        var headerStyle = new GUIStyle();
                        headerStyle.margin = new RectOffset(0, 0, 0, 0);
                        headerStyle.padding = new RectOffset(0, 0, 0, 0);
                        headerStyle.fixedHeight = 20;

                        EditorGUILayout.BeginHorizontal(headerStyle, GUILayout.MaxWidth(50));

                        var catItemsCollapsed = true;

                        for (var i = 0; i < matchingItems.Count; i++)
                        {
                            var item = matchingItems[i];

                            if (!item.eventExpanded)
                            {
                                continue;
                            }
                            catItemsCollapsed = false;
                            break;
                        }

                        GUI.backgroundColor = Color.white;

                        var tooltip = catItemsCollapsed ? "Click to expand all items in this category" : "Click to collapse all items in this category";
                        var btnText = catItemsCollapsed ? "Expand" : "Collapse";

                        GUI.contentColor = DTGUIHelper.BrightButtonColor;
                        if (GUILayout.Button(new GUIContent(btnText, tooltip), EditorStyles.toolbarButton, GUILayout.Width(60), GUILayout.Height(16)))
                        {
                            ExpandCollapseCategory(cat.CatName, catItemsCollapsed);
                        }
                        GUI.contentColor = Color.white;

                        if (!Application.isPlaying)
                        {
                            if (c > 0)
                            {
                                // the up arrow.
                                var upArrow = MasterAudioInspectorResources.UpArrowTexture;
                                if (GUILayout.Button(new GUIContent(upArrow, "Click to shift Category up"),
                                                     EditorStyles.toolbarButton, GUILayout.Width(24), GUILayout.Height(16)))
                                {
                                    indexToShiftUp = c;
                                }
                            }
                            else
                            {
                                GUILayout.Button("", EditorStyles.toolbarButton, GUILayout.Width(24), GUILayout.Height(16));
                            }

                            if (c < _creator.customEventCategories.Count - 1)
                            {
                                // The down arrow will move things towards the end of the List
                                var dnArrow = MasterAudioInspectorResources.DownArrowTexture;
                                if (GUILayout.Button(new GUIContent(dnArrow, "Click to shift Category down"),
                                                     EditorStyles.toolbarButton, GUILayout.Width(24), GUILayout.Height(16)))
                                {
                                    indexToShiftDown = c;
                                }
                            }
                            else
                            {
                                GUILayout.Button("", EditorStyles.toolbarButton, GUILayout.Width(24), GUILayout.Height(16));
                            }

                            var settingsIcon = new GUIContent(MasterAudioInspectorResources.GearTexture,
                                                              "Click to edit Category");

                            GUI.backgroundColor = Color.white;
                            if (GUILayout.Button(settingsIcon, EditorStyles.toolbarButton, GUILayout.Width(24),
                                                 GUILayout.Height(16)))
                            {
                                catEditing = cat;
                            }
                            if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete Category"), EditorStyles.toolbarButton, GUILayout.MaxWidth(36)))
                            {
                                catToDelete = cat;
                            }
                        }

                        GUILayout.Space(4);

                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();

                        GUI.backgroundColor = DTGUIHelper.BrightTextColor;
                        var tex = EditorGUILayout.TextField("", cat.ProspectiveName);
                        if (tex != cat.ProspectiveName)
                        {
                            cat.ProspectiveName = tex;
                            _isDirty = true;
                        }

                        var buttonPressed = DTGUIHelper.AddCancelSaveButtons("Custom Event Category");

                        switch (buttonPressed)
                        {
                            case DTGUIHelper.DTFunctionButtons.Cancel:
                                cat.IsEditing = false;
                                cat.ProspectiveName = cat.CatName;
                                _isDirty = true;
                                break;
                            case DTGUIHelper.DTFunctionButtons.Save:
                                catRenaming = cat;
                                break;
                        }

                        GUILayout.Space(4);
                    }

                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();

                    if (cat.IsEditing)
                    {
                        DTGUIHelper.VerticalSpace(2);
                    }

                    matchingItems.Sort(delegate (CustomEvent x, CustomEvent y)
                    {
                        // ReSharper disable PossibleNullReferenceException
                        return string.Compare(x.EventName, y.EventName, StringComparison.Ordinal);
                        // ReSharper restore PossibleNullReferenceException
                    });

                    if (!hasItems)
                    {
                        DTGUIHelper.BeginGroupedControls();
                        DTGUIHelper.ShowLargeBarAlert("This Category is empty. Add / move some items or you may delete it.");
                        DTGUIHelper.EndGroupedControls();
                    }

                    GUI.contentColor = Color.white;

                    if (cat.IsExpanded)
                    {
                        if (hasItems)
                        {
                            DTGUIHelper.BeginGroupedControls();
                        }

                        for (var i = 0; i < matchingItems.Count; i++)
                        {
                            EditorGUI.indentLevel = 1;
                            var anEvent = matchingItems[i];

                            DTGUIHelper.StartGroupHeader();

                            EditorGUILayout.BeginHorizontal();

                            if (!anEvent.IsEditing || Application.isPlaying)
                            {
                                var exp2 = DTGUIHelper.Foldout(anEvent.eventExpanded, anEvent.EventName);
                                if (exp2 != anEvent.eventExpanded)
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand Custom Event");
                                    anEvent.eventExpanded = exp2;
                                }

                                GUILayout.FlexibleSpace();
                                if (!Application.isPlaying)
                                {
                                    GUI.backgroundColor = DTGUIHelper.BrightButtonColor;
                                    var newCatIndex = catNames.IndexOf(anEvent.categoryName);
                                    var newEventCat = EditorGUILayout.Popup(newCatIndex, catNames.ToArray(),
                                                                            GUILayout.Width(130));
                                    if (newEventCat != newCatIndex)
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator,
                                                                                    "change Custom Event Category");
                                        anEvent.categoryName = catNames[newEventCat];
                                    }
                                    GUI.backgroundColor = Color.white;
                                }

                                if (!Application.isPlaying)
                                {
                                    GUI.backgroundColor = Color.white;
                                    var settingsIcon = new GUIContent(MasterAudioInspectorResources.GearTexture,
                                                                      "Click to edit Custom Event Name");
                                    if (GUILayout.Button(settingsIcon, EditorStyles.toolbarButton, GUILayout.Width(24),
                                                         GUILayout.Height(16)))
                                    {
                                        eventEditing = anEvent;
                                    }

                                    if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete Event"), EditorStyles.toolbarButton, GUILayout.MaxWidth(36)))
                                    {
                                        eventToDelete = anEvent;
                                    }
                                }
                                GUI.contentColor = Color.white;
                            }
                            else
                            {
                                EditorGUI.indentLevel = 0;
                                GUI.backgroundColor = DTGUIHelper.BrightTextColor;
                                var tex = EditorGUILayout.TextField("", anEvent.ProspectiveName);
                                if (tex != anEvent.ProspectiveName)
                                {
                                    anEvent.ProspectiveName = tex;
                                    _isDirty = true;
                                }

                                var buttonPressed = DTGUIHelper.AddCancelSaveButtons("Custom Event");

                                switch (buttonPressed)
                                {
                                    case DTGUIHelper.DTFunctionButtons.Cancel:
                                        anEvent.IsEditing = false;
                                        anEvent.ProspectiveName = cat.CatName;
                                        _isDirty = true;
                                        break;
                                    case DTGUIHelper.DTFunctionButtons.Save:
                                        eventRenaming = anEvent;
                                        break;
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();

                            if (!anEvent.eventExpanded)
                            {
                                EditorGUILayout.EndVertical();
                                continue;
                            }

                            DTGUIHelper.ResetColors();
                            EditorGUI.indentLevel = 0;

                            var rcvMode =
                                (MasterAudio.CustomEventReceiveMode)
                                    EditorGUILayout.EnumPopup("Send To Receivers", anEvent.eventReceiveMode);
                            if (rcvMode != anEvent.eventReceiveMode)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Send To Receivers");
                                anEvent.eventReceiveMode = rcvMode;
                            }

                            if (rcvMode == MasterAudio.CustomEventReceiveMode.WhenDistanceLessThan ||
                                rcvMode == MasterAudio.CustomEventReceiveMode.WhenDistanceMoreThan)
                            {
                                var newDist = EditorGUILayout.Slider("Distance Threshold", anEvent.distanceThreshold, 0f,
                                                                     float.MaxValue);
                                if (newDist != anEvent.distanceThreshold)
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator,
                                                                                "change Distance Threshold");
                                    anEvent.distanceThreshold = newDist;
                                }
                            }

                            if (rcvMode != MasterAudio.CustomEventReceiveMode.Never)
                            {
                                var rcvFilter =
                                    (MasterAudio.EventReceiveFilter)
                                        EditorGUILayout.EnumPopup("Valid Receivers", anEvent.eventRcvFilterMode);
                                if (rcvFilter != anEvent.eventRcvFilterMode)
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "change Valid Receivers");
                                    anEvent.eventRcvFilterMode = rcvFilter;
                                }

                                switch (anEvent.eventRcvFilterMode)
                                {
                                    case MasterAudio.EventReceiveFilter.Closest:
                                    case MasterAudio.EventReceiveFilter.Random:
                                        var newQty = EditorGUILayout.IntField("Valid Qty", anEvent.filterModeQty);
                                        if (newQty != anEvent.filterModeQty)
                                        {
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator,
                                                                                        "change Valid Qty");
                                            anEvent.filterModeQty = System.Math.Max(1, newQty);
                                        }
                                        break;
                                }
                            }

                            EditorGUILayout.EndVertical();
                        }

                        if (hasItems)
                        {
                            DTGUIHelper.EndGroupedControls();
                        }
                    }

                    if (c < _creator.customEventCategories.Count - 1)
                    {
                        DTGUIHelper.VerticalSpace(3);
                    }
                }
                DTGUIHelper.EndGroupHeader();

                if (eventToDelete != null)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Delete Custom Event");
                    _creator.customEventsToCreate.Remove(eventToDelete);
                }

                if (indexToShiftUp.HasValue)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "shift up Category");
                    var item = _creator.customEventCategories[indexToShiftUp.Value];
                    _creator.customEventCategories.Insert(indexToShiftUp.Value - 1, item);
                    _creator.customEventCategories.RemoveAt(indexToShiftUp.Value + 1);
                    _isDirty = true;
                }

                if (indexToShiftDown.HasValue)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "shift down Category");
                    var index = indexToShiftDown.Value + 1;
                    var item = _creator.customEventCategories[index];
                    _creator.customEventCategories.Insert(index - 1, item);
                    _creator.customEventCategories.RemoveAt(index + 1);
                    _isDirty = true;
                }

                if (catToDelete != null)
                {
                    if (_creator.customEventsToCreate.FindAll(delegate (CustomEvent x)
                    {
                        return x.categoryName == catToDelete.CatName;
                    }).Count > 0)
                    {
                        DTGUIHelper.ShowAlert("You cannot delete a Category with Custom Events in it. Move or delete the items first.");
                    }
                    else if (_creator.customEventCategories.Count <= 1)
                    {
                        DTGUIHelper.ShowAlert("You cannot delete the last Category.");
                    }
                    else
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Delete Category");
                        _creator.customEventCategories.Remove(catToDelete);
                        _isDirty = true;
                    }
                }

                if (catRenaming != null)
                {
                    // ReSharper disable once ForCanBeConvertedToForeach
                    var isValidName = true;

                    if (string.IsNullOrEmpty(catRenaming.ProspectiveName))
                    {
                        isValidName = false;
                        DTGUIHelper.ShowAlert("You cannot have a blank Category name.");
                    }

                    if (isValidName)
                    {
                        // ReSharper disable once ForCanBeConvertedToForeach
                        for (var c = 0; c < _creator.customEventCategories.Count; c++)
                        {
                            var cat = _creator.customEventCategories[c];
                            // ReSharper disable once InvertIf
                            if (cat != catRenaming && cat.CatName == catRenaming.ProspectiveName)
                            {
                                isValidName = false;
                                DTGUIHelper.ShowAlert("You already have a Category named '" + catRenaming.ProspectiveName + "'. Category names must be unique.");
                            }
                        }

                        if (isValidName)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Undo change Category name.");

                            // ReSharper disable once ForCanBeConvertedToForeach
                            for (var i = 0; i < _creator.customEventsToCreate.Count; i++)
                            {
                                var item = _creator.customEventsToCreate[i];
                                if (item.categoryName == catRenaming.CatName)
                                {
                                    item.categoryName = catRenaming.ProspectiveName;
                                }
                            }

                            catRenaming.CatName = catRenaming.ProspectiveName;
                            catRenaming.IsEditing = false;
                            _isDirty = true;
                        }
                    }
                }

                if (catEditing != null)
                {
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var c = 0; c < _creator.customEventCategories.Count; c++)
                    {
                        var cat = _creator.customEventCategories[c];
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (catEditing == cat)
                        {
                            cat.IsEditing = true;
                        }
                        else
                        {
                            cat.IsEditing = false;
                        }

                        _isDirty = true;
                    }
                }

                if (eventEditing != null)
                {
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var c = 0; c < _creator.customEventsToCreate.Count; c++)
                    {
                        var evt = _creator.customEventsToCreate[c];
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (eventEditing == evt)
                        {
                            evt.IsEditing = true;
                        }
                        else
                        {
                            evt.IsEditing = false;
                        }

                        _isDirty = true;
                    }
                }

                if (eventRenaming != null)
                {
                    // ReSharper disable once ForCanBeConvertedToForeach
                    var isValidName = true;

                    if (string.IsNullOrEmpty(eventRenaming.ProspectiveName))
                    {
                        isValidName = false;
                        DTGUIHelper.ShowAlert("You cannot have a blank Custom Event name.");
                    }

                    if (isValidName)
                    {
                        // ReSharper disable once ForCanBeConvertedToForeach
                        for (var c = 0; c < _creator.customEventsToCreate.Count; c++)
                        {
                            var evt = _creator.customEventsToCreate[c];
                            // ReSharper disable once InvertIf
                            if (evt != eventRenaming && evt.EventName == eventRenaming.ProspectiveName)
                            {
                                isValidName = false;
                                DTGUIHelper.ShowAlert("You already have a Custom Event named '" + eventRenaming.ProspectiveName + "'. Custom Event names must be unique.");
                            }
                        }

                        if (isValidName)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator,
                                                                        "Undo change Custom Event name.");

                            eventRenaming.EventName = eventRenaming.ProspectiveName;
                            eventRenaming.IsEditing = false;
                            _isDirty = true;
                        }
                    }
                }

                DTGUIHelper.EndGroupedControls();
            }

            // End Show Custom Events

            if (GUI.changed || _isDirty)
            {
                EditorUtility.SetDirty(target);
            }

            //DrawDefaultInspector();
        }

        private Transform CreateGroup(AudioClip aClip)
        {
            if (_creator.groupTemplate == null)
            {
                DTGUIHelper.ShowAlert("Your 'Group Template' field is empty, please assign it in debug mode. Drag the 'DynamicSoundGroup' prefab from MasterAudio/Sources/Prefabs into that field, then switch back to normal mode.");
                return null;
            }

            var groupName = UtilStrings.TrimSpace(aClip.CachedName());

            var matchingGroup = _groups.Find(delegate (DynamicSoundGroup obj)
            {
                return obj.transform.name == groupName;
            });

            if (matchingGroup != null)
            {
                DTGUIHelper.ShowAlert("You already have a Group named '" + groupName + "'. \n\nPlease rename this Group when finished to be unique.");
            }

            var spawnedGroup = (GameObject)Instantiate(_creator.groupTemplate, _creator.transform.position, Quaternion.identity);
            spawnedGroup.name = groupName;

            AudioUndoHelper.CreateObjectForUndo(spawnedGroup, "create Dynamic Group");
            spawnedGroup.transform.parent = _creator.transform;

            CreateVariation(spawnedGroup.transform, aClip);

            return spawnedGroup.transform;
        }

        private void CreateVariation(Transform aGroup, AudioClip aClip)
        {
            if (_creator.variationTemplate == null)
            {
                DTGUIHelper.ShowAlert("Your 'Variation Template' field is empty, please assign it in debug mode. Drag the 'DynamicGroupVariation' prefab from MasterAudio/Sources/Prefabs into that field, then switch back to normal mode.");
                return;
            }

            var clipName = UtilStrings.TrimSpace(aClip.CachedName());

            var myGroup = aGroup.GetComponent<DynamicSoundGroup>();

            var matches = myGroup.groupVariations.FindAll(delegate (DynamicGroupVariation obj)
            {
                return obj.name == clipName;
            });

            if (matches.Count > 0)
            {
                DTGUIHelper.ShowAlert("You already have a variation for this Group named '" + clipName + "'. \n\nPlease rename these variations when finished to be unique, or you may not be able to play them by name if you have a need to.");
            }

            var spawnedVar = (GameObject)Instantiate(_creator.variationTemplate, _creator.transform.position, Quaternion.identity);
            spawnedVar.name = clipName;

            spawnedVar.transform.parent = aGroup;

            var dynamicVar = spawnedVar.GetComponent<DynamicGroupVariation>();
            dynamicVar.audLocation = _creator.bulkVariationMode;

            switch (_creator.bulkVariationMode)
            {
                case MasterAudio.AudioLocation.Clip:
                    dynamicVar.VarAudio.clip = aClip;
                    break;
                case MasterAudio.AudioLocation.ResourceFile:
                    var useLocalization = false;
                    var resourceFileName = DTGUIHelper.GetResourcePath(aClip, ref useLocalization);
                    if (string.IsNullOrEmpty(resourceFileName))
                    {
                        resourceFileName = aClip.CachedName();
                    }

                    dynamicVar.resourceFileName = resourceFileName;
                    dynamicVar.useLocalization = useLocalization;
                    break;
#if ADDRESSABLES_ENABLED
            case MasterAudio.AudioLocation.Addressable:
                dynamicVar.audioClipAddressable = AddressableEditorHelper.CreateAssetReferenceFromObject(aClip);
                break;
#endif
            }

            CopyFromAudioSourceTemplate(_creator, dynamicVar.VarAudio, false);
        }

        private void ExpandCollapseCategory(string category, bool isExpand)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle expand / collapse all items in Category");

            foreach (var item in _creator.customEventsToCreate)
            {
                if (item.categoryName != category)
                {
                    continue;
                }

                item.eventExpanded = isExpand;
            }
        }

        private void CreateCustomEvent(string newEventName, string defaultCategory)
        {
            if (_creator.customEventsToCreate.FindAll(delegate (CustomEvent obj)
            {
                return obj.EventName == newEventName;
            }).Count > 0)
            {
                DTGUIHelper.ShowAlert("You already have a Custom Event named '" + newEventName + "'. Please choose a different name.");
                return;
            }

            var newEvent = new CustomEvent(newEventName);
            newEvent.categoryName = defaultCategory;

            _creator.customEventsToCreate.Add(newEvent);
        }

        private void CreateCategory()
        {
            if (string.IsNullOrEmpty(_creator.newCustomEventCategoryName))
            {
                DTGUIHelper.ShowAlert("You cannot have a blank Category name.");
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var c = 0; c < _creator.customEventCategories.Count; c++)
            {
                var cat = _creator.customEventCategories[c];
                // ReSharper disable once InvertIf
                if (cat.CatName == _creator.newCustomEventCategoryName)
                {
                    DTGUIHelper.ShowAlert("You already have a Category named '" + _creator.newCustomEventCategoryName + "'. Category names must be unique.");
                    return;
                }
            }

            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Create New Category");

            var newCat = new CustomEventCategory
            {
                CatName = _creator.newCustomEventCategoryName,
                ProspectiveName = _creator.newCustomEventCategoryName
            };

            _creator.customEventCategories.Add(newCat);
        }

        private void RenameEvent(CustomEvent cEvent)
        {
            var match = _creator.customEventsToCreate.FindAll(delegate (CustomEvent obj)
            {
                return obj.EventName == cEvent.ProspectiveName;
            });

            if (match.Count > 0)
            {
                DTGUIHelper.ShowAlert("You already have a custom event named '" + cEvent.ProspectiveName + "' configured here. Please choose a different name.");
                return;
            }

            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Rename Custom Event");
            cEvent.EventName = cEvent.ProspectiveName;
        }

        private void PreviewGroup(DynamicSoundGroup aGroup)
        {
            var rndIndex = UnityEngine.Random.Range(0, aGroup.groupVariations.Count);
            var rndVar = aGroup.groupVariations[rndIndex];

            var previewer = MasterAudioInspector.GetPreviewer();

            var randPitch = SoundGroupVariationInspector.GetRandomPreviewPitch(rndVar);
            var varVol = SoundGroupVariationInspector.GetRandomPreviewVolume(rndVar);

            if (previewer != null)
            {
                MasterAudioInspector.StopPreviewer();
                previewer.pitch = randPitch;
            }

            var calcVolume = varVol * rndVar.ParentGroup.groupMasterVolume;

            switch (rndVar.audLocation)
            {
                case MasterAudio.AudioLocation.ResourceFile:
                    var fileName = AudioResourceOptimizer.GetLocalizedDynamicSoundGroupFileName(_creator.previewLanguage, rndVar.useLocalization, rndVar.resourceFileName);

                    var clip = Resources.Load(fileName) as AudioClip;
                    if (clip != null)
                    {
                        if (previewer != null)
                        {
                            DTGUIHelper.PlaySilentWakeUpPreview(previewer, clip);
                            previewer.PlayOneShot(clip, rndVar.VarAudio.volume * aGroup.groupMasterVolume);
                        }
                    }
                    else
                    {
                        DTGUIHelper.ShowAlert("Could not find Resource file: " + fileName);
                    }
                    break;
                case MasterAudio.AudioLocation.Clip:
                    if (previewer != null)
                    {
                        DTGUIHelper.PlaySilentWakeUpPreview(previewer, rndVar.VarAudio.clip);
                        previewer.PlayOneShot(rndVar.VarAudio.clip, calcVolume);
                    }
                    break;
#if ADDRESSABLES_ENABLED
            case MasterAudio.AudioLocation.Addressable:
                DTGUIHelper.PreviewAddressable(rndVar.audioClipAddressable, previewer, calcVolume);
                break;
#endif

            }
        }

        private List<string> GroupNameList {
            get {
                var groupNames = new List<string> { MasterAudio.NoGroupName };

                foreach (var t in _groups)
                {
                    groupNames.Add(t.name);
                }

                return groupNames;
            }
        }

        private void DeleteBus(int busIndex)
        {
            var groupsWithBus = new List<DynamicSoundGroup>();
            var groupsWithHigherBus = new List<DynamicSoundGroup>();

            foreach (var aGroup in _groups)
            {
                if (aGroup.busIndex == -1)
                {
                    continue;
                }
                if (aGroup.busIndex == busIndex + DynamicSoundGroupCreator.HardCodedBusOptions)
                {
                    groupsWithBus.Add(aGroup);
                }
                else if (aGroup.busIndex > busIndex + DynamicSoundGroupCreator.HardCodedBusOptions)
                {
                    groupsWithHigherBus.Add(aGroup);
                }
            }

            var allObjects = new List<UnityEngine.Object> { _creator };
            foreach (var g in groupsWithBus)
            {
                allObjects.Add(g);
            }

            foreach (var g in groupsWithHigherBus)
            {
                allObjects.Add(g);
            }

            AudioUndoHelper.RecordObjectsForUndo(allObjects.ToArray(), "delete Bus");

            // change all
            _creator.groupBuses.RemoveAt(busIndex);

            foreach (var group in groupsWithBus)
            {
                group.busIndex = -1;
            }

            foreach (var group in groupsWithHigherBus)
            {
                group.busIndex--;
            }
        }

        private void CreateBus(int groupIndex, bool isExisting)
        {
            var sourceGroup = _groups[groupIndex];

            var affectedObjects = new UnityEngine.Object[] {
            _creator,
            sourceGroup
        };

            AudioUndoHelper.RecordObjectsForUndo(affectedObjects, "create Bus");

            var newBusName = isExisting ? ExistingNameName : MasterAudioInspector.RenameMeBusName;

            var newBus = new GroupBus
            {
                busName = newBusName,
                isExisting = isExisting
            };


            _creator.groupBuses.Add(newBus);

            sourceGroup.busIndex = DynamicSoundGroupCreator.HardCodedBusOptions + _creator.groupBuses.Count - 1;
        }

        private void ExpandCollapseAllPlaylists(bool expand)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "toggle Expand / Collapse Playlists");

            foreach (var aList in _creator.musicPlaylists)
            {
                aList.isExpanded = expand;

                foreach (var aSong in aList.MusicSettings)
                {
                    aSong.isExpanded = expand;
                }
            }
        }

        private void EqualizePlaylistVolumes(List<MusicSetting> playlistClips)
        {
            var clips = new Dictionary<MusicSetting, float>();

            if (playlistClips.Count < 2)
            {
                DTGUIHelper.ShowAlert("You must have at least 2 clips in a Playlist to use this function.");
                return;
            }

            var lowestVolume = 1f;

            foreach (var setting in playlistClips)
            {
                var ac = setting.clip;

                switch (setting.audLocation)
                {
                    case MasterAudio.AudioLocation.Clip:
                        if (ac == null)
                        {
                            continue;
                        }
                        break;
                    case MasterAudio.AudioLocation.ResourceFile:
                        ac = Resources.Load(setting.resourceFileName) as AudioClip;
                        if (ac == null)
                        {
                            Debug.LogError("Song '" + setting.resourceFileName + "' could not be loaded and is being skipped.");
                            continue;
                        }
                        break;
#if ADDRESSABLES_ENABLED
                case MasterAudio.AudioLocation.Addressable:
                    ac = DTGUIHelper.EditModeLoadAddressable(setting.audioClipAddressable);
                    break;
#endif
                }

                var average = 0f;
                var buffer = new float[ac.samples];

                Debug.Log("Measuring amplitude of '" + ac.name + "'.");

                try
                {
                    ac.GetData(buffer, 0);
                }
                catch
                {
                    Debug.Log("Could not read data from compressed sample. Skipping '" + setting.clip.CachedName() + "'.");
                    continue;
                }

                for (var c = 0; c < ac.samples; c++)
                {
                    average += Mathf.Pow(buffer[c], 2);
                }

                average = Mathf.Sqrt(1f / ac.samples * average);

                if (average == 0)
                {
                    Debug.LogError("Song '" + setting.songName + "' is being excluded because it's compressed or streaming.");
                    continue;
                }

                if (average < lowestVolume)
                {
                    lowestVolume = average;
                }

                clips.Add(setting, average);
            }

            if (clips.Count < 2)
            {
                DTGUIHelper.ShowAlert("You must have at least 2 clips in a Playlist to use this function.");
                return;
            }

            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Equalize Song Volumes");

            foreach (var kv in clips)
            {
                var adjustedVol = lowestVolume / kv.Value;
                //set your volume for each song in your playlist.
                kv.Key.volume = adjustedVol;
            }
        }

        private void AddSongToPlaylist(MasterAudio.Playlist pList, AudioClip aClip)
        {
            MusicSetting mus;

            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "add Song");

            mus = new MusicSetting
            {
                volume = 1f,
                pitch = 1f,
                isExpanded = true,
                audLocation = pList.bulkLocationMode
            };

            switch (pList.bulkLocationMode)
            {
                case MasterAudio.AudioLocation.Clip:
                    mus.clip = aClip;
                    if (aClip != null)
                    {
                        mus.songName = aClip.CachedName();
                    }
                    break;
                case MasterAudio.AudioLocation.ResourceFile:
                    var unused = false;
                    var resourceFileName = DTGUIHelper.GetResourcePath(aClip, ref unused);
                    if (string.IsNullOrEmpty(resourceFileName))
                    {
                        resourceFileName = aClip.CachedName();
                    }

                    mus.resourceFileName = resourceFileName;
                    mus.songName = aClip.CachedName();
                    break;
#if ADDRESSABLES_ENABLED
            case MasterAudio.AudioLocation.Addressable:
                mus.audioClipAddressable = AddressableEditorHelper.CreateAssetReferenceFromObject(aClip);
                mus.songName = AddressableEditorHelper.EditTimeAddressableName(mus.audioClipAddressable);
                break;
#endif
            }

            foreach (var property in pList.songMetadataProps)
            {
                if (!property.AllSongsMustContain)
                {
                    continue;
                }

                switch (property.PropertyType)
                {
                    case SongMetadataProperty.MetadataPropertyType.Boolean:
                        var bVal = new SongMetadataBoolValue(property);
                        mus.metadataBoolValues.Add(bVal);
                        break;
                    case SongMetadataProperty.MetadataPropertyType.String:
                        var sVal = new SongMetadataStringValue(property);
                        mus.metadataStringValues.Add(sVal);
                        break;
                    case SongMetadataProperty.MetadataPropertyType.Integer:
                        var iVal = new SongMetadataIntValue(property);
                        mus.metadataIntValues.Add(iVal);
                        break;
                    case SongMetadataProperty.MetadataPropertyType.Float:
                        var fVal = new SongMetadataFloatValue(property);
                        mus.metadataFloatValues.Add(fVal);
                        break;
                }
            }

            pList.MusicSettings.Add(mus);
        }

        private void SortCustomEvents()
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Sort Custom Events Alpha");

            _creator.customEventsToCreate.Sort(delegate (CustomEvent x, CustomEvent y)
            {
                return x.EventName.CompareTo(y.EventName);
            });
        }

        private void ExpandCollapseCustomEvents(bool shouldExpand)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Expand / Collapse All Custom Events");

            foreach (var t in _creator.customEventsToCreate)
            {
                t.eventExpanded = shouldExpand;
            }
        }

        private void SortSongsAlpha(MasterAudio.Playlist aList)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Sort Playlist Songs Alpha");

            aList.MusicSettings.Sort(delegate (MusicSetting x, MusicSetting y)
            {
                return x.songName.CompareTo(y.songName);
            });
        }

        private void ExpandCollapseSongs(MasterAudio.Playlist aList, bool shouldExpand)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Expand / Collapse Playlist Songs");

            foreach (var t in aList.MusicSettings)
            {
                t.isExpanded = shouldExpand;
            }
        }

        private void AddAudioSourceTemplate(GameObject temp)
        {
            if (_audioSourceTemplateNames.Contains(temp.name))
            {
                Debug.LogError("There is already an Audio Source Template named '" + temp.name + "'. The names of Templates must be unique.");
                return;
            }

            if (temp.GetComponent<AudioSource>() == null)
            {
                Debug.LogError("This is not an Audio Source Template. It must have an Audio Source component in the top-level.");
                return;
            }

            var hasAudioClips = false;
            for (var i = 0; i < temp.transform.childCount; i++)
            {
                var aChild = temp.transform.GetChild(i);
                var aud = aChild.transform.GetComponent<AudioSource>();
                if (aud == null)
                {
                    continue;
                }

                if (aud.clip != null)
                {
                    hasAudioClips = true;
                }
            }

            if (hasAudioClips)
            {
                Debug.LogError("Audio Source Templates cannot include any Audio Clips. Please remove them and try again.");
                return;
            }

            var hasFilters = false;
            for (var i = 0; i < temp.transform.childCount; i++)
            {
                var aChild = temp.transform.GetChild(i);

                if (aChild.GetComponent<AudioDistortionFilter>() != null
                    || aChild.GetComponent<AudioHighPassFilter>() != null
                    || aChild.GetComponent<AudioLowPassFilter>() != null
                    || aChild.GetComponent<AudioReverbFilter>() != null
                    || aChild.GetComponent<AudioEchoFilter>() != null
                    || aChild.GetComponent<AudioChorusFilter>() != null)
                {

                    hasFilters = true;
                }
            }

            if (hasFilters)
            {
                Debug.LogError("Audio Source Templates cannot include any Filter FX in their Variations. Please remove them and try again.");
                return;
            }

            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Add Audio Source Template");
            _creator.audioSourceTemplates.Add(temp.gameObject);
            _creator.audioSourceTemplates.Sort(delegate (GameObject x, GameObject y)
            {
                return x.name.CompareTo(y.name);
            });

            Debug.Log("Added Audio Source Template '" + temp.name + "'");
        }

        // ReSharper disable once UnusedMember.Local
        private static GameObject SelectedAudioSourceTemplate(DynamicSoundGroupCreator creator)
        {
            if (creator.audioSourceTemplates.Count == 0)
            {
                return null;
            }

            var selTemplate = creator.audioSourceTemplates.Find(delegate (GameObject obj)
            {
                return obj.name == creator.audioSourceTemplateName;
            });

            return selTemplate;
        }

        private void CreateMetadataProperty(MasterAudio.Playlist list, string propName, SongMetadataProperty.MetadataPropertyType propType, bool required, bool canHaveMult)
        {
            if (propName != null)
            {
                propName = propName.Replace(" ", "");
            }
            if (string.IsNullOrEmpty(propName))
            {
                DTGUIHelper.ShowAlert("You must give a name to your new Meta Property.");
                return;
            }

            var match = list.songMetadataProps.Find(delegate (SongMetadataProperty p)
            {
                return p.PropertyName == propName;
            });

            if (match != null)
            {
                DTGUIHelper.ShowAlert("You already have a Metadata Property named '" + propName + "'. Names must be unique.");
                return;
            }

            if (propType == SongMetadataProperty.MetadataPropertyType.Boolean)
            {
                canHaveMult = false;
            }

            var newProp = new SongMetadataProperty(propName, propType, required, canHaveMult);

            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Create Metadata Property");

            if (required)
            {
                for (var s = 0; s < list.MusicSettings.Count; s++)
                {
                    var aSong = list.MusicSettings[s];

                    switch (propType)
                    {
                        case SongMetadataProperty.MetadataPropertyType.Boolean:
                            var bVal = new SongMetadataBoolValue(newProp);
                            aSong.metadataBoolValues.Add(bVal);
                            break;
                        case SongMetadataProperty.MetadataPropertyType.String:
                            var sVal = new SongMetadataStringValue(newProp);
                            aSong.metadataStringValues.Add(sVal);
                            break;
                        case SongMetadataProperty.MetadataPropertyType.Integer:
                            var iVal = new SongMetadataIntValue(newProp);
                            aSong.metadataIntValues.Add(iVal);
                            break;
                        case SongMetadataProperty.MetadataPropertyType.Float:
                            var fVal = new SongMetadataFloatValue(newProp);
                            aSong.metadataFloatValues.Add(fVal);
                            break;
                    }
                }
            }

            list.songMetadataProps.Add(newProp);
        }

        private void MakePropertyRequired(SongMetadataProperty property, MasterAudio.Playlist aList)
        {
            // add one if there isn't one.
            for (var i = 0; i < aList.MusicSettings.Count; i++)
            {
                var aSong = aList.MusicSettings[i];

                switch (property.PropertyType)
                {
                    case SongMetadataProperty.MetadataPropertyType.String:
                        if (aSong.metadataStringValues.FindIndex(delegate (SongMetadataStringValue v)
                        {
                            return v.PropertyName == property.PropertyName;
                        }) < 0)
                        {
                            aSong.metadataStringValues.Add(new SongMetadataStringValue(property));
                        }
                        break;
                    case SongMetadataProperty.MetadataPropertyType.Boolean:
                        if (aSong.metadataBoolValues.FindIndex(delegate (SongMetadataBoolValue b)
                        {
                            return b.PropertyName == property.PropertyName;
                        }) < 0)
                        {
                            aSong.metadataBoolValues.Add(new SongMetadataBoolValue(property));
                        }
                        break;
                    case SongMetadataProperty.MetadataPropertyType.Integer:
                        if (aSong.metadataIntValues.FindIndex(delegate (SongMetadataIntValue n)
                        {
                            return n.PropertyName == property.PropertyName;
                        }) < 0)
                        {
                            aSong.metadataIntValues.Add(new SongMetadataIntValue(property));
                        }
                        break;
                    case SongMetadataProperty.MetadataPropertyType.Float:
                        if (aSong.metadataFloatValues.FindIndex(delegate (SongMetadataFloatValue f)
                        {
                            return f.PropertyName == property.PropertyName;
                        }) < 0)
                        {
                            aSong.metadataFloatValues.Add(new SongMetadataFloatValue(property));
                        }
                        break;
                }
            }
        }

        private void DeleteMetadataProperty(MasterAudio.Playlist aList, int index)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _creator, "Delete Metadata Property");

            var deadProperty = aList.songMetadataProps[index];
            var deadName = deadProperty.PropertyName;

            switch (deadProperty.PropertyType)
            {
                case SongMetadataProperty.MetadataPropertyType.String:
                    for (var i = 0; i < aList.MusicSettings.Count; i++)
                    {
                        var aSong = aList.MusicSettings[i];

                        aSong.metadataStringValues.RemoveAll(delegate (SongMetadataStringValue val)
                        {
                            return val.PropertyName == deadName;
                        });
                    }
                    break;
                case SongMetadataProperty.MetadataPropertyType.Boolean:
                    for (var i = 0; i < aList.MusicSettings.Count; i++)
                    {
                        var aSong = aList.MusicSettings[i];
                        aSong.metadataBoolValues.RemoveAll(delegate (SongMetadataBoolValue val)
                        {
                            return val.PropertyName == deadName;
                        });
                    }
                    break;
                case SongMetadataProperty.MetadataPropertyType.Integer:
                    for (var i = 0; i < aList.MusicSettings.Count; i++)
                    {
                        var aSong = aList.MusicSettings[i];
                        aSong.metadataIntValues.RemoveAll(delegate (SongMetadataIntValue val)
                        {
                            return val.PropertyName == deadName;
                        });
                    }
                    break;
                case SongMetadataProperty.MetadataPropertyType.Float:
                    for (var i = 0; i < aList.MusicSettings.Count; i++)
                    {
                        var aSong = aList.MusicSettings[i];
                        aSong.metadataFloatValues.RemoveAll(delegate (SongMetadataFloatValue val)
                        {
                            return val.PropertyName == deadName;
                        });
                    }
                    break;
            }

            aList.songMetadataProps.Remove(deadProperty);
        }

        public static void CopyFromAudioSourceTemplate(DynamicSoundGroupCreator creator, AudioSource oldAudSrc, bool showError)
        {
            var selSource = SelectedAudioSourceTemplate(creator);
            if (selSource == null)
            {
                if (showError)
                {
                    Debug.LogError("No Audio Source Template selected.");
                }
                return;
            }

            var templateAudio = selSource.GetComponent<AudioSource>();

            var oldPitch = oldAudSrc.pitch;
            var oldLoop = oldAudSrc.loop;
            var oldClip = oldAudSrc.clip;
            var oldVol = oldAudSrc.volume;

            UnityEditorInternal.ComponentUtility.CopyComponent(templateAudio);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(oldAudSrc);

            oldAudSrc.pitch = oldPitch;
            oldAudSrc.loop = oldLoop;
            oldAudSrc.clip = oldClip;
            oldAudSrc.volume = oldVol;
        }
    }
}