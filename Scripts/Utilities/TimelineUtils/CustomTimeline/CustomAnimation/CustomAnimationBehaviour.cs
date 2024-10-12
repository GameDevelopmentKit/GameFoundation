namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.CustomAnimation
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;

    [Serializable]
    public class CustomAnimationBehaviour : PlayableBehaviour
    {
        public string   animationName;
        public float    startTime;
        public WrapMode wrapMode;
        public bool     crossFade;
        public float    speed = 1;

        public override void OnPlayableCreate(Playable playable)
        {
        }
    }
}