using UnityEngine;
using UnityEditor;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(DynamicGroupVariation))]
    // ReSharper disable once CheckNamespace
    public class DynamicGroupVariationInspector : Editor
    {
        private DynamicGroupVariation _variation;

        // ReSharper disable once FunctionComplexityOverflow
        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel = 1;
            var isDirty = false;

            _variation = (DynamicGroupVariation)target;

            if (MasterAudioInspectorResources.LogoTexture != null)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            EditorGUI.indentLevel = 0;  // Space will handle this for the header
            var previewLang = SystemLanguage.English;
            GameObject dgscGO = null;

            if (_variation.transform.parent != null && _variation.transform.parent.parent != null)
            {
                var parentParent = _variation.transform.parent.parent;

                dgscGO = parentParent.gameObject;

                var dgsc = dgscGO.GetComponent<DynamicSoundGroupCreator>();
                if (dgsc != null)
                {
                    previewLang = dgsc.previewLanguage;
                }
            }

            if (dgscGO == null)
            {
                DTGUIHelper.ShowRedError("This prefab must have a GameObject 2 parents up. Prefab broken.");
                return;
            }

            AudioSource previewer;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.contentColor = DTGUIHelper.BrightButtonColor;
            if (GUILayout.Button(new GUIContent("Back to Group", "Select Group in Hierarchy"), EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                // ReSharper disable once PossibleNullReferenceException
                Selection.activeObject = _variation.transform.parent.gameObject;
            }
            GUILayout.FlexibleSpace();
            GUI.contentColor = Color.white;

            var buttonPressed = DTGUIHelper.AddDynamicVariationButtons();

            switch (buttonPressed)
            {
                case DTGUIHelper.DTFunctionButtons.Play:
                    isDirty = true;

                    previewer = MasterAudioInspector.GetPreviewer();

                    var randPitch = SoundGroupVariationInspector.GetRandomPreviewPitch(_variation);
                    var varVol = SoundGroupVariationInspector.GetRandomPreviewVolume(_variation);

                    if (previewer != null)
                    {
                        MasterAudioInspector.StopPreviewer();
                        previewer.pitch = randPitch;
                    }

                    var calcVolume = varVol * _variation.ParentGroup.groupMasterVolume;

                    switch (_variation.audLocation)
                    {
                        case MasterAudio.AudioLocation.ResourceFile:
                            var fileName = AudioResourceOptimizer.GetLocalizedDynamicSoundGroupFileName(previewLang, _variation.useLocalization, _variation.resourceFileName);

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
                            break;
                        case MasterAudio.AudioLocation.Clip:
                            if (previewer != null)
                            {
                                DTGUIHelper.PlaySilentWakeUpPreview(previewer, _variation.VarAudio.clip);
                                previewer.PlayOneShot(_variation.VarAudio.clip, calcVolume);
                            }
                            break;
#if ADDRESSABLES_ENABLED
                    case MasterAudio.AudioLocation.Addressable:
                        DTGUIHelper.PreviewAddressable(_variation.audioClipAddressable, previewer, calcVolume);
                        break;
#endif
                    }

                    break;
                case DTGUIHelper.DTFunctionButtons.Stop:
                    MasterAudioInspector.StopPreviewer();
                    break;
            }

            EditorGUILayout.EndHorizontal();

            DTGUIHelper.HelpHeader("https://www.dtdevtools.com/docs/masteraudio/SoundGroupVariations.htm");

            if (!Application.isPlaying)
            {
                DTGUIHelper.ShowColorWarning(MasterAudio.PreviewText);
            }

            if (!Application.isPlaying)
            {
                var newAlias = EditorGUILayout.TextField("Clip Id (optional)", _variation.clipAlias);

                if (newAlias != _variation.clipAlias)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Clip Id");
                    _variation.clipAlias = newAlias;
                }
            }

            var oldLocation = _variation.audLocation;
            EditorGUILayout.BeginHorizontal();
            var newLocation = (MasterAudio.AudioLocation)EditorGUILayout.EnumPopup("Audio Origin", _variation.audLocation);
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/SoundGroupVariations.htm#AudioOrigin");
            EditorGUILayout.EndHorizontal();

            if (newLocation != oldLocation)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Audio Origin");
                _variation.audLocation = newLocation;
            }

            if (oldLocation != _variation.audLocation && oldLocation == MasterAudio.AudioLocation.Clip)
            {
                if (_variation.VarAudio.clip != null)
                {
                    Debug.Log("Audio clip removed to prevent unnecessary memory usage.");
                }
                _variation.VarAudio.clip = null;
            }

            switch (_variation.audLocation)
            {
                case MasterAudio.AudioLocation.Clip:
                    var newClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", _variation.VarAudio.clip, typeof(AudioClip), false);

                    if (newClip != _variation.VarAudio.clip)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation.VarAudio, "assign Audio Clip");
                        _variation.VarAudio.clip = newClip;
                    }
                    break;
#if ADDRESSABLES_ENABLED
            case MasterAudio.AudioLocation.Addressable:
                serializedObject.Update();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(DynamicGroupVariation.audioClipAddressable)), true);
                serializedObject.ApplyModifiedProperties();

                if (!DTGUIHelper.IsAddressableTypeValid(_variation.audioClipAddressable, _variation.name)) {
                    _variation.audioClipAddressable = null;
                    isDirty = true;
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

                    string newFilename;

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

                                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Resource filename");

                                    var useLocalization = false;
                                    newFilename = DTGUIHelper.GetResourcePath(aClip, ref useLocalization);
                                    if (string.IsNullOrEmpty(newFilename))
                                    {
                                        newFilename = aClip.CachedName();
                                    }

                                    _variation.resourceFileName = newFilename;
                                    _variation.useLocalization = useLocalization;
                                    break;
                                }
                            }
                            Event.current.Use();
                            break;
                    }
                    EditorGUILayout.EndVertical();

                    newFilename = EditorGUILayout.TextField("Resource Filename", _variation.resourceFileName);
                    if (newFilename != _variation.resourceFileName)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Resource filename");
                        _variation.resourceFileName = newFilename;
                    }

                    EditorGUI.indentLevel = 1;

                    var newLocal = EditorGUILayout.Toggle("Use Localized Folder", _variation.useLocalization);
                    if (newLocal != _variation.useLocalization)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "toggle Use Localized Folder");
                        _variation.useLocalization = newLocal;
                    }

                    break;
            }

            EditorGUI.indentLevel = 0;

            var newProbability = EditorGUILayout.IntSlider("Probability to Play (%)", _variation.probabilityToPlay, 0, 100);
            if (newProbability != _variation.probabilityToPlay)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Probability to Play (%)");
                _variation.probabilityToPlay = newProbability;
            }

            if (_variation.probabilityToPlay < 100)
            {
                DTGUIHelper.ShowLargeBarAlert("Since Probability to Play is less than 100%, you will not always hear this Variation when it's selected to play.");
            }

            var newVolume = EditorGUILayout.Slider("Volume", _variation.VarAudio.volume, 0f, 1f);
            if (newVolume != _variation.VarAudio.volume)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation.VarAudio, "change Volume");
                _variation.VarAudio.volume = newVolume;
            }

            var newPitch = DTGUIHelper.DisplayPitchField(_variation.VarAudio.pitch);
            if (newPitch != _variation.VarAudio.pitch)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation.VarAudio, "change Pitch");
                _variation.VarAudio.pitch = newPitch;
            }

            if (_variation.ParentGroup.curVariationMode == MasterAudioGroup.VariationMode.LoopedChain)
            {
                DTGUIHelper.ShowLargeBarAlert(MasterAudio.LoopDisabledLoopedChain);
            }
            else if (_variation.useRandomStartTime && _variation.randomEndPercent != 100f)
            {
                DTGUIHelper.ShowLargeBarAlert(MasterAudio.LoopDisabledCustomEnd);
            }
            else
            {
                var newLoop = EditorGUILayout.Toggle("Loop Clip", _variation.VarAudio.loop);
                if (newLoop != _variation.VarAudio.loop)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation.VarAudio, "toggle Loop");
                    _variation.VarAudio.loop = newLoop;
                }
            }

            EditorGUILayout.BeginHorizontal();
            var newWeight = EditorGUILayout.IntSlider("Voices / Weight", _variation.weight, 0, 100);
            DTGUIHelper.AddHelpIconNoStyle("https://www.dtdevtools.com/docs/masteraudio/SoundGroupVariations.htm#Voices");
            EditorGUILayout.EndHorizontal();
            if (newWeight != _variation.weight)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Voices / Weight");
                _variation.weight = newWeight;
            }


            DTGUIHelper.StartGroupHeader();

            var newUseRndPitch = EditorGUILayout.BeginToggleGroup(" Use Random Pitch", _variation.useRandomPitch);
            if (newUseRndPitch != _variation.useRandomPitch)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "toggle Use Random Pitch");
                _variation.useRandomPitch = newUseRndPitch;
            }
            DTGUIHelper.EndGroupHeader();

            if (_variation.useRandomPitch)
            {
                var newMode = (SoundGroupVariation.RandomPitchMode)EditorGUILayout.EnumPopup("Pitch Compute Mode", _variation.randomPitchMode);
                if (newMode != _variation.randomPitchMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Pitch Compute Mode");
                    _variation.randomPitchMode = newMode;
                }

                var newPitchMin = DTGUIHelper.DisplayPitchField(_variation.randomPitchMin, "Random Pitch Min");
                if (newPitchMin != _variation.randomPitchMin)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Random Pitch Min");
                    _variation.randomPitchMin = newPitchMin;
                    if (_variation.randomPitchMax <= _variation.randomPitchMin)
                    {
                        _variation.randomPitchMax = _variation.randomPitchMin;
                    }
                }

                var newPitchMax = DTGUIHelper.DisplayPitchField(_variation.randomPitchMax, "Random Pitch Max");
                if (newPitchMax != _variation.randomPitchMax)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Random Pitch Max");
                    _variation.randomPitchMax = newPitchMax;
                    if (_variation.randomPitchMin > _variation.randomPitchMax)
                    {
                        _variation.randomPitchMin = _variation.randomPitchMax;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            var newUseRndVol = EditorGUILayout.BeginToggleGroup(" Use Random Volume", _variation.useRandomVolume);
            if (newUseRndVol != _variation.useRandomVolume)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "toggle Use Random Volume");
                _variation.useRandomVolume = newUseRndVol;
            }
            DTGUIHelper.EndGroupHeader();

            if (_variation.useRandomVolume)
            {
                var newMode = (SoundGroupVariation.RandomVolumeMode)EditorGUILayout.EnumPopup("Volume Compute Mode", _variation.randomVolumeMode);
                if (newMode != _variation.randomVolumeMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Volume Compute Mode");
                    _variation.randomVolumeMode = newMode;
                }

                var volMin = 0f;
                if (_variation.randomVolumeMode == SoundGroupVariation.RandomVolumeMode.AddToClipVolume)
                {
                    volMin = -1f;
                }

                var newVolMin = DTGUIHelper.DisplayVolumeField(_variation.randomVolumeMin, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, volMin, true, "Random Volume Min");
                if (newVolMin != _variation.randomVolumeMin)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Random Volume Min");
                    _variation.randomVolumeMin = newVolMin;
                    if (_variation.randomVolumeMax <= _variation.randomVolumeMin)
                    {
                        _variation.randomVolumeMax = _variation.randomVolumeMin;
                    }
                }

                var newVolMax = DTGUIHelper.DisplayVolumeField(_variation.randomVolumeMax, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, volMin, true, "Random Volume Max");
                if (newVolMax != _variation.randomVolumeMax)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Random Volume Max");
                    _variation.randomVolumeMax = newVolMax;
                    if (_variation.randomVolumeMin > _variation.randomVolumeMax)
                    {
                        _variation.randomVolumeMin = _variation.randomVolumeMax;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            var newSilence = EditorGUILayout.BeginToggleGroup(" Use Random Delay", _variation.useIntroSilence);
            if (newSilence != _variation.useIntroSilence)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "toggle Use Random Delay");
                _variation.useIntroSilence = newSilence;
            }
            DTGUIHelper.EndGroupHeader();

            if (_variation.useIntroSilence)
            {
                var newSilenceMin = EditorGUILayout.Slider("Delay Min (sec)", _variation.introSilenceMin, 0f, 100f);
                if (newSilenceMin != _variation.introSilenceMin)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Delay Min (sec)");
                    _variation.introSilenceMin = newSilenceMin;
                    if (_variation.introSilenceMin > _variation.introSilenceMax)
                    {
                        _variation.introSilenceMax = newSilenceMin;
                    }
                }

                var newSilenceMax = EditorGUILayout.Slider("Delay Max (sec)", _variation.introSilenceMax, 0f, 100f);
                if (newSilenceMax != _variation.introSilenceMax)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Delay Max (sec)");
                    _variation.introSilenceMax = newSilenceMax;
                    if (_variation.introSilenceMax < _variation.introSilenceMin)
                    {
                        _variation.introSilenceMin = newSilenceMax;
                    }
                }
            }
            EditorGUILayout.EndToggleGroup();

            DTGUIHelper.StartGroupHeader();

            var newStart = EditorGUILayout.BeginToggleGroup(" Use Custom Start/End Position", _variation.useRandomStartTime);
            if (newStart != _variation.useRandomStartTime)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "toggle Use Custom Start/End Position");
                _variation.useRandomStartTime = newStart;
            }
            DTGUIHelper.EndGroupHeader();

            if (_variation.useRandomStartTime)
            {
                var newMin = EditorGUILayout.Slider("Start Min (%)", _variation.randomStartMinPercent, 0f, 100f);
                if (newMin != _variation.randomStartMinPercent)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Start Min (%)");
                    _variation.randomStartMinPercent = newMin;
                    if (_variation.randomStartMaxPercent <= _variation.randomStartMinPercent)
                    {
                        _variation.randomStartMaxPercent = _variation.randomStartMinPercent;
                    }
                }

                var newMax = EditorGUILayout.Slider("Start Max (%)", _variation.randomStartMaxPercent, 0f, 100f);
                if (newMax != _variation.randomStartMaxPercent)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Start Max (%)");
                    _variation.randomStartMaxPercent = newMax;
                    if (_variation.randomStartMinPercent > _variation.randomStartMaxPercent)
                    {
                        _variation.randomStartMinPercent = _variation.randomStartMaxPercent;
                    }
                }

                var newEnd = EditorGUILayout.Slider("End (%)", _variation.randomEndPercent, 0f, 100f);
                if (newEnd != _variation.randomEndPercent || _variation.randomEndPercent < _variation.randomStartMaxPercent)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change End (%)");
                    _variation.randomEndPercent = newEnd;
                    if (_variation.randomEndPercent < _variation.randomStartMaxPercent)
                    {
                        _variation.randomEndPercent = _variation.randomStartMaxPercent;
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();

            if (_variation.VarAudio.loop)
            {
                DTGUIHelper.StartGroupHeader();

                newStart = EditorGUILayout.BeginToggleGroup(" Use Finite Looping", _variation.useCustomLooping);
                if (newStart != _variation.useCustomLooping)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation,
                        "toggle Use Finite Looping");
                    _variation.useCustomLooping = newStart;
                }
                DTGUIHelper.EndGroupHeader();

                if (_variation.useCustomLooping)
                {
                    var newMin = EditorGUILayout.IntSlider("Min Loops", _variation.minCustomLoops, 1, 100);
                    if (newMin != _variation.minCustomLoops)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Min Loops");
                        _variation.minCustomLoops = newMin;
                        if (_variation.maxCustomLoops <= _variation.minCustomLoops)
                        {
                            _variation.maxCustomLoops = _variation.minCustomLoops;
                        }
                    }

                    var newMax = EditorGUILayout.IntSlider("Max Loops", _variation.maxCustomLoops, 1, 100);
                    if (newMax != _variation.maxCustomLoops)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Max Loops");
                        _variation.maxCustomLoops = newMax;
                        if (_variation.minCustomLoops > _variation.maxCustomLoops)
                        {
                            _variation.minCustomLoops = _variation.maxCustomLoops;
                        }
                    }
                }

                EditorGUILayout.EndToggleGroup();
            }

            DTGUIHelper.StartGroupHeader();

            var newUseFades = EditorGUILayout.BeginToggleGroup(" Use Custom Fading", _variation.useFades);
            if (newUseFades != _variation.useFades)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "toggle Use Custom Fading");
                _variation.useFades = newUseFades;
            }
            DTGUIHelper.EndGroupHeader();

            if (_variation.useFades)
            {
                var newFadeIn = EditorGUILayout.Slider("Fade In Time (sec)", _variation.fadeInTime, 0f, 10f);
                if (newFadeIn != _variation.fadeInTime)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Fade In Time");
                    _variation.fadeInTime = newFadeIn;
                }

                var newFadeOut = EditorGUILayout.Slider("Fade Out time (sec)", _variation.fadeOutTime, 0f, 10f);
                if (newFadeOut != _variation.fadeOutTime)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref isDirty, _variation, "change Fade Out Time");
                    _variation.fadeOutTime = newFadeOut;
                }

                if (_variation.VarAudio.loop)
                {
                    DTGUIHelper.ShowColorWarning("Looped clips will not automatically use the custom fade out. You will need to call FadeOutNowAndStop() on the Variation to use the fade.");
                }
            }

            EditorGUILayout.EndToggleGroup();

            if (GUI.changed || isDirty)
            {
                EditorUtility.SetDirty(target);
            }

            //DrawDefaultInspector();
        }
    }
}