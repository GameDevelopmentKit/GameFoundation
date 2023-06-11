using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(PlaylistController))]
    // ReSharper disable once CheckNamespace
    public class PlaylistControllerInspector : Editor
    {
        private List<string> _customEventNames;

        // ReSharper disable once FunctionComplexityOverflow
        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel = 0;

            var controller = (PlaylistController)target;

            MasterAudio.Instance = null;

            var ma = MasterAudio.SafeInstance;
            var maInScene = ma != null;

            if (maInScene)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
                _customEventNames = ma.CustomEventNames;
            }

            DTGUIHelper.HelpHeader("https://www.dtdevtools.com/docs/masteraudio/PlaylistControllers.htm", "https://www.dtdevtools.com/API/masteraudio/class_dark_tonic_1_1_master_audio_1_1_playlist_controller.html");

            var isDirty = false;

            EditorGUILayout.BeginHorizontal();

            var newVol = DTGUIHelper.DisplayVolumeField(controller._playlistVolume, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, 0f, true, "Playlist Volume");
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (newVol != controller._playlistVolume)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Playlist Volume");
                controller.PlaylistVolume = newVol;
            }

            var buttonPressed = DTGUIHelper.AddPlaylistControllerGOButtons(controller, "Playlist Controller");
            EditorGUILayout.EndHorizontal();

            switch (buttonPressed)
            {
                case DTGUIHelper.DTFunctionButtons.Mute:
                    controller.ToggleMutePlaylist();
                    break;
            }

            List<string> playlistNames = null;

            if (maInScene)
            {
                // ReSharper disable once PossibleNullReferenceException
                playlistNames = MasterAudio.Instance.PlaylistNamesOnly;

                if (!Application.isPlaying)
                {
                    var plNames = MasterAudio.Instance.PlaylistNames;
                    var existingIndex = plNames.IndexOf(controller.startPlaylistName);

                    int? groupIndex = null;

                    var noPl = false;
                    var noMatch = false;

                    if (existingIndex >= 1)
                    {
                        groupIndex = EditorGUILayout.Popup("Initial Playlist", existingIndex, plNames.ToArray());
                        if (existingIndex == 1)
                        {
                            noPl = true;
                        }
                    }
                    else if (existingIndex == -1 && controller.startPlaylistName == MasterAudio.NoGroupName)
                    {
                        groupIndex = EditorGUILayout.Popup("Initial Playlist", existingIndex, plNames.ToArray());
                    }
                    else
                    {
                        // non-match
                        noMatch = true;
                        var newPlaylist = EditorGUILayout.TextField("Initial Playlist", controller.startPlaylistName);
                        if (newPlaylist != controller.startPlaylistName)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Initial Playlist");
                            controller.startPlaylistName = newPlaylist;
                        }

                        var newIndex = EditorGUILayout.Popup("All Playlists", -1, plNames.ToArray());
                        if (newIndex >= 0)
                        {
                            groupIndex = newIndex;
                        }
                    }

                    if (noPl)
                    {
                        DTGUIHelper.ShowRedError("Initial Playlist not specified. No music will play.");
                    }
                    else if (noMatch)
                    {
                        DTGUIHelper.ShowRedError(
                            "Initial Playlist found no match. Type in or choose one from 'All Playlists'.");
                    }

                    if (groupIndex.HasValue)
                    {
                        if (existingIndex != groupIndex.Value)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Initial Playlist");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (groupIndex.Value == -1)
                        {
                            controller.startPlaylistName = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.startPlaylistName = plNames[groupIndex.Value];
                        }
                    }
                }
            }


            var syncGroupList = new List<string>();
            for (var i = 0; i < 4; i++)
            {
                syncGroupList.Add((i + 1).ToString());
            }
            syncGroupList.Insert(0, MasterAudio.NoGroupName);

            var syncIndex = syncGroupList.IndexOf(controller.syncGroupNum.ToString());
            if (syncIndex == -1)
            {
                syncIndex = 0;
            }
            var newSync = EditorGUILayout.Popup("Controller Sync Group", syncIndex, syncGroupList.ToArray());
            if (newSync != syncIndex)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Controller Sync Group");
                controller.syncGroupNum = newSync;
            }

            EditorGUI.indentLevel = 0;

            var newChan = (AudioMixerGroup)EditorGUILayout.ObjectField("Mixer Group", controller.mixerChannel, typeof(AudioMixerGroup), false);
            if (newChan != controller.mixerChannel)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Unity Mixer Group");
                controller.mixerChannel = newChan;

                if (Application.isPlaying)
                {
                    controller.RouteToMixerChannel(newChan);
                }
            }

            var newPausedPlay = EditorGUILayout.Toggle("Ignore Listener Pause", controller.ignoreListenerPause);
            if (newPausedPlay != controller.ignoreListenerPause)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle Ignore Listener Pause");
                controller.ignoreListenerPause = newPausedPlay;
            }

#if DISABLE_3D_SOUND
#else
            if (!maInScene || ma.musicSpatialBlendType == MasterAudio.AllMusicSpatialBlendType.AllowDifferentPerController)
            {
                var newMusicSpatialType = (MasterAudio.ItemSpatialBlendType)EditorGUILayout.EnumPopup("Spatial Blend Rule", controller.spatialBlendType);
                if (newMusicSpatialType != controller.spatialBlendType)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Spatial Blend Rule");
                    controller.spatialBlendType = newMusicSpatialType;
                    if (Application.isPlaying)
                    {
                        controller.SetSpatialBlend();
                    }
                }

                switch (controller.spatialBlendType)
                {
                    case MasterAudio.ItemSpatialBlendType.ForceToCustom:
                        EditorGUI.indentLevel = 1;
                        DTGUIHelper.ShowLargeBarAlert(MasterAudioInspector.SpatialBlendSliderText);
                        var newMusic3D = EditorGUILayout.Slider("Spatial Blend", controller.spatialBlend, 0f, 1f);
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (newMusic3D != controller.spatialBlend)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Spatial Blend");
                            controller.spatialBlend = newMusic3D;

                            if (Application.isPlaying)
                            {
                                controller.SetSpatialBlend();
                            }
                        }
                        break;
                }
            }
            else
            {
                DTGUIHelper.ShowLargeBarAlert("Spatial Blend is currently controlled globally in the Master Audio prefab.");
            }
#endif

            if (maInScene && Application.isPlaying)
            {
                MasterAudioInspector.DisplayJukebox(new List<PlaylistController> { controller }, playlistNames);
            }

            EditorGUI.indentLevel = 0;

            var newAwake = EditorGUILayout.Toggle("Start Playlist on Awake?", controller.startPlaylistOnAwake);
            if (newAwake != controller.startPlaylistOnAwake)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle Start Playlist on Awake");
                controller.startPlaylistOnAwake = newAwake;
            }

            var newShuffle = EditorGUILayout.Toggle("Shuffle Mode", controller.isShuffle);
            if (newShuffle != controller.isShuffle)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle Shuffle Mode");
                controller.isShuffle = newShuffle;
            }

            var newLoop = EditorGUILayout.Toggle("Loop Playlists", controller.loopPlaylist);
            if (newLoop != controller.loopPlaylist)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle Loop Playlists");
                controller.loopPlaylist = newLoop;
            }

            var newAuto = EditorGUILayout.Toggle("Auto advance clips", controller.isAutoAdvance);
            if (newAuto != controller.isAutoAdvance)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle Auto advance clips");
                controller.isAutoAdvance = newAuto;
            }

            DTGUIHelper.ShowColorWarning("Note: auto advance will not advance past a looped track.");

            DTGUIHelper.StartGroupHeader();

            var newUse = EditorGUILayout.BeginToggleGroup(" Initialized Event", controller.initializedEventExpanded);
            if (newUse != controller.initializedEventExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle expand Initialized Event");
                controller.initializedEventExpanded = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (controller.initializedEventExpanded)
            {
                DTGUIHelper.ShowColorWarning("When this Playlist Controller is done initializing, fire Custom Event below.");

                if (maInScene)
                {
                    var existingIndex = _customEventNames.IndexOf(controller.initializedCustomEvent);

                    int? customEventIndex = null;

                    EditorGUI.indentLevel = 0;

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
                    else if (existingIndex == -1 && controller.initializedCustomEvent == MasterAudio.NoGroupName)
                    {
                        customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    }
                    else
                    { // non-match
                        noMatch = true;
                        var newEventName = EditorGUILayout.TextField("Custom Event Name", controller.initializedCustomEvent);
                        if (newEventName != controller.initializedCustomEvent)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event Name");
                            controller.initializedCustomEvent = newEventName;
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (customEventIndex.Value == -1)
                        {
                            controller.initializedCustomEvent = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.initializedCustomEvent = _customEventNames[customEventIndex.Value];
                        }
                    }
                }
                else
                {
                    var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", controller.initializedCustomEvent);
                    if (newCustomEvent != controller.initializedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "Custom Event Name");
                        controller.initializedCustomEvent = newCustomEvent;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            newUse = EditorGUILayout.BeginToggleGroup(" Crossfade Started Event", controller.crossfadeStartedExpanded);
            if (newUse != controller.crossfadeStartedExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle expand Crossfade Started Event");
                controller.crossfadeStartedExpanded = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (controller.crossfadeStartedExpanded)
            {
                DTGUIHelper.ShowColorWarning("When this Playlist Controller starts a crossfade, fire Custom Event below.");

                if (maInScene)
                {
                    var existingIndex = _customEventNames.IndexOf(controller.crossfadeStartedCustomEvent);

                    int? customEventIndex = null;

                    EditorGUI.indentLevel = 0;

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
                    else if (existingIndex == -1 && controller.crossfadeStartedCustomEvent == MasterAudio.NoGroupName)
                    {
                        customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    }
                    else
                    { // non-match
                        noMatch = true;
                        var newEventName = EditorGUILayout.TextField("Custom Event Name", controller.crossfadeStartedCustomEvent);
                        if (newEventName != controller.crossfadeStartedCustomEvent)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event Name");
                            controller.crossfadeStartedCustomEvent = newEventName;
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (customEventIndex.Value == -1)
                        {
                            controller.crossfadeStartedCustomEvent = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.crossfadeStartedCustomEvent = _customEventNames[customEventIndex.Value];
                        }
                    }
                }
                else
                {
                    var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", controller.crossfadeStartedCustomEvent);
                    if (newCustomEvent != controller.crossfadeStartedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "Custom Event Name");
                        controller.crossfadeStartedCustomEvent = newCustomEvent;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            newUse = EditorGUILayout.BeginToggleGroup(" Playlist Started Event", controller.playlistStartedEventExpanded);
            if (newUse != controller.playlistStartedEventExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle expand Playlist Started Event");
                controller.playlistStartedEventExpanded = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (controller.playlistStartedEventExpanded)
            {
                DTGUIHelper.ShowColorWarning("When this Playlist Controller starts a Playlist, fire Custom Event below.");

                if (maInScene)
                {
                    var existingIndex = _customEventNames.IndexOf(controller.playlistStartedCustomEvent);

                    int? customEventIndex = null;

                    EditorGUI.indentLevel = 0;

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
                    else if (existingIndex == -1 && controller.playlistStartedCustomEvent == MasterAudio.NoGroupName)
                    {
                        customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    }
                    else
                    { // non-match
                        noMatch = true;
                        var newEventName = EditorGUILayout.TextField("Custom Event Name", controller.playlistStartedCustomEvent);
                        if (newEventName != controller.playlistStartedCustomEvent)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event Name");
                            controller.playlistStartedCustomEvent = newEventName;
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (customEventIndex.Value == -1)
                        {
                            controller.playlistStartedCustomEvent = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.playlistStartedCustomEvent = _customEventNames[customEventIndex.Value];
                        }
                    }
                }
                else
                {
                    var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", controller.playlistStartedCustomEvent);
                    if (newCustomEvent != controller.playlistStartedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "Custom Event Name");
                        controller.playlistStartedCustomEvent = newCustomEvent;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            newUse = EditorGUILayout.BeginToggleGroup(" Playlist Ended Event", controller.playlistEndedEventExpanded);
            if (newUse != controller.playlistEndedEventExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle expand Playlist Ended Event");
                controller.playlistEndedEventExpanded = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (controller.playlistEndedEventExpanded)
            {
                DTGUIHelper.ShowColorWarning("When this Playlist Controller ends a Playlist (last song ends), fire Custom Event below.");

                if (maInScene)
                {
                    var existingIndex = _customEventNames.IndexOf(controller.playlistEndedCustomEvent);

                    int? customEventIndex = null;

                    EditorGUI.indentLevel = 0;

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
                    else if (existingIndex == -1 && controller.playlistEndedCustomEvent == MasterAudio.NoGroupName)
                    {
                        customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    }
                    else
                    { // non-match
                        noMatch = true;
                        var newEventName = EditorGUILayout.TextField("Custom Event Name", controller.playlistEndedCustomEvent);
                        if (newEventName != controller.playlistEndedCustomEvent)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event Name");
                            controller.playlistEndedCustomEvent = newEventName;
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (customEventIndex.Value == -1)
                        {
                            controller.playlistEndedCustomEvent = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.playlistEndedCustomEvent = _customEventNames[customEventIndex.Value];
                        }
                    }
                }
                else
                {
                    var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", controller.playlistEndedCustomEvent);
                    if (newCustomEvent != controller.playlistEndedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "Custom Event Name");
                        controller.playlistEndedCustomEvent = newCustomEvent;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            newUse = EditorGUILayout.BeginToggleGroup(" Song Changed Event", controller.songChangedEventExpanded);
            if (newUse != controller.songChangedEventExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle expand Song Changed Event");
                controller.songChangedEventExpanded = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (controller.songChangedEventExpanded)
            {
                DTGUIHelper.ShowColorWarning("When song changes, fire Custom Event below.");

                if (maInScene)
                {
                    var existingIndex = _customEventNames.IndexOf(controller.songChangedCustomEvent);

                    int? customEventIndex = null;

                    EditorGUI.indentLevel = 0;

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
                    else if (existingIndex == -1 && controller.songChangedCustomEvent == MasterAudio.NoGroupName)
                    {
                        customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    }
                    else
                    { // non-match
                        noMatch = true;
                        var newEventName = EditorGUILayout.TextField("Custom Event Name", controller.songChangedCustomEvent);
                        if (newEventName != controller.songChangedCustomEvent)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event Name");
                            controller.songChangedCustomEvent = newEventName;
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (customEventIndex.Value == -1)
                        {
                            controller.songChangedCustomEvent = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.songChangedCustomEvent = _customEventNames[customEventIndex.Value];
                        }
                    }
                }
                else
                {
                    var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", controller.songChangedCustomEvent);
                    if (newCustomEvent != controller.songChangedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "Custom Event Name");
                        controller.songChangedCustomEvent = newCustomEvent;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            newUse = EditorGUILayout.BeginToggleGroup(" Song Ended Event", controller.songEndedEventExpanded);
            if (newUse != controller.songEndedEventExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle expand Song Ended Event");
                controller.songEndedEventExpanded = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (controller.songEndedEventExpanded)
            {
                DTGUIHelper.ShowColorWarning("When a song ends, fire Custom Event below.");

                if (maInScene)
                {
                    var existingIndex = _customEventNames.IndexOf(controller.songEndedCustomEvent);

                    int? customEventIndex = null;

                    EditorGUI.indentLevel = 0;

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
                    else if (existingIndex == -1 && controller.songEndedCustomEvent == MasterAudio.NoGroupName)
                    {
                        customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    }
                    else
                    { // non-match
                        noMatch = true;
                        var newEventName = EditorGUILayout.TextField("Custom Event Name", controller.songEndedCustomEvent);
                        if (newEventName != controller.songEndedCustomEvent)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event Name");
                            controller.songEndedCustomEvent = newEventName;
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (customEventIndex.Value == -1)
                        {
                            controller.songEndedCustomEvent = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.songEndedCustomEvent = _customEventNames[customEventIndex.Value];
                        }
                    }
                }
                else
                {
                    var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", controller.songEndedCustomEvent);
                    if (newCustomEvent != controller.songEndedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "Custom Event Name");
                        controller.songEndedCustomEvent = newCustomEvent;
                    }
                }
            }
            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            newUse = EditorGUILayout.BeginToggleGroup(" Song Looped Event", controller.songLoopedEventExpanded);
            if (newUse != controller.songLoopedEventExpanded)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "toggle expand Song Looped Event");
                controller.songLoopedEventExpanded = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (controller.songLoopedEventExpanded)
            {
                DTGUIHelper.ShowColorWarning("When this Playlist Controller loops a song, fire Custom Event below.");

                if (maInScene)
                {
                    var existingIndex = _customEventNames.IndexOf(controller.songLoopedCustomEvent);

                    int? customEventIndex = null;

                    EditorGUI.indentLevel = 0;

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
                    else if (existingIndex == -1 && controller.songLoopedCustomEvent == MasterAudio.NoGroupName)
                    {
                        customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                    }
                    else
                    { // non-match
                        noMatch = true;
                        var newEventName = EditorGUILayout.TextField("Custom Event Name", controller.songLoopedCustomEvent);
                        if (newEventName != controller.songLoopedCustomEvent)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event Name");
                            controller.songLoopedCustomEvent = newEventName;
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
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "change Custom Event");
                        }
                        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                        if (customEventIndex.Value == -1)
                        {
                            controller.songLoopedCustomEvent = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            controller.songLoopedCustomEvent = _customEventNames[customEventIndex.Value];
                        }
                    }
                }
                else
                {
                    var newCustomEvent = EditorGUILayout.TextField("Custom Event Name", controller.songLoopedCustomEvent);
                    if (newCustomEvent != controller.songLoopedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, controller, "Custom Event Name");
                        controller.songLoopedCustomEvent = newCustomEvent;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();
            //DTGUIHelper.AddSpaceForNonU5(2);

            if (GUI.changed || isDirty)
            {
                EditorUtility.SetDirty(target);
            }

            //DrawDefaultInspector();
        }
    }
}