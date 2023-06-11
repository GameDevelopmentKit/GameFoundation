using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace DarkTonic.MasterAudio.EditorScripts
{
    [CustomEditor(typeof(AmbientSound))]
    // ReSharper disable once CheckNamespace
    public class AmbientSoundInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            MasterAudio.Instance = null;

            var _ma = MasterAudio.Instance;
            var _maInScene = _ma != null;

            if (_maInScene)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.LogoTexture);
            }

            DTGUIHelper.HelpHeader("https://www.dtdevtools.com/docs/masteraudio/AmbientSound.htm");

            var canUse = true;

#if !PHY3D_ENABLED
            canUse = false;
#endif
            if (!canUse)
            {
                DTGUIHelper.ShowLargeBarAlert("You must enable 3D Physics support for your Collider or Trigger or this script will not function. Use the Welcome Window to do that, then this Inspector will show its controls.");
                return;
            }

            var _isDirty = false;

            var _sounds = (DarkTonic.MasterAudio.AmbientSound)target;

            var _groupNames = new List<string>();

            if (_maInScene)
            {
                // ReSharper disable once PossibleNullReferenceException
                _groupNames = _ma.GroupNames;
            }

            PopulateItemNames(_groupNames);

            if (GUI.changed || _isDirty)
            {
                EditorUtility.SetDirty(target);
            }

            if (_maInScene)
            {
                var existingIndex = _groupNames.IndexOf(_sounds.AmbientSoundGroup);

                int? groupIndex = null;

                var noGroup = false;
                var noMatch = false;

                if (existingIndex >= 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    groupIndex = EditorGUILayout.Popup("Ambient Sound Group", existingIndex, _groupNames.ToArray());
                    if (existingIndex == 1)
                    {
                        noGroup = true;
                    }

                    var isUsingVideoPlayersGroup = false;

                    if (_groupNames[groupIndex.Value] == MasterAudio.VideoPlayerSoundGroupName)
                    {
                        isUsingVideoPlayersGroup = true;
                    }

                    if (groupIndex > MasterAudio.HardCodedBusOptions - 1)
                    {
                        var button = DTGUIHelper.AddSettingsButton("Ambient Sound Group");
                        switch (button)
                        {
                            case DTGUIHelper.DTFunctionButtons.Go:
                                var grp = _groupNames[existingIndex];
                                var trs = MasterAudio.FindGroupTransform(grp);
                                if (trs != null)
                                {
                                    Selection.activeObject = trs;
                                }
                                break;
                        }

                        var buttonPress = DTGUIHelper.AddDynamicVariationButtons();
                        var sType = _groupNames[existingIndex];

                        switch (buttonPress)
                        {
                            case DTGUIHelper.DTFunctionButtons.Play:
                                DTGUIHelper.PreviewSoundGroup(sType);
                                break;
                            case DTGUIHelper.DTFunctionButtons.Stop:
                                DTGUIHelper.StopPreview(sType);
                                break;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    if (isUsingVideoPlayersGroup)
                    {
                        DTGUIHelper.ShowRedError(MasterAudio.VideoPlayersSoundGroupSelectedError);
                    }
                }
                else if (existingIndex == -1 && _sounds.AmbientSoundGroup == MasterAudio.NoGroupName)
                {
                    groupIndex = EditorGUILayout.Popup("Ambient Sound Group", existingIndex, _groupNames.ToArray());
                }
                else
                { // non-match
                    noMatch = true;
                    var newSound = EditorGUILayout.TextField("Ambient Sound Group", _sounds.AmbientSoundGroup);
                    if (newSound != _sounds.AmbientSoundGroup)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Ambient Sound Group");
                        _sounds.AmbientSoundGroup = newSound;
                        _sounds.CalculateRadius();
                    }

                    var newIndex = EditorGUILayout.Popup("All Sound Groups", -1, _groupNames.ToArray());
                    if (newIndex >= 0)
                    {
                        groupIndex = newIndex;
                    }
                }

                if (noGroup)
                {
                    DTGUIHelper.ShowRedError("No Ambient Sound Group specified.");
                }
                else if (noMatch)
                {
                    DTGUIHelper.ShowRedError("Ambient Sound Group found no match. Type in or choose one.");
                }

                if (groupIndex.HasValue)
                {
                    if (existingIndex != groupIndex.Value)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Ambient Sound Group");
                    }
                    switch (groupIndex.Value)
                    {
                        case -1:
                            _sounds.AmbientSoundGroup = MasterAudio.NoGroupName;
                            break;
                        default:
                            _sounds.AmbientSoundGroup = _groupNames[groupIndex.Value];
                            break;
                    }
                    _sounds.CalculateRadius();
                }
            }
            else
            {
                var newSType = EditorGUILayout.TextField("Ambient Sound Group", _sounds.AmbientSoundGroup);
                if (newSType != _sounds.AmbientSoundGroup)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Ambient Sound Group");
                    _sounds.CalculateRadius();
                    _sounds.AmbientSoundGroup = newSType;
                }
            }

            var newVol = DTGUIHelper.DisplayVolumeField(_sounds.playVolume, DTGUIHelper.VolumeFieldType.None, MasterAudio.MixerWidthMode.Normal, 0f, true);
            if (newVol != _sounds.playVolume)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Volume");
                _sounds.playVolume = newVol;
            }

            var newVarType = (EventSounds.VariationType)EditorGUILayout.EnumPopup("Variation Mode", _sounds.variationType);
            if (newVarType != _sounds.variationType)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Variation Mode");
                _sounds.CalculateRadius();
                _sounds.variationType = newVarType;
            }

            if (_sounds.variationType == EventSounds.VariationType.PlaySpecific)
            {
                var newVarName = EditorGUILayout.TextField("Variation Name", _sounds.variationName);
                if (newVarName != _sounds.variationName)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Variation Name");
                    _sounds.CalculateRadius();
                    _sounds.variationName = newVarName;
                }

                if (string.IsNullOrEmpty(_sounds.variationName))
                {
                    DTGUIHelper.ShowRedError("Variation Name is empty. No sound will play.");
                }
            }

            var newExitMode = (MasterAudio.AmbientSoundExitMode)EditorGUILayout.EnumPopup("Trigger Exit Behavior", _sounds.exitMode);
            if (newExitMode != _sounds.exitMode)
            {
                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Trigger Exit Behavior");
                _sounds.exitMode = newExitMode;
            }

            if (_sounds.exitMode == MasterAudio.AmbientSoundExitMode.FadeSound)
            {
                var newFadeTime = EditorGUILayout.Slider("Fade Time (sec)", _sounds.exitFadeTime, .2f, 10f);
                if (newFadeTime != _sounds.exitFadeTime)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Fade Time (sec)");
                    _sounds.exitFadeTime = newFadeTime;
                }

                var reEnterMode = (MasterAudio.AmbientSoundReEnterMode)EditorGUILayout.EnumPopup("Trigger Re-Enter Behavior", _sounds.reEnterMode);
                if (reEnterMode != _sounds.reEnterMode)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Trigger Re-Enter Behavior");
                    _sounds.reEnterMode = reEnterMode;
                }

                if (_sounds.reEnterMode == MasterAudio.AmbientSoundReEnterMode.FadeInSameSound)
                {
                    var newFadeIn = EditorGUILayout.Slider("Fade In Time (sec)", _sounds.reEnterFadeTime, .2f, 10f);
                    if (newFadeIn != _sounds.reEnterFadeTime)
                    {
                        AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "change Fade In Time (sec)");
                        _sounds.reEnterFadeTime = newFadeIn;
                    }
                }
            }

            var aud = _sounds.GetNamedOrFirstAudioSource();
            if (aud != null)
            {
                var newMin = EditorGUILayout.Slider("Min Distance", aud.minDistance, .1f, aud.maxDistance);
                if (newMin != aud.minDistance)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, aud, "change Min Distance");

                    switch (_sounds.variationType)
                    {
                        case EventSounds.VariationType.PlayRandom:
                            var sources = _sounds.GetAllVariationAudioSources();
                            if (sources != null)
                            {
                                for (var i = 0; i < sources.Count; i++)
                                {
                                    var src = sources[i];
                                    src.minDistance = newMin;
                                    EditorUtility.SetDirty(src);
                                }
                            }
                            break;
                        case EventSounds.VariationType.PlaySpecific:
                            aud.minDistance = newMin;
                            EditorUtility.SetDirty(aud);
                            break;
                    }
                }

                var newMax = EditorGUILayout.Slider("Max Distance", aud.maxDistance, aud.minDistance, 1000000f);
                if (newMax != aud.maxDistance)
                {
                    AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, aud, "change Max Distance");

                    switch (_sounds.variationType)
                    {
                        case EventSounds.VariationType.PlayRandom:
                            var sources = _sounds.GetAllVariationAudioSources();
                            if (sources != null)
                            {
                                for (var i = 0; i < sources.Count; i++)
                                {
                                    var src = sources[i];
                                    src.maxDistance = newMax;
                                    EditorUtility.SetDirty(src);
                                }
                            }
                            break;
                        case EventSounds.VariationType.PlaySpecific:
                            aud.maxDistance = newMax;
                            EditorUtility.SetDirty(aud);
                            break;
                    }
                }
                switch (_sounds.variationType)
                {
                    case EventSounds.VariationType.PlayRandom:
                        DTGUIHelper.ShowLargeBarAlert("Adjusting the Min/Max Distance field will change the Min/Max Distance field(s) on the Audio Source of every Variation in the selected Sound Group.");
                        break;
                    case EventSounds.VariationType.PlaySpecific:
                        DTGUIHelper.ShowLargeBarAlert("Adjusting the Min/Max Distance field will change the Min/Max Distance field(s) on the Audio Source for the selected Variation in the selected Sound Group.");
                        break;
                }
                DTGUIHelper.ShowColorWarning("You can also bulk apply Min/Max Distance and other Audio Source properties with Audio Source Templates using the Master Audio Mixer.");
            }

	        DTGUIHelper.StartGroupHeader();
	        var newClosest = GUILayout.Toggle(_sounds.UseClosestColliderPosition, new GUIContent(" Use Closest Collider Position", "Using this option, the Audio Source will be updated every frame to the closest position on the caller's collider(s)."));
	        if (newClosest != _sounds.UseClosestColliderPosition) {
	            AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "toggle Follow Caller");
	            _sounds.UseClosestColliderPosition = newClosest;
	        }

	        EditorGUILayout.EndVertical();
	        if (_sounds.UseClosestColliderPosition) {
	            var newTop = EditorGUILayout.Toggle("Use Top Collider", _sounds.UseTopCollider);
	            if (newTop != _sounds.UseTopCollider) {
	                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "toggle Use Top Collider");
	                _sounds.UseTopCollider = newTop;
	            }
	            var newChild = EditorGUILayout.Toggle("Use Child G.O. Colliders", _sounds.IncludeChildColliders);
	            if (newChild != _sounds.IncludeChildColliders) {
	                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "toggle Use Child G.O. Colliders");
	                _sounds.IncludeChildColliders = newChild;
	            }

	            var colliderObjects = new List<GameObject>();

	            if (_sounds.UseTopCollider) {
	                var collider = _sounds.GetComponent<Collider>();
	                var collider2d = _sounds.GetComponent<Collider2D>();
	                if (collider != null) {
	                    colliderObjects.Add(collider.gameObject);
	                } else if (collider2d != null) {
	                    colliderObjects.Add(collider2d.gameObject);
	                }
	            }
	            if (_sounds.IncludeChildColliders) {
	                for (var i = 0; i < _sounds.transform.childCount; i++) {
	                    var child = _sounds.transform.GetChild(i);
	                    var collider = child.GetComponent<Collider>();
	                    var collider2d = child.GetComponent<Collider2D>();
	                    if (collider != null) {
	                        colliderObjects.Add(collider.gameObject);
	                    } else if (collider2d != null) {
	                        colliderObjects.Add(collider2d.gameObject);
	                    }
	                }
	            }

	            if (colliderObjects.Count == 0) {
	                DTGUIHelper.ShowRedError("You have zero Colliders selected, so this functionality will not work.");
	            } else {
	                EditorGUILayout.BeginHorizontal();
	                DTGUIHelper.ShowColorWarning("Colliders used: " + colliderObjects.Count);
	                if (GUILayout.Button("Select\nColliders", GUILayout.Width(70))) {
	                    Selection.objects = colliderObjects.ToArray();
	                }
	                EditorGUILayout.EndHorizontal();
	            }

	            EditorGUILayout.EndVertical();
	        } else {
	            EditorGUILayout.EndVertical();

	            DTGUIHelper.StartGroupHeader();
	            var newFollow =
	                    GUILayout.Toggle(_sounds.FollowCaller, new GUIContent(" Follow Caller",
	                            "This option is useful if your caller ever moves, as it will make the Audio Source follow to the location of the caller every frame."));
	            if (newFollow != _sounds.FollowCaller) {
	                AudioUndoHelper.RecordObjectPropertyForUndo(ref _isDirty, _sounds, "toggle Follow Caller");
	                _sounds.FollowCaller = newFollow;
	            }
	            EditorGUILayout.EndVertical();
	            if (_sounds.FollowCaller) {
	                DTGUIHelper.ShowColorWarning("Will follow caller at runtime.");
	            }
	            EditorGUILayout.EndVertical();
	        }

            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Follower Object");
                EditorGUILayout.ObjectField(_sounds.RuntimeFollower, typeof(Transform), false);
                EditorGUILayout.EndHorizontal();
            }

            //DrawDefaultInspector();
        }

        protected virtual void PopulateItemNames(List<string> groups)
        {
            if (groups == null)
            {
                groups = new List<string>();
            }

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
            if (groups.Count > 1)
            { // "type in" back to index 0 (sort puts it at #1)
                groups.Insert(0, groups[1]);
            }
        }
    }
}