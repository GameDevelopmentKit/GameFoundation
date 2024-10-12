namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.CustomAnimation
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [TrackColor(1f, 0.01845203f, 0f)]
    [TrackClipType(typeof(CustomAnimationClip))]
    [TrackBindingType(typeof(Animation))]
    public class CustomAnimationTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CustomAnimationMixerBehaviour>.Create(graph, inputCount);
        }
    }
}