namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPUITextCounter {
    using TMPro;
    using UnityEngine.Playables;

    public class TMPUITextCounterMixerBehaviour : PlayableBehaviour {
        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            TextMeshProUGUI trackBinding = playerData as TextMeshProUGUI;

            if (!trackBinding)
                return;

            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++) {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<TMPUITextCounterBehaviour> inputPlayable = (ScriptPlayable<TMPUITextCounterBehaviour>) playable.GetInput(i);
                TMPUITextCounterBehaviour input = inputPlayable.GetBehaviour();

                // Use the above variables to process each frame of this playable.

            }
        }
    }
}
