namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPTextCounter
{
    using TMPro;
    using UnityEngine.Playables;

    public class TMPTextCounterMixerBehaviour : PlayableBehaviour
    {
        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as TextMeshPro;

            if (!trackBinding) return;

            var inputCount = playable.GetInputCount();

            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight   = playable.GetInputWeight(i);
                var inputPlayable = (ScriptPlayable<TMPTextCounterBehaviour>)playable.GetInput(i);
                var input         = inputPlayable.GetBehaviour();

                // Use the above variables to process each frame of this playable.
            }
        }
    }
}