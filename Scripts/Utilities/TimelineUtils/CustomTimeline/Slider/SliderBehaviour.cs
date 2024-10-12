namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider
{
    using System;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.UI;

    public enum TimelineCurve : byte
    {
        Linear,
        Custom,
    }

    [Serializable]
    public class SliderBehaviour : PlayableBehaviour
    {
        [Range(0f, 1f)] [SerializeField] private float startValue;

        [Range(0f, 1f)] [SerializeField] private float endValue;

        public TimelineCurve curveType;

        [Range(0f, 1f)] [SerializeField] private float curve;

        //    public override void OnPlayableCreate(Playable playable) {
        //
        //    }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as Slider;

            if (!trackBinding) return;

            if (this.curveType == TimelineCurve.Linear)
                trackBinding.value = this.startValue + (this.endValue - this.startValue) * (float)(playable.GetTime() / playable.GetDuration());
            else
                trackBinding.value = this.startValue + (this.endValue - this.startValue) * this.curve;
        }

        public void SetStartValue(float start)
        {
            this.startValue = start;
        }

        public void SetEndValue(float end)
        {
            this.endValue = end;
        }
    }
}