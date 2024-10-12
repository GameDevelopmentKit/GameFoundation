using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.Timeline
{
    public sealed class VideoSchedulerPlayableBehaviour : PlayableBehaviour
    {
        private IEnumerable<TimelineClip> m_Clips;
        private PlayableDirector          m_Director;

        internal PlayableDirector director { get => this.m_Director; set => this.m_Director = value; }

        internal IEnumerable<TimelineClip> clips { get => this.m_Clips; set => this.m_Clips = value; }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (this.m_Clips == null) return;

            var inputPort = 0;
            foreach (var clip in this.m_Clips)
            {
                var scriptPlayable =
                    (ScriptPlayable<VideoPlayableBehaviour>)playable.GetInput(inputPort);

                var videoPlayableBehaviour = scriptPlayable.GetBehaviour();

                if (videoPlayableBehaviour != null)
                {
                    var preloadTime = Math.Max(0.0, videoPlayableBehaviour.preloadTime);
                    if (this.m_Director.time >= clip.start + clip.duration || this.m_Director.time <= clip.start - preloadTime)
                        videoPlayableBehaviour.StopVideo();
                    else if (this.m_Director.time > clip.start - preloadTime) videoPlayableBehaviour.PrepareVideo();
                }

                ++inputPort;
            }
        }
    }
}