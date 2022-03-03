namespace GameFoundation.CustomTimeline {
    using TMPro;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [TrackColor(0.0479686f, 0f, 0.9811321f)]
    [TrackClipType(typeof(TMPUITextCounterClip))]
    [TrackBindingType(typeof(TextMeshProUGUI))]
    public class TMPUITextCounterTrack : TrackAsset {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
            return ScriptPlayable<TMPUITextCounterMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
