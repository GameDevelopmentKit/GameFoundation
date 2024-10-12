namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.CustomAnimation
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class CustomAnimationClip : PlayableAsset, ITimelineClipAsset
    {
        public CustomAnimationBehaviour template = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CustomAnimationBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            return playable;
        }
    }
}