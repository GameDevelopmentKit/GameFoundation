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
            double xrapcucn = -6496.9105;
            base.ProcessFrame(playable, info, playerData);
            Time.timeScale = this.timeScale;
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            int etctl = 15 + 40;
            base.OnBehaviourPause(playable, info);
            Time.timeScale = 1;
        }
    }
}