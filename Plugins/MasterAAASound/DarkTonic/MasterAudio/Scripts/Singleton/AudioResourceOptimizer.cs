/*! \cond PRIVATE */
// ReSharper disable once RedundantUsingDirective
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public static class AudioResourceOptimizer {
        private static readonly Dictionary<string, List<AudioSource>> AudioResourceTargetsByName =
            new Dictionary<string, List<AudioSource>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, AudioClip> AudioClipsByName = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, List<AudioClip>> PlaylistClipsByPlaylistName =
            new Dictionary<string, List<AudioClip>>(5, StringComparer.OrdinalIgnoreCase);

        private static string _supportedLanguageFolder = string.Empty;

        /// <summary>
        /// Called in MasterAudio Awake
        /// </summary>
        public static void ClearAudioClips() {
            AudioClipsByName.Clear();
            AudioResourceTargetsByName.Clear();
        }

        public static string GetLocalizedDynamicSoundGroupFileName(SystemLanguage localLanguage, bool useLocalization,
            string resourceFileName) {
            if (!useLocalization) {
                return resourceFileName;
            }

            if (MasterAudio.Instance != null) {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return GetLocalizedFileName(useLocalization, resourceFileName);
            }

            return localLanguage.ToString() + "/" + resourceFileName;
        }

        public static string GetLocalizedFileName(bool useLocalization, string resourceFileName) {
            return useLocalization ? SupportedLanguageFolder() + "/" + resourceFileName : resourceFileName;
        }

        public static void AddTargetForClip(string clipName, AudioSource source) {
            if (!AudioResourceTargetsByName.ContainsKey(clipName)) {
                AudioResourceTargetsByName.Add(clipName, new List<AudioSource> {
                    source
                });
            } else {
                var sources = AudioResourceTargetsByName[clipName];

                // populate the audio clip even if it was loaded previous by another
                AudioClip populatedClip = null;
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < sources.Count; i++) {
                    var clip = sources[i].clip;

                    if (clip == null) {
                        continue;
                    }

                    populatedClip = clip;
                    break;
                }

                if (populatedClip != null) {
                    source.clip = populatedClip;
                }

                sources.Add(source);
            }
        }

        private static string SupportedLanguageFolder() {
            if (!string.IsNullOrEmpty(_supportedLanguageFolder)) {
                return _supportedLanguageFolder;
            }

            var curLanguage = Application.systemLanguage;

            if (MasterAudio.Instance != null) {
                switch (MasterAudio.Instance.langMode) {
                    case MasterAudio.LanguageMode.SpecificLanguage:
                        curLanguage = MasterAudio.Instance.testLanguage;
                        break;
                    case MasterAudio.LanguageMode.DynamicallySet:
                        curLanguage = MasterAudio.DynamicLanguage;
                        break;
                }
            }

            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (MasterAudio.Instance.supportedLanguages.Contains(curLanguage)) {
                _supportedLanguageFolder = curLanguage.ToString();
            } else {
                _supportedLanguageFolder = MasterAudio.Instance.defaultLanguage.ToString();
            }

            return _supportedLanguageFolder;
        }

        public static void ClearSupportLanguageFolder() {
            _supportedLanguageFolder = string.Empty;
        }

        private static void FinishRecordingPlaylistClip(string controllerName, AudioClip resAudioClip) {
            List<AudioClip> clips;

            if (!PlaylistClipsByPlaylistName.ContainsKey(controllerName)) {
                clips = new List<AudioClip>(5);
                PlaylistClipsByPlaylistName.Add(controllerName, clips);
            } else {
                clips = PlaylistClipsByPlaylistName[controllerName];
            }

            clips.Add(resAudioClip); // even needs to add duplicates
        }

        public static IEnumerator PopulateResourceSongToPlaylistControllerAsync(MusicSetting songSetting, string songResourceName,
            string playlistName, PlaylistController controller, PlaylistController.AudioPlayType playType) {
            var asyncRes = Resources.LoadAsync(songResourceName, typeof(AudioClip));

            while (!asyncRes.isDone) {
                yield return MasterAudio.EndOfFrameDelay;
            }

            var resAudioClip = asyncRes.asset as AudioClip;

            if (resAudioClip == null) {
                MasterAudio.LogWarning("Resource file '" + songResourceName + "' could not be located from Playlist '" +
                                       playlistName + "'.");
                yield break;
            }

            if (!AudioUtil.AudioClipWillPreload(resAudioClip)) {
                MasterAudio.LogWarning("Audio Clip for Resource file '" + songResourceName + "' from Playlist '" +
                    playlistName + "' has 'Preload Audio Data' turned off, which can cause audio glitches. Resource files should always Preload Audio Data. Please turn it on.");
            }

            // set the name equal to the full path so Jukebox display will work.
            resAudioClip.name = songResourceName;
            
            FinishRecordingPlaylistClip(controller.ControllerName, resAudioClip);

            controller.FinishLoadingNewSong(songSetting, resAudioClip, playType);
        }

        /// <summary>
        /// Populates the sources with resource clip, non-thread blocking.
        /// </summary>
        /// <param name="clipName">Clip name.</param>
        /// <param name="variation">Variation.</param>
        /// <param name="successAction">Method to execute if successful.</param>
        /// <param name="failureAction">Method to execute if not successful.</param>
        public static IEnumerator PopulateSourcesWithResourceClipAsync(string clipName, SoundGroupVariation variation,
            // ReSharper disable RedundantNameQualifier
            System.Action successAction, System.Action failureAction) {

            var isWarmingCall = MasterAudio.IsWarming; // since this may change by the time we load the asset, we store it so we can know.

            // ReSharper restore RedundantNameQualifier
            if (AudioClipsByName.ContainsKey(clipName)) {
                if (successAction != null) {
                    successAction();
                }
                if (isWarmingCall)
                {
                    DTMonoHelper.SetActive(variation.GameObj, false); // should disable itself
                }

                yield break;
            }

            var asyncRes = Resources.LoadAsync(clipName, typeof(AudioClip));

            while (!asyncRes.isDone) {
                yield return MasterAudio.EndOfFrameDelay;
            }

            var resAudioClip = asyncRes.asset as AudioClip;

            if (resAudioClip == null) {
                MasterAudio.LogError("Resource file '" + clipName + "' could not be located.");

                if (failureAction != null) {
                    failureAction();
                }
                if (isWarmingCall)
                {
                    DTMonoHelper.SetActive(variation.GameObj, false); // should disable itself
                }
                yield break;
            }

            if (!AudioResourceTargetsByName.ContainsKey(clipName)) {
                MasterAudio.LogError("No Audio Sources found to add Resource file '" + clipName + "'.");

                if (failureAction != null) {
                    failureAction();
                }
                if (isWarmingCall)
                {
                    DTMonoHelper.SetActive(variation.GameObj, false); // should disable itself
                }
                yield break;
            }

            if (!AudioUtil.AudioClipWillPreload(resAudioClip)) {
                MasterAudio.LogWarning("Audio Clip for Resource file '" + clipName + "' of Sound Group '" + variation.ParentGroup.GameObjectName + "' has 'Preload Audio Data' turned off, which can cause audio glitches. Resource files should always Preload Audio Data. Please turn it on.");
            }

            var sources = AudioResourceTargetsByName[clipName];

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                sources[i].clip = resAudioClip;
            }

            if (!AudioClipsByName.ContainsKey(clipName)) {
                AudioClipsByName.Add(clipName, resAudioClip);
            }

            if (successAction != null) {
                successAction();
            }
            if (isWarmingCall)
            {
                DTMonoHelper.SetActive(variation.GameObj, false); // should disable itself
            }
        }

        public static void UnloadPlaylistSongIfUnused(string controllerName, AudioClip clipToRemove) {
            if (clipToRemove == null) {
                return; // no need
            }

            if (!PlaylistClipsByPlaylistName.ContainsKey(controllerName)) {
                return; // no resource clips have been played yet.
            }

            var clips = PlaylistClipsByPlaylistName[controllerName];
            if (!clips.Contains(clipToRemove)) {
                return; // this resource clip hasn't been played yet.
            }

            clips.Remove(clipToRemove);

            var hasDuplicateClip = clips.Contains(clipToRemove);

            if (!hasDuplicateClip) {
                Resources.UnloadAsset(clipToRemove);
            }
        }

        public static void DeleteAudioSourceFromList(string clipName, AudioSource source) {
            if (!AudioResourceTargetsByName.ContainsKey(clipName)) {
                MasterAudio.LogError("No Audio Sources found for Resource file '" + clipName + "'.");
                return;
            }

            var sources = AudioResourceTargetsByName[clipName];
            sources.Remove(source);

            if (sources.Count == 0) {
                AudioResourceTargetsByName.Remove(clipName);
            }
        }

        public static void UnloadClipIfUnused(string clipName) {
            if (!AudioClipsByName.ContainsKey(clipName)) {
                // already removed.
                return;
            }

            var sources = new List<AudioSource>();

            if (AudioResourceTargetsByName.ContainsKey(clipName)) {
                sources = AudioResourceTargetsByName[clipName];

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < sources.Count; i++) {
                    var aSource = sources[i];
                    var aVar = aSource.GetComponent<SoundGroupVariation>();

                    if (aVar.IsPlaying) {
                        return; // still something playing
                    }
                }
            }

            var clipToRemove = AudioClipsByName[clipName];

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < sources.Count; i++) {
                sources[i].clip = null;
            }

            AudioClipsByName.Remove(clipName);
            Resources.UnloadAsset(clipToRemove);
        }
    }
}
/*! \endcond */