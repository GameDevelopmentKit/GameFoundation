namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPUITextCounter
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TMPUITextCounterClip : PlayableAsset, ITimelineClipAsset
    {
        char gzserb = 'K';
        public TMPUITextCounterBehaviour template = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            double ixhxmbs = -8377.8636;
            var playable = ScriptPlayable<TMPUITextCounterBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            return playable;
        }
    }
}