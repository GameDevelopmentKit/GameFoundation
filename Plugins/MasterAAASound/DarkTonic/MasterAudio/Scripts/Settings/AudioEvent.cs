/*! \cond PRIVATE */
using System;
// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;
using UnityEngine.Audio;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
    public class AudioEvent {
        // ReSharper disable InconsistentNaming
        public string actionName = "Your action name";
        public bool isExpanded = true;
        public string soundType = MasterAudio.NoGroupName;
        public bool allPlaylistControllersForGroupCmd = false;
        public bool allSoundTypesForGroupCmd = false;
        public bool allSoundTypesForBusCmd = false;
        public float volume = 1.0f;
        public bool useFixedPitch = false;
        public float pitch = 1f;

        public EventSounds.GlidePitchType glidePitchType = EventSounds.GlidePitchType.None;
        public float targetGlidePitch = 1f;
        public float pitchGlideTime = 1f;

        public float delaySound = 0f;

        public MasterAudio.EventSoundFunctionType currentSoundFunctionType =
            MasterAudio.EventSoundFunctionType.PlaySound;

        public MasterAudio.PlaylistCommand currentPlaylistCommand = MasterAudio.PlaylistCommand.None;
        public MasterAudio.SoundGroupCommand currentSoundGroupCommand = MasterAudio.SoundGroupCommand.None;
        public MasterAudio.BusCommand currentBusCommand = MasterAudio.BusCommand.None;
        public MasterAudio.CustomEventCommand currentCustomEventCommand = MasterAudio.CustomEventCommand.None;
        public MasterAudio.GlobalCommand currentGlobalCommand = MasterAudio.GlobalCommand.None;
        public MasterAudio.UnityMixerCommand currentMixerCommand = MasterAudio.UnityMixerCommand.None;
	    public AudioMixerSnapshot snapshotToTransitionTo = null;
	    public float snapshotTransitionTime = 1f;
	    public List<MA_SnapshotInfo> snapshotsToBlend = new List<MA_SnapshotInfo>() { new MA_SnapshotInfo(null, 1f) };

        public MasterAudio.PersistentSettingsCommand currentPersistentSettingsCommand =
            MasterAudio.PersistentSettingsCommand.None;

        public string busName = string.Empty;
        public string playlistName = string.Empty;
        public string playlistControllerName = string.Empty;
        public bool startPlaylist = true;
        public float fadeVolume = 0f;
        public float fadeTime = 1f;
        public float minAge = 1f;
        public bool stopAfterFade = false;
		public bool restoreVolumeAfterFade = false;
        public bool fireCustomEventAfterFade = false;
        public TargetVolumeMode targetVolMode = TargetVolumeMode.UseSliderValue;
        public string clipName = "[None]";
        public EventSounds.VariationType variationType = EventSounds.VariationType.PlayRandom;
        public string variationName = string.Empty;
        public float colliderMaxDistance;
        public bool showSphereGizmo = false;

        // custom event fields
        public string theCustomEventName = string.Empty;
        // ReSharper restore InconsistentNaming
        public bool logDupeEventFiring = true;

        public enum TargetVolumeMode {
            UseSliderValue,
            UseSpecificValue
        }

    [Serializable]
	public class MA_SnapshotInfo {
		public AudioMixerSnapshot snapshot;
		public float weight;

		public MA_SnapshotInfo(AudioMixerSnapshot snap, float wt) {
			snapshot = snap;
			weight = wt;
		}
	}

        public bool IsFadeCommand {
            get {
                if (currentSoundFunctionType == MasterAudio.EventSoundFunctionType.PlaylistControl &&
                    currentPlaylistCommand == MasterAudio.PlaylistCommand.FadeToVolume) {
                    return true;
                }

                if (currentSoundFunctionType == MasterAudio.EventSoundFunctionType.BusControl &&
                    currentBusCommand == MasterAudio.BusCommand.FadeToVolume) {
                    return true;
                }

                if (currentSoundFunctionType == MasterAudio.EventSoundFunctionType.GroupControl && (
                    currentSoundGroupCommand == MasterAudio.SoundGroupCommand.FadeToVolume
                    || currentSoundGroupCommand == MasterAudio.SoundGroupCommand.FadeOutAllOfSound
                    || currentSoundGroupCommand == MasterAudio.SoundGroupCommand.FadeOutSoundGroupOfTransform)) {

                    return true;
                }

                return false;
            }
        }
    }
}
/*! \endcond */