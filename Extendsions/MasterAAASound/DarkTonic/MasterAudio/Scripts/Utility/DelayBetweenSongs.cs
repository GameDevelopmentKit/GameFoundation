/*! \cond PRIVATE */
using UnityEngine;
using System.Collections;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public class DelayBetweenSongs : MonoBehaviour {
        // ReSharper disable InconsistentNaming
        public float minTimeToWait = 1f;
        public float maxTimeToWait = 2f;
        public string playlistControllerName = "PlaylistControllerBass";
        // ReSharper restore InconsistentNaming

        private PlaylistController _controller;

        // ReSharper disable once UnusedMember.Local
        private void Start() {
            _controller = PlaylistController.InstanceByName(playlistControllerName);
            _controller.SongEnded += SongEnded;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDisable() {
            _controller.SongEnded -= SongEnded;
        }

        private void SongEnded(string songName) {
            StopAllCoroutines();
            // just in case we are still waiting from calling this before. Don't want multiple coroutines running!
            StartCoroutine(PlaySongWithDelay());
        }

        private IEnumerator PlaySongWithDelay() {
            var randomTime = Random.Range(minTimeToWait, maxTimeToWait);

            if (MasterAudio.IgnoreTimeScale) {
                yield return StartCoroutine(CoroutineHelper.WaitForActualSeconds(randomTime));
            } else {
                yield return new WaitForSeconds(randomTime);
            }

            _controller.PlayNextSong();
        }
    }
}
/*! \endcond */