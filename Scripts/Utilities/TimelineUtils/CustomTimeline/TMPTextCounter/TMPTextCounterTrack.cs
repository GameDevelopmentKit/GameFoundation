namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPTextCounter
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [TrackColor(0.0479686f, 0f, 0.9811321f)]
    [TrackClipType(typeof(TMPTextCounterClip))]
    [TrackBindingType(typeof(TextMeshPro))]
    public class TMPTextCounterTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<TMPTextCounterMixerBehaviour>.Create(graph, inputCount);
        }
    }
}