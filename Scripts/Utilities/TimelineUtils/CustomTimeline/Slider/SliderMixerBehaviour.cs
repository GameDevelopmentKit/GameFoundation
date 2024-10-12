namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider
{
    using UnityEngine.Playables;
    using UnityEngine.UI;

    public class SliderMixerBehaviour : PlayableBehaviour
    {
        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as Slider;

            if (!trackBinding) return;

            var inputCount = playable.GetInputCount();

            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight   = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<SliderBehaviour>)playable.GetInput(i);
                var input         = inputPlayable.GetBehaviour();

                // Use the above variables to process each frame of this playable.
            }
        }
    }
}