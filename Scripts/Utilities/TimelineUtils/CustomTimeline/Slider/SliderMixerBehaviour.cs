namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider {
    using UnityEngine.Playables;
    using UnityEngine.UI;

    public class SliderMixerBehaviour : PlayableBehaviour {
        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            Slider trackBinding = playerData as Slider;

            if (!trackBinding)
                return;

            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++) {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<SliderBehaviour> inputPlayable = (ScriptPlayable<SliderBehaviour>) playable.GetInput(i);
                SliderBehaviour input = inputPlayable.GetBehaviour();

                // Use the above variables to process each frame of this playable.

            }
        }
    }
}
