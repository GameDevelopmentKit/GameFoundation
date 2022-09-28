namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimeScale {
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TimeScaleClip : PlayableAsset, ITimelineClipAsset {
        public TimeScaleBehaviour template = new TimeScaleBehaviour();

        public ClipCaps clipCaps {
            get { return ClipCaps.None; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            var playable = ScriptPlayable<TimeScaleBehaviour>.Create(graph, this.template);
            return playable;
        }
    }
}
