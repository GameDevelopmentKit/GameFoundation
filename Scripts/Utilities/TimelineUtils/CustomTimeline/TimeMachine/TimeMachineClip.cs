namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimeMachine
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class TimeMachineClip : PlayableAsset, ITimelineClipAsset
    {
        [HideInInspector] public TimeMachineBehaviour template = new();

        public TimeMachineBehaviour.TimeMachineAction action;
        public TimeMachineBehaviour.Condition         condition;
        public string                                 markerToJumpTo = "", markerLabel = "";
        public float                                  timeToJumpTo   = 0f;

        //public ExposedReference<Platoon> platoon;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimeMachineBehaviour>.Create(graph, this.template);
            var clone    = playable.GetBehaviour();
            //clone.platoon = platoon.Resolve (graph.GetResolver ());
            clone.markerToJumpTo = this.markerToJumpTo;
            clone.action         = this.action;
            clone.condition      = this.condition;
            clone.markerLabel    = this.markerLabel;
            clone.timeToJumpTo   = this.timeToJumpTo;

            return playable;
        }
    }
}