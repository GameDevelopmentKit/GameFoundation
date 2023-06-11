using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(DynamicSoundGroup))]
    // ReSharper disable once CheckNamespace
    public class DynamicSoundGroupInspector : Editor
    {
        private DynamicSoundGroup _group;
        private bool _isValid = true;
        private List<string> _groupNames;
        private DynamicSoundGroupCreator _dgsc;
        private List<string> _customEventNames;

        // ReSharper disable once FunctionComplexityOverflow
        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel = 0;
            var isDirty = false;
            _isValid = true;

            _group = (DynamicSoundGroup)target;

            _group = RescanChildren(_group);

            if (_group == null)
            {
                return;
            }

            GameObject dgscGO = null;

            var theParent = _group.transform.parent;
            if (theParent != null)
            {
                dgscGO = theParent.gameObject;
            }

            if (dgscGO == null)
            {
                DTGUIHelper.ShowRedError("This prefab must have a GameObject above it. Prefab broken.");
                _isValid = false;
            }

            var previewLang = SystemLanguage.English;

            if (!_isValid)
            {
                return;
            }

            if (dgscGO != null)
            {
                _dgsc = dgscGO.GetComponent<DynamicSoundGroupCreator>();
            }
            if (_dgsc != null)
            {
                previewLang = _dgsc.previewLanguage;
            }
            AudioSource previewer = null;

            var ma = MasterAudio.Instance;
            var maInScene = ma != null;

            _customEventNames = new List<string>();

            if (maInScene)
            {
                _groupNames = ma.GroupNames;
                _groupNames.Remove(_group.name);
                _customEventNames = ma.CustomEventNames;
            }
            else
            {
                _customEventNames = MasterAudio.CustomEventHardCodedNames;
            }

            var eventAdded = false;
            if (_dgsc != null)
            { // in SGO, this is null
                for (var i = 0; i < _dgsc.customEventsToCreate.Count; i++)
                {
                    var evt = _dgsc.customEventsToCreate[i];
                    if (_customEventNames.Contains(evt.EventName))
                    {
                        continue;
                    }

                    eventAdded = true;
                    _customEventNames.Add(evt.EventName);
                }
            }

            if (eventAdded)
            {
                _customEventNames.Sort();
                if (_customEventNames.Count > 1)
                {
                    _customEventNames.Insert(0, _customEventNames[1]);
                }
            }

            if (MasterAudioInspectorResources.LogoTexture != null)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.contentColor = DTGUIHelper.BrightButtonColor;
            if (GUILayout.Button(new GUIContent("Up to Parent", "Select Group in Hierarchy"), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                Selection.activeObject = _group.transform.parent.gameObject;
            }
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DTGUIHelper.HelpHeader("https://www.dtdevtools.com/docs/masteraudio/SoundGroups.htm");

            var newVol = DTGUIHelper.DisplayVolumeField(_group.groupMasterVolume, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, 0f, true, "Group Master Volume");
            if (newVol != _group.groupMasterVolume)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Group Master Volume");
                _group.groupMasterVolume = newVol;
            }

            DTGUIHelper.ShowColorWarning("The Spatial Blend Rule below will only be used if the Master Audio prefab allows.");

            var newSpatialType = (MasterAudio.ItemSpatialBlendType)EditorGUILayout.EnumPopup("Spatial Blend Rule", _group.spatialBlendType);
            if (newSpatialType != _group.spatialBlendType)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Spatial Blend Rule");
                _group.spatialBlendType = newSpatialType;
            }

#if DISABLE_3D_SOUND
#else
            switch (_group.spatialBlendType)
            {
                case MasterAudio.ItemSpatialBlendType.ForceToCustom:
                    EditorGUI.indentLevel = 1;
                    DTGUIHelper.ShowLargeBarAlert(MasterAudioInspector.SpatialBlendSliderText);
                    var newBlend = EditorGUILayout.Slider("Spatial Blend", _group.spatialBlend, 0f, 1f);
                    if (newBlend != _group.spatialBlend)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Spatial Blend");
                        _group.spatialBlend = newBlend;
                    }
                    break;
            }
#endif

            EditorGUI.indentLevel = 0;

            DTGUIHelper.ShowColorWarning("The Group Play Rule below will only be used if the Master Audio prefab allows.");

            var newPlayType = (MasterAudio.DefaultGroupPlayType)EditorGUILayout.EnumPopup("Group Play Rule", _group.groupPlayType);
            if (newPlayType != _group.groupPlayType)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "Change Group Play Rule");
                _group.groupPlayType = newPlayType;
            }

            EditorGUILayout.BeginHorizontal();
            var newTargetGone = (MasterAudioGroup.TargetDespawnedBehavior)EditorGUILayout.EnumPopup("Caller Despawned Mode", _group.targetDespawnedBehavior);
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/SoundGroups.htm#CallerDespawned");
            EditorGUILayout.EndHorizontal();
            if (newTargetGone != _group.targetDespawnedBehavior)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "Change Caller Despawned Mode");
                _group.targetDespawnedBehavior = newTargetGone;
            }

#if ADDRESSABLES_ENABLED
        var newDelay = EditorGUILayout.IntSlider(new GUIContent("Unused Addressable Life (sec)", "To avoid reloading frequently used Addressables, you can keep them loaded when not in use for up to X seconds. Playing the Addressable again will reset the stopwatch back to zero when its finished playing."), _group.addressableUnusedSecondsLifespan, 0, 1800);
        if (newDelay != _group.addressableUnusedSecondsLifespan) {
            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "Change Unused Addressable Life (sec)");
            _group.addressableUnusedSecondsLifespan = newDelay;
        }
#endif

            if (_group.targetDespawnedBehavior == MasterAudioGroup.TargetDespawnedBehavior.FadeOut)
            {
                EditorGUI.indentLevel = 1;
                var newFade = EditorGUILayout.Slider("Fade Out Time", _group.despawnFadeTime, .1f, 20f);
                if (newFade != _group.despawnFadeTime)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "Change Called Despawned Fade Out Time");
                    _group.despawnFadeTime = newFade;
                }
            }

            EditorGUI.indentLevel = 0;

            if (!maInScene || ma.prioritizeOnDistance)
            {
                var newContinual = EditorGUILayout.Toggle("Use Clip Age Priority", _group.useClipAgePriority);
                if (newContinual != _group.useClipAgePriority)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Use Clip Age Priority");
                    _group.useClipAgePriority = newContinual;
                }
            }

            if (!maInScene || ma.prioritizeOnDistance)
            {
                var hiPri = EditorGUILayout.Toggle("Always Highest Priority", _group.alwaysHighestPriority);
                if (hiPri != _group.alwaysHighestPriority)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Always Highest Priority");
                    _group.alwaysHighestPriority = hiPri;
                }
            }

            var newComments = EditorGUILayout.TextField("Comments (For You)", _group.comments);
            if (_group.comments != newComments)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Comments");
                _group.comments = newComments;
            }

            var newPausedPlay = EditorGUILayout.Toggle("Ignore Listener Pause", _group.ignoreListenerPause);
            if (newPausedPlay != _group.ignoreListenerPause)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Ignore Listener Pause");
                _group.ignoreListenerPause = newPausedPlay;
            }

            if (_group.busIndex >= MasterAudio.HardCodedBusOptions)
            {
                var newIsInterruptible = EditorGUILayout.Toggle(
                    new GUIContent("Uninterruptible",
                        "Making this Group Uninterruptible means it has max Importance and its Variations must play their entire duration. No other Sound Group will interrupt it when requested to play on the same Bus when Max Voices is reached."),
                    _group.isUninterruptible);
                if (newIsInterruptible != _group.isUninterruptible)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Uninterruptible");
                    _group.isUninterruptible = newIsInterruptible;
                }

                if (!_group.isUninterruptible)
                {
                    var newImportance = EditorGUILayout.IntSlider(
                        new GUIContent("Importance",
                            "In Dialog setting, only Variations of equal or higher importance may interrupt this Variation."),
                        _group.importance, 0, 10);
                    if (newImportance != _group.importance)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Importance");
                        _group.importance = newImportance;
                    }
                }
            }

            var newLog = EditorGUILayout.Toggle("Log Sounds", _group.logSound);
            if (newLog != _group.logSound)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Log Sounds");
                _group.logSound = newLog;
            }

            var showOcclusionSettings = false;

            if (!maInScene || (MasterAudio.Instance.useOcclusion && MasterAudio.Instance.occlusionSelectType == MasterAudio.OcclusionSelectionType.TurnOnPerBusOrGroup))
            {
                DTGUIHelper.ShowLargeBarAlert("The Occlusion settings below will only be used if the Master Audio prefab is set to allow Occlusion.");
                showOcclusionSettings = true;
            }
            else if (MasterAudio.Instance.useOcclusion)
            {
                showOcclusionSettings = true;
            }

            if (showOcclusionSettings)
            {
                DTGUIHelper.StartGroupHeader();
                EditorGUILayout.BeginHorizontal();

                var showActivateOcclusion = false;

                var showOverrides = false;

                if (maInScene)
                {
                    switch (MasterAudio.Instance.occlusionSelectType)
                    {
                        case MasterAudio.OcclusionSelectionType.TurnOnPerBusOrGroup:
                            showActivateOcclusion = true;
                            break;
                        default:
                        case MasterAudio.OcclusionSelectionType.AllGroups:
                            GUILayout.Label("Occlusion: On");
                            GUILayout.Label(new GUIContent(MasterAudioInspectorResources.ReadyTexture,
                                "Occlusion turned on for all Groups"), EditorStyles.toolbarButton, GUILayout.Width(24));
                            showOverrides = true;
                            break;
                    }
                }
                else
                {
                    showActivateOcclusion = true;
                }

                if (showActivateOcclusion)
                {
                    var newOcc = GUILayout.Toggle(_group.isUsingOcclusion, " Use Occlusion");
                    if (newOcc != _group.isUsingOcclusion)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Use Occlusion");
                        _group.isUsingOcclusion = newOcc;
                    }

                    if (_group.isUsingOcclusion)
                    {
                        showOverrides = true;
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                if (showOverrides)
                {
                    var newOverride = EditorGUILayout.Toggle("Override Ray Cast Offset", _group.willOcclusionOverrideRaycastOffset);
                    if (newOverride != _group.willOcclusionOverrideRaycastOffset)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Override Ray Cast Offset");
                        _group.willOcclusionOverrideRaycastOffset = newOverride;
                    }

                    EditorGUI.indentLevel = 1;
                    if (_group.willOcclusionOverrideRaycastOffset)
                    {
                        var newOffset = EditorGUILayout.Slider("Ray Cast Origin Offset", _group.occlusionRayCastOffset, 0f, 500f);
                        if (newOffset != _group.occlusionRayCastOffset)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Override Ray Cast Origin Offset");
                            _group.occlusionRayCastOffset = newOffset;
                        }
                    }

                    EditorGUI.indentLevel = 0;
                    newOverride = EditorGUILayout.Toggle("Override Frequencies", _group.willOcclusionOverrideFrequencies);
                    if (newOverride != _group.willOcclusionOverrideFrequencies)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Override Frequencies");
                        _group.willOcclusionOverrideFrequencies = newOverride;
                    }

                    if (_group.willOcclusionOverrideFrequencies)
                    {
                        EditorGUI.indentLevel = 1;
                        var newMaxCutoff = EditorGUILayout.Slider(new GUIContent("Max Occl. Cutoff Freq.", "This frequency will be used for cutoff for maximum occlusion (occluded nearest to sound emitter)"),
                            _group.occlusionMaxCutoffFreq, AudioUtil.DefaultMaxOcclusionCutoffFrequency, _group.occlusionMinCutoffFreq);
                        if (newMaxCutoff != _group.occlusionMaxCutoffFreq)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Max Occl. Cutoff Freq.");
                            _group.occlusionMaxCutoffFreq = newMaxCutoff;
                        }

                        var newMinCutoff = EditorGUILayout.Slider(new GUIContent("Min Occl. Cutoff Freq.", "This frequency will be used for no occlusion (nothing blocking the sound emitter from the AudioListener)"),
                            _group.occlusionMinCutoffFreq, _group.occlusionMaxCutoffFreq, AudioUtil.DefaultMinOcclusionCutoffFrequency);
                        if (newMinCutoff != _group.occlusionMinCutoffFreq)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Min Occl. Cutoff Freq.");
                            _group.occlusionMinCutoffFreq = newMinCutoff;
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel = 0;

            DTGUIHelper.StartGroupHeader();

            var newVarSequence = (MasterAudioGroup.VariationSequence)EditorGUILayout.EnumPopup("Variation Sequence", _group.curVariationSequence);
            if (newVarSequence != _group.curVariationSequence)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Variation Sequence");
                _group.curVariationSequence = newVarSequence;
            }

            EditorGUILayout.EndVertical();

            switch (_group.curVariationSequence)
            {
                case MasterAudioGroup.VariationSequence.TopToBottom:
                    if (!Application.isPlaying)
                    {
                        DTGUIHelper.ShowColorWarning("Previewing ignores the Top To Bottom setting and will do random Variations.");
                    }

                    var newUseInactive = EditorGUILayout.BeginToggleGroup(" Refill Variation Pool After Inactive Time", _group.useInactivePeriodPoolRefill);
                    if (newUseInactive != _group.useInactivePeriodPoolRefill)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Inactive Refill");
                        _group.useInactivePeriodPoolRefill = newUseInactive;
                    }

                    EditorGUI.indentLevel = 1;
                    var newInactivePeriod = EditorGUILayout.Slider(" Inactive Time (sec)", _group.inactivePeriodSeconds, .2f, 30f);
                    if (newInactivePeriod != _group.inactivePeriodSeconds)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Inactive Time");
                        _group.inactivePeriodSeconds = newInactivePeriod;
                    }

                    EditorGUILayout.EndToggleGroup();
                    break;
                case MasterAudioGroup.VariationSequence.Randomized:
                    if (_group.groupVariations.Count >= MasterAudioGroup.MinNoRepeatVariations)
                    {
                        var newRepeat = EditorGUILayout.Toggle("No-Repeat On Refill", _group.useNoRepeatRefill);
                        if (newRepeat != _group.useNoRepeatRefill)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change No-Repeat On Refill");
                            _group.useNoRepeatRefill = newRepeat;
                        }
                    }
                    else
                    {
                        DTGUIHelper.ShowLargeBarAlert("No-Repeat is disabled unless you have at least 3 Variations.");
                    }
                    break;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel = 0;
            DTGUIHelper.StartGroupHeader();
            EditorGUILayout.BeginHorizontal();
            var newVarMode = (MasterAudioGroup.VariationMode)EditorGUILayout.EnumPopup("Variation Mode", _group.curVariationMode);
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/SoundGroups.htm#VarMode");
            EditorGUILayout.EndHorizontal();
            if (newVarMode != _group.curVariationMode)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Variation Mode");
                _group.curVariationMode = newVarMode;
            }
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel = 0;
            switch (_group.curVariationMode)
            {
                case MasterAudioGroup.VariationMode.LoopedChain:
                    DTGUIHelper.ShowColorWarning("In this mode, only one Variation can be played at a time.");

                    var newLoopMode = (MasterAudioGroup.ChainedLoopLoopMode)EditorGUILayout.EnumPopup("Loop Mode", _group.chainLoopMode);
                    if (newLoopMode != _group.chainLoopMode)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Loop Mode");
                        _group.chainLoopMode = newLoopMode;
                    }

                    if (_group.chainLoopMode == MasterAudioGroup.ChainedLoopLoopMode.NumberOfLoops)
                    {
                        var newLoopCount = EditorGUILayout.IntSlider("Number of Loops", _group.chainLoopNumLoops, 1, 500);
                        if (newLoopCount != _group.chainLoopNumLoops)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Number of Loops");
                            _group.chainLoopNumLoops = newLoopCount;
                        }
                    }

                    var newDelayMin = EditorGUILayout.Slider("Clip Change Delay Min (sec)", _group.chainLoopDelayMin, 0f, 300f);
                    if (newDelayMin != _group.chainLoopDelayMin)
                    {
                        if (_group.chainLoopDelayMax < newDelayMin)
                        {
                            _group.chainLoopDelayMax = newDelayMin;
                        }
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Chained Clip Delay Min");
                        _group.chainLoopDelayMin = newDelayMin;
                    }

                    var newDelayMax = EditorGUILayout.Slider("Clip Change Delay Max (sec)", _group.chainLoopDelayMax, 0f, 300f);
                    if (newDelayMax != _group.chainLoopDelayMax)
                    {
                        if (newDelayMax < _group.chainLoopDelayMin)
                        {
                            newDelayMax = _group.chainLoopDelayMin;
                        }
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Chained Clip Delay Max");
                        _group.chainLoopDelayMax = newDelayMax;
                    }
                    break;
                case MasterAudioGroup.VariationMode.Normal:
                    EditorGUILayout.BeginHorizontal();
                    var newRetrigger = EditorGUILayout.IntSlider("Retrigger Percentage", _group.retriggerPercentage, 0, 100);
                    DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/SoundGroups.htm#Retrigger");
                    EditorGUILayout.EndHorizontal();
                    if (newRetrigger != _group.retriggerPercentage)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Retrigger Percentage");
                        _group.retriggerPercentage = newRetrigger;
                    }

                    var newLimitPoly = EditorGUILayout.Toggle("Limit Polyphony", _group.limitPolyphony);
                    if (newLimitPoly != _group.limitPolyphony)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Limit Polyphony");
                        _group.limitPolyphony = newLimitPoly;
                    }
                    if (_group.limitPolyphony)
                    {
                        var maxVoices = 0;
                        foreach (var variation in _group.groupVariations)
                        {
                            maxVoices += variation.weight;
                        }

                        var newVoiceLimit = EditorGUILayout.IntSlider("Polyphony Voice Limit", _group.voiceLimitCount, 1, maxVoices);
                        if (newVoiceLimit != _group.voiceLimitCount)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Polyphony Voice Limit");
                            _group.voiceLimitCount = newVoiceLimit;
                        }
                    }

                    var newLimitMode = (MasterAudioGroup.LimitMode)EditorGUILayout.EnumPopup("Replay Limit Mode", _group.limitMode);
                    if (newLimitMode != _group.limitMode)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Replay Limit Mode");
                        _group.limitMode = newLimitMode;
                    }

                    switch (_group.limitMode)
                    {
                        case MasterAudioGroup.LimitMode.FrameBased:
                            var newFrameLimit = EditorGUILayout.IntSlider("Min Frames Between", _group.limitPerXFrames, 1, 120);
                            if (newFrameLimit != _group.limitPerXFrames)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Min Frames Between");
                                _group.limitPerXFrames = newFrameLimit;
                            }
                            break;
                        case MasterAudioGroup.LimitMode.TimeBased:
                            var newMinTime = EditorGUILayout.Slider("Min Seconds Between", _group.minimumTimeBetween, 0.05f, 10f);
                            if (newMinTime != _group.minimumTimeBetween)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Min Seconds Between");
                                _group.minimumTimeBetween = newMinTime;
                            }
                            break;
                    }
                    break;
                case MasterAudioGroup.VariationMode.Dialog:
                    DTGUIHelper.ShowColorWarning("In this mode, only one Variation can be played at a time. Use the 'Importance' field in each Variation to control which Variations can interrupt which.");

                    var newUseDialog = EditorGUILayout.Toggle("Dialog Custom Fade?", _group.useDialogFadeOut);
                    if (newUseDialog != _group.useDialogFadeOut)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Dialog Custom Fade?");
                        _group.useDialogFadeOut = newUseDialog;
                    }

                    if (_group.useDialogFadeOut)
                    {
                        var newFadeTime = EditorGUILayout.Slider("Custom Fade Out Time", _group.dialogFadeOutTime, 0.1f, 20f);
                        if (newFadeTime != _group.dialogFadeOutTime)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Custom Fade Out Time");
                            _group.dialogFadeOutTime = newFadeTime;
                        }
                    }
                    break;
            }
            EditorGUILayout.EndVertical();

            DTGUIHelper.StartGroupHeader();
            EditorGUI.indentLevel = 0;

            var canCopy = false;

            var linkedlabel = "Linked Group Settings";
            if (!_group.expandLinkedGroups)
            {
                linkedlabel += " (" + (_group.childSoundGroups.Count + _group.endLinkedGroups.Count) + ")";
            }

            EditorGUI.indentLevel = 1;
            var newActive = DTGUIHelper.Foldout(_group.expandLinkedGroups, linkedlabel);
            if (newActive != _group.expandLinkedGroups)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Linked Group Settings");
                _group.expandLinkedGroups = newActive;
            }

            int? stopIndexToDelete = null;
            int? startIndexToDelete = null;

            if (_group.expandLinkedGroups)
            {
                EditorGUI.indentLevel = 0;
                EditorGUILayout.EndVertical();

                var hasNoStartLinkedGroups = _group.childSoundGroups.Count == 0;
                var hasNoEndLinkedGroups = _group.endLinkedGroups.Count == 0;

                if (hasNoStartLinkedGroups)
                {
                    DTGUIHelper.ShowLargeBarAlert("You have no 'Start' Linked Groups set up.");
                }
                else
                {
                    GUILayout.Label("Groups to play when '" + _group.name + "' Variations start play", EditorStyles.boldLabel);
                    if (_group.childSoundGroups.Count > 1)
                    {
                        var newType = (MasterAudio.LinkedGroupSelectionType)EditorGUILayout.EnumPopup("Linked Groups To Play", _group.linkedStartGroupSelectionType);
                        if (newType != _group.linkedStartGroupSelectionType)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Linked Groups To Play");
                            _group.linkedStartGroupSelectionType = newType;
                        }
                    }
                }

                EditorGUI.indentLevel = 0;
                for (var i = 0; i < _group.childSoundGroups.Count; i++)
                {
                    var aGroup = _group.childSoundGroups[i];
                    GUI.contentColor = Color.white;
                    if (maInScene)
                    {
                        var existingIndex = _groupNames.IndexOf(aGroup);

                        int? groupIndex = null;

                        EditorGUI.indentLevel = 0;

                        var noGroup = false;
                        var noMatch = false;

                        if (existingIndex >= 1)
                        {
                            EditorGUILayout.BeginHorizontal();
                            groupIndex = EditorGUILayout.Popup("Sound Group", existingIndex, _groupNames.ToArray());
                            if (existingIndex == 1)
                            {
                                noGroup = true;
                            }

                            if (groupIndex > MasterAudio.HardCodedBusOptions - 1)
                            {
                                var buttonPressed = DTGUIHelper.AddSettingsButton("Linked Sound Group");
                                if (buttonPressed == DTGUIHelper.DTFunctionButtons.Go)
                                {
                                    var grp = _groupNames[existingIndex];
                                    var trs = MasterAudio.FindGroupTransform(grp);
                                    if (trs != null)
                                    {
                                        Selection.activeObject = trs;
                                    }
                                }
                            }
                            var deletePressed = DTGUIHelper.AddDeleteIcon(false, "Linked Group");
                            if (deletePressed == DTGUIHelper.DTFunctionButtons.Remove)
                            {
                                startIndexToDelete = i;
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                        else if (existingIndex == -1 && aGroup == MasterAudio.NoGroupName)
                        {
                            groupIndex = EditorGUILayout.Popup("Sound Group", existingIndex, _groupNames.ToArray());
                        }
                        else
                        { // non-match
                            noMatch = true;
                            var newSound = EditorGUILayout.TextField("Sound Group", aGroup);
                            if (newSound != aGroup)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Sound Group");
                                _group.childSoundGroups[i] = newSound;
                            }

                            var newIndex = EditorGUILayout.Popup("All Sound Groups", -1, _groupNames.ToArray());
                            if (newIndex >= 0)
                            {
                                groupIndex = newIndex;
                            }
                        }

                        if (noGroup)
                        {
                            DTGUIHelper.ShowRedError("No Sound Group specified.");
                        }
                        else if (noMatch)
                        {
                            DTGUIHelper.ShowRedError("Sound Group found no match. Type in or choose one.");
                        }

                        if (!groupIndex.HasValue)
                        {
                            continue;
                        }

                        if (existingIndex != groupIndex.Value)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Sound Group");
                        }
                        if (groupIndex.Value == -1)
                        {
                            _group.childSoundGroups[i] = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            _group.childSoundGroups[i] = _groupNames[groupIndex.Value];
                        }
                    }
                    else
                    {
                        var newSType = EditorGUILayout.TextField("Sound Group", aGroup);
                        if (newSType == aGroup)
                        {
                            continue;
                        }

                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Sound Group");
                        _group.childSoundGroups[i] = newSType;
                    }
                }

                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Add 'Start' Linked Group"), EditorStyles.toolbarButton,
                    GUILayout.Width(150)))
                {
                    _group.childSoundGroups.Add(MasterAudio.NoGroupName);
                }

                EditorGUILayout.EndHorizontal();
                DTGUIHelper.VerticalSpace(2);

                GUI.contentColor = Color.white;
                if (hasNoEndLinkedGroups)
                {
                    DTGUIHelper.ShowLargeBarAlert("You have no 'Stop' Linked Groups set up.");
                }
                else
                {
                    GUILayout.Label("Groups to play when '" + _group.name + "' Variations stop", EditorStyles.boldLabel);
                    if (_group.endLinkedGroups.Count > 1)
                    {
                        var newType = (MasterAudio.LinkedGroupSelectionType)EditorGUILayout.EnumPopup("Linked Groups To Play", _group.linkedStopGroupSelectionType);
                        if (newType != _group.linkedStopGroupSelectionType)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Linked Groups To Play");
                            _group.linkedStopGroupSelectionType = newType;
                        }
                    }
                }

                EditorGUI.indentLevel = 0;
                for (var i = 0; i < _group.endLinkedGroups.Count; i++)
                {
                    var aGroup = _group.endLinkedGroups[i];
                    if (maInScene)
                    {
                        var existingIndex = _groupNames.IndexOf(aGroup);

                        int? groupIndex = null;

                        EditorGUI.indentLevel = 0;

                        var noGroup = false;
                        var noMatch = false;

                        if (existingIndex >= 1)
                        {
                            EditorGUILayout.BeginHorizontal();
                            groupIndex = EditorGUILayout.Popup("Sound Group", existingIndex, _groupNames.ToArray());
                            if (existingIndex == 1)
                            {
                                noGroup = true;
                            }

                            if (groupIndex > MasterAudio.HardCodedBusOptions - 1)
                            {
                                var buttonPressed = DTGUIHelper.AddSettingsButton("Linked Sound Group");
                                if (buttonPressed == DTGUIHelper.DTFunctionButtons.Go)
                                {
                                    var grp = _groupNames[existingIndex];
                                    var trs = MasterAudio.FindGroupTransform(grp);
                                    if (trs != null)
                                    {
                                        Selection.activeObject = trs;
                                    }
                                }
                            }
                            var deletePressed = DTGUIHelper.AddDeleteIcon(false, "Linked Group");
                            if (deletePressed == DTGUIHelper.DTFunctionButtons.Remove)
                            {
                                stopIndexToDelete = i;
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                        else if (existingIndex == -1 && aGroup == MasterAudio.NoGroupName)
                        {
                            groupIndex = EditorGUILayout.Popup("Sound Group", existingIndex, _groupNames.ToArray());
                        }
                        else
                        { // non-match
                            noMatch = true;
                            var newSound = EditorGUILayout.TextField("Sound Group", aGroup);
                            if (newSound != aGroup)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Sound Group");
                                _group.endLinkedGroups[i] = newSound;
                            }

                            var newIndex = EditorGUILayout.Popup("All Sound Groups", -1, _groupNames.ToArray());
                            if (newIndex >= 0)
                            {
                                groupIndex = newIndex;
                            }
                        }

                        if (noGroup)
                        {
                            DTGUIHelper.ShowRedError("No Sound Group specified.");
                        }
                        else if (noMatch)
                        {
                            DTGUIHelper.ShowRedError("Sound Group found no match. Type in or choose one.");
                        }

                        if (!groupIndex.HasValue)
                        {
                            continue;
                        }

                        if (existingIndex != groupIndex.Value)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Sound Group");
                        }
                        if (groupIndex.Value == -1)
                        {
                            _group.endLinkedGroups[i] = MasterAudio.NoGroupName;
                        }
                        else
                        {
                            _group.endLinkedGroups[i] = _groupNames[groupIndex.Value];
                        }
                    }
                    else
                    {
                        var newSType = EditorGUILayout.TextField("Sound Group", aGroup);
                        if (newSType == aGroup)
                        {
                            continue;
                        }

                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Sound Group");
                        _group.endLinkedGroups[i] = newSType;
                    }
                }

                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Add 'Stop' Linked Group"), EditorStyles.toolbarButton,
                    GUILayout.Width(150)))
                {
                    _group.endLinkedGroups.Add(MasterAudio.NoGroupName);
                }

                if (startIndexToDelete.HasValue)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "Delete Linked Group");
                    _group.childSoundGroups.RemoveAt(startIndexToDelete.Value);
                }

                if (stopIndexToDelete.HasValue)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "Delete Linked Group");
                    _group.endLinkedGroups.RemoveAt(stopIndexToDelete.Value);
                }
                EditorGUILayout.EndHorizontal();

                GUI.contentColor = Color.white;

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }



            EditorGUI.indentLevel = 0;

            DTGUIHelper.StartGroupHeader();
            var newUse = EditorGUILayout.BeginToggleGroup(" Group Played Event", _group.soundPlayedEventActive);
            if (newUse != _group.soundPlayedEventActive)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle expand Group Played Event");
                _group.soundPlayedEventActive = newUse;
            }
            DTGUIHelper.EndGroupHeader();

            GUI.color = Color.white;

            if (_group.soundPlayedEventActive)
            {
                DTGUIHelper.ShowColorWarning("When this Group plays, fire Custom Event below.");

                var existingIndex = _customEventNames.IndexOf(_group.soundPlayedCustomEvent);

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
                else if (existingIndex == -1 && _group.soundPlayedCustomEvent == MasterAudio.NoGroupName)
                {
                    customEventIndex = EditorGUILayout.Popup("Custom Event Name", existingIndex, _customEventNames.ToArray());
                }
                else
                { // non-match
                    noMatch = true;
                    var newEventName = EditorGUILayout.TextField("Custom Event Name", _group.soundPlayedCustomEvent);
                    if (newEventName != _group.soundPlayedCustomEvent)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Custom Event Name");
                        _group.soundPlayedCustomEvent = newEventName;
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
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Custom Event");
                    }
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (customEventIndex.Value == -1)
                    {
                        _group.soundPlayedCustomEvent = MasterAudio.NoGroupName;
                    }
                    else
                    {
                        _group.soundPlayedCustomEvent = _customEventNames[customEventIndex.Value];
                    }
                }
            }
            EditorGUILayout.EndToggleGroup();

            if (!Application.isPlaying)
            {
                DTGUIHelper.StartGroupHeader();
                EditorGUI.indentLevel = 1;
                EditorGUILayout.BeginHorizontal();
                var newBulk = GUILayout.Toggle(_group.copySettingsExpanded, " Bulk Edit");
                DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/SoundGroups.htm#CopySettings");
                EditorGUILayout.EndHorizontal();
                if (newBulk != _group.copySettingsExpanded)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "toggle Bulk Edit");
                    _group.copySettingsExpanded = newBulk;
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel = 0;
                GUI.color = Color.white;

                if (_group.copySettingsExpanded)
                {
                    if (_group.groupVariations.Count == 0)
                    {
                        DTGUIHelper.ShowLargeBarAlert("You currently have no Variations in this Group.");
                    }
                    else if (_group.groupVariations.Count == 1)
                    {
                        DTGUIHelper.ShowLargeBarAlert("You only have a single Variation in this Group. Nothing to copy to.");
                    }
                    else
                    {
                        canCopy = true;
                    }
                    if (canCopy)
                    {
                        var totalVars = _group.groupVariations.Count;
                        var selectedVars = GetNumChecked();
                        DTGUIHelper.ShowLargeBarAlert(selectedVars + " of " + totalVars + " Variations selected - adjustments to a selected Variation will affect all selected Variations.");
                        EditorGUILayout.BeginHorizontal();
                        GUI.contentColor = DTGUIHelper.BrightButtonColor;
                        if (GUILayout.Button("Check All", EditorStyles.toolbarButton, GUILayout.Width(80)))
                        {
                            CheckAll();
                        }
                        GUILayout.Space(10);
                        if (GUILayout.Button("Uncheck All", EditorStyles.toolbarButton, GUILayout.Width(80)))
                        {
                            UncheckAll();
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    GUI.contentColor = Color.white;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel = 0;

            int? deadChildIndex = null;

            if (!Application.isPlaying)
            {
                DTGUIHelper.StartGroupHeader();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Actions", EditorStyles.wordWrappedLabel, GUILayout.Width(50f));
                GUILayout.Space(30);
                GUI.contentColor = DTGUIHelper.BrightButtonColor;

                var buttonText = "Collapse All";
                var allCollapsed = true;

                foreach (var t in _group.groupVariations)
                {
                    if (!t.isExpanded)
                    {
                        continue;
                    }

                    allCollapsed = false;
                    break;
                }

                if (allCollapsed)
                {
                    buttonText = "Expand All";
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(buttonText), EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    isDirty = true;
                    ExpandCollapseAll(allCollapsed);
                }
                GUILayout.Space(10);

                if (GUILayout.Button(new GUIContent("Eq. Voices", "Reset Voices to one"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    isDirty = true;
                    EqualizeWeights(_group);
                }

                GUILayout.Space(10);
                if (GUILayout.Button(new GUIContent("Eq. Volumes"), EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    EqualizeVariationVolumes(_group.groupVariations);
                }

                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();

                DTGUIHelper.VerticalSpace(1);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Localization", EditorStyles.wordWrappedLabel, GUILayout.Width(80f));
                GUILayout.FlexibleSpace();
                GUI.contentColor = DTGUIHelper.BrightButtonColor;
                if (GUILayout.Button(new GUIContent("All Use Loc.", "Check the 'Use Localized Folder' checkbox for all Variations."), EditorStyles.toolbarButton, GUILayout.Width(125)))
                {
                    isDirty = true;
                    BulkUseLocalization(_group.groupVariations, true);
                }

                GUILayout.Space(10);
                if (GUILayout.Button(new GUIContent("None Use Loc.", "Uncheck the 'Use Localized Folder' checkbox for all Variations."), EditorStyles.toolbarButton, GUILayout.Width(125)))
                {
                    isDirty = true;
                    BulkUseLocalization(_group.groupVariations, false);
                }

                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();

                var newBulkMode = (MasterAudio.AudioLocation)EditorGUILayout.EnumPopup("Variation Create Mode", _group.bulkVariationMode);
                if (newBulkMode != _group.bulkVariationMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _group, "change Bulk Variation Mode");
                    _group.bulkVariationMode = newBulkMode;
                }
                switch (_group.bulkVariationMode)
                {
                    case MasterAudio.AudioLocation.ResourceFile:
                        DTGUIHelper.ShowColorWarning("Resource mode: make sure to drag from Resource folders only.");
                        break;
                }

                DTGUIHelper.EndGroupHeader();
            }

            DTGUIHelper.VerticalSpace(2);

            if (DTGUIHelper.IsPrefabInProjectView(_group.gameObject))
            {
                DTGUIHelper.ShowLargeBarAlert("You are in Project View and cannot create Variations.");
            }
            else if (DTGUIHelper.IsInPrefabMode(_group.gameObject))
            {
                DTGUIHelper.ShowLargeBarAlert("You are in Prefab Mode and cannot create Variations.");
            }
            else if (Application.isPlaying)
            {
                DTGUIHelper.ShowLargeBarAlert("You are running and cannot create Variations.");
            } else { 
                // new variation settings
                EditorGUILayout.BeginVertical();
                var anEvent = Event.current;

                GUI.color = DTGUIHelper.DragAreaColor;

                var dragArea = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
                GUI.Box(dragArea, MasterAudio.DragAudioTip + " to create Variations!");

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
                                    foreach (var assetPath in assetPaths)
                                    {
                                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(assetPath));
                                        if (clip == null)
                                        {
                                            continue;
                                        }

                                        CreateVariation(_group, clip);
                                    }

                                    continue;
                                }

                                var aClip = dragged as AudioClip;
                                if (aClip == null)
                                {
                                    continue;
                                }

                                CreateVariation(_group, aClip);
                            }
                        }
                        Event.current.Use();
                        break;
                }
                EditorGUILayout.EndVertical();
                // end new variation settings
            }

            if (_group.groupVariations.Count == 0)
            {
                DTGUIHelper.ShowRedError("You currently have no Variations.");
            }
            else
            {
                for (var i = 0; i < _group.groupVariations.Count; i++)
                {
                    var variation = _group.groupVariations[i];

                    var state = variation.isExpanded;
                    var text = variation.name;

                    DTGUIHelper.ShowCollapsibleSection(ref state, text);

                    GUI.backgroundColor = Color.white;
                    if (!state)
                    {
                        GUILayout.Space(3f);
                    }

                    if (state != variation.isExpanded)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, variation, "toggle Expand Variation");
                        variation.isExpanded = state;
                    }

                    EditorGUI.indentLevel = 0;

                    var headerStyle = new GUIStyle();
#if UNITY_2019_3_OR_NEWER
                headerStyle.margin = new RectOffset(0, 0, 0, 0);
                headerStyle.padding = new RectOffset(0, 0, 0, 0);
#else
                    headerStyle.margin = new RectOffset(0, 0, 1, 0);
                    headerStyle.padding = new RectOffset(0, 0, 1, 1);
#endif
                    headerStyle.fixedHeight = 18;

                    EditorGUILayout.BeginHorizontal(headerStyle, GUILayout.MaxWidth(50));

                    if (canCopy)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                        var newChecked = EditorGUILayout.Toggle(variation.isChecked, GUILayout.Width(16), GUILayout.Height(16));
                        if (newChecked != variation.isChecked)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, variation, "toggle check Variation");
                            variation.isChecked = newChecked;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.GearTexture, "Click to goto Variation"), EditorStyles.toolbarButton, GUILayout.Height(16), GUILayout.Width(40)))
                    {
                        Selection.activeObject = variation;
                    }

                    if (!Application.isPlaying)
                    {
                        if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.CopyTexture, "Click to clone Variation"), EditorStyles.toolbarButton, GUILayout.Height(16), GUILayout.Width(40))) {
                            CloneVariation(i);
                        }
                    }

                    var varIsDirty = false;

                    var buttonPressed = DTGUIHelper.AddDynamicGroupButtons(_group);

                    if (!Application.isPlaying)
                    {
                        if (GUILayout.Button(new GUIContent(MasterAudioInspectorResources.DeleteTexture, "Click to delete this Variation"), EditorStyles.toolbarButton, GUILayout.Height(16), GUILayout.Width(40)))
                        {
                            deadChildIndex = i;
                            isDirty = true;
                        }
                    }

                    GUILayout.Space(4);
                    EditorGUILayout.EndHorizontal();
                    DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/SoundGroups.htm#Variations");

                    switch (buttonPressed)
                    {
                        case DTGUIHelper.DTFunctionButtons.Play:
                            isDirty = true;

                            previewer = MasterAudioInspector.GetPreviewer();

                            var randPitch = SoundGroupVariationInspector.GetRandomPreviewPitch(variation);
                            var varVol = SoundGroupVariationInspector.GetRandomPreviewVolume(variation);

                            if (previewer != null)
                            {
                                MasterAudioInspector.StopPreviewer();
                                previewer.pitch = randPitch;
                            }

                            var calcVolume = varVol * variation.ParentGroup.groupMasterVolume;

                            switch (variation.audLocation)
                            {
                                case MasterAudio.AudioLocation.ResourceFile:
                                    if (previewer != null)
                                    {
                                        var fileName = AudioResourceOptimizer.GetLocalizedDynamicSoundGroupFileName(previewLang, variation.useLocalization, variation.resourceFileName);
                                        var clip = Resources.Load(fileName) as AudioClip;
                                        if (clip != null)
                                        {
                                            if (previewer != null)
                                            {
                                                DTGUIHelper.PlaySilentWakeUpPreview(previewer, clip);
                                                previewer.PlayOneShot(clip, calcVolume);
                                            }
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
                                        DTGUIHelper.PlaySilentWakeUpPreview(previewer, variation.VarAudio.clip);
                                        previewer.PlayOneShot(variation.VarAudio.clip, calcVolume);
                                    }
                                    break;
#if ADDRESSABLES_ENABLED
                            case MasterAudio.AudioLocation.Addressable:
                                DTGUIHelper.PreviewAddressable(variation.audioClipAddressable, previewer, calcVolume);
                                break;
#endif
                            }

                            break;
                        case DTGUIHelper.DTFunctionButtons.Stop:
                            MasterAudioInspector.StopPreviewer();
                            isDirty = true;
                            break;
                    }

                    EditorGUILayout.EndHorizontal();

                    GUI.backgroundColor = Color.white;

                    if (!variation.isExpanded)
                    {
                        DTGUIHelper.VerticalSpace(3);
                        continue;
                    }

                    DTGUIHelper.BeginGroupedControls();

                    if (!Application.isPlaying)
                    {
                        DTGUIHelper.ShowColorWarning(MasterAudio.PreviewText);
                    }
                    if (variation.VarAudio == null)
                    {
                        DTGUIHelper.ShowRedError(string.Format("The Variation: '{0}' has no Audio Source.", variation.name));
                        break;
                    }

                    var newAlias = EditorGUILayout.TextField("Clip Id (optional)", variation.clipAlias);

                    if (newAlias != variation.clipAlias)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Clip Id");
                        variation.clipAlias = newAlias;
                    }

                    var oldLocation = variation.audLocation;
                    var newLocation = (MasterAudio.AudioLocation)EditorGUILayout.EnumPopup("Audio Origin", variation.audLocation);
                    if (newLocation != variation.audLocation)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Audio Origin");
                        variation.audLocation = newLocation;
                    }

                    if (oldLocation != variation.audLocation && oldLocation == MasterAudio.AudioLocation.Clip)
                    {
                        if (variation.VarAudio.clip != null)
                        {
                            Debug.Log("Audio clip removed to prevent unnecessary memory usage.");
                        }
                        variation.VarAudio.clip = null;
                    }

                    switch (variation.audLocation)
                    {
                        case MasterAudio.AudioLocation.Clip:
                            var newClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", variation.VarAudio.clip, typeof(AudioClip), false);
                            if (newClip != variation.VarAudio.clip)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation.VarAudio, "change Audio Clip");
                                variation.VarAudio.clip = newClip;
                            }
                            break;
#if ADDRESSABLES_ENABLED
                    case MasterAudio.AudioLocation.Addressable:
                        var varSerialized = new SerializedObject(variation);
                        varSerialized.Update();
                        EditorGUILayout.PropertyField(varSerialized.FindProperty(nameof(DynamicGroupVariation.audioClipAddressable)), true);
                        varSerialized.ApplyModifiedProperties();

                        if (!DTGUIHelper.IsAddressableTypeValid(variation.audioClipAddressable, variation.name)) {
                            variation.audioClipAddressable = null;
                            varIsDirty = true;
                        }
                        break;
#endif
                        case MasterAudio.AudioLocation.ResourceFile:
                            EditorGUILayout.BeginVertical();
                            var anEvent = Event.current;

                            GUI.color = DTGUIHelper.DragAreaColor;
                            var dragArea = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
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

                                            var useLocalization = false;
                                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Resource Filename");

                                            var newsFilename = DTGUIHelper.GetResourcePath(aClip, ref useLocalization);

                                            variation.resourceFileName = newsFilename;
                                            variation.useLocalization = useLocalization;
                                        }
                                    }
                                    Event.current.Use();
                                    break;
                            }
                            EditorGUILayout.EndVertical();

                            var newFilename = EditorGUILayout.TextField("Resource Filename", variation.resourceFileName);
                            if (newFilename != variation.resourceFileName)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Resource Filename");
                                variation.resourceFileName = newFilename;
                            }

                            EditorGUI.indentLevel = 1;

                            var newLocal = EditorGUILayout.Toggle("Use Localized Folder", variation.useLocalization);
                            if (newLocal != variation.useLocalization)
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, variation, "toggle Use Localized Folder");
                                variation.useLocalization = newLocal;
                            }

                            break;
                    }

                    EditorGUI.indentLevel = 0;

                    switch (variation.ParentGroup.curVariationMode)
                    {
                        case MasterAudioGroup.VariationMode.Dialog:
                            var newIsInterruptible = EditorGUILayout.Toggle(new GUIContent("Uninterruptible", "In Dialog setting, making this Variation Uninterruptible means it has max Importance and must play its entire duration. No other clip will interrupt it."), variation.isUninterruptible);
                            if (newIsInterruptible != variation.isUninterruptible)
                            {
                                if (_group.copySettingsExpanded && variation.isChecked)
                                {
                                    CopyIsUninterruptible(newIsInterruptible);
                                }
                                else
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, variation, "toggle Uninterruptible");
                                    variation.isUninterruptible = newIsInterruptible;
                                }
                            }

                            if (!variation.isUninterruptible)
                            {
                                var newImportance = EditorGUILayout.IntSlider(new GUIContent("Importance", "In Dialog setting, only Variations of equal or higher importance may interrupt this Variation."), variation.importance, 0, 10);
                                if (newImportance != variation.importance)
                                {
                                    if (_group.copySettingsExpanded && variation.isChecked)
                                    {
                                        CopyImportance(newImportance);
                                    }
                                    else
                                    {
                                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, variation, "change Importance");
                                        variation.importance = newImportance;
                                    }
                                }
                            }
                            break;
                    }

                    var newProbability = EditorGUILayout.IntSlider("Probability to Play (%)", variation.probabilityToPlay, 0, 100);
                    if (newProbability != variation.probabilityToPlay)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyProbabilityToPlay(newProbability);
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, variation, "change Probability to Play (%)");
                        }
                        else
                        {
                            variation.probabilityToPlay = newProbability;
                        }
                    }

                    if (variation.probabilityToPlay < 100)
                    {
                        DTGUIHelper.ShowLargeBarAlert("Since Probability to Play is less than 100%, you will not always hear this Variation when it's selected to play.");
                    }

                    var newVolume = DTGUIHelper.DisplayVolumeField(variation.VarAudio.volume, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, 0f, true);
                    if (newVolume != variation.VarAudio.volume)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyVolumes(newVolume);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation.VarAudio, "change Volume");
                            variation.VarAudio.volume = newVolume;
                        }
                    }

                    var newPitch = DTGUIHelper.DisplayPitchField(variation.VarAudio.pitch);
                    if (newPitch != variation.VarAudio.pitch)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyPitches(newPitch);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation.VarAudio, "change Pitch");
                            variation.VarAudio.pitch = newPitch;
                        }
                    }

                    if (_group.curVariationMode == MasterAudioGroup.VariationMode.LoopedChain)
                    {
                        DTGUIHelper.ShowLargeBarAlert(MasterAudio.LoopDisabledLoopedChain);
                    }
                    else if (variation.useRandomStartTime && variation.randomEndPercent != 100f)
                    {
                        DTGUIHelper.ShowLargeBarAlert(MasterAudio.LoopDisabledCustomEnd);
                    }
                    else
                    {
                        var newLoop = EditorGUILayout.Toggle("Loop Clip", variation.VarAudio.loop);
                        if (newLoop != variation.VarAudio.loop)
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation.VarAudio,
                                "toggle Loop Clip");
                            variation.VarAudio.loop = newLoop;
                        }
                    }

                    var newWeight = EditorGUILayout.IntSlider("Voices / Weight", variation.weight, 0, 100);
                    if (newWeight != variation.weight)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyWeight(newWeight);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Voices / Weight");
                            variation.weight = newWeight;
                        }
                    }

                    DTGUIHelper.StartGroupHeader();

                    var newUseRndPitch = EditorGUILayout.BeginToggleGroup(" Use Random Pitch", variation.useRandomPitch);
                    if (newUseRndPitch != variation.useRandomPitch)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyUseRandomPitch(newUseRndPitch);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "toggle Use Random Pitch");
                            variation.useRandomPitch = newUseRndPitch;
                        }
                    }
                    DTGUIHelper.EndGroupHeader();

                    if (variation.useRandomPitch)
                    {
                        var newMode = (SoundGroupVariation.RandomPitchMode)EditorGUILayout.EnumPopup("Pitch Compute Mode", variation.randomPitchMode);
                        if (newMode != variation.randomPitchMode)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomPitchMode(newMode);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Pitch Compute Mode");
                                variation.randomPitchMode = newMode;
                            }
                        }

                        var newPitchMin = DTGUIHelper.DisplayPitchField(variation.randomPitchMin, "Random Pitch Min");
                        if (newPitchMin != variation.randomPitchMin)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomPitchMin(newPitchMin);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Random Pitch Min");
                                variation.randomPitchMin = newPitchMin;
                            }
                            if (variation.randomPitchMax <= variation.randomPitchMin)
                            {
                                variation.randomPitchMax = variation.randomPitchMin;
                            }
                        }

                        var newPitchMax = DTGUIHelper.DisplayPitchField(variation.randomPitchMax, "Random Pitch Max");
                        if (newPitchMax != variation.randomPitchMax)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomPitchMax(newPitchMax);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Random Pitch Max");
                                variation.randomPitchMax = newPitchMax;
                            }
                            if (variation.randomPitchMin > variation.randomPitchMax)
                            {
                                variation.randomPitchMin = variation.randomPitchMax;
                            }
                        }
                    }

                    EditorGUILayout.EndToggleGroup();

                    DTGUIHelper.StartGroupHeader();

                    var newUseRndVol = EditorGUILayout.BeginToggleGroup(" Use Random Volume", variation.useRandomVolume);
                    if (newUseRndVol != variation.useRandomVolume)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyUseRandomVolume(newUseRndVol);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "toggle Use Random Volume");
                            variation.useRandomVolume = newUseRndVol;
                        }
                    }
                    DTGUIHelper.EndGroupHeader();

                    if (variation.useRandomVolume)
                    {
                        var newMode = (SoundGroupVariation.RandomVolumeMode)EditorGUILayout.EnumPopup("Volume Compute Mode", variation.randomVolumeMode);
                        if (newMode != variation.randomVolumeMode)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomVolumeMode(newMode);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Volume Compute Mode");
                                variation.randomVolumeMode = newMode;
                            }
                        }

                        var volMin = 0f;
                        if (variation.randomVolumeMode == SoundGroupVariation.RandomVolumeMode.AddToClipVolume)
                        {
                            volMin = -1f;
                        }

                        var newVolMin = DTGUIHelper.DisplayVolumeField(variation.randomVolumeMin, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, volMin, true, "Random Volume Min");
                        if (newVolMin != variation.randomVolumeMin)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomVolumeMin(newVolMin);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Random Volume Min");
                                variation.randomVolumeMin = newVolMin;
                            }
                            if (variation.randomVolumeMax <= variation.randomVolumeMin)
                            {
                                variation.randomVolumeMax = variation.randomVolumeMin;
                            }
                        }

                        var newVolMax = DTGUIHelper.DisplayVolumeField(variation.randomVolumeMax, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, volMin, true, "Random Volume Max");
                        if (newVolMax != variation.randomVolumeMax)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomVolumeMax(newVolMax);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Random Volume Max");
                                variation.randomVolumeMax = newVolMax;
                            }
                            if (variation.randomVolumeMin > variation.randomVolumeMax)
                            {
                                variation.randomVolumeMin = variation.randomVolumeMax;
                            }
                        }
                    }

                    EditorGUILayout.EndToggleGroup();

                    DTGUIHelper.StartGroupHeader();
                    var newSilence = EditorGUILayout.BeginToggleGroup(" Use Random Delay", variation.useIntroSilence);
                    if (newSilence != variation.useIntroSilence)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyUseRandomDelay(newSilence);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "toggle Use Random Delay");
                            variation.useIntroSilence = newSilence;
                        }
                    }
                    DTGUIHelper.EndGroupHeader();

                    if (variation.useIntroSilence)
                    {
                        var newSilenceMin = EditorGUILayout.Slider("Delay Min (sec)", variation.introSilenceMin, 0f, 100f);
                        if (newSilenceMin != variation.introSilenceMin)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomDelayMin(newSilenceMin);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Delay Min (sec)");
                                variation.introSilenceMin = newSilenceMin;
                            }
                            if (variation.introSilenceMin > variation.introSilenceMax)
                            {
                                variation.introSilenceMax = newSilenceMin;
                            }
                        }

                        var newSilenceMax = EditorGUILayout.Slider("Delay Max (sec)", variation.introSilenceMax, 0f, 100f);
                        if (newSilenceMax != variation.introSilenceMax)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyRandomDelayMax(newSilenceMax);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Delay Max (sec)");
                                variation.introSilenceMax = newSilenceMax;
                            }
                            if (variation.introSilenceMax < variation.introSilenceMin)
                            {
                                variation.introSilenceMin = newSilenceMax;
                            }
                        }
                    }

                    EditorGUILayout.EndToggleGroup();

                    DTGUIHelper.StartGroupHeader();
                    var newStart = EditorGUILayout.BeginToggleGroup(" Use Custom Start/End Position", variation.useRandomStartTime);
                    if (newStart != variation.useRandomStartTime)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyUseCustomStartEnd(newStart);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "toggle Use Custom Start/End Position");
                            variation.useRandomStartTime = newStart;
                        }
                    }
                    DTGUIHelper.EndGroupHeader();

                    if (variation.useRandomStartTime)
                    {
                        var newMin = EditorGUILayout.Slider("Start Min (%)", variation.randomStartMinPercent, 0f, 100f);
                        if (newMin != variation.randomStartMinPercent)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyStartMin(newMin);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Start Min (%)");
                                variation.randomStartMinPercent = newMin;
                            }
                            if (variation.randomStartMaxPercent <= variation.randomStartMinPercent)
                            {
                                variation.randomStartMaxPercent = variation.randomStartMinPercent;
                            }
                        }

                        var newMax = EditorGUILayout.Slider("Start Max (%)", variation.randomStartMaxPercent, 0f, 100f);
                        if (newMax != variation.randomStartMaxPercent)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyStartMax(newMax);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Start Max (%)");
                                variation.randomStartMaxPercent = newMax;
                            }
                            if (variation.randomStartMinPercent > variation.randomStartMaxPercent)
                            {
                                variation.randomStartMinPercent = variation.randomStartMaxPercent;
                            }
                        }

                        var newEnd = EditorGUILayout.Slider("End (%)", variation.randomEndPercent, 0f, 100f);
                        if (newEnd != variation.randomEndPercent || variation.randomEndPercent < variation.randomStartMaxPercent)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyEnd(newEnd);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change End (%)");
                                variation.randomEndPercent = newEnd;
                            }
                            if (variation.randomEndPercent < variation.randomStartMaxPercent)
                            {
                                variation.randomEndPercent = variation.randomStartMaxPercent;
                            }
                        }
                    }

                    EditorGUILayout.EndToggleGroup();

                    if (variation.VarAudio.loop)
                    {
                        DTGUIHelper.StartGroupHeader();

                        newStart = EditorGUILayout.BeginToggleGroup(" Use Finite Looping", variation.useCustomLooping);
                        if (newStart != variation.useCustomLooping)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyUseCustomLooping(newStart);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "toggle Use Finite Looping");
                                variation.useCustomLooping = newStart;
                            }
                        }
                        DTGUIHelper.EndGroupHeader();

                        if (variation.useCustomLooping)
                        {
                            var newMin = EditorGUILayout.IntSlider("Min Loops", variation.minCustomLoops, 1, 100);
                            if (newMin != variation.minCustomLoops)
                            {
                                if (_group.copySettingsExpanded && variation.isChecked)
                                {
                                    CopyCustomLoopingMin(newMin);
                                }
                                else
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Min Loops");
                                    variation.minCustomLoops = newMin;
                                }
                                if (variation.maxCustomLoops <= variation.minCustomLoops)
                                {
                                    variation.maxCustomLoops = variation.minCustomLoops;
                                }
                            }

                            var newMax = EditorGUILayout.IntSlider("Max Loops", variation.maxCustomLoops, 1, 100);
                            if (newMax != variation.maxCustomLoops)
                            {
                                if (_group.copySettingsExpanded && variation.isChecked)
                                {
                                    CopyCustomLoopingMax(newMax);
                                }
                                else
                                {
                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Max Loops");
                                    variation.maxCustomLoops = newMax;
                                }
                                if (variation.minCustomLoops > variation.maxCustomLoops)
                                {
                                    variation.minCustomLoops = variation.maxCustomLoops;
                                }
                            }
                        }

                        EditorGUILayout.EndToggleGroup();
                    }

                    DTGUIHelper.StartGroupHeader();
                    var newFades = EditorGUILayout.BeginToggleGroup(" Use Custom Fading", variation.useFades);
                    if (newFades != variation.useFades)
                    {
                        if (_group.copySettingsExpanded && variation.isChecked)
                        {
                            CopyUseCustomFade(newFades);
                        }
                        else
                        {
                            AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "toggle Use Custom Fading");
                            variation.useFades = newFades;
                        }
                    }
                    DTGUIHelper.EndGroupHeader();

                    if (variation.useFades)
                    {
                        var newFadeIn = EditorGUILayout.Slider("Fade In Time (sec)", variation.fadeInTime, 0f, 10f);
                        if (newFadeIn != variation.fadeInTime)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyFadeInTime(newFadeIn);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Fade In Time");
                                variation.fadeInTime = newFadeIn;
                            }
                        }

                        var newFadeOut = EditorGUILayout.Slider("Fade Out time (sec)", variation.fadeOutTime, 0f, 10f);
                        if (newFadeOut != variation.fadeOutTime)
                        {
                            if (_group.copySettingsExpanded && variation.isChecked)
                            {
                                CopyFadeOutTime(newFadeOut);
                            }
                            else
                            {
                                AudioUndoHelper.RecordObjectPropertyForUndo(ref varIsDirty, variation, "change Fade Out Time");
                                variation.fadeOutTime = newFadeOut;
                            }
                        }
                    }
                    EditorGUILayout.EndToggleGroup();
                    DTGUIHelper.EndGroupedControls();

                    DTGUIHelper.VerticalSpace(3);

                    if (!varIsDirty)
                    {
                        continue;
                    }
                    EditorUtility.SetDirty(variation.VarAudio);
                    EditorUtility.SetDirty(variation);
                }
            }

            if (deadChildIndex.HasValue)
            {
                var deadVar = _group.groupVariations[deadChildIndex.Value];

                if (deadVar != null)
                {
                    var wasDestroyed = false;

                    if (PrefabUtility.IsPartOfPrefabInstance(_group)) {
                        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_group);
                        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                        var groupParent = prefabRoot.transform.Find(_group.name);
                        if (groupParent != null) {
                            var deadTrans = groupParent.Find(deadVar.name);

                            if (groupParent != null) {
                                // Destroy child objects or components on rootGO
                                DestroyImmediate(deadTrans.gameObject); // can't undo
                                wasDestroyed = true;
                            } 
                        } 

                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    } 
                
                    if (!wasDestroyed) {
                        AudioUndoHelper.DestroyForUndo(deadVar.gameObject);
                    }

                    // delete variation from list.
                    if (_group.groupVariations.Count >= deadChildIndex.Value)
                    {
                        _group.groupVariations.RemoveAt(deadChildIndex.Value);
                    }
                }
            }


            if (GUI.changed || isDirty)
            {
                EditorUtility.SetDirty(target);
            }

            //DrawDefaultInspector();
        }

        private static DynamicSoundGroup RescanChildren(DynamicSoundGroup group)
        {
            var newChildren = new List<DynamicGroupVariation>();

            var childNames = new List<string>();

            for (var i = 0; i < group.transform.childCount; i++)
            {
                var child = group.transform.GetChild(i);

                if (!Application.isPlaying)
                {
                    if (childNames.Contains(child.name))
                    {
                        DTGUIHelper.ShowRedError("You have more than one Variation named: " + child.name + ".");
                        DTGUIHelper.ShowRedError("Please ensure each Variation of this Group has a unique name.");
                    }
                }

                childNames.Add(child.name);

                var variation = child.GetComponent<DynamicGroupVariation>();

                newChildren.Add(variation);
            }

            group.groupVariations = newChildren;
            return group;
        }

        public void EqualizeWeights(DynamicSoundGroup grp)
        {
            var variations = new DynamicGroupVariation[grp.groupVariations.Count];

            for (var i = 0; i < grp.groupVariations.Count; i++)
            {
                var variation = grp.groupVariations[i];
                variations[i] = variation;
            }

            AudioUndoHelper.RecordObjectsForUndo(variations, "Equalize Voices");

            foreach (var vari in variations)
            {
                vari.weight = 1;
            }
        }

        private static void EqualizeVariationVolumes(List<DynamicGroupVariation> variations)
        {
            var clips = new Dictionary<DynamicGroupVariation, float>();

            if (variations.Count < 2)
            {
                DTGUIHelper.ShowAlert("You must have at least 2 Variations to use this function.");
                return;
            }

            var lowestVolume = 1f;

            foreach (var setting in variations)
            {
                AudioClip ac = null;

                switch (setting.audLocation)
                {
                    case MasterAudio.AudioLocation.Clip:
                        if (setting.VarAudio.clip == null)
                        {
                            continue;
                        }
                        ac = setting.VarAudio.clip;
                        break;
#if ADDRESSABLES_ENABLED
                case MasterAudio.AudioLocation.Addressable:
                    ac = DTGUIHelper.EditModeLoadAddressable(setting.audioClipAddressable);
                    break;
#endif
                    case MasterAudio.AudioLocation.ResourceFile:
                        if (string.IsNullOrEmpty(setting.resourceFileName))
                        {
                            continue;
                        }

                        ac = Resources.Load(setting.resourceFileName) as AudioClip;

                        if (ac == null)
                        {
                            continue; // bad resource path
                        }
                        break;
                }

                if (!AudioUtil.IsClipReadyToPlay(ac))
                {
                    Debug.Log("Clip is not ready to play (streaming?). Skipping '" + setting.name + "'.");
                    continue;
                }

                var average = 0f;
                // ReSharper disable once PossibleNullReferenceException
                var buffer = new float[ac.samples];

                Debug.Log("Measuring amplitude of '" + ac.name + "'.");

                ac.GetData(buffer, 0);

                for (var c = 0; c < ac.samples; c++)
                {
                    average += Mathf.Pow(buffer[c], 2);
                }

                average = Mathf.Sqrt(1f / ac.samples * average);

                if (average < lowestVolume)
                {
                    lowestVolume = average;
                }

                if (average == 0f)
                {
                    // don't factor in.
                    continue;
                }
                clips.Add(setting, average);
            }

            if (clips.Count < 2)
            {
                DTGUIHelper.ShowAlert("You must have at least 2 Variations with non-compressed, non-streaming clips to use this function.");
                return;
            }

            foreach (var kv in clips)
            {
                if (kv.Value == 0)
                {
                    // skip
                    continue;
                }
                var adjustedVol = lowestVolume / kv.Value;
                //set your volume for each Variation in your Sound Group.
                kv.Key.VarAudio.volume = adjustedVol;
            }
        }

        public void CreateVariation(DynamicSoundGroup group, AudioClip clip)
        {
            var useLocalization = false;

            var clipName = clip.CachedName();

            if (group.transform.GetChildTransform(clipName) != null)
            {
                DTGUIHelper.ShowAlert("You already have a Variation for this Group named '" + clipName + "'. \n\nPlease rename these Variations when finished to be unique, or you may not be able to play them by name if you have a need to.");
            }

            var newVar = (GameObject)Instantiate(_group.variationTemplate, _group.transform.position, Quaternion.identity);
            AudioUndoHelper.CreateObjectForUndo(newVar, "create Variation");

            newVar.transform.name = clipName;
            newVar.transform.parent = group.transform;
            var variation = newVar.GetComponent<DynamicGroupVariation>();
            variation.audLocation = group.bulkVariationMode;

            switch (group.bulkVariationMode)
            {
                case MasterAudio.AudioLocation.Clip:
                    variation.VarAudio.clip = clip;
                    break;
                case MasterAudio.AudioLocation.ResourceFile:
                    var resourceFileName = DTGUIHelper.GetResourcePath(clip, ref useLocalization);
                    if (string.IsNullOrEmpty(resourceFileName))
                    {
                        resourceFileName = clip.CachedName();
                    }

                    variation.resourceFileName = resourceFileName;
                    variation.useLocalization = useLocalization;
                    break;
#if ADDRESSABLES_ENABLED
            case MasterAudio.AudioLocation.Addressable:
                variation.audioClipAddressable = AddressableEditorHelper.CreateAssetReferenceFromObject(clip);
                break;
#endif
            }

            DynamicSoundGroupCreatorInspector.CopyFromAudioSourceTemplate(_dgsc, variation.VarAudio, false);
        }

        private static void BulkUseLocalization(List<DynamicGroupVariation> variations, bool shouldUse)
        {
            foreach (var setting in variations)
            {
                if (setting.audLocation != MasterAudio.AudioLocation.ResourceFile)
                {
                    continue;
                }

                setting.useLocalization = shouldUse;
            }
        }

        private List<DynamicGroupVariation> GetNonMatchingVariations()
        {
            var changedVars = new List<DynamicGroupVariation>();

            for (var i = 0; i < _group.groupVariations.Count; i++)
            {
                var vari = _group.groupVariations[i];
                if (!vari.isChecked)
                {
                    continue;
                }

                changedVars.Add(vari);
            }

            return changedVars;
        }

        private void ExpandCollapseAll(bool expand)
        {
            var vars = new List<DynamicGroupVariation>();

            foreach (var t in _group.groupVariations)
            {
                vars.Add(t);
            }

            AudioUndoHelper.RecordObjectsForUndo(vars.ToArray(), "toggle Expand / Collapse Variations");
            foreach (var t in vars)
            {
                t.isExpanded = expand;
            }
        }

        private int GetNumChecked()
        {
            var numChecked = 0;
            for (var i = 0; i < _group.groupVariations.Count; i++)
            {
                var vari = _group.groupVariations[i];
                if (vari.isChecked)
                {
                    numChecked++;
                }
            }

            return numChecked;
        }

        private void CheckAll()
        {
            var vars = new List<DynamicGroupVariation>();

            for (var i = 0; i < _group.groupVariations.Count; i++)
            {
                var vari = _group.groupVariations[i];
                vars.Add(vari);
            }

            AudioUndoHelper.RecordObjectsForUndo(vars.ToArray(), "check Variations");

            foreach (var t in vars)
            {
                t.isChecked = true;
            }
        }

        private void UncheckAll()
        {
            var vars = new List<DynamicGroupVariation>();

            for (var i = 0; i < _group.groupVariations.Count; i++)
            {
                var vari = _group.groupVariations[i];
                vars.Add(vari);
            }

            AudioUndoHelper.RecordObjectsForUndo(vars.ToArray(), "check Variations");

            foreach (var t in vars)
            {
                t.isChecked = false;
            }
        }

        private List<DynamicGroupVariation> GetSelectedVariations()
        {
            var changedVars = new List<DynamicGroupVariation>();

            for (var i = 0; i < _group.groupVariations.Count; i++)
            {
                var vari = _group.groupVariations[i];
                if (!vari.isChecked)
                {
                    continue;
                }

                changedVars.Add(vari);
            }

            return changedVars;
        }

        private void CopyProbabilityToPlay(int newProb)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Probabilities to Play");
            }

            foreach (var aVar in changedVars)
            {
                aVar.probabilityToPlay = newProb;
                changed++;
            }

            Debug.LogWarning(changed + " Variation Probability to Play(s) changed.");
        }

        private void CopyVolumes(float newVol)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Volumes");
            }

            foreach (var aVar in changedVars)
            {
                aVar.VarAudio.volume = newVol;
                changed++;
            }

            Debug.LogWarning(changed + " Variation Volume(s) changed.");
        }

        private void CopyPitches(float newPitch)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Pitches");
            }

            foreach (var aVar in changedVars)
            {
                aVar.VarAudio.pitch = newPitch;
                changed++;
            }

            Debug.LogWarning(changed + " Variation Pitch(es) changed.");
        }

        private void CopyWeight(int newWeight)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Weight");
            }

            foreach (var aVar in changedVars)
            {
                aVar.weight = newWeight;
                changed++;
            }

            Debug.LogWarning(changed + " Weight(s) changed.");
        }

        private void CopyUseRandomPitch(bool newUse)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Pitch");
            }

            foreach (var aVar in changedVars)
            {
                aVar.useRandomPitch = newUse;
                changed++;
            }

            Debug.LogWarning(changed + " Use Random Pitch(es) changed.");
        }

        private void CopyRandomPitchMode(SoundGroupVariation.RandomPitchMode newMode)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Pitch");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomPitchMode = newMode;
                changed++;
            }

            Debug.LogWarning(changed + " Random Pitch Mode(s) changed.");
        }

        private void CopyRandomPitchMin(float minPitch)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Pitch");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomPitchMin = minPitch;
                changed++;
            }

            Debug.LogWarning(changed + " Random Pitch Min(s) changed.");
        }

        private void CopyRandomPitchMax(float maxPitch)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Pitch");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomPitchMax = maxPitch;
                changed++;
            }

            Debug.LogWarning(changed + " Random Pitch Max(s) changed.");
        }

        private void CopyUseRandomVolume(bool useRand)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Volume");
            }

            foreach (var aVar in changedVars)
            {
                aVar.useRandomVolume = useRand;
                changed++;
            }

            Debug.LogWarning(changed + " Use Random Volume(s) changed.");
        }

        private void CopyRandomVolumeMode(SoundGroupVariation.RandomVolumeMode mode)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Volume");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomVolumeMode = mode;
                changed++;
            }

            Debug.LogWarning(changed + " Random Volume Mode(s) changed.");
        }

        private void CopyRandomVolumeMin(float minVol)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Volume");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomVolumeMin = minVol;
                changed++;
            }

            Debug.LogWarning(changed + " Random Volume Min(s) changed.");
        }

        private void CopyRandomVolumeMax(float maxVol)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Random Volume");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomVolumeMax = maxVol;
                changed++;
            }

            Debug.LogWarning(changed + " Random Volume Max(s) changed.");
        }

        private void CopyUseRandomDelay(bool useIntroSilence)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Use Random Delay");
            }

            foreach (var aVar in changedVars)
            {
                aVar.useIntroSilence = useIntroSilence;
                changed++;
            }

            Debug.LogWarning(changed + " Use Random Delay(s) changed.");
        }

        private void CopyRandomDelayMin(float minSilence)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Random Delay Min");
            }

            foreach (var aVar in changedVars)
            {
                aVar.introSilenceMin = minSilence;
                changed++;
            }

            Debug.LogWarning(changed + " Use Random Delay Min(s) changed.");
        }

        private void CopyRandomDelayMax(float maxSilence)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Random Delay Max");
            }

            foreach (var aVar in changedVars)
            {
                aVar.introSilenceMax = maxSilence;
                changed++;
            }

            Debug.LogWarning(changed + " Use Random Delay Max(s) changed.");
        }

        private void CopyUseCustomStartEnd(bool newUse)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Custom Start/End Position");
            }

            foreach (var aVar in changedVars)
            {
                aVar.useRandomStartTime = newUse;
                changed++;
            }

            Debug.LogWarning(changed + " Custom Use Custom Start/End Position(s) changed.");
        }

        private void CopyStartMin(float minPercent)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Custom Start/End Position");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomStartMinPercent = minPercent;
                changed++;
            }

            Debug.LogWarning(changed + " Custom Start Min Percent(s) changed.");
        }

        private void CopyStartMax(float maxPercent)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Custom Start/End Position");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomStartMaxPercent = maxPercent;
                //aVar.randomEndPercent = variation.randomEndPercent;
                changed++;
            }

            Debug.LogWarning(changed + " Custom Start Min Percent(s) changed.");
        }

        private void CopyEnd(float endPercent)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Variation Custom Start/End Position");
            }

            foreach (var aVar in changedVars)
            {
                aVar.randomEndPercent = endPercent;
                changed++;
            }

            Debug.LogWarning(changed + " Custom End Percent(s) changed.");
        }

        private void CopyUseCustomLooping(bool newUse)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Use Finite Looping");
            }

            foreach (var aVar in changedVars)
            {
                aVar.useCustomLooping = newUse;
                changed++;
            }

            Debug.LogWarning(changed + " Use Finite Looping(s) changed.");
        }

        private void CopyCustomLoopingMin(int minLoops)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Min Loops");
            }

            foreach (var aVar in changedVars)
            {
                aVar.minCustomLoops = minLoops;
                changed++;
            }

            Debug.LogWarning(changed + " Min Loops changed.");
        }

        private void CopyCustomLoopingMax(int maxLoops)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Max Loops");
            }

            foreach (var aVar in changedVars)
            {
                aVar.maxCustomLoops = maxLoops;
                changed++;
            }

            Debug.LogWarning(changed + " Max Loops changed.");
        }

        private void CopyUseCustomFade(bool newUse)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Use Custom Fade");
            }

            foreach (var aVar in changedVars)
            {
                aVar.useFades = newUse;
                changed++;
            }

            Debug.LogWarning(changed + " Use Custom Fade(s) changed.");
        }

        private void CloneVariation(int index) {
            var gameObj = _group.groupVariations[index].gameObject;

            if (PrefabUtility.IsPartOfPrefabInstance(_group)) {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_group);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                var groupParent = prefabRoot.transform.Find(_group.name);
                if (groupParent != null) {
                    var varTrans = groupParent.Find(gameObj.name);

                    if (varTrans != null) {
                        // Destroy child objects or components on rootGO
                        var newVar = DTGUIHelper.DuplicateGameObject(varTrans.gameObject, groupParent.name, _group.groupVariations.Count + 1);
                        newVar.transform.parent = groupParent;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            } else {
                var dupe = DTGUIHelper.DuplicateGameObject(gameObj, _group.name, _group.groupVariations.Count + 1);

                if (dupe == null) {
                    return;
                }

                dupe.transform.parent = _group.transform;
            }
        }

        private void CopyFadeInTime(float fadeInTime)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Fade In Time");
            }

            foreach (var aVar in changedVars)
            {
                aVar.fadeInTime = fadeInTime;
                changed++;
            }

            Debug.LogWarning(changed + " Fade In Time(s) changed.");
        }

        private void CopyFadeOutTime(float fadeOutTime)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Fade Out Time");
            }

            foreach (var aVar in changedVars)
            {
                aVar.fadeOutTime = fadeOutTime;
                changed++;
            }

            Debug.LogWarning(changed + " Fade Out Time(s) changed.");
        }

        private void CopyImportance(int importance)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Importance");
            }

            foreach (var aVar in changedVars)
            {
                aVar.importance = importance;
                changed++;
            }

            Debug.LogWarning(changed + " Importance(s) changed.");
        }

        private void CopyIsUninterruptible(bool isUninterruptible)
        {
            var changed = 0;

            var changedVars = GetSelectedVariations();

            if (changedVars.Count > 0)
            {
                AudioUndoHelper.RecordObjectsForUndo(changedVars.ToArray(), "change Uninterruptible");
            }

            foreach (var aVar in changedVars)
            {
                aVar.isUninterruptible = isUninterruptible;
                changed++;
            }

            Debug.LogWarning(changed + " Uninterruptible(s) changed.");
        }
    }
}