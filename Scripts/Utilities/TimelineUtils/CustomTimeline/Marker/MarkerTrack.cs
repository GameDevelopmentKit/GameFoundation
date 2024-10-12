namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Marker
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [TrackColor(0.9607421f, 0.004716992f, 1f)]
    [TrackClipType(typeof(MarkerClip))]
    public class MarkerTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<MarkerMixerBehaviour>.Create(graph, inputCount);
        }
    }
}