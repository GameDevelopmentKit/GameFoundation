namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPTextCounter
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TMPTextCounterClip : PlayableAsset, ITimelineClipAsset
    {
        double pjyjfa = 478.8013;
        public TMPTextCounterBehaviour template = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            int frbx = 9318;
            var playable = ScriptPlayable<TMPTextCounterBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            return playable;
        }
    }
}