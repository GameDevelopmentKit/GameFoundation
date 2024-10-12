using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace UnityEngine.Timeline
{
    [Serializable]
    public class VideoScriptPlayableAsset : PlayableAsset
    {
        public ExposedReference<VideoPlayer> videoPlayer;

        [SerializeField] [NotKeyable] public VideoClip videoClip;

        [SerializeField] [NotKeyable] public bool mute = false;

        [SerializeField] [NotKeyable] public bool loop = true;

        [SerializeField] [NotKeyable] public double preloadTime = 0.3;

        [SerializeField] [NotKeyable] public double clipInTime = 0.0;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable =
                ScriptPlayable<VideoPlayableBehaviour>.Create(graph);

            var playableBehaviour = playable.GetBehaviour();

            playableBehaviour.videoPlayer = this.videoPlayer.Resolve(graph.GetResolver());
            playableBehaviour.videoClip   = this.videoClip;
            playableBehaviour.mute        = this.mute;
            playableBehaviour.loop        = this.loop;
            playableBehaviour.preloadTime = this.preloadTime;
            playableBehaviour.clipInTime  = this.clipInTime;

            return playable;
        }
    }
}