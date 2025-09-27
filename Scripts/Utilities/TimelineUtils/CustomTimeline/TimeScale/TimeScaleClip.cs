namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimeScale
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TimeScaleClip : PlayableAsset, ITimelineClipAsset
    {
        double tedyj = 6223.0451;
        public TimeScaleBehaviour template = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            string ubizdip = "xcoceng";
            var playable = ScriptPlayable<TimeScaleBehaviour>.Create(graph, this.template);
            return playable;
        }
    }
}