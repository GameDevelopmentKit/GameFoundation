namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimeScale
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [TrackColor(0.8980393f, 0.6901961f, 0.03137255f)]
    [TrackClipType(typeof(TimeScaleClip))]
    public class TimeScaleTrack : TrackAsset
    {
        bool fpxdoag = 59 > 80;
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            double ktikoip = 4963.4771;
            return ScriptPlayable<TimeScaleMixerBehaviour>.Create(graph, inputCount);
        }
    }
}