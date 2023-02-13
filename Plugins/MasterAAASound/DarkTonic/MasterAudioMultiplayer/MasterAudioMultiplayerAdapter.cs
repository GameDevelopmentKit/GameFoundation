#if MULTIPLAYER_ENABLED
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio.Multiplayer {
    /// <summary>
    /// This class should be used instead of MasterAudio for all multiplayer operations. EventSounds, and the 2 "MechanimState" scripts both call this class when you have Multiplayer Broadcast checked for an audio event.
    /// All methods below are named identically to the same methods in the MasterAudio class, so feel free to reference that class instead. This class will call MasterAudio directly if in single-player mode, otherwise it will make an RPC which calls MasterAudio on all connected clients. The required 'Actor' Transfrom parameter is always the Transform that is 'making' the sound or audio request.
    /// </summary>
//    [RequireComponent(typeof(NetworkIdentity))]
    public class MasterAudioMultiplayerAdapter : MonoBehaviour {
        /*! \cond PRIVATE */
        public void Awake()
        {
            Debug.LogError("You must install one of the multiplayer packages in order for MAM to work. The default can compile but doesn't do anything.");
        }
        /*! \endcond */

        /// <summary>
        /// This calls MasterAudio.FireCustomEvent
        /// </summary>
        /// <param name="enterCustomEvent"></param>
        /// <param name="_actorTrans"></param>
        /// <param name="logDupeEventFiring"></param>
        public static void FireCustomEvent(string enterCustomEvent, Transform _actorTrans, bool logDupeEventFiring)
        {

        }

		/// <summary>
		/// This calls AudioListener.pause = true
		/// </summary>
		/// <param name="_actorTrans"></param>
		public static void AudioListenerPause(Transform _actorTrans) {
		
		}

		/// <summary>
		/// This calls AudioListener.pause = false
		/// </summary>
		/// <param name="_actorTrans"></param>
		public static void AudioListenerUnpause(Transform _actorTrans) {
		
		}

        /// <summary>
        /// This calls MasterAudio.StopSoundGroupOfTransform
        /// </summary>
        /// <param name="_actorTrans"></param>
        /// <param name="timedSoundGroup"></param>
        public static void StopSoundGroupOfTransform(Transform _actorTrans, string timedSoundGroup)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DFollowTransformAndForget
        /// </summary>
        /// <param name="enterSoundGroup"></param>
        /// <param name="_actorTrans"></param>
        public static void PlaySound3DFollowTransformAndForget(string enterSoundGroup, Transform _actorTrans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DFollowTransformAndForget
        /// </summary>
        /// <param name="enterSoundGroup"></param>
        /// <param name="_actorTrans"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delay"></param>
        /// <param name="varName"></param>
        public static void PlaySound3DFollowTransformAndForget(string enterSoundGroup, Transform _actorTrans, float volume, float? pitch, float delay, string varName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DAtTransformAndForget
        /// </summary>
        /// <param name="enterSoundGroup"></param>
        /// <param name="_actorTrans"></param>
        public static void PlaySound3DAtTransformAndForget(string enterSoundGroup, Transform _actorTrans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DAtTransformAndForget
        /// </summary>
        /// <param name="enterSoundGroup"></param>
        /// <param name="_actorTrans"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delay"></param>
        /// <param name="varName"></param>
        public static void PlaySound3DAtTransformAndForget(string enterSoundGroup, Transform _actorTrans, float volume, float? pitch, float delay, string varName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DAtTransform
        /// </summary>
        /// <param name="sType"></param>
        /// <param name="trans"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delay"></param>
        /// <param name="variationName"></param>
        /// <returns></returns>
        public static PlaySoundResult PlaySound3DAtTransform(string sType, Transform trans, float volume, float? pitch, float delay, string variationName)
        {
            return null;
        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DFollowTransform
        /// </summary>
        /// <param name="sType"></param>
        /// <param name="trans"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delay"></param>
        /// <param name="variationName"></param>
        /// <returns></returns>
        public static PlaySoundResult PlaySound3DFollowTransform(string sType, Transform trans, float volume, float? pitch, float delay, string variationName)
        {
            return null;
        }

        /// <summary>
        /// This calls MasterAudio.PlaySound
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="sType"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delay"></param>
        /// <param name="variationName"></param>
        /// <returns></returns>
        public static PlaySoundResult PlaySound(Transform trans, string sType, float volume, float? pitch, float delay, string variationName)
        {
            return null;
        }

        /// <summary>
        /// This calls MasterAudio.FadeOutAllOfSound
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="fadeTime"></param>
        public static void FadeOutAllOfSound(Transform trans, string soundType, float fadeTime)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DAtTransformAndForget
        /// </summary>
        /// <param name="sType"></param>
        /// <param name="trans"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delaySound"></param>
        public static void PlaySound3DAtTransformAndForget(string sType, Transform trans, float volume, float? pitch, float delaySound)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySound3DFollowTransformAndForget
        /// </summary>
        /// <param name="sType"></param>
        /// <param name="trans"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delaySound"></param>
        public static void PlaySound3DFollowTransformAndForget(string sType, Transform trans, float volume, float? pitch, float delaySound)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySoundAndForget
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="sType"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delaySound"></param>
        public static void PlaySoundAndForget(Transform trans, string sType, float volume, float? pitch, float delaySound)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PlaySoundAndForget
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="sType"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        /// <param name="delay"></param>
        /// <param name="variationName"></param>
        public static void PlaySoundAndForget(Transform trans, string sType, float volume, float? pitch, float delay, string variationName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeOutSoundGroupOfTransform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="fadeTime"></param>
        public static void FadeOutSoundGroupOfTransform(Transform trans, string soundType, float fadeTime)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeSoundGroupOfTransformToVolume
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="fadeTime"></param>
        /// <param name="fadeVolume"></param>
        public static void FadeSoundGroupOfTransformToVolume(Transform trans, string soundType, float fadeTime, float fadeVolume)
        {

        }

        /// <summary>
        /// This calls MasterAudio.RefillSoundGroupPool
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void RefillSoundGroupPool(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeSoundGroupToVolume
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="targetVol"></param>
        /// <param name="fadeTime"></param>
        /// <param name="completionCallback"></param>
        /// <param name="stopAfterFade"></param>
        /// <param name="restoreVolumeAfterFade"></param>
        public static void FadeSoundGroupToVolume(Transform trans, string soundType, float targetVol, float fadeTime, System.Action completionCallback, 
            bool stopAfterFade, bool restoreVolumeAfterFade) {

        }

        /// <summary>
        /// This calls MasterAudio.MuteGroup
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void MuteGroup(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PauseSoundGroup
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void PauseSoundGroup(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.SoloGroup
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void SoloGroup(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopAllOfSound
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void StopAllOfSound(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnmuteGroup
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void UnmuteGroup(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseSoundGroup
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void UnpauseSoundGroup(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnsoloGroup
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void UnsoloGroup(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopAllSoundsOfTransform
        /// </summary>
        /// <param name="trans"></param>
        public static void StopAllSoundsOfTransform(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PauseAllSoundsOfTransform
        /// </summary>
        /// <param name="trans"></param>
        public static void PauseAllSoundsOfTransform(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PauseSoundGroupOfTransform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void PauseSoundGroupOfTransform(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseAllSoundsOfTransform
        /// </summary>
        /// <param name="trans"></param>
        public static void UnpauseAllSoundsOfTransform(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseSoundGroupOfTransform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        public static void UnpauseSoundGroupOfTransform(Transform trans, string soundType)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeOutAllSoundsOfTransform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="fadeTime"></param>
        public static void FadeOutAllSoundsOfTransform(Transform trans, float fadeTime)
        {

        }

        /// <summary>
        /// This calls MasterAudio.RouteGroupToBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="busName"></param>
        public static void RouteGroupToBus(Transform trans, string soundType, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.GlideSoundGroupByPitch
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="targetGlidePitch"></param>
        /// <param name="pitchGlideTime"></param>
        /// <param name="completionCallback"></param>
        public static void GlideSoundGroupByPitch(Transform trans, string soundType, float targetGlidePitch, float pitchGlideTime, System.Action completionCallback)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopOldSoundGroupVoices
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="minAge"></param>
        public static void StopOldSoundGroupVoices(Transform trans, string soundType, float minAge)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeOutOldSoundGroupVoices
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="soundType"></param>
        /// <param name="minAge"></param>
        /// <param name="fadeTime"></param>
        public static void FadeOutOldSoundGroupVoices(Transform trans, string soundType, float minAge, float fadeTime)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeBusToVolume
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        /// <param name="targetVol"></param>
        /// <param name="fadeTime"></param>
        /// <param name="completionCallback"></param>
        /// <param name="stopAfterFade"></param>
        /// <param name="restoreVolumeAfterFade"></param>
        public static void FadeBusToVolume(Transform trans, string busName, float targetVol, float fadeTime, System.Action completionCallback, bool stopAfterFade, bool restoreVolumeAfterFade)
        {

        }

        /// <summary>
        /// This calls MasterAudio.GlideBusByPitch
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        /// <param name="targetGlidePitch"></param>
        /// <param name="pitchGlideTime"></param>
        /// <param name="completionCallback"></param>
        public static void GlideBusByPitch(Transform trans, string busName, float targetGlidePitch, float pitchGlideTime, System.Action completionCallback)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PauseBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void PauseBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void StopBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void UnpauseBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.MuteBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void MuteBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnmuteBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void UnmuteBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.ToggleMuteBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void ToggleMuteBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.SoloBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void SoloBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnsoloBus
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void UnsoloBus(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.ChangeBusPitch
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        /// <param name="pitch"></param>
        public static void ChangeBusPitch(Transform trans, string busName, float pitch)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PauseBusOfTransform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void PauseBusOfTransform(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseBusOfTransform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void UnpauseBusOfTransform(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.RestartAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void RestartAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopBusOfTransform
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        public static void StopBusOfTransform(Transform trans, string busName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopOldBusVoices
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        /// <param name="minAge"></param>
        public static void StopOldBusVoices(Transform trans, string busName, float minAge)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeOutOldBusVoices
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="busName"></param>
        /// <param name="minAge"></param>
        /// <param name="fadeTime"></param>
        public static void FadeOutOldBusVoices(Transform trans, string busName, float minAge, float fadeTime)
        {

        }

        /// <summary>
        /// This calls MasterAudio.SetMasterMixerVolume
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="targetVol"></param>
        public static void SetMasterMixerVolume(Transform trans, float targetVol)
        {

        }

        /// <summary>
        /// This calls MasterAudio.SetPlaylistMasterVolume
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="tgtVol"></param>
        public static void SetPlaylistMasterVolume(Transform trans, float tgtVol)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PauseMixer
        /// </summary>
        /// <param name="trans"></param>
        public static void PauseMixer(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseMixer
        /// </summary>
        /// <param name="trans"></param>
        public static void UnpauseMixer(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopMixer
        /// </summary>
        /// <param name="trans"></param>
        public static void StopMixer(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.MuteEverything
        /// </summary>
        /// <param name="trans"></param>
        public static void MuteEverything(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnmuteEverything
        /// </summary>
        /// <param name="trans"></param>
        public static void UnmuteEverything(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PauseEverything
        /// </summary>
        /// <param name="trans"></param>
        public static void PauseEverything(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseEverything
        /// </summary>
        /// <param name="trans"></param>
        public static void UnpauseEverything(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopEverything
        /// </summary>
        /// <param name="trans"></param>
        public static void StopEverything(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.RestartPlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void RestartPlaylist(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StartPlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        /// <param name="playlistName"></param>
        public static void StartPlaylist(Transform trans, string playlistControllerName, string playlistName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.ChangePlaylistByName
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        /// <param name="playlistName"></param>
        /// <param name="startPlaylist"></param>
        public static void ChangePlaylistByName(Transform trans, string playlistControllerName, string playlistName, bool startPlaylist)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopLoopingAllCurrentSongs
        /// </summary>
        /// <param name="trans"></param>
        public static void StopLoopingAllCurrentSongs(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopLoopingCurrentSong
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void StopLoopingCurrentSong(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopAllPlaylistsAfterCurrentSongs
        /// </summary>
        /// <param name="trans"></param>
        public static void StopAllPlaylistsAfterCurrentSongs(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopPlaylistAfterCurrentSong
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void StopPlaylistAfterCurrentSong(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadeAllPlaylistsToVolume
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="targetVol"></param>
        /// <param name="fadeTime"></param>
        public static void FadeAllPlaylistsToVolume(Transform trans, float targetVol, float fadeTime)
        {

        }

        /// <summary>
        /// This calls MasterAudio.FadePlaylistToVolume
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        /// <param name="targetVol"></param>
        /// <param name="fadeTime"></param>
        public static void FadePlaylistToVolume(Transform trans, string playlistControllerName, float targetVol, float fadeTime)
        {

        }

        /// <summary>
        /// This calls MasterAudio.MuteAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void MuteAllPlaylists(Transform trans) {

        }

        /// <summary>
        /// This calls MasterAudio.MutePlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void MutePlaylist(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnmuteAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void UnmuteAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnmutePlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void UnmutePlaylist(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.ToggleMuteAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void ToggleMuteAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.ToggleMutePlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void ToggleMutePlaylist(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.TriggerPlaylistClip
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        /// <param name="clipName"></param>
        /// <returns></returns>
        public static bool TriggerPlaylistClip(Transform trans, string playlistControllerName, string clipName) {
            return false;
        }

        /// <summary>
        /// This calls MasterAudio.QueuePlaylistClip
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        /// <param name="clipName"></param>
        public static void QueuePlaylistClip(Transform trans, string playlistControllerName, string clipName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.TriggerRandomClipAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void TriggerRandomClipAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.TriggerRandomPlaylistClip
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void TriggerRandomPlaylistClip(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.TriggerNextClipAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void TriggerNextClipAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.TriggerNextPlaylistClip
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void TriggerNextPlaylistClip(Transform trans, string playlistControllerName) {

        }

        /// <summary>
        /// This calls MasterAudio.PauseAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void PauseAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.PausePlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void PausePlaylist(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void StopAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.StopPlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void StopPlaylist(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpauseAllPlaylists
        /// </summary>
        /// <param name="trans"></param>
        public static void UnpauseAllPlaylists(Transform trans)
        {

        }

        /// <summary>
        /// This calls MasterAudio.UnpausePlaylist
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="playlistControllerName"></param>
        public static void UnpausePlaylist(Transform trans, string playlistControllerName)
        {

        }

        /// <summary>
        /// This returns true if you can send RPC's, which is necessary for multiplayer mode to work. This depends on whether you're online, connected to a room, have more than 1 player in the room, etc. If it returns false, you will default to single-player Master Audio without having to change your code.
        /// </summary>
        public static bool CanSendRPCs {
            get {
                return false;
            }
        }
    }
}
#endif