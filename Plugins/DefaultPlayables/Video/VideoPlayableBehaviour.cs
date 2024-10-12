using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace UnityEngine.Timeline
{
    public class VideoPlayableBehaviour : PlayableBehaviour
    {
        public VideoPlayer videoPlayer;
        public VideoClip   videoClip;
        public bool        mute        = false;
        public bool        loop        = true;
        public double      preloadTime = 0.3;
        public double      clipInTime  = 0.0;

        private bool playedOnce = false;
        private bool preparing  = false;

        public void PrepareVideo()
        {
            if (this.videoPlayer == null || this.videoClip == null) return;

            this.videoPlayer.targetCameraAlpha = 0.0f;

            if (this.videoPlayer.clip != this.videoClip) this.StopVideo();

            if (this.videoPlayer.isPrepared || this.preparing) return;

            this.videoPlayer.source            = VideoSource.VideoClip;
            this.videoPlayer.clip              = this.videoClip;
            this.videoPlayer.playOnAwake       = false;
            this.videoPlayer.waitForFirstFrame = true;
            this.videoPlayer.isLooping         = this.loop;

            for (ushort i = 0; i < this.videoClip.audioTrackCount; ++i)
            {
                if (this.videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                {
                    this.videoPlayer.SetDirectAudioMute(i, this.mute || !Application.isPlaying);
                }
                else if (this.videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
                {
                    var audioSource                           = this.videoPlayer.GetTargetAudioSource(i);
                    if (audioSource != null) audioSource.mute = this.mute || !Application.isPlaying;
                }
            }

            this.videoPlayer.loopPointReached += this.LoopPointReached;
            this.videoPlayer.time             =  this.clipInTime;
            this.videoPlayer.Prepare();
            this.preparing = true;
        }

        private void LoopPointReached(VideoPlayer vp)
        {
            this.playedOnce = !this.loop;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (this.videoPlayer == null || this.videoClip == null) return;

            this.videoPlayer.timeReference = Application.isPlaying ? VideoTimeReference.ExternalTime : VideoTimeReference.Freerun;

            if (this.videoPlayer.isPlaying && Application.isPlaying)
                this.videoPlayer.externalReferenceTime = playable.GetTime();
            else if (!Application.isPlaying) this.SyncVideoToPlayable(playable);
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (this.videoPlayer == null) return;

            if (!this.playedOnce)
            {
                this.PlayVideo();
                this.SyncVideoToPlayable(playable);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (this.videoPlayer == null) return;

            if (Application.isPlaying)
                this.PauseVideo();
            else
                this.StopVideo();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (this.videoPlayer == null || this.videoPlayer.clip == null) return;

            this.videoPlayer.targetCameraAlpha = info.weight;

            if (Application.isPlaying)
                for (ushort i = 0; i < this.videoPlayer.clip.audioTrackCount; ++i)
                {
                    if (this.videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                    {
                        this.videoPlayer.SetDirectAudioVolume(i, info.weight);
                    }
                    else if (this.videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
                    {
                        var audioSource                             = this.videoPlayer.GetTargetAudioSource(i);
                        if (audioSource != null) audioSource.volume = info.weight;
                    }
                }
        }

        public override void OnGraphStart(Playable playable)
        {
            this.playedOnce = false;
        }

        public override void OnGraphStop(Playable playable)
        {
            if (!Application.isPlaying) this.StopVideo();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            this.StopVideo();
        }

        public void PlayVideo()
        {
            if (this.videoPlayer == null) return;

            this.videoPlayer.Play();
            this.preparing = false;

            if (!Application.isPlaying) this.PauseVideo();
        }

        public void PauseVideo()
        {
            if (this.videoPlayer == null) return;

            this.videoPlayer.Pause();
            this.preparing = false;
        }

        public void StopVideo()
        {
            if (this.videoPlayer == null) return;

            this.playedOnce = false;
            this.videoPlayer.Stop();
            this.preparing = false;
        }

        private void SyncVideoToPlayable(Playable playable)
        {
            if (this.videoPlayer == null || this.videoPlayer.clip == null) return;

            this.videoPlayer.time = (this.clipInTime + playable.GetTime() * this.videoPlayer.playbackSpeed) % this.videoPlayer.clip.length;
        }
    }
}