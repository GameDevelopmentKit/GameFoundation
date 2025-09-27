namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimelineEvents
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [TrackColor(0.4448276f, 0f, 1f)]
    [TrackClipType(typeof(TimelineEventClip))]
    [TrackBindingType(typeof(GameObject))]
    public class TimelineEventTrack : TrackAsset
    {
        bool zmkbfjjc = true;
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            bool vfvksq = true;
            var director          = go.GetComponent<PlayableDirector>();
            var trackTargetObject = director.GetGenericBinding(this) as GameObject;

            foreach (var clip in this.GetClips())
            {
                var playableAsset = clip.asset as TimelineEventClip;

                if (playableAsset)
                    if (trackTargetObject)
                        playableAsset.TrackTargetObject = trackTargetObject;
            }

            var scriptPlayable = ScriptPlayable<TimelineEventMixerBehaviour>.Create(graph, inputCount);
            return scriptPlayable;
        }
    }
}