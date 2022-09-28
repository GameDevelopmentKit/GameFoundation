namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPTextCounter {
    using TMPro;
    using UnityEngine.Playables;

    public class TMPTextCounterMixerBehaviour : PlayableBehaviour {
        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            TextMeshPro trackBinding = playerData as TextMeshPro;

            if (!trackBinding)
                return;

            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++) {
                float inputWeight = playable.GetInputWeight(i);
                ScriptPlayable<TMPTextCounterBehaviour> inputPlayable = (ScriptPlayable<TMPTextCounterBehaviour>) playable.GetInput(i);
                TMPTextCounterBehaviour input = inputPlayable.GetBehaviour();

                // Use the above variables to process each frame of this playable.

            }
        }
    }
}
