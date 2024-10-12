using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.AI;

public class NavMeshAgentControlMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var trackBinding = playerData as NavMeshAgent;

        if (!trackBinding) return;

        var inputCount = playable.GetInputCount();

        for (var i = 0; i < inputCount; i++)
        {
            var inputWeight   = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<NavMeshAgentControlBehaviour>)playable.GetInput(i);
            var input         = inputPlayable.GetBehaviour();

            if (inputWeight > 0.5f && !input.destinationSet && input.destination)
            {
                if (!trackBinding.isOnNavMesh) continue;

                trackBinding.SetDestination(input.destination.position);
                input.destinationSet = true;
            }
        }
    }
}