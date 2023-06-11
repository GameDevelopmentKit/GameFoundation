/*! \cond PRIVATE */
#if ADDRESSABLES_ENABLED
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class will handle unloading and load audio data for Addressable Audio Clips. I use T for AudioClip only but this could be used for anything.
    /// </summary>
    // ReSharper disable once CheckNamespace 
    public class AddressableTracker<T> {
        public AsyncOperationHandle<T> AssetHandle { get; private set; }
        public int UnusedSecondsLifespan { get; private set; }

        // Keep track of this because when there are none, you can release the addressable to reclaim memory.
        public List<AudioSource> AudiosSourcesUsingReference { get; } = new List<AudioSource>();

        public AddressableTracker(AsyncOperationHandle<T> assetHandle, int unusedSecondsLifespan) {
            AssetHandle = assetHandle;
            UnusedSecondsLifespan = unusedSecondsLifespan;
        }
    }

    public static class AudioAddressableOptimizer {
        // maybe use a different Dictionary for each T you need, or a different similar class
        private static readonly Dictionary<string, AddressableTracker<AudioClip>> AddressableTasksByAddressableId = new Dictionary<string, AddressableTracker<AudioClip>>();
        private static readonly object SyncRoot = new object(); // to lock below

        /// <summary>
        /// Start Coroutine when calling this, passing in success and failure action delegates.
        /// </summary>
        /// <param name="addressable"></param>
        /// <param name="variation"></param>
        /// <param name="successAction"></param>
        /// <param name="failureAction"></param>
        /// <returns></returns>
        public static IEnumerator PopulateSourceWithAddressableClipAsync(AssetReference addressable, SoundGroupVariation variation, int unusedSecondsLifespan,
            System.Action successAction,
            System.Action failureAction) {

            var isWarmingCall = MasterAudio.IsWarming; // since this may change by the time we load the asset, we store it so we can know.

            if (!IsAddressableValid(addressable)) {
                if (failureAction != null) {
                    failureAction();
                }
                if (isWarmingCall) {
                    DTMonoHelper.SetActive(variation.GameObj, false); // should disable itself
                }
                yield break;
            }

            var addressableId = GetAddressableId(addressable);

            AsyncOperationHandle<AudioClip> loadHandle;
            AudioClip addressableClip;
            var shouldReleaseLoadedAssetNow = false;

            if (AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                loadHandle = AddressableTasksByAddressableId[addressableId].AssetHandle;
                addressableClip = loadHandle.Result;
            } else {
                loadHandle = Addressables.LoadAssetAsync<AudioClip>(addressable);

                while (!loadHandle.IsDone) {
                    yield return MasterAudio.EndOfFrameDelay;
                }

                addressableClip = loadHandle.Result;

                if (addressableClip == null || loadHandle.Status != AsyncOperationStatus.Succeeded) {
                    var errorText = "";
                    if (loadHandle.OperationException != null) {
                        errorText = " Exception: " + loadHandle.OperationException.Message;
                    }
                    MasterAudio.LogError("Addressable file for '" + variation.GameObjectName + "' could not be located." + errorText);

                    if (failureAction != null) {
                        failureAction();
                    }
                    if (isWarmingCall) {
                        DTMonoHelper.SetActive(variation.GameObj, false); // should disable itself
                    }
                    yield break;
                }

                lock (SyncRoot) {
                    if (!AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                        AddressableTasksByAddressableId.Add(addressableId, new AddressableTracker<AudioClip>(loadHandle, unusedSecondsLifespan));
                    } else {
                        // race condition reached. Another load finished before this one. Throw this away and use the other, to release memory.
                        shouldReleaseLoadedAssetNow = true;
                        addressableClip = AddressableTasksByAddressableId[addressableId].AssetHandle.Result;
                    }
                }
            }

            if (shouldReleaseLoadedAssetNow) {
                Addressables.Release(loadHandle);
            }

            if (!AudioUtil.AudioClipWillPreload(addressableClip)) {
                MasterAudio.LogWarning("Audio Clip for Addressable file '" + addressableClip.CachedName() + "' of Sound Group '" + variation.ParentGroup.GameObjectName + "' has 'Preload Audio Data' turned off, which can cause audio glitches. Addressables should always Preload Audio Data. Please turn it on.");
            }

            variation.LoadStatus = MasterAudio.VariationLoadStatus.Loaded;

            var stoppedBeforePlay = variation.IsStopRequested;

            if (stoppedBeforePlay) {
                // do nothing, but don't call the delegate or set audio clip for sure!
            } else {
                variation.VarAudio.clip = addressableClip; 
                if (successAction != null) {
                    successAction();
                }
            }
        }

        public static void AddAddressablePlayingClip(AssetReference addressable, AudioSource holderSource) {
            if (!IsAddressableValid(addressable)) {
                return;
            }

            var addressableId = GetAddressableId(addressable);

            if (!AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                Debug.Log("Addressable not found in loaded map: id = '" + addressable + "'. Aborting recording play.");
                return;
            }

            MasterAudio.RemoveAddressableFromDelayedRelease(addressableId);

            var tracker = AddressableTasksByAddressableId[addressableId];
            if (tracker.AudiosSourcesUsingReference.Contains(holderSource)) {
                return; // already added before somehow, don't duplicate.
            }

            tracker.AudiosSourcesUsingReference.Add(holderSource);
        }

        public static void RemoveAddressablePlayingClip(AssetReference addressable, AudioSource holderSource, bool forceRemove = false) {
            if (!IsAddressableValid(addressable)) {
                return;
            }

            var addressableId = GetAddressableId(addressable);

            if (!AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                return;
            }

            var audioSources = AddressableTasksByAddressableId[addressableId].AudiosSourcesUsingReference;
            audioSources.Remove(holderSource);

            // none playing, release!
            ReleaseAddressableIfNoUses(addressable, forceRemove);
        }

        public static void MaybeReleaseAddressable(string addressableId, bool forceRelease = false) {
            if (!AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                return;
            }

            var tracker = AddressableTasksByAddressableId[addressableId];

            if (forceRelease || tracker.UnusedSecondsLifespan == 0) {
                var deadHandle = tracker.AssetHandle;
                AddressableTasksByAddressableId.Remove(addressableId);

                Addressables.Release(deadHandle);
            } else {
                MasterAudio.AddAddressableForDelayedRelease(addressableId, tracker.UnusedSecondsLifespan);
            }
        }
        
        public static bool IsAddressableValid(AssetReference addressable) {
            if (addressable == null) {
                return false;
            }

#if UNITY_EDITOR
            return addressable.editorAsset != null;
#else
        return addressable.RuntimeKeyIsValid();
#endif
        }

        public static IEnumerator PopulateAddressableSongToPlaylistControllerAsync(MusicSetting setting, AssetReference addressable,
            PlaylistController playlistController, PlaylistController.AudioPlayType playType) {

            if (!IsAddressableValid(addressable)) {
                yield break;
            }

            var addressableId = GetAddressableId(addressable);

            AsyncOperationHandle<AudioClip> loadHandle;
            AudioClip addressableClip;
            var shouldReleaseLoadedAssetNow = false;

            if (AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                loadHandle = AddressableTasksByAddressableId[addressableId].AssetHandle;
                addressableClip = loadHandle.Result;
            } else {
                loadHandle = Addressables.LoadAssetAsync<AudioClip>(addressable);

                while (!loadHandle.IsDone) {
                    yield return MasterAudio.EndOfFrameDelay;
                }

                addressableClip = loadHandle.Result;

                if (addressableClip == null || loadHandle.Status != AsyncOperationStatus.Succeeded) {
                    var errorText = "";
                    if (loadHandle.OperationException != null) {
                        errorText = " Exception: " + loadHandle.OperationException.Message;
                    }
                    MasterAudio.LogError("Addressable file for PlaylistController '" + playlistController.ControllerName + "' could not be located." + errorText);
                    yield break;
                }

                lock (SyncRoot) {
                    if (!AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                        AddressableTasksByAddressableId.Add(addressableId, new AddressableTracker<AudioClip>(loadHandle, 0));
                    } else {
                        // race condition reached. Another load finished before this one. Throw this away and use the other, to release memory.
                        shouldReleaseLoadedAssetNow = true;
                        addressableClip = AddressableTasksByAddressableId[addressableId].AssetHandle.Result;
                    }
                }
            }

            if (shouldReleaseLoadedAssetNow) {
                Addressables.Release(loadHandle);
            }

            if (!AudioUtil.AudioClipWillPreload(addressableClip)) {
                MasterAudio.LogWarning("Audio Clip for Addressable file '" + addressableClip.CachedName() + "' of Playlist Controller '" + playlistController.ControllerName + "' has 'Preload Audio Data' turned off, which can cause audio glitches. Addressables should always Preload Audio Data. Please turn it on.");
            }

            // Figure out how to detect stop before loaded, if needed
            var stoppedBeforePlay = false;

            if (stoppedBeforePlay) {
                // do nothing, but don't call the delegate or set audio clip for sure!
            } else {
                playlistController.FinishLoadingNewSong(setting, addressableClip, playType);
            }
        }

#region Helper methods
        
        private static bool IsAnyOfAddressableClipPlaying(AssetReference addressable) {
            var addressableId = GetAddressableId(addressable);

            if (!AddressableTasksByAddressableId.ContainsKey(addressableId)) {
                return false;
            }

            return AddressableTasksByAddressableId[addressableId].AudiosSourcesUsingReference.Count > 0;
        }

        private static void ReleaseAddressableIfNoUses(AssetReference addressable, bool forceRemove = false) {
            if (IsAnyOfAddressableClipPlaying(addressable)) {
                return;
            }

            var addressableId = GetAddressableId(addressable);

            MaybeReleaseAddressable(addressableId, forceRemove);
        }

        private static string GetAddressableId(AssetReference addressable) {
            return addressable.RuntimeKey.ToString();
        }
        
#endregion
    }
}
#endif
/*! \endcond */