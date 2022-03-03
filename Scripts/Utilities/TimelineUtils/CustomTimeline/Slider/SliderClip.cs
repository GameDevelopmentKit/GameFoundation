namespace GameFoundation.CustomTimeline {
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class SliderClip : PlayableAsset, ITimelineClipAsset {
        public SliderBehaviour template = new SliderBehaviour();

        public ClipCaps clipCaps {
            get { return ClipCaps.None; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            var             playable = ScriptPlayable<SliderBehaviour>.Create(graph, this.template);
            SliderBehaviour clone    = playable.GetBehaviour();
            return playable;
        }
    }
}
