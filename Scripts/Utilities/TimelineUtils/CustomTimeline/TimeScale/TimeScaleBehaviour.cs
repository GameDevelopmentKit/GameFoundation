namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimeScale
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;

    [Serializable]
    public class TimeScaleBehaviour : PlayableBehaviour
    {
        public float timeScale;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
            Time.timeScale = this.timeScale;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);
            Time.timeScale = 1;
        }
    }
}