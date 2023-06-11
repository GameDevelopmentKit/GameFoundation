using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(SoundGroupOrganizer))]
    // ReSharper disable once CheckNamespace
    public class SoundGroupOrganizerInspector : Editor
    {
        private SoundGroupOrganizer _organizer;
        private List<DynamicSoundGroup> _groups;
        private bool _isDirty;
        AudioSource previewer;

        // ReSharper disable once FunctionComplexityOverflow
        public override void OnInspectorGUI()
        {
            _isDirty = false;

            if (MasterAudioInspectorResources.LogoTexture != null)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            _organizer = (SoundGroupOrganizer)target;

            if (Application.isPlaying)
            {
                DTGUIHelper.ShowRedError("Sound Group Inspector cannot be used at runtime. Press stop to use it.");
                return;
            }

            DTGUIHelper.HelpHeader("https://www.dtdevtools.com/docs/masteraudio/SoundGroupOrganizer.htm");

            _groups = ScanForGroups();

            var isInProjectView = DTGUIHelper.IsPrefabInProjectView(_organizer.gameObject);

            if (isInProjectView)
            {
                DTGUIHelper.ShowLargeBarAlert("You are in Project View and cannot edit this Game Object from here.");
                return;
            }

            if (DTGUIHelper.IsLinkedToDarkTonicPrefabFolder(_organizer))
            {
                DTGUIHelper.MakePrefabMessage();
                return;
            }

            if (MasterAudio.Instance == null)
            {
                var newLang = (SystemLanguage)EditorGUILayout.EnumPopup(new GUIContent("Preview Language", "This setting is only used (and visible) to choose the previewing language when there's no Master Audio prefab in the Scene (language settings are grabbed from there normally). This should only happen when you're using a Master Audio prefab from a previous Scene in persistent mode."), _organizer.previewLanguage);
                if (newLang != _organizer.previewLanguage)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Preview Language");
                    _organizer.previewLanguage = newLang;
                }
            }

            var ma = MasterAudio.Instance;

            var sources = new List<GameObject>();
            if (ma != null)
            {
                sources.Add(ma.gameObject);
            }

            var dgscs = FindObjectsOfType(typeof(DynamicSoundGroupCreator));
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var t in dgscs)
            {
                var dsgc = (DynamicSoundGroupCreator)t;
                sources.Add(dsgc.gameObject);
            }

            var sourceNames = new List<string>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var t in sources)
            {
                sourceNames.Add(t.name);
            }

            var scannedDest = false;

            var newType = (SoundGroupOrganizer.MAItemType)EditorGUILayout.EnumPopup("Item Type", _organizer.itemType);
            if (newType != _organizer.itemType)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Item Type");
                _organizer.itemType = newType;
            }

            var newMode = (SoundGroupOrganizer.TransferMode)EditorGUILayout.EnumPopup("Transfer Mode", _organizer.transMode);
            if (newMode != _organizer.transMode)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Transfer Mode");
                _organizer.transMode = newMode;

                RescanDestinationGroups();
                scannedDest = true;
            }

            if (!scannedDest && _organizer.selectedDestSoundGroups.Count == 0)
            {
                RescanDestinationGroups();
                // ReSharper disable once RedundantAssignment
                scannedDest = true;
            }

            var shouldRescanGroups = false;
            var hasRescannedGroups = false;
            var shouldRescanEvents = false;
            var hasRescannedEvents = false;

            if (_organizer.itemType == SoundGroupOrganizer.MAItemType.SoundGroups)
            {
                switch (_organizer.transMode)
                {
                    case SoundGroupOrganizer.TransferMode.Import:
                        if (sources.Count == 0)
                        {
                            DTGUIHelper.ShowRedError("You have no Master Audio or Dynamic Sound Group Creator prefabs in this Scene. Can't import.");
                        }
                        else if (isInProjectView)
                        {
                            DTGUIHelper.ShowRedError("You are in Project View and can't import. Create this prefab with Master Audio Manager.");
                        }
                        else
                        {
                            var srcIndex = sources.IndexOf(_organizer.sourceObject);
                            if (srcIndex < 0)
                            {
                                srcIndex = 0;
                            }

                            DTGUIHelper.StartGroupHeader();
                            var newIndex = EditorGUILayout.Popup("Source Object", srcIndex, sourceNames.ToArray());
                            if (newIndex != srcIndex)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Source Object");
                            }
                            EditorGUILayout.EndVertical();

                            var newSource = sources[newIndex];
                            var hasSourceChanged = newSource != _organizer.sourceObject;
                            _organizer.sourceObject = newSource;

                            if (!hasRescannedGroups && (hasSourceChanged || _organizer.selectedSourceSoundGroups.Count == 0))
                            {
                                if (RescanSourceGroups())
                                {
                                    hasRescannedGroups = true;
                                }
                            }

                            if (!hasRescannedGroups && _organizer.selectedSourceSoundGroups.Count != _organizer.sourceObject.transform.childCount)
                            {
                                if (RescanSourceGroups())
                                {
                                    hasRescannedGroups = true;
                                }
                            }

                            if (_organizer.sourceObject != null)
                            {
                                if (_organizer.selectedSourceSoundGroups.Count > 0)
                                {
                                    DTGUIHelper.ShowLargeBarAlert("Check Groups to Import below and click 'Import'");
                                }
                                else
                                {
                                    DTGUIHelper.ShowRedError("Source Object has no Groups to import.");
                                }

                                EditorGUI.indentLevel = 0;

                                foreach (var aGroup in _organizer.selectedSourceSoundGroups)
                                {
                                    if (!hasRescannedGroups && aGroup.Go == null)
                                    {
                                        shouldRescanGroups = true;
                                        continue;
                                    }

                                    var newSel = EditorGUILayout.Toggle(aGroup.Go.name, aGroup.IsSelected);
                                    if (newSel == aGroup.IsSelected)
                                    {
                                        continue;
                                    }

                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle Sound Group selection");
                                    aGroup.IsSelected = newSel;
                                }
                            }

                            if (!hasRescannedGroups && shouldRescanGroups)
                            {
                                if (RescanSourceGroups())
                                {
                                    // ReSharper disable once RedundantAssignment
                                    hasRescannedGroups = true;
                                }
                            }

                            if (_organizer.selectedSourceSoundGroups.Count > 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(10);
                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                if (GUILayout.Button(new GUIContent("Import", "Import Selected Groups"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    ImportSelectedGroups();
                                }

                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Check All", "Check all Groups above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllSourceGroups(true);
                                }
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Uncheck All", "Uncheck all Groups above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllSourceGroups(false);
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUILayout.EndVertical();
                        }
                        break;
                    case SoundGroupOrganizer.TransferMode.Export:
                        if (_groups.Count == 0)
                        {
                            DTGUIHelper.ShowRedError("You have no Groups to export. Import or create some first.");
                        }
                        else if (sources.Count == 0)
                        {
                            DTGUIHelper.ShowRedError("You have no Master Audio or Dynamic Sound Group Creator prefabs in this Scene to export to.");
                        }
                        else
                        {
                            var destIndex = sources.IndexOf(_organizer.destObject);
                            if (destIndex < 0)
                            {
                                destIndex = 0;
                            }

                            DTGUIHelper.StartGroupHeader();

                            var newIndex = EditorGUILayout.Popup("Destination Object", destIndex, sourceNames.ToArray());
                            if (newIndex != destIndex)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Destination Object");
                            }
                            var newDest = sources[newIndex];
                            EditorGUILayout.EndVertical();

                            _organizer.destObject = newDest;
                            DTGUIHelper.ShowLargeBarAlert("Check Groups to export (same as Group Control below) and click 'Export'");

                            if (_organizer.destObject != null)
                            {
                                EditorGUI.indentLevel = 0;

                                foreach (var aGroup in _organizer.selectedDestSoundGroups)
                                {
                                    if (!hasRescannedGroups && aGroup.Go == null)
                                    {
                                        shouldRescanGroups = true;
                                        continue;
                                    }

                                    var newSel = EditorGUILayout.Toggle(aGroup.Go.name, aGroup.IsSelected);
                                    if (newSel == aGroup.IsSelected)
                                    {
                                        continue;
                                    }

                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle Sound Group selection");
                                    aGroup.IsSelected = newSel;
                                }
                            }

                            if (!hasRescannedGroups && shouldRescanGroups)
                            {
                                RescanDestinationGroups();
                                // ReSharper disable once RedundantAssignment
                                hasRescannedGroups = true;
                            }

                            if (_organizer.selectedDestSoundGroups.Count > 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(10);
                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                if (GUILayout.Button(new GUIContent("Export", "Export Selected Groups"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    ExportSelectedGroups();
                                }

                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Check All", "Check all Groups above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllDestGroups(true);
                                }
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Uncheck All", "Uncheck all Groups above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllDestGroups(false);
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUILayout.EndVertical();
                        }

                        break;
                }
            }
            else
            {
                // custom events
                switch (_organizer.transMode)
                {
                    case SoundGroupOrganizer.TransferMode.Import:
                        if (sources.Count == 0)
                        {
                            DTGUIHelper.ShowRedError("You have no Master Audio or Dynamic Sound Group Creator prefabs in this Scene. Can't import.");
                        }
                        else if (isInProjectView)
                        {
                            DTGUIHelper.ShowRedError("You are in Project View and can't import. Create this prefab with Master Audio Manager.");
                        }
                        else
                        {
                            var srcMa = _organizer.sourceObject.GetComponent<MasterAudio>();
                            var srcDgsc = _organizer.sourceObject.GetComponent<DynamicSoundGroupCreator>();

                            // ReSharper disable once InconsistentNaming
                            var isSourceMA = srcMa != null;
                            // ReSharper disable once InconsistentNaming
                            var isSourceDGSC = srcDgsc != null;

                            List<CustomEvent> sourceEvents = null;

                            if (isSourceMA)
                            {
                                sourceEvents = srcMa.customEvents;
                            }
                            else if (isSourceDGSC)
                            {
                                sourceEvents = srcDgsc.customEventsToCreate;
                            }

                            var srcIndex = sources.IndexOf(_organizer.sourceObject);
                            if (srcIndex < 0)
                            {
                                srcIndex = 0;
                            }

                            DTGUIHelper.StartGroupHeader();

                            var newIndex = EditorGUILayout.Popup("Source Object", srcIndex, sourceNames.ToArray());
                            if (newIndex != srcIndex)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Source Object");
                            }
                            EditorGUILayout.EndVertical();

                            var newSource = sources[newIndex];
                            if (!hasRescannedEvents && newSource != _organizer.sourceObject || _organizer.selectedSourceCustomEvents.Count == 0)
                            {
                                if (RescanSourceEvents(sourceEvents))
                                {
                                    hasRescannedEvents = true;
                                }
                            }
                            _organizer.sourceObject = newSource;

                            if (!hasRescannedEvents && _organizer.selectedSourceCustomEvents.Count != sourceEvents.Count)
                            {
                                if (RescanSourceEvents(sourceEvents))
                                {
                                    hasRescannedEvents = true;
                                }
                            }

                            if (_organizer.sourceObject != null)
                            {
                                if (_organizer.selectedSourceCustomEvents.Count > 0)
                                {
                                    DTGUIHelper.ShowLargeBarAlert("Check Custom Events to Import below and click 'Import'");
                                }
                                else
                                {
                                    DTGUIHelper.ShowRedError("Source Object has no Custom Events to import.");
                                }

                                EditorGUI.indentLevel = 0;

                                foreach (var aEvent in _organizer.selectedSourceCustomEvents)
                                {
                                    if (!hasRescannedEvents && aEvent.Event == null)
                                    {
                                        shouldRescanEvents = true;
                                        continue;
                                    }

                                    var newSel = EditorGUILayout.Toggle(aEvent.Event.EventName, aEvent.IsSelected);
                                    if (newSel == aEvent.IsSelected)
                                    {
                                        continue;
                                    }

                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle Custom Event selection");
                                    aEvent.IsSelected = newSel;
                                }
                            }

                            if (!hasRescannedEvents && shouldRescanEvents)
                            {
                                RescanDestinationEvents();
                                // ReSharper disable once RedundantAssignment
                                hasRescannedEvents = true;
                            }

                            if (_organizer.selectedSourceCustomEvents.Count > 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(10);
                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                if (GUILayout.Button(new GUIContent("Import", "Import Selected Events"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    ImportSelectedEvents();
                                    RescanDestinationEvents();
                                }

                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Check All", "Check all Events above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllSourceEvents(true);
                                }
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Uncheck All", "Uncheck all Events above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllSourceEvents(false);
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUILayout.EndVertical();
                        }
                        break;
                    case SoundGroupOrganizer.TransferMode.Export:
                        if (_organizer.customEvents.Count == 0)
                        {
                            DTGUIHelper.ShowRedError("You have no Custom Events to export. Import or create some first.");
                        }
                        else if (sources.Count == 0)
                        {
                            DTGUIHelper.ShowRedError("You have no Master Audio or Dynamic Sound Group Creator prefabs in this Scene to export to.");
                        }
                        else
                        {
                            var destIndex = sources.IndexOf(_organizer.destObject);
                            if (destIndex < 0)
                            {
                                destIndex = 0;
                            }

                            DTGUIHelper.StartGroupHeader();

                            var newIndex = EditorGUILayout.Popup("Destination Object", destIndex, sourceNames.ToArray());
                            if (newIndex != destIndex)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Destination Object");
                            }
                            EditorGUILayout.EndVertical();

                            var newDest = sources[newIndex];

                            _organizer.destObject = newDest;

                            if (_organizer.destObject != null)
                            {
                                if (_organizer.selectedDestCustomEvents.Count == 0)
                                {
                                    DTGUIHelper.ShowRedError("You have no Custom Events to export");
                                }
                                else
                                {
                                    DTGUIHelper.ShowLargeBarAlert("Check Custom Events to export (same as Custom Events below) and click 'Export'");
                                }

                                EditorGUI.indentLevel = 0;

                                if (_organizer.selectedDestCustomEvents.Count != _organizer.customEvents.Count)
                                {
                                    shouldRescanEvents = true;
                                }
                                if (!hasRescannedEvents && shouldRescanEvents)
                                {
                                    RescanDestinationEvents();
                                    // ReSharper disable once RedundantAssignment
                                    hasRescannedEvents = true;
                                }

                                foreach (var aEvent in _organizer.selectedDestCustomEvents)
                                {
                                    var newSel = EditorGUILayout.Toggle(aEvent.Event.EventName, aEvent.IsSelected);
                                    if (newSel == aEvent.IsSelected)
                                    {
                                        continue;
                                    }

                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle Custom Event selection");
                                    aEvent.IsSelected = newSel;
                                }
                            }

                            if (_organizer.selectedDestCustomEvents.Count > 0)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(10);
                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                if (GUILayout.Button(new GUIContent("Export", "Export Selected Custom Events"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    ExportSelectedEvents();
                                }

                                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Check All", "Check all Custom Events above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllDestEvents(true);
                                }
                                GUILayout.Space(10);
                                if (GUILayout.Button(new GUIContent("Uncheck All", "Uncheck all Custom Events above"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                                {
                                    CheckUncheckAllDestEvents(false);
                                }
                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUILayout.EndVertical();
                        }

                        break;
                }
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            GUI.contentColor = Color.white;
            var sliderIndicatorChars = 6;
            var sliderWidth = 40;

            if (MasterAudio.UseDbScaleForVolume)
            {
                sliderIndicatorChars = 9;
                sliderWidth = 56;
            }

            EditorGUI.indentLevel = 0;

            if (_organizer.itemType == SoundGroupOrganizer.MAItemType.SoundGroups)
            {
                // ReSharper disable once ConvertToConstant.Local
                var text = "Group Control";

                var collapsed = true;

                DTGUIHelper.ShowCollapsibleSection(ref collapsed, text, false);
                EditorGUILayout.EndHorizontal();

                DTGUIHelper.BeginGroupedControls();
                var newDragMode = (MasterAudio.DragGroupMode)EditorGUILayout.EnumPopup("Bulk Creation Mode", _organizer.curDragGroupMode);
                if (newDragMode != _organizer.curDragGroupMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Bulk Creation Mode");
                    _organizer.curDragGroupMode = newDragMode;
                }

                var bulkMode = (MasterAudio.AudioLocation)EditorGUILayout.EnumPopup("Variation Create Mode", _organizer.bulkVariationMode);
                if (bulkMode != _organizer.bulkVariationMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Variation Mode");
                    _organizer.bulkVariationMode = bulkMode;
                }

                if (_groups.Count > 0)
                {
                    var newUseTextGroupFilter = EditorGUILayout.Toggle("Use Text Group Filter", _organizer.useTextGroupFilter);
                    if (newUseTextGroupFilter != _organizer.useTextGroupFilter)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle Use Text Group Filter");
                        _organizer.useTextGroupFilter = newUseTextGroupFilter;
                    }

                    if (_organizer.useTextGroupFilter)
                    {
                        EditorGUI.indentLevel = 1;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Label("Text Group Filter", GUILayout.Width(140));
                        var newTextFilter = GUILayout.TextField(_organizer.textGroupFilter, GUILayout.Width(180));
                        if (newTextFilter != _organizer.textGroupFilter)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Text Group Filter");
                            _organizer.textGroupFilter = newTextFilter;
                        }
                        GUILayout.Space(10);
                        GUI.contentColor = DTGUIHelper.BrightButtonColor;
                        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(70)))
                        {
                            _organizer.textGroupFilter = string.Empty;
                        }
                        GUI.contentColor = Color.white;
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Separator();
                    }
                }

                EditorGUI.indentLevel = 0;

                // create groups start
                EditorGUILayout.BeginVertical();
                var aEvent = Event.current;

                var groupAdded = false;

                if (isInProjectView)
                {
                    DTGUIHelper.ShowLargeBarAlert("You are in Project View and cannot create or delete Groups.");
                }
                else if (DTGUIHelper.IsInPrefabMode(_organizer.gameObject))
                {
                    DTGUIHelper.ShowLargeBarAlert("You are in Prefab Mode and cannot create Groups.");
                }
                else if (Application.isPlaying)
                {
                    DTGUIHelper.ShowLargeBarAlert("You are running and cannot create Groups.");
                }
                else
                {
                    //DTGUIHelper.ShowRedError("Make sure this prefab is not in a gameplay Scene. Use a special Sandbox Scene.");
                    GUI.color = DTGUIHelper.DragAreaColor;

                    var dragAreaGroup = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
                    GUI.Box(dragAreaGroup, MasterAudio.DragAudioTip + " to create groups!");

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
                                    if (_organizer.curDragGroupMode == MasterAudio.DragGroupMode.OneGroupPerClip)
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
                                    groupAdded = true;

                                    _isDirty = true;
                                }
                            }
                            Event.current.Use();
                            break;
                    }
                }

                EditorGUILayout.EndVertical();
                // create groups end

                if (groupAdded)
                {
                    RescanDestinationGroups();
                }

                var filteredGroups = new List<DynamicSoundGroup>();
                filteredGroups.AddRange(_groups);

                if (_organizer.useTextGroupFilter)
                {
                    if (!string.IsNullOrEmpty(_organizer.textGroupFilter))
                    {
                        filteredGroups.RemoveAll(delegate (DynamicSoundGroup obj)
                        {
                            return !obj.transform.name.ToLower().Contains(_organizer.textGroupFilter.ToLower());
                        });
                    }
                }

                GUI.color = Color.white;

                if (_groups.Count == 0)
                {
                    DTGUIHelper.ShowLargeBarAlert("You currently have no Sound Groups created.");
                }
                else
                {
                    var groupsFiltered = _groups.Count - filteredGroups.Count;
                    if (groupsFiltered > 0)
                    {
                        DTGUIHelper.ShowLargeBarAlert(string.Format("{0}/{1} Group(s) filtered out.", groupsFiltered, _groups.Count));
                    }
                }

                int? indexToDelete = null;

                GUI.color = Color.white;

                filteredGroups.Sort(delegate (DynamicSoundGroup x, DynamicSoundGroup y)
                {
                    return x.name.CompareTo(y.name);
                });

                DTGUIHelper.ResetColors();

                for (var i = 0; i < filteredGroups.Count; i++)
                {
                    var aGroup = filteredGroups[i];

                    var groupDirty = false;

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    GUILayout.Label(aGroup.name, GUILayout.Width(150));

                    GUILayout.FlexibleSpace();

                    GUI.contentColor = Color.white;
                    GUI.color = DTGUIHelper.BrightButtonColor;

                    GUI.color = Color.white;

                    GUI.contentColor = DTGUIHelper.BrightTextColor;
                    GUILayout.TextField(DTGUIHelper.DisplayVolumeNumber(aGroup.groupMasterVolume, sliderIndicatorChars), sliderIndicatorChars, EditorStyles.miniLabel, GUILayout.Width(sliderWidth));

                    var newVol = DTGUIHelper.DisplayVolumeField(aGroup.groupMasterVolume, DTGUIHelper.VolumeFieldType.DynamicMixerGroup, MasterAudio.MixerWidthMode.Normal);
                    if (newVol != aGroup.groupMasterVolume)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref groupDirty, aGroup, "change Group Volume");
                        aGroup.groupMasterVolume = newVol;
                    }

                    GUI.contentColor = Color.white;

                    var buttonPressed = DTGUIHelper.AddDynamicGroupButtons(_organizer.gameObject);
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

                if (indexToDelete.HasValue)
                {
                    var groupToDelete = filteredGroups[indexToDelete.Value];
                    var wasDestroyed = false;

                    if (PrefabUtility.IsPartOfPrefabInstance(_organizer)) {
                        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_organizer);
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
                        // delete variation from Hierarchy
                        AudioUndoHelper.DestroyForUndo(groupToDelete.gameObject);
                    }
                }

                if (filteredGroups.Count > 0)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(6);

                    GUI.contentColor = DTGUIHelper.BrightButtonColor;
                    if (GUILayout.Button(new GUIContent("Max Group Volumes", "Reset all group volumes to full"), EditorStyles.toolbarButton, GUILayout.Width(120)))
                    {
                        AudioUndoHelper.RecordObjectsForUndo(filteredGroups.ToArray(), "Max Group Volumes");

                        foreach (var aGroup in filteredGroups)
                        {
                            aGroup.groupMasterVolume = 1f;
                        }
                    }
                    GUI.contentColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }

                DTGUIHelper.EndGroupedControls();
            }
            else
            {
                // custom events
                EditorGUI.indentLevel = 0;

                // ReSharper disable once ConvertToConstant.Local
                var text = "Custom Event Control";
                var collapsed = true;

                DTGUIHelper.ShowCollapsibleSection(ref collapsed, text, false);
                EditorGUILayout.EndHorizontal();

                var catNames = new List<string>(_organizer.customEventCategories.Count);
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _organizer.customEventCategories.Count; i++)
                {
                    catNames.Add(_organizer.customEventCategories[i].CatName);
                }

                var selCatIndex = catNames.IndexOf(_organizer.addToCustomEventCategoryName);

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
                var newEvent = EditorGUILayout.TextField("New Event Name", _organizer.newEventName);
                if (newEvent != _organizer.newEventName)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change New Event Name");
                    _organizer.newEventName = newEvent;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button("Create New Event", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    CreateCustomEvent(_organizer.newEventName, defaultCat);
                }
                GUILayout.Space(10);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;

                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                DTGUIHelper.StartGroupHeader(0, false);
                DTGUIHelper.ResetColors();
                var newCat = EditorGUILayout.TextField("New Category Name", _organizer.newCustomEventCategoryName);
                if (newCat != _organizer.newCustomEventCategoryName)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change New Category Name");
                    _organizer.newCustomEventCategoryName = newCat;
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

                EditorGUILayout.EndVertical();
                DTGUIHelper.ResetColors();

                GUI.backgroundColor = DTGUIHelper.BrightButtonColor;

                var newIndex = EditorGUILayout.Popup("Default Event Category", selCatIndex, catNames.ToArray());
                if (newIndex != selCatIndex)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Default Event Category");
                    _organizer.addToCustomEventCategoryName = catNames[newIndex];
                }

                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);

                var hasExpanded = false;
                foreach (var t in _organizer.customEvents)
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

                DTGUIHelper.StartGroupHeader(1);

                for (var c = 0; c < _organizer.customEventCategories.Count; c++)
                {
                    var cat = _organizer.customEventCategories[c];

                    EditorGUI.indentLevel = 0;

                    var matchingItems = new List<CustomEvent>();
                    matchingItems.AddRange(_organizer.customEvents);
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle expand Custom Event Category");
                            cat.IsExpanded = state2;
                        }

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

                        var headerStyle = new GUIStyle();
                        headerStyle.margin = new RectOffset(0, 0, 0, 0);
                        headerStyle.padding = new RectOffset(0, 0, 0, 0);
                        headerStyle.fixedHeight = 20;

                        EditorGUILayout.BeginHorizontal(headerStyle, GUILayout.MaxWidth(50));

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

                            if (c < _organizer.customEventCategories.Count - 1)
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
                        else
                        {
                            GUILayout.Space(4);
                        }

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
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle expand Custom Event");
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
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Custom Event Category");
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
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Send To Receivers");
                                anEvent.eventReceiveMode = rcvMode;
                            }

                            if (rcvMode == MasterAudio.CustomEventReceiveMode.WhenDistanceLessThan ||
                                rcvMode == MasterAudio.CustomEventReceiveMode.WhenDistanceMoreThan)
                            {
                                var newDist = EditorGUILayout.Slider("Distance Threshold", anEvent.distanceThreshold, 0f,
                                                                     float.MaxValue);
                                if (newDist != anEvent.distanceThreshold)
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer,
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
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "change Valid Receivers");
                                    anEvent.eventRcvFilterMode = rcvFilter;
                                }

                                switch (anEvent.eventRcvFilterMode)
                                {
                                    case MasterAudio.EventReceiveFilter.Closest:
                                    case MasterAudio.EventReceiveFilter.Random:
                                        var newQty = EditorGUILayout.IntField("Valid Qty", anEvent.filterModeQty);
                                        if (newQty != anEvent.filterModeQty)
                                        {
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer,
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

                    if (c < _organizer.customEventCategories.Count - 1)
                    {
                        DTGUIHelper.VerticalSpace(3);
                    }
                }
                DTGUIHelper.EndGroupHeader();

                if (eventToDelete != null)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "Delete Custom Event");
                    _organizer.customEvents.Remove(eventToDelete);
                }

                if (indexToShiftUp.HasValue)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "shift up Category");
                    var item = _organizer.customEventCategories[indexToShiftUp.Value];
                    _organizer.customEventCategories.Insert(indexToShiftUp.Value - 1, item);
                    _organizer.customEventCategories.RemoveAt(indexToShiftUp.Value + 1);
                    _isDirty = true;
                }

                if (indexToShiftDown.HasValue)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "shift down Category");
                    var index = indexToShiftDown.Value + 1;
                    var item = _organizer.customEventCategories[index];
                    _organizer.customEventCategories.Insert(index - 1, item);
                    _organizer.customEventCategories.RemoveAt(index + 1);
                    _isDirty = true;
                }

                if (catToDelete != null)
                {
                    if (_organizer.customEvents.FindAll(delegate (CustomEvent x)
                    {
                        return x.categoryName == catToDelete.CatName;
                    }).Count > 0)
                    {
                        DTGUIHelper.ShowAlert("You cannot delete a Category with Custom Events in it. Move or delete the items first.");
                    }
                    else if (_organizer.customEventCategories.Count <= 1)
                    {
                        DTGUIHelper.ShowAlert("You cannot delete the last Category.");
                    }
                    else
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "Delete Category");
                        _organizer.customEventCategories.Remove(catToDelete);
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
                        for (var c = 0; c < _organizer.customEventCategories.Count; c++)
                        {
                            var cat = _organizer.customEventCategories[c];
                            // ReSharper disable once InvertIf
                            if (cat != catRenaming && cat.CatName == catRenaming.ProspectiveName)
                            {
                                isValidName = false;
                                DTGUIHelper.ShowAlert("You already have a Category named '" + catRenaming.ProspectiveName + "'. Category names must be unique.");
                            }
                        }

                        if (isValidName)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "Undo change Category name.");

                            // ReSharper disable once ForCanBeConvertedToForeach
                            for (var i = 0; i < _organizer.customEvents.Count; i++)
                            {
                                var item = _organizer.customEvents[i];
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
                    for (var c = 0; c < _organizer.customEventCategories.Count; c++)
                    {
                        var cat = _organizer.customEventCategories[c];
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
                    for (var c = 0; c < _organizer.customEvents.Count; c++)
                    {
                        var evt = _organizer.customEvents[c];
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
                        for (var c = 0; c < _organizer.customEvents.Count; c++)
                        {
                            var evt = _organizer.customEvents[c];
                            // ReSharper disable once InvertIf
                            if (evt != eventRenaming && evt.EventName == eventRenaming.ProspectiveName)
                            {
                                isValidName = false;
                                DTGUIHelper.ShowAlert("You already have a Custom Event named '" + eventRenaming.ProspectiveName + "'. Custom Event names must be unique.");
                            }
                        }

                        if (isValidName)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer,
                                                                        "Undo change Custom Event name.");

                            eventRenaming.EventName = eventRenaming.ProspectiveName;
                            eventRenaming.IsEditing = false;
                            _isDirty = true;
                        }
                    }
                }

                DTGUIHelper.EndGroupedControls();
            }

            if (GUI.changed || _isDirty)
            {
                EditorUtility.SetDirty(target);
            }

            //DrawDefaultInspector();
        }

        private void RescanDestinationGroups()
        {
            _organizer.selectedDestSoundGroups.Clear();

            for (var i = 0; i < _organizer.transform.childCount; i++)
            {
                var aGroup = _organizer.transform.GetChild(i);
                _organizer.selectedDestSoundGroups.Add(
                    new SoundGroupOrganizer.SoundGroupSelection(aGroup.gameObject, false));
            }
        }

        private void RescanDestinationEvents()
        {
            _organizer.selectedDestCustomEvents.Clear();

            foreach (var aEvent in _organizer.customEvents)
            {
                _organizer.selectedDestCustomEvents.Add(
                    new SoundGroupOrganizer.CustomEventSelection(aEvent, false));
            }
        }

        private bool RescanSourceGroups()
        {
            if (_organizer.sourceObject == null)
            {
                return false;
            }

            _organizer.selectedSourceSoundGroups.Clear();
            for (var i = 0; i < _organizer.sourceObject.transform.childCount; i++)
            {
                var aGroup = _organizer.sourceObject.transform.GetChild(i);
                _organizer.selectedSourceSoundGroups.Add(
                    new SoundGroupOrganizer.SoundGroupSelection(aGroup.gameObject, false));
            }

            _isDirty = true;
            return true;
        }

        private bool RescanSourceEvents(List<CustomEvent> sourceEvents)
        {
            if (_organizer.sourceObject == null)
            {
                return false;
            }

            _organizer.selectedSourceCustomEvents.Clear();

            foreach (var anEvent in sourceEvents)
            {
                _organizer.selectedSourceCustomEvents.Add(
                    new SoundGroupOrganizer.CustomEventSelection(anEvent, false));
            }

            _isDirty = true;
            return true;
        }

        private void CheckUncheckAllDestGroups(bool shouldCheck)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "check/uncheck All destination Groups");

            foreach (var t in _organizer.selectedDestSoundGroups)
            {
                t.IsSelected = shouldCheck;
            }
        }

        private void CheckUncheckAllDestEvents(bool shouldCheck)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "check/uncheck All destination Custom Events");

            foreach (var t in _organizer.selectedDestCustomEvents)
            {
                t.IsSelected = shouldCheck;
            }
        }

        private void CheckUncheckAllSourceGroups(bool shouldCheck)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "check/uncheck All source Groups");

            foreach (var t in _organizer.selectedSourceSoundGroups)
            {
                t.IsSelected = shouldCheck;
            }
        }

        private void CheckUncheckAllSourceEvents(bool shouldCheck)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "check/uncheck All source Custom Events");

            foreach (var t in _organizer.selectedSourceCustomEvents)
            {
                t.IsSelected = shouldCheck;
            }
        }

        private Transform CreateGroup(AudioClip aClip)
        {
            if (_organizer.dynGroupTemplate == null)
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

            var spawnedGroup = (GameObject)Instantiate(_organizer.dynGroupTemplate, _organizer.transform.position, Quaternion.identity);
            spawnedGroup.name = groupName;

            AudioUndoHelper.CreateObjectForUndo(spawnedGroup, "create Dynamic Group");
            spawnedGroup.transform.parent = _organizer.transform;

            CreateVariation(spawnedGroup.transform, aClip);

            return spawnedGroup.transform;
        }

        private void CreateVariation(Transform aGroup, AudioClip aClip)
        {
            if (_organizer.dynVariationTemplate == null)
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

            var spawnedVar = (GameObject)Instantiate(_organizer.dynVariationTemplate, _organizer.transform.position, Quaternion.identity);
            spawnedVar.name = clipName;

            spawnedVar.transform.parent = aGroup;

            var dynamicVar = spawnedVar.GetComponent<DynamicGroupVariation>();
            dynamicVar.audLocation = _organizer.bulkVariationMode;

            switch (_organizer.bulkVariationMode)
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
        }

        private List<DynamicSoundGroup> ScanForGroups()
        {
            var groups = new List<DynamicSoundGroup>();

            for (var i = 0; i < _organizer.transform.childCount; i++)
            {
                var aChild = _organizer.transform.GetChild(i);

                var grp = aChild.GetComponent<DynamicSoundGroup>();
                if (grp == null)
                {
                    continue;
                }

                grp.groupVariations = VariationsForGroup(aChild.transform);

                groups.Add(grp);
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

        private void PreviewGroup(DynamicSoundGroup aGroup)
        {
            previewer = MasterAudioInspector.GetPreviewer();

            var rndIndex = Random.Range(0, aGroup.groupVariations.Count);
            var rndVar = aGroup.groupVariations[rndIndex];

            var randPitch = SoundGroupVariationInspector.GetRandomPreviewPitch(rndVar);
            var varVol = SoundGroupVariationInspector.GetRandomPreviewVolume(rndVar);

            if (previewer != null)
            {
                MasterAudioInspector.StopPreviewer();
                previewer.pitch = randPitch;
            }

            var calcVolume = varVol * aGroup.groupMasterVolume;

            switch (rndVar.audLocation)
            {
                case MasterAudio.AudioLocation.ResourceFile:
                    if (previewer != null)
                    {
                        var fileName = AudioResourceOptimizer.GetLocalizedDynamicSoundGroupFileName(_organizer.previewLanguage, rndVar.useLocalization, rndVar.resourceFileName);

                        var clip = Resources.Load(fileName) as AudioClip;
                        if (clip != null)
                        {
                            DTGUIHelper.PlaySilentWakeUpPreview(previewer, clip);
                            previewer.PlayOneShot(clip, rndVar.VarAudio.volume);
                        }
                        else
                        {
                            DTGUIHelper.ShowAlert("Could not find Resource file: " + fileName);
                        }
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

        private void ImportSelectedGroups()
        {
            if (_organizer.sourceObject == null)
            {
                return;
            }

            var imported = 0;
            var skipped = 0;

            foreach (var item in _organizer.selectedSourceSoundGroups)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                var grp = item.Go;
                var dynGrp = grp.GetComponent<DynamicSoundGroup>();
                var maGrp = grp.GetComponent<MasterAudioGroup>();

                var wasSkipped = false;

                foreach (var t in _groups)
                {
                    if (t.name != grp.name)
                    {
                        continue;
                    }

                    Debug.LogError("Group '" + grp.name + "' skipped because there's already a Group with that name in your Organizer. If you wish to import the Group, please delete the one in the Organizer first.");
                    skipped++;
                    wasSkipped = true;
                    break;
                }

                if (wasSkipped)
                {
                    continue;
                }

                if (dynGrp != null)
                {
                    ImportDynamicGroup(dynGrp);
                    imported++;
                }
                else if (maGrp != null)
                {
                    ImportMAGroup(maGrp);
                    imported++;
                }
                else
                {
                    Debug.LogError("Invalid Group '" + grp.name + "'. It's set up wrong. Contact DarkTonic for assistance.");
                }
            }

            var summaryText = imported + " Group(s) imported.";
            if (skipped == 0)
            {
                Debug.Log(summaryText);
            }
        }

        private void ImportSelectedEvents()
        {
            if (_organizer.sourceObject == null)
            {
                return;
            }

            var imported = 0;
            var skipped = 0;

            foreach (var item in _organizer.selectedSourceCustomEvents)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                var evt = item.Event;

                var wasSkipped = false;

                foreach (var t in _organizer.customEvents)
                {
                    if (t.EventName != evt.EventName)
                    {
                        continue;
                    }

                    Debug.LogError("Custom Event '" + evt.EventName + "' skipped because there's already a Custom Event with that name in your Organizer. If you wish to import the Custom Event, please delete the one in the Organizer first.");
                    skipped++;
                    wasSkipped = true;
                    break;
                }

                if (wasSkipped)
                {
                    continue;
                }

                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "import Organizer Custom Event(s)");

                var catName = evt.categoryName;
                if (_organizer.customEventCategories.FindAll(delegate (CustomEventCategory cat)
                {
                    return cat.CatName == catName;
                }).Count == 0)
                {
                    _organizer.customEventCategories.Add(new CustomEventCategory
                    {
                        CatName = catName,
                        ProspectiveName = catName
                    });
                }

                _organizer.customEvents.Add(new CustomEvent(item.Event.EventName)
                {
                    distanceThreshold = item.Event.distanceThreshold,
                    eventExpanded = item.Event.eventExpanded,
                    eventReceiveMode = item.Event.eventReceiveMode,
                    ProspectiveName = item.Event.ProspectiveName,
                    filterModeQty = item.Event.filterModeQty,
                    eventRcvFilterMode = item.Event.eventRcvFilterMode,
                    categoryName = item.Event.categoryName
                });
                imported++;
            }

            var summaryText = imported + " Custom Event(s) imported.";
            if (skipped == 0)
            {
                Debug.Log(summaryText);
            }
        }

        private GameObject CreateBlankGroup(string grpName)
        {
            var spawnedGroup = (GameObject)Instantiate(_organizer.dynGroupTemplate, _organizer.transform.position, Quaternion.identity);
            spawnedGroup.name = grpName;

            AudioUndoHelper.CreateObjectForUndo(spawnedGroup, "import Organizer Group(s)");
            spawnedGroup.transform.parent = _organizer.transform;
            return spawnedGroup;
        }

        private void ImportDynamicGroup(DynamicSoundGroup aGroup)
        {
            var newGroup = CreateBlankGroup(aGroup.name);

            var groupTrans = newGroup.transform;

            for (var i = 0; i < aGroup.transform.childCount; i++)
            {
                var aVariation = aGroup.transform.GetChild(i).GetComponent<SoundGroupVariation>();

                var newVariation = (GameObject)Instantiate(_organizer.dynVariationTemplate.gameObject, groupTrans.position, Quaternion.identity);
                newVariation.transform.parent = groupTrans;

                var variation = newVariation.GetComponent<DynamicGroupVariation>();

                var clipName = aVariation.name;

                var aVarAudio = aVariation.GetComponent<AudioSource>();

                UnityEditorInternal.ComponentUtility.CopyComponent(aVarAudio);
                // ReSharper disable once ArrangeStaticMemberQualifier
                GameObject.DestroyImmediate(variation.VarAudio);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(variation.gameObject);
                UnityEditorInternal.ComponentUtility.MoveComponentUp(variation.VarAudio);

                switch (aVariation.audLocation)
                {
                    case MasterAudio.AudioLocation.Clip:
                        var clip = aVarAudio.clip;
                        if (clip == null)
                        {
                            continue;
                        }
                        variation.VarAudio.clip = clip;
                        break;
                    case MasterAudio.AudioLocation.ResourceFile:
                        variation.resourceFileName = aVariation.resourceFileName;
                        variation.useLocalization = aVariation.useLocalization;
                        break;
#if ADDRESSABLES_ENABLED
                case MasterAudio.AudioLocation.Addressable:
                    variation.audioClipAddressable = aVariation.audioClipAddressable;
                    break;
#endif
                }

                ResonanceAudioHelper.CopyResonanceAudioSource(aVariation, variation);
                OculusAudioHelper.CopyOculusAudioSource(aVariation, variation);

                variation.audLocation = aVariation.audLocation;
                variation.VarAudio.dopplerLevel = aVarAudio.dopplerLevel;
                variation.VarAudio.maxDistance = aVarAudio.maxDistance;
                variation.VarAudio.minDistance = aVarAudio.minDistance;
                variation.VarAudio.bypassEffects = aVarAudio.bypassEffects;
                variation.VarAudio.ignoreListenerVolume = aVarAudio.ignoreListenerVolume;
                variation.VarAudio.mute = aVarAudio.mute;

                variation.VarAudio.panStereo = aVarAudio.panStereo;

                variation.VarAudio.rolloffMode = aVarAudio.rolloffMode;
                variation.VarAudio.spread = aVarAudio.spread;

                variation.VarAudio.loop = aVarAudio.loop;
                variation.VarAudio.pitch = aVarAudio.pitch;
                variation.transform.name = clipName;
                variation.isExpanded = aVariation.isExpanded;

                variation.probabilityToPlay = aVariation.probabilityToPlay;
                variation.weight = aVariation.weight;

                variation.isUninterruptible = aVariation.isUninterruptible;
                variation.importance = aVariation.importance;

                variation.clipAlias = aVariation.clipAlias;
                variation.useRandomPitch = aVariation.useRandomPitch;
                variation.randomPitchMode = aVariation.randomPitchMode;
                variation.randomPitchMin = aVariation.randomPitchMin;
                variation.randomPitchMax = aVariation.randomPitchMax;

                variation.useRandomVolume = aVariation.useRandomVolume;
                variation.randomVolumeMode = aVariation.randomVolumeMode;
                variation.randomVolumeMin = aVariation.randomVolumeMin;
                variation.randomVolumeMax = aVariation.randomVolumeMax;

                variation.useFades = aVariation.useFades;
                variation.fadeInTime = aVariation.fadeInTime;
                variation.fadeOutTime = aVariation.fadeOutTime;

                variation.useIntroSilence = aVariation.useIntroSilence;
                variation.introSilenceMin = aVariation.introSilenceMin;
                variation.introSilenceMax = aVariation.introSilenceMax;

                variation.useRandomStartTime = aVariation.useRandomStartTime;
                variation.randomStartMinPercent = aVariation.randomStartMinPercent;
                variation.randomStartMaxPercent = aVariation.randomStartMaxPercent;
                variation.randomEndPercent = aVariation.randomEndPercent;

                variation.useCustomLooping = aVariation.useCustomLooping;
                variation.minCustomLoops = aVariation.minCustomLoops;
                variation.maxCustomLoops = aVariation.maxCustomLoops;

                // remove unused filter FX
                if (variation.LowPassFilter != null && !variation.LowPassFilter.enabled)
                {
                    Destroy(variation.LowPassFilter);
                }
                if (variation.HighPassFilter != null && !variation.HighPassFilter.enabled)
                {
                    Destroy(variation.HighPassFilter);
                }
                if (variation.DistortionFilter != null && !variation.DistortionFilter.enabled)
                {
                    Destroy(variation.DistortionFilter);
                }
                if (variation.ChorusFilter != null && !variation.ChorusFilter.enabled)
                {
                    Destroy(variation.ChorusFilter);
                }
                if (variation.EchoFilter != null && !variation.EchoFilter.enabled)
                {
                    Destroy(variation.EchoFilter);
                }
                if (variation.ReverbFilter != null && !variation.ReverbFilter.enabled)
                {
                    Destroy(variation.ReverbFilter);
                }
            }
            // added to Hierarchy!

            // populate sounds for playing!
            var groupScript = newGroup.GetComponent<DynamicSoundGroup>();
            // populate other properties.
            groupScript.retriggerPercentage = aGroup.retriggerPercentage;
            groupScript.groupMasterVolume = aGroup.groupMasterVolume;
            groupScript.limitMode = aGroup.limitMode;
            groupScript.limitPerXFrames = aGroup.limitPerXFrames;
            groupScript.minimumTimeBetween = aGroup.minimumTimeBetween;
            groupScript.useClipAgePriority = aGroup.useClipAgePriority;
            groupScript.limitPolyphony = aGroup.limitPolyphony;
            groupScript.voiceLimitCount = aGroup.voiceLimitCount;
            groupScript.curVariationSequence = aGroup.curVariationSequence;
            groupScript.useInactivePeriodPoolRefill = aGroup.useInactivePeriodPoolRefill;
            groupScript.inactivePeriodSeconds = aGroup.inactivePeriodSeconds;
            groupScript.curVariationMode = aGroup.curVariationMode;
            groupScript.useDialogFadeOut = aGroup.useDialogFadeOut;
            groupScript.dialogFadeOutTime = aGroup.dialogFadeOutTime;

            groupScript.isUninterruptible = aGroup.isUninterruptible;
            groupScript.importance = aGroup.importance;

            groupScript.chainLoopDelayMin = aGroup.chainLoopDelayMin;
            groupScript.chainLoopDelayMax = aGroup.chainLoopDelayMax;
            groupScript.chainLoopMode = aGroup.chainLoopMode;
            groupScript.chainLoopNumLoops = aGroup.chainLoopNumLoops;

            groupScript.expandLinkedGroups = aGroup.expandLinkedGroups;
            groupScript.childSoundGroups = aGroup.childSoundGroups;
            groupScript.endLinkedGroups = aGroup.endLinkedGroups;
            groupScript.linkedStartGroupSelectionType = aGroup.linkedStartGroupSelectionType;
            groupScript.linkedStopGroupSelectionType = aGroup.linkedStopGroupSelectionType;

            groupScript.spatialBlendType = aGroup.spatialBlendType;
            groupScript.spatialBlend = aGroup.spatialBlend;

            groupScript.groupPlayType = aGroup.groupPlayType;

            groupScript.targetDespawnedBehavior = aGroup.targetDespawnedBehavior;
            groupScript.despawnFadeTime = aGroup.despawnFadeTime;

            groupScript.isUsingOcclusion = aGroup.isUsingOcclusion;

            groupScript.logSound = aGroup.logSound;
            groupScript.comments = aGroup.comments;
            groupScript.ignoreListenerPause = aGroup.ignoreListenerPause;
            groupScript.alwaysHighestPriority = aGroup.alwaysHighestPriority;
#if ADDRESSABLES_ENABLED
        groupScript.addressableUnusedSecondsLifespan = aGroup.addressableUnusedSecondsLifespan;
#endif

            var dyn = aGroup.GetComponentInParent<DynamicSoundGroupCreator>();
            if (aGroup.busIndex > 0)
            {
                var srcBus = dyn.groupBuses[aGroup.busIndex - DynamicSoundGroupCreator.HardCodedBusOptions];
                if (srcBus.isExisting)
                {
                    groupScript.isExistingBus = true;
                }
                groupScript.busName = srcBus.busName;
            }

            groupScript.isCopiedFromDGSC = true;
        }

        // ReSharper disable once InconsistentNaming
        private void ImportMAGroup(MasterAudioGroup aGroup)
        {
            var newGroup = CreateBlankGroup(aGroup.GameObjectName);

            var groupTrans = newGroup.transform;

            for(var i = 0; i < aGroup.transform.childCount; i++)
            {
                var aVariation = aGroup.transform.GetChild(i).GetComponent<SoundGroupVariation>();

                var newVariation = (GameObject)Instantiate(_organizer.dynVariationTemplate.gameObject, groupTrans.position, Quaternion.identity);
                newVariation.transform.parent = groupTrans;

                var variation = newVariation.GetComponent<DynamicGroupVariation>();

                var clipName = aVariation.GameObjectName;

                var aVarAudio = aVariation.GetComponent<AudioSource>();

                UnityEditorInternal.ComponentUtility.CopyComponent(aVarAudio);
                // ReSharper disable once ArrangeStaticMemberQualifier
                GameObject.DestroyImmediate(variation.VarAudio);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(variation.gameObject);
                UnityEditorInternal.ComponentUtility.MoveComponentUp(variation.VarAudio);

                switch (aVariation.audLocation)
                {
                    case MasterAudio.AudioLocation.Clip:
                        var clip = aVarAudio.clip;
                        if (clip == null)
                        {
                            continue;
                        }
                        variation.VarAudio.clip = clip;
                        break;
                    case MasterAudio.AudioLocation.ResourceFile:
                        variation.resourceFileName = aVariation.resourceFileName;
                        variation.useLocalization = aVariation.useLocalization;
                        break;
#if ADDRESSABLES_ENABLED
                case MasterAudio.AudioLocation.Addressable:
                    variation.audioClipAddressable = aVariation.audioClipAddressable;
                    break;
#endif
                }

                ResonanceAudioHelper.CopyResonanceAudioSource(aVariation, variation);
                OculusAudioHelper.CopyOculusAudioSource(aVariation, variation);

                variation.audLocation = aVariation.audLocation;
                variation.VarAudio.dopplerLevel = aVarAudio.dopplerLevel;
                variation.VarAudio.maxDistance = aVarAudio.maxDistance;
                variation.VarAudio.minDistance = aVarAudio.minDistance;
                variation.VarAudio.bypassEffects = aVarAudio.bypassEffects;
                variation.VarAudio.ignoreListenerVolume = aVarAudio.ignoreListenerVolume;
                variation.VarAudio.mute = aVarAudio.mute;

                variation.VarAudio.panStereo = aVarAudio.panStereo;

                variation.VarAudio.rolloffMode = aVarAudio.rolloffMode;
                variation.VarAudio.spread = aVarAudio.spread;

                variation.VarAudio.loop = aVarAudio.loop;
                variation.VarAudio.pitch = aVarAudio.pitch;
                variation.transform.name = clipName;
                variation.isExpanded = aVariation.isExpanded;

                variation.probabilityToPlay = aVariation.probabilityToPlay;
                variation.weight = aVariation.weight;

                variation.isUninterruptible = aVariation.isUninterruptible;
                variation.importance = aVariation.importance;

                variation.clipAlias = aVariation.clipAlias;
                variation.useRandomPitch = aVariation.useRandomPitch;
                variation.randomPitchMode = aVariation.randomPitchMode;
                variation.randomPitchMin = aVariation.randomPitchMin;
                variation.randomPitchMax = aVariation.randomPitchMax;

                variation.useRandomVolume = aVariation.useRandomVolume;
                variation.randomVolumeMode = aVariation.randomVolumeMode;
                variation.randomVolumeMin = aVariation.randomVolumeMin;
                variation.randomVolumeMax = aVariation.randomVolumeMax;

                variation.useFades = aVariation.useFades;
                variation.fadeInTime = aVariation.fadeInTime;
                variation.fadeOutTime = aVariation.fadeOutTime;

                variation.useIntroSilence = aVariation.useIntroSilence;
                variation.introSilenceMin = aVariation.introSilenceMin;
                variation.introSilenceMax = aVariation.introSilenceMax;

                variation.useRandomStartTime = aVariation.useRandomStartTime;
                variation.randomStartMinPercent = aVariation.randomStartMinPercent;
                variation.randomStartMaxPercent = aVariation.randomStartMaxPercent;
                variation.randomEndPercent = aVariation.randomEndPercent;

                variation.useCustomLooping = aVariation.useCustomLooping;
                variation.minCustomLoops = aVariation.minCustomLoops;
                variation.maxCustomLoops = aVariation.maxCustomLoops;

                // remove unused filter FX
                if (variation.LowPassFilter != null && !variation.LowPassFilter.enabled)
                {
                    Destroy(variation.LowPassFilter);
                }
                if (variation.HighPassFilter != null && !variation.HighPassFilter.enabled)
                {
                    Destroy(variation.HighPassFilter);
                }
                if (variation.DistortionFilter != null && !variation.DistortionFilter.enabled)
                {
                    Destroy(variation.DistortionFilter);
                }
                if (variation.ChorusFilter != null && !variation.ChorusFilter.enabled)
                {
                    Destroy(variation.ChorusFilter);
                }
                if (variation.EchoFilter != null && !variation.EchoFilter.enabled)
                {
                    Destroy(variation.EchoFilter);
                }
                if (variation.ReverbFilter != null && !variation.ReverbFilter.enabled)
                {
                    Destroy(variation.ReverbFilter);
                }
            }
            // added to Hierarchy!

            // populate sounds for playing!
            var groupScript = newGroup.GetComponent<DynamicSoundGroup>();
            // populate other properties.
            groupScript.retriggerPercentage = aGroup.retriggerPercentage;
            groupScript.groupMasterVolume = aGroup.groupMasterVolume;
            groupScript.limitMode = aGroup.limitMode;
            groupScript.limitPerXFrames = aGroup.limitPerXFrames;
            groupScript.minimumTimeBetween = aGroup.minimumTimeBetween;
            groupScript.useClipAgePriority = aGroup.useClipAgePriority;
            groupScript.limitPolyphony = aGroup.limitPolyphony;
            groupScript.voiceLimitCount = aGroup.voiceLimitCount;
            groupScript.curVariationSequence = aGroup.curVariationSequence;
            groupScript.useInactivePeriodPoolRefill = aGroup.useInactivePeriodPoolRefill;
            groupScript.inactivePeriodSeconds = aGroup.inactivePeriodSeconds;
            groupScript.curVariationMode = aGroup.curVariationMode;
            groupScript.useDialogFadeOut = aGroup.useDialogFadeOut;
            groupScript.dialogFadeOutTime = aGroup.dialogFadeOutTime;

            groupScript.isUninterruptible = aGroup.isUninterruptible;
            groupScript.importance = aGroup.importance;

            groupScript.chainLoopDelayMin = aGroup.chainLoopDelayMin;
            groupScript.chainLoopDelayMax = aGroup.chainLoopDelayMax;
            groupScript.chainLoopMode = aGroup.chainLoopMode;
            groupScript.chainLoopNumLoops = aGroup.chainLoopNumLoops;

            groupScript.soundPlayedEventActive = aGroup.soundPlayedEventActive;
            groupScript.soundPlayedCustomEvent = aGroup.soundPlayedCustomEvent;

            groupScript.spatialBlendType = aGroup.spatialBlendType;
            groupScript.spatialBlend = aGroup.spatialBlend;

            groupScript.groupPlayType = aGroup.groupPlayType;

            groupScript.targetDespawnedBehavior = aGroup.targetDespawnedBehavior;
            groupScript.despawnFadeTime = aGroup.despawnFadeTime;

            groupScript.isUsingOcclusion = aGroup.isUsingOcclusion;

            groupScript.comments = aGroup.comments;
            groupScript.ignoreListenerPause = aGroup.ignoreListenerPause;
            groupScript.logSound = aGroup.logSound;
            groupScript.alwaysHighestPriority = aGroup.alwaysHighestPriority;
#if ADDRESSABLES_ENABLED
        groupScript.addressableUnusedSecondsLifespan = aGroup.addressableUnusedSecondsLifespan;
#endif


            var dyn = aGroup.GetComponentInParent<MasterAudio>();
            if (aGroup.busIndex > 0)
            {
                groupScript.busName = dyn.groupBuses[aGroup.busIndex - MasterAudio.HardCodedBusOptions].busName;
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        private void ExportGroupToDgsc(DynamicSoundGroup aGroup)
        {
            var newGroup = (GameObject)Instantiate(_organizer.dynGroupTemplate, _organizer.transform.position, Quaternion.identity);
            newGroup.name = aGroup.name;
            newGroup.transform.position = _organizer.destObject.transform.position;

            AudioUndoHelper.CreateObjectForUndo(newGroup, "export Group(s)");
            newGroup.transform.parent = _organizer.destObject.transform;

            var groupTrans = newGroup.transform;

            foreach (var t in aGroup.groupVariations)
            {
                var aVariation = t;

                var newVariation = (GameObject)Instantiate(_organizer.dynVariationTemplate.gameObject, groupTrans.position, Quaternion.identity);
                newVariation.transform.parent = groupTrans;
                newVariation.transform.position = groupTrans.position;

                var variation = newVariation.GetComponent<DynamicGroupVariation>();

                var clipName = aVariation.name;

                var aVarAudio = aVariation.GetComponent<AudioSource>();

                UnityEditorInternal.ComponentUtility.CopyComponent(aVarAudio);
                // ReSharper disable once ArrangeStaticMemberQualifier
                GameObject.DestroyImmediate(variation.VarAudio);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(variation.gameObject);
                UnityEditorInternal.ComponentUtility.MoveComponentUp(variation.VarAudio);

                switch (aVariation.audLocation)
                {
                    case MasterAudio.AudioLocation.Clip:
                        var clip = aVarAudio.clip;
                        if (clip == null)
                        {
                            continue;
                        }
                        variation.VarAudio.clip = clip;
                        break;
                    case MasterAudio.AudioLocation.ResourceFile:
                        variation.resourceFileName = aVariation.resourceFileName;
                        variation.useLocalization = aVariation.useLocalization;
                        break;
#if ADDRESSABLES_ENABLED
                case MasterAudio.AudioLocation.Addressable:
                    variation.audioClipAddressable = aVariation.audioClipAddressable;
                    break;
#endif
                }

                ResonanceAudioHelper.CopyResonanceAudioSource(aVariation, variation);
                OculusAudioHelper.CopyOculusAudioSource(aVariation, variation);

                variation.audLocation = aVariation.audLocation;
                variation.VarAudio.dopplerLevel = aVarAudio.dopplerLevel;
                variation.VarAudio.maxDistance = aVarAudio.maxDistance;
                variation.VarAudio.minDistance = aVarAudio.minDistance;
                variation.VarAudio.bypassEffects = aVarAudio.bypassEffects;
                variation.VarAudio.ignoreListenerVolume = aVarAudio.ignoreListenerVolume;
                variation.VarAudio.mute = aVarAudio.mute;

                variation.VarAudio.panStereo = aVarAudio.panStereo;

                variation.VarAudio.rolloffMode = aVarAudio.rolloffMode;
                variation.VarAudio.spread = aVarAudio.spread;

                variation.VarAudio.loop = aVarAudio.loop;
                variation.VarAudio.pitch = aVarAudio.pitch;
                variation.transform.name = clipName;
                variation.isExpanded = aVariation.isExpanded;

                variation.probabilityToPlay = aVariation.probabilityToPlay;
                variation.weight = aVariation.weight;

                variation.isUninterruptible = aVariation.isUninterruptible;
                variation.importance = aVariation.importance;

                variation.clipAlias = aVariation.clipAlias;
                variation.useRandomPitch = aVariation.useRandomPitch;
                variation.randomPitchMode = aVariation.randomPitchMode;
                variation.randomPitchMin = aVariation.randomPitchMin;
                variation.randomPitchMax = aVariation.randomPitchMax;

                variation.useRandomVolume = aVariation.useRandomVolume;
                variation.randomVolumeMode = aVariation.randomVolumeMode;
                variation.randomVolumeMin = aVariation.randomVolumeMin;
                variation.randomVolumeMax = aVariation.randomVolumeMax;

                variation.useFades = aVariation.useFades;
                variation.fadeInTime = aVariation.fadeInTime;
                variation.fadeOutTime = aVariation.fadeOutTime;

                variation.useIntroSilence = aVariation.useIntroSilence;
                variation.introSilenceMin = aVariation.introSilenceMin;
                variation.introSilenceMax = aVariation.introSilenceMax;

                variation.useRandomStartTime = aVariation.useRandomStartTime;
                variation.randomStartMinPercent = aVariation.randomStartMinPercent;
                variation.randomStartMaxPercent = aVariation.randomStartMaxPercent;
                variation.randomEndPercent = aVariation.randomEndPercent;

                variation.useCustomLooping = aVariation.useCustomLooping;
                variation.minCustomLoops = aVariation.minCustomLoops;
                variation.maxCustomLoops = aVariation.maxCustomLoops;

                // remove unused filter FX
                if (variation.LowPassFilter != null && !variation.LowPassFilter.enabled)
                {
                    Destroy(variation.LowPassFilter);
                }
                if (variation.HighPassFilter != null && !variation.HighPassFilter.enabled)
                {
                    Destroy(variation.HighPassFilter);
                }
                if (variation.DistortionFilter != null && !variation.DistortionFilter.enabled)
                {
                    Destroy(variation.DistortionFilter);
                }
                if (variation.ChorusFilter != null && !variation.ChorusFilter.enabled)
                {
                    Destroy(variation.ChorusFilter);
                }
                if (variation.EchoFilter != null && !variation.EchoFilter.enabled)
                {
                    Destroy(variation.EchoFilter);
                }
                if (variation.ReverbFilter != null && !variation.ReverbFilter.enabled)
                {
                    Destroy(variation.ReverbFilter);
                }
            }
            // added to Hierarchy!

            // populate sounds for playing!
            var groupScript = newGroup.GetComponent<DynamicSoundGroup>();
            // populate other properties.
            groupScript.retriggerPercentage = aGroup.retriggerPercentage;
            groupScript.groupMasterVolume = aGroup.groupMasterVolume;
            groupScript.limitMode = aGroup.limitMode;
            groupScript.limitPerXFrames = aGroup.limitPerXFrames;
            groupScript.minimumTimeBetween = aGroup.minimumTimeBetween;
            groupScript.useClipAgePriority = aGroup.useClipAgePriority;
            groupScript.limitPolyphony = aGroup.limitPolyphony;
            groupScript.voiceLimitCount = aGroup.voiceLimitCount;
            groupScript.curVariationSequence = aGroup.curVariationSequence;
            groupScript.useInactivePeriodPoolRefill = aGroup.useInactivePeriodPoolRefill;
            groupScript.inactivePeriodSeconds = aGroup.inactivePeriodSeconds;
            groupScript.curVariationMode = aGroup.curVariationMode;
            groupScript.useDialogFadeOut = aGroup.useDialogFadeOut;
            groupScript.dialogFadeOutTime = aGroup.dialogFadeOutTime;

            groupScript.isUninterruptible = aGroup.isUninterruptible;
            groupScript.importance = aGroup.importance;

            groupScript.chainLoopDelayMin = aGroup.chainLoopDelayMin;
            groupScript.chainLoopDelayMax = aGroup.chainLoopDelayMax;
            groupScript.chainLoopMode = aGroup.chainLoopMode;
            groupScript.chainLoopNumLoops = aGroup.chainLoopNumLoops;

            groupScript.expandLinkedGroups = aGroup.expandLinkedGroups;
            groupScript.childSoundGroups = aGroup.childSoundGroups;
            groupScript.endLinkedGroups = aGroup.endLinkedGroups;
            groupScript.linkedStartGroupSelectionType = aGroup.linkedStartGroupSelectionType;
            groupScript.linkedStopGroupSelectionType = aGroup.linkedStopGroupSelectionType;

            groupScript.spatialBlendType = aGroup.spatialBlendType;
            groupScript.spatialBlend = aGroup.spatialBlend;

            groupScript.groupPlayType = aGroup.groupPlayType;

            groupScript.targetDespawnedBehavior = aGroup.targetDespawnedBehavior;
            groupScript.despawnFadeTime = aGroup.despawnFadeTime;

            groupScript.isUsingOcclusion = aGroup.isUsingOcclusion;

            groupScript.comments = aGroup.comments;
            groupScript.ignoreListenerPause = aGroup.ignoreListenerPause;
            groupScript.logSound = aGroup.logSound;
            groupScript.alwaysHighestPriority = aGroup.alwaysHighestPriority;
#if ADDRESSABLES_ENABLED
        groupScript.addressableUnusedSecondsLifespan = aGroup.addressableUnusedSecondsLifespan;
#endif

            var dyn = groupScript.GetComponentInParent<DynamicSoundGroupCreator>();
            if (!string.IsNullOrEmpty(aGroup.busName))
            {
                var busIndex = -1;

                var targetBus = dyn.groupBuses.Find(delegate (GroupBus obj)
                {
                    return obj.busName == aGroup.busName;
                });

                if (targetBus != null)
                {
                    busIndex = dyn.groupBuses.IndexOf(targetBus) + DynamicSoundGroupCreator.HardCodedBusOptions;
                }

                if (busIndex < 0)
                { // didn't find bus.
                    if (aGroup.isCopiedFromDGSC)
                    {
                        // create bus on DGSC
                        dyn.groupBuses.Add(new GroupBus()
                        {
                            busName = aGroup.busName,
                            isExisting = aGroup.isExistingBus
                        });
                    }
                    else
                    {
                        // create bus on DGSC
                        dyn.groupBuses.Add(new GroupBus()
                        {
                            busName = aGroup.busName,
                            isExisting = true
                        });
                    }

                    targetBus = dyn.groupBuses.Find(delegate (GroupBus obj)
                    {
                        return obj.busName == aGroup.busName;
                    });

                    if (targetBus != null)
                    {
                        busIndex = dyn.groupBuses.IndexOf(targetBus) + DynamicSoundGroupCreator.HardCodedBusOptions;
                    }
                }

                groupScript.busIndex = busIndex;
                groupScript.busName = aGroup.busName;
            }
        }

        private void ExportGroupToMA(DynamicSoundGroup aGroup)
        {
            var newGroup = (GameObject)Instantiate(_organizer.maGroupTemplate, _organizer.transform.position, Quaternion.identity);
            newGroup.name = aGroup.name;
            newGroup.transform.position = _organizer.destObject.transform.position;

            AudioUndoHelper.CreateObjectForUndo(newGroup, "export Group(s)");
            newGroup.transform.parent = _organizer.destObject.transform;

            var groupTrans = newGroup.transform;

            foreach (var aVariation in aGroup.groupVariations)
            {
                var newVariation = (GameObject)Instantiate(_organizer.maVariationTemplate.gameObject, groupTrans.position, Quaternion.identity);
                newVariation.transform.parent = groupTrans;
                newVariation.transform.position = groupTrans.position;

                var variation = newVariation.GetComponent<SoundGroupVariation>();

                var clipName = aVariation.name;

                var aVarAudio = aVariation.GetComponent<AudioSource>();

                UnityEditorInternal.ComponentUtility.CopyComponent(aVarAudio);
                // ReSharper disable once ArrangeStaticMemberQualifier
                GameObject.DestroyImmediate(variation.VarAudio);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(variation.gameObject);
                UnityEditorInternal.ComponentUtility.MoveComponentUp(variation.VarAudio);

                switch (aVariation.audLocation)
                {
                    case MasterAudio.AudioLocation.Clip:
                        var clip = aVarAudio.clip;
                        if (clip == null)
                        {
                            continue;
                        }
                        variation.VarAudio.clip = clip;
                        break;
                    case MasterAudio.AudioLocation.ResourceFile:
                        variation.resourceFileName = aVariation.resourceFileName;
                        variation.useLocalization = aVariation.useLocalization;
                        break;
#if ADDRESSABLES_ENABLED
                case MasterAudio.AudioLocation.Addressable:
                    variation.audioClipAddressable = aVariation.audioClipAddressable;
                    break;
#endif
                }

                ResonanceAudioHelper.CopyResonanceAudioSource(aVariation, variation);
                OculusAudioHelper.CopyOculusAudioSource(aVariation, variation);

                variation.audLocation = aVariation.audLocation;
                variation.VarAudio.dopplerLevel = aVarAudio.dopplerLevel;
                variation.VarAudio.maxDistance = aVarAudio.maxDistance;
                variation.VarAudio.minDistance = aVarAudio.minDistance;
                variation.VarAudio.bypassEffects = aVarAudio.bypassEffects;
                variation.VarAudio.ignoreListenerVolume = aVarAudio.ignoreListenerVolume;
                variation.VarAudio.mute = aVarAudio.mute;

                variation.VarAudio.panStereo = aVarAudio.panStereo;

                variation.VarAudio.rolloffMode = aVarAudio.rolloffMode;
                variation.VarAudio.spread = aVarAudio.spread;

                variation.VarAudio.loop = aVarAudio.loop;
                variation.VarAudio.pitch = aVarAudio.pitch;
                variation.transform.name = clipName;
                variation.isExpanded = aVariation.isExpanded;

                variation.probabilityToPlay = aVariation.probabilityToPlay;
                variation.weight = aVariation.weight;

                variation.isUninterruptible = aVariation.isUninterruptible;
                variation.importance = aVariation.importance;

                variation.clipAlias = aVariation.clipAlias;
                variation.useRandomPitch = aVariation.useRandomPitch;
                variation.randomPitchMode = aVariation.randomPitchMode;
                variation.randomPitchMin = aVariation.randomPitchMin;
                variation.randomPitchMax = aVariation.randomPitchMax;

                variation.useRandomVolume = aVariation.useRandomVolume;
                variation.randomVolumeMode = aVariation.randomVolumeMode;
                variation.randomVolumeMin = aVariation.randomVolumeMin;
                variation.randomVolumeMax = aVariation.randomVolumeMax;

                variation.useFades = aVariation.useFades;
                variation.fadeInTime = aVariation.fadeInTime;
                variation.fadeOutTime = aVariation.fadeOutTime;

                variation.useIntroSilence = aVariation.useIntroSilence;
                variation.introSilenceMin = aVariation.introSilenceMin;
                variation.introSilenceMax = aVariation.introSilenceMax;

                variation.useRandomStartTime = aVariation.useRandomStartTime;
                variation.randomStartMinPercent = aVariation.randomStartMinPercent;
                variation.randomStartMaxPercent = aVariation.randomStartMaxPercent;
                variation.randomEndPercent = aVariation.randomEndPercent;

                variation.useCustomLooping = aVariation.useCustomLooping;
                variation.minCustomLoops = aVariation.minCustomLoops;
                variation.maxCustomLoops = aVariation.maxCustomLoops;

                // remove unused filter FX
                if (variation.LowPassFilter != null && !variation.LowPassFilter.enabled)
                {
                    Destroy(variation.LowPassFilter);
                }
                if (variation.HighPassFilter != null && !variation.HighPassFilter.enabled)
                {
                    Destroy(variation.HighPassFilter);
                }
                if (variation.DistortionFilter != null && !variation.DistortionFilter.enabled)
                {
                    Destroy(variation.DistortionFilter);
                }
                if (variation.ChorusFilter != null && !variation.ChorusFilter.enabled)
                {
                    Destroy(variation.ChorusFilter);
                }
                if (variation.EchoFilter != null && !variation.EchoFilter.enabled)
                {
                    Destroy(variation.EchoFilter);
                }
                if (variation.ReverbFilter != null && !variation.ReverbFilter.enabled)
                {
                    Destroy(variation.ReverbFilter);
                }
            }
            // added to Hierarchy!

            // populate sounds for playing!
            var groupScript = newGroup.GetComponent<MasterAudioGroup>();
            // populate other properties.
            groupScript.retriggerPercentage = aGroup.retriggerPercentage;
            groupScript.groupMasterVolume = aGroup.groupMasterVolume;
            groupScript.limitMode = aGroup.limitMode;
            groupScript.limitPerXFrames = aGroup.limitPerXFrames;
            groupScript.minimumTimeBetween = aGroup.minimumTimeBetween;
            groupScript.useClipAgePriority = aGroup.useClipAgePriority;
            groupScript.limitPolyphony = aGroup.limitPolyphony;
            groupScript.voiceLimitCount = aGroup.voiceLimitCount;
            groupScript.curVariationSequence = aGroup.curVariationSequence;
            groupScript.useInactivePeriodPoolRefill = aGroup.useInactivePeriodPoolRefill;
            groupScript.inactivePeriodSeconds = aGroup.inactivePeriodSeconds;
            groupScript.curVariationMode = aGroup.curVariationMode;
            groupScript.useDialogFadeOut = aGroup.useDialogFadeOut;
            groupScript.dialogFadeOutTime = aGroup.dialogFadeOutTime;

            groupScript.isUninterruptible = aGroup.isUninterruptible;
            groupScript.importance = aGroup.importance;

            groupScript.chainLoopDelayMin = aGroup.chainLoopDelayMin;
            groupScript.chainLoopDelayMax = aGroup.chainLoopDelayMax;
            groupScript.chainLoopMode = aGroup.chainLoopMode;
            groupScript.chainLoopNumLoops = aGroup.chainLoopNumLoops;

            groupScript.expandLinkedGroups = aGroup.expandLinkedGroups;
            groupScript.childSoundGroups = aGroup.childSoundGroups;
            groupScript.endLinkedGroups = aGroup.endLinkedGroups;
            groupScript.linkedStartGroupSelectionType = aGroup.linkedStartGroupSelectionType;
            groupScript.linkedStopGroupSelectionType = aGroup.linkedStopGroupSelectionType;

            groupScript.soundPlayedEventActive = aGroup.soundPlayedEventActive;
            groupScript.soundPlayedCustomEvent = aGroup.soundPlayedCustomEvent;

            groupScript.spatialBlendType = aGroup.spatialBlendType;
            groupScript.spatialBlend = aGroup.spatialBlend;

            groupScript.groupPlayType = aGroup.groupPlayType;

            groupScript.targetDespawnedBehavior = aGroup.targetDespawnedBehavior;
            groupScript.despawnFadeTime = aGroup.despawnFadeTime;

            groupScript.isUsingOcclusion = aGroup.isUsingOcclusion;

            groupScript.comments = aGroup.comments;
            groupScript.ignoreListenerPause = aGroup.ignoreListenerPause;
            groupScript.logSound = aGroup.logSound;
            groupScript.alwaysHighestPriority = aGroup.alwaysHighestPriority;
#if ADDRESSABLES_ENABLED
        groupScript.addressableUnusedSecondsLifespan = aGroup.addressableUnusedSecondsLifespan;
#endif

            var dyn = groupScript.GetComponentInParent<MasterAudio>();
            if (!string.IsNullOrEmpty(aGroup.busName))
            {
                var busIndex = -1;

                var targetBus = dyn.groupBuses.Find(delegate (GroupBus obj)
                {
                    return obj.busName == aGroup.busName;
                });

                if (targetBus != null)
                {
                    busIndex = dyn.groupBuses.IndexOf(targetBus) + MasterAudio.HardCodedBusOptions;
                }

                if (busIndex < 0)
                { // didn't find bus.
                  // create bus on DGSC
                    dyn.groupBuses.Add(new GroupBus()
                    {
                        busName = aGroup.busName
                    });

                    targetBus = dyn.groupBuses.Find(delegate (GroupBus obj)
                    {
                        return obj.busName == aGroup.busName;
                    });

                    if (targetBus != null)
                    {
                        busIndex = dyn.groupBuses.IndexOf(targetBus) + MasterAudio.HardCodedBusOptions;
                    }
                }

                groupScript.busIndex = busIndex;
            }
        }

        private void ExportSelectedGroups()
        {
            if (_organizer.destObject == null)
            {
                return;
            }

            var exported = 0;
            var skipped = 0;

            // ReSharper disable once InconsistentNaming
            var isDestMA = _organizer.destObject.GetComponent<MasterAudio>() != null;
            // ReSharper disable once InconsistentNaming
            var isDestDGSC = _organizer.destObject.GetComponent<DynamicSoundGroupCreator>() != null;

            if (!isDestMA && !isDestDGSC)
            {
                Debug.LogError("Invalid Destination Object '" + _organizer.destObject.name + "'. It's set up wrong. Aborting Export. Contact DarkTonic for assistance.");
                return;
            }

            foreach (var item in _organizer.selectedDestSoundGroups)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                var wasSkipped = false;
                var grp = item.Go.GetComponent<DynamicSoundGroup>();

                if (isDestDGSC)
                {
                    for (var g = 0; g < _organizer.destObject.transform.childCount; g++)
                    {
                        var aGroup = _organizer.destObject.transform.GetChild(g);
                        if (aGroup.name != grp.name)
                        {
                            continue;
                        }

                        Debug.LogError("Group '" + grp.name + "' skipped because there's already a Group with that name in the destination Dynamic Sound Group Creator object. If you wish to export the Group, please delete the one in the DSGC object first.");
                        skipped++;
                        wasSkipped = true;
                    }

                    if (wasSkipped)
                    {
                        continue;
                    }

                    ExportGroupToDgsc(grp);
                    exported++;
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                }
                else if (isDestMA)
                {
                    for (var g = 0; g < _organizer.destObject.transform.childCount; g++)
                    {
                        var aGroup = _organizer.destObject.transform.GetChild(g);
                        if (aGroup.name != grp.name)
                        {
                            continue;
                        }

                        Debug.LogError("Group '" + grp.name + "' skipped because there's already a Group with that name in the destination Master Audio object. If you wish to export the Group, please delete the one in the MA object first.");
                        skipped++;
                        wasSkipped = true;
                    }

                    if (wasSkipped)
                    {
                        continue;
                    }

                    ExportGroupToMA(grp);
                    exported++;
                }
            }

            var summaryText = exported + " Group(s) exported.";
            if (skipped == 0)
            {
                Debug.Log(summaryText);
            }
        }

        private void ExportSelectedEvents()
        {
            if (_organizer.destObject == null)
            {
                return;
            }

            var exported = 0;
            var skipped = 0;

            var ma = _organizer.destObject.GetComponent<MasterAudio>();
            var dgsc = _organizer.destObject.GetComponent<DynamicSoundGroupCreator>();

            var isDestMa = ma != null;
            var isDestDgsc = dgsc != null;

            if (!isDestMa && !isDestDgsc)
            {
                Debug.LogError("Invalid Destination Object '" + _organizer.destObject.name + "'. It's set up wrong. Aborting Export. Contact DarkTonic for assistance.");
                return;
            }

            foreach (var item in _organizer.selectedDestCustomEvents)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                var wasSkipped = false;
                var evt = item.Event;
                var catName = evt.categoryName;

                if (isDestDgsc)
                {
                    foreach (var aEvt in dgsc.customEventsToCreate)
                    {
                        if (aEvt.EventName != evt.EventName)
                        {
                            continue;
                        }

                        Debug.LogError("Group '" + evt.EventName + "' skipped because there's already a Custom Event with that name in the destination Dynamic Sound Group Creator object. If you wish to export the Custom Event, please delete the one in the DSGC object first.");
                        skipped++;
                        wasSkipped = true;
                    }

                    if (wasSkipped)
                    {
                        continue;
                    }

                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, dgsc, "export Custom Event(s)");

                    if (dgsc.customEventCategories.FindAll(delegate (CustomEventCategory cat)
                    {
                        return cat.CatName == catName;
                    }).Count == 0)
                    {
                        dgsc.customEventCategories.Add(new CustomEventCategory
                        {
                            CatName = catName,
                            ProspectiveName = catName
                        });
                    }

                    dgsc.customEventsToCreate.Add(new CustomEvent(evt.EventName)
                    {
                        distanceThreshold = evt.distanceThreshold,
                        eventExpanded = evt.eventExpanded,
                        eventReceiveMode = evt.eventReceiveMode,
                        ProspectiveName = evt.EventName,
                        filterModeQty = evt.filterModeQty,
                        eventRcvFilterMode = evt.eventRcvFilterMode,
                        categoryName = evt.categoryName
                    });

                    exported++;
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                }
                else if (isDestMa)
                {
                    foreach (var aEvt in ma.customEvents)
                    {
                        if (aEvt.EventName != evt.EventName)
                        {
                            continue;
                        }

                        Debug.LogError("Custom Event '" + evt.EventName + "' skipped because there's already a Custom Event with that name in the destination Master Audio object. If you wish to export the Custom Event, please delete the one in the MA object first.");
                        skipped++;
                        wasSkipped = true;
                    }

                    if (wasSkipped)
                    {
                        continue;
                    }

                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, ma, "export Custom Event(s)");

                    if (ma.customEventCategories.FindAll(delegate (CustomEventCategory cat)
                    {
                        return cat.CatName == catName;
                    }).Count == 0)
                    {
                        ma.customEventCategories.Add(new CustomEventCategory
                        {
                            CatName = catName,
                            ProspectiveName = catName
                        });
                    }

                    ma.customEvents.Add(new CustomEvent(evt.EventName)
                    {
                        distanceThreshold = evt.distanceThreshold,
                        eventExpanded = evt.eventExpanded,
                        eventReceiveMode = evt.eventReceiveMode,
                        ProspectiveName = evt.EventName,
                        filterModeQty = evt.filterModeQty,
                        eventRcvFilterMode = evt.eventRcvFilterMode,
                        categoryName = evt.categoryName
                    });

                    exported++;
                }
            }

            var summaryText = exported + " Custom Event(s) exported.";
            if (skipped == 0)
            {
                Debug.Log(summaryText);
            }
        }

        private void ExpandCollapseCustomEvents(bool shouldExpand)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "Expand / Collapse All Custom Events");

            foreach (var t in _organizer.customEvents)
            {
                t.eventExpanded = shouldExpand;
            }
        }

        private void CreateCategory()
        {
            if (string.IsNullOrEmpty(_organizer.newCustomEventCategoryName))
            {
                DTGUIHelper.ShowAlert("You cannot have a blank Category name.");
                return;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var c = 0; c < _organizer.customEventCategories.Count; c++)
            {
                var cat = _organizer.customEventCategories[c];
                // ReSharper disable once InvertIf
                if (cat.CatName == _organizer.newCustomEventCategoryName)
                {
                    DTGUIHelper.ShowAlert("You already have a Category named '" + _organizer.newCustomEventCategoryName + "'. Category names must be unique.");
                    return;
                }
            }

            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "Create New Category");

            var newCat = new CustomEventCategory
            {
                CatName = _organizer.newCustomEventCategoryName,
                ProspectiveName = _organizer.newCustomEventCategoryName
            };

            _organizer.customEventCategories.Add(newCat);
        }

        private void CreateCustomEvent(string newEventName, string defaultCategory)
        {
            if (_organizer.customEvents.FindAll(delegate (CustomEvent obj)
            {
                return obj.EventName == newEventName;
            }).Count > 0)
            {
                DTGUIHelper.ShowAlert("You already have a Custom Event named '" + newEventName + "'. Please choose a different name.");
                return;
            }

            var newEvent = new CustomEvent(newEventName);
            newEvent.categoryName = defaultCategory;

            _organizer.customEvents.Add(newEvent);
        }

        private void ExpandCollapseCategory(string category, bool isExpand)
        {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _organizer, "toggle expand / collapse all items in Category");

            foreach (var item in _organizer.customEvents)
            {
                if (item.categoryName != category)
                {
                    continue;
                }

                item.eventExpanded = isExpand;
            }
        }
    }
}