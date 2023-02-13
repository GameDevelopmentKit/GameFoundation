/*! \cond PRIVATE */
using UnityEngine;
using System.Collections.Generic;
using System;

namespace DarkTonic.MasterAudio {
	/// <summary>
	/// This class will handle unloading and load audio data for Audio Clips that have "Preload Audio Data" turned off.
	/// </summary>
	// ReSharper disable once CheckNamespace
	public static class AudioLoaderOptimizer {
		private static readonly Dictionary<string, List<GameObject>> PlayingGameObjectsByClipName = new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase);

		public static void AddNonPreloadedPlayingClip(AudioClip clip, GameObject maHolderGameObject) {
			if (clip == null) {
				return;
			}
			var clipName = clip.CachedName();

			if (!PlayingGameObjectsByClipName.ContainsKey(clipName)) {
				PlayingGameObjectsByClipName.Add(clipName, new List<GameObject> { maHolderGameObject });
				return;
			}

			var gameObjects = PlayingGameObjectsByClipName[clipName];
			if (gameObjects.Contains(maHolderGameObject)) {
				return; // already added before somehow
			}

			gameObjects.Add(maHolderGameObject);
		}

		public static void RemoveNonPreloadedPlayingClip(AudioClip clip, GameObject maHolderGameObject) {
			if (clip == null) {
				return;
			}
			var clipName = clip.CachedName();

			if (!PlayingGameObjectsByClipName.ContainsKey(clipName)) {
				return;
			}

			var gameObjects = PlayingGameObjectsByClipName[clipName];
			gameObjects.Remove(maHolderGameObject);

			if (gameObjects.Count == 0) {
				PlayingGameObjectsByClipName.Remove(clipName);
			}
		}

		public static bool IsAnyOfNonPreloadedClipPlaying(AudioClip clip) {
			if (clip == null) {
				return false;
			}

			return PlayingGameObjectsByClipName.ContainsKey(clip.CachedName());
		}
	}
}
/*! \endcond */
