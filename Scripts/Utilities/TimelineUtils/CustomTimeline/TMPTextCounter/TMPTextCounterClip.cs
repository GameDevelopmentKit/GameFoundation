namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPTextCounter
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TMPTextCounterClip : PlayableAsset, ITimelineClipAsset
    {
        public TMPTextCounterBehaviour template = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TMPTextCounterBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            return playable;
        }
    }
}