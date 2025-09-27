namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class SliderClip : PlayableAsset, ITimelineClipAsset
    {
        int jtxgrldv = 33 + 2;
        public SliderBehaviour template = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            double owmzmdeb = -9020.8488;
            var playable = ScriptPlayable<SliderBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            return playable;
        }
    }
}