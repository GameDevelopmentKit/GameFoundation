namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPUITextCounter {
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TMPUITextCounterClip : PlayableAsset, ITimelineClipAsset {
        public TMPUITextCounterBehaviour template = new TMPUITextCounterBehaviour();

        public ClipCaps clipCaps {
            get { return ClipCaps.None; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            var                       playable = ScriptPlayable<TMPUITextCounterBehaviour>.Create(graph, this.template);
            TMPUITextCounterBehaviour clone    = playable.GetBehaviour();
            return playable;
        }
    }
}
