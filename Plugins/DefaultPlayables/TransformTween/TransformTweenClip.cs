using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TransformTweenClip : PlayableAsset, ITimelineClipAsset
{
    public TransformTweenBehaviour     template = new();
    public ExposedReference<Transform> startLocation;
    public ExposedReference<Transform> endLocation;

    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TransformTweenBehaviour>.Create(graph, this.template);
        var clone    = playable.GetBehaviour();
        clone.startLocation = this.startLocation.Resolve(graph.GetResolver());
        clone.endLocation   = this.endLocation.Resolve(graph.GetResolver());
        return playable;
    }
}