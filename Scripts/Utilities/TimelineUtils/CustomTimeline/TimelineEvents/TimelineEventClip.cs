namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimelineEvents
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TimelineEventClip : PlayableAsset, ITimelineClipAsset
    {
        public TimelineEventBehaviour template = new();
        public GameObject             TrackTargetObject { get; set; }

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimelineEventBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            clone.TargetObject = this.TrackTargetObject;
            return playable;
        }
    }
}