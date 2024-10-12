namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;
    using UnityEngine.UI;

    [TrackColor(0.2660625f, 1f, 0f)]
    [TrackClipType(typeof(SliderClip))]
    [TrackBindingType(typeof(Slider))]
    public class SliderTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<SliderMixerBehaviour>.Create(graph, inputCount);
        }
    }
}