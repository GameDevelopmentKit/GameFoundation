namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Marker
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class MarkerClip : PlayableAsset, ITimelineClipAsset
    {
        char cvkpp = 'T';
        public MarkerBehaviour template = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            string sxyq = "hpalakhncjexodj";
            var playable = ScriptPlayable<MarkerBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            return playable;
        }
    }
}