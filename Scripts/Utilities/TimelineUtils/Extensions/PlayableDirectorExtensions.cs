namespace GameFoundation.Scripts.Utilities.TimelineUtils.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Marker;
    using GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider;
    using GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPUITextCounter;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;
    using Object = UnityEngine.Object;

    public static class PlayableDirectorExtensions
    {
        /// <summary>
        /// Reset a timeline to its starting state.
        /// </summary>
        public static void Reset(this PlayableDirector playableDirector, double time = 0d)
        {
            playableDirector.time = time;
            playableDirector.Evaluate();
            playableDirector.Stop();
        }

        /// <summary>
        /// Alternative way to reset a timeline to its starting state.
        /// </summary>
        public static void Reset2(this PlayableDirector playableDirector, double time = 0d)
        {
            playableDirector.time = time;
            playableDirector.Stop();
            playableDirector.Evaluate();
        }

        /// <summary>
        /// Get output track by its name
        /// </summary>
        public static TrackAsset GetOutputTrack(this PlayableDirector playableDirector, string trackName)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            if (timelineAsset == null)
            {
                Debug.LogWarning("PlayableDirector has no timeline asset ???");
                return null;
            }

            return timelineAsset.GetOutputTracks().FirstOrDefault(outputTrack => outputTrack.name == trackName);
        }

        public static GroupTrack GetGroupTrack(this PlayableDirector playableDirector, string groupName)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            if (timelineAsset == null)
            {
                Debug.LogWarning("PlayableDirector has no timeline asset ???");
                return null;
            }

            return (GroupTrack)timelineAsset.GetRootTracks()
                .FirstOrDefault(outputTrack => outputTrack.name == groupName && outputTrack is GroupTrack);
        }

        public static T GetOutputTrack<T>(this PlayableDirector playableDirector, string trackName)
            where T : TrackAsset
        {
            return (T)playableDirector.GetOutputTrack(trackName);
        }

        public static T GetOutputTrack<T>(this PlayableDirector playableDirector, int trackIndex) where T : TrackAsset
        {
            var tmp = ((TimelineAsset)playableDirector.playableAsset).GetOutputTracks().ToList();
            if (tmp.Count <= trackIndex) return null;

            return (T)tmp[trackIndex];
        }

        /// <summary>
        /// Bind object to playable director.
        /// </summary>
        /// <param name="playableDirector"><seealso cref="PlayableDirector"/> to bind.</param>
        /// <param name="trackIndex">Index of the track to bind.</param>
        /// <param name="objectToBind">Object to bind to track.</param>
        public static void Bind(this PlayableDirector playableDirector, int trackIndex, Object objectToBind)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            if (timelineAsset == null)
            {
                Debug.LogError($"Can't load timeline {playableDirector}");
                return;
            }

            var tmp = timelineAsset.outputs.ToList();
            if (tmp.Count <= trackIndex)
            {
                Debug.Log($"Can't get output track at {trackIndex} since there are only {tmp.Count} tracks");
                return;
            }

            var track = (TrackAsset)tmp[trackIndex].sourceObject;
            playableDirector.SetGenericBinding(track, objectToBind);
        }

        /// <summary>
        /// Bind object to playable director.
        /// </summary>
        /// <param name="playableDirector"><seealso cref="PlayableDirector"/> to bind.</param>
        /// <param name="trackName">Name of the track to bind.</param>
        /// <param name="objectToBind">Object to bind to track.</param>
        public static void Bind(this PlayableDirector playableDirector, string trackName, Object objectToBind)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            foreach (var outputBinding in timelineAsset.outputs)
            {
                if (outputBinding.streamName != trackName) continue;

                var track = outputBinding.sourceObject;
                playableDirector.SetGenericBinding(track, objectToBind);
                return;
            }

            Debug.LogError($"Track {trackName} not found..");
        }

        public static void Bind(this PlayableDirector playableDirector, Dictionary<string, Object> trackNameToBindingObject)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            foreach (var outputBinding in timelineAsset.outputs)
            {
                if (!trackNameToBindingObject.TryGetValue(outputBinding.streamName, out var objectToBind)) continue;

                var track = outputBinding.sourceObject;
                playableDirector.SetGenericBinding(track, objectToBind);
            }
        }

        /// <summary>
        /// Bind multiple objects to multiple tracks in playable director.
        /// </summary>
        /// <param name="playableDirector"><seealso cref="PlayableDirector"/> to bind.</param>
        /// <param name="trackIndices">Indices of the tracks to bind.</param>
        /// <param name="objectsToBind">Objects to bind to tracks.</param>
        public static void Bind(
            this PlayableDirector playableDirector,
            List<int>             trackIndices,
            List<Object>          objectsToBind
        )
        {
            if (trackIndices == null || objectsToBind == null || trackIndices.Count != objectsToBind.Count)
            {
                Debug.LogError("Input data error, can't bind..");
                return;
            }

            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            var tmp           = timelineAsset.outputs.ToList();
            for (var i = 0; i < trackIndices.Count; i++)
            {
                var trackIndex = trackIndices[i];
                if (tmp.Count <= trackIndex)
                {
                    Debug.Log($"Can't get output track at {trackIndex} since there are only {tmp.Count} tracks");
                    continue;
                }

                var track = (TrackAsset)tmp[trackIndex].sourceObject;
                playableDirector.SetGenericBinding(track, objectsToBind[i]);
            }
        }

        /// <summary>
        /// Start playing a playable director at a specific time.
        /// </summary>
        public static void Play(this PlayableDirector playableDirector, double startTime, bool force = false)
        {
            playableDirector.time = startTime;
            if (force) playableDirector.Evaluate();
            playableDirector.Play();
        }

        /// <summary>
        /// Start playing a playable director at a specific time and then pause at a specific time.
        /// </summary>
        public static void Play(
            this PlayableDirector playableDirector,
            double                startTime,
            double                endTime,
            bool                  force       = false,
            bool                  pauseOrStop = true
        )
        {
            playableDirector.time = startTime;
            if (force) playableDirector.Evaluate();
            var helper                                  = playableDirector.GetComponent<PlayableDirectorHelper>();
            if (helper == null) helper                  = playableDirector.gameObject.AddComponent<PlayableDirectorHelper>();
            var tmp                                     = new List<Action<double>>(helper.RegisteredActions);
            foreach (var action in tmp) helper.OnUpdate -= action;

            tmp.Clear();

            void Lambda(double time)
            {
                if (helper.PlayableDirector.time < endTime) return;
                if (pauseOrStop)
                    helper.PlayableDirector.Pause();
                else
                    helper.PlayableDirector.Stop();

                helper.OnUpdate -= Lambda;
            }

            helper.OnUpdate += Lambda;

            if (!playableDirector.gameObject.activeInHierarchy) Debug.LogWarning($"Playable director {playableDirector.gameObject.name} is not active");
            playableDirector.Play();
        }

        public static void PlayTimeScale(
            this PlayableDirector playableDirector,
            float                 timeScale,
            double                startTime = -1d,
            double                endTime   = -1d
        )
        {
            playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            var helper                 = playableDirector.GetComponent<PlayableDirectorHelper>();
            if (helper == null) helper = playableDirector.gameObject.AddComponent<PlayableDirectorHelper>();

            if (Math.Abs(startTime + 1d) > 0.001d)
            {
                playableDirector.time = startTime;
                playableDirector.Evaluate();
                helper.StartTime = startTime;
            }

            if (Math.Abs(endTime + 1d) > 0.001d) helper.EndTime = endTime;

            helper.TimeScale = timeScale;
        }

        /// <summary>
        /// Play a timeline to the moment a marker clip start.
        /// </summary>
        /// <param name="playableDirector">PlayableDirector to play.</param>
        /// <param name="markerTrackName">Name of the track <seealso cref="Marker.MarkerTrack"/></param>
        /// <param name="markerClipName">Name of the clip <seealso cref="MarkerClip"/></param>
        public static void Play(this PlayableDirector playableDirector, string markerTrackName, string markerClipName)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            var tracks        = new List<TrackAsset>();
            foreach (var t in timelineAsset.GetOutputTracks())
                //Debug.Log($"Check track {t.name} vs {markerTrackName}");
                if (!t.muted && t.name == markerTrackName)
                    tracks.Add(t);

            if (tracks.Count == 0)
            {
                Debug.LogError($"No track name {markerTrackName} found !");
                playableDirector.Play();
                return;
            }

            var time = 0d;

            foreach (var track in tracks)
            foreach (var clip in track.GetClips())
                //Debug.Log($"Check clip {clip.displayName} vs {markerClipName} of track {track.name}");
                if (clip.displayName == markerClipName)
                    time = clip.start;

            playableDirector.time = time;
            playableDirector.Evaluate();
            playableDirector.Play();
        }

        /// <summary>
        /// Jump a timeline to the moment a marker clip start.
        /// </summary>
        /// <param name="playableDirector">PlayableDirector to play.</param>
        /// <param name="markerTrackName">Name of the track <seealso cref="Marker.MarkerTrack"/></param>
        /// <param name="markerClipName">Name of the clip <seealso cref="MarkerClip"/></param>
        public static void JumpTo(
            this PlayableDirector playableDirector,
            string                markerTrackName,
            string                markerClipName
        )
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            var tracks        = new List<TrackAsset>();
            foreach (var t in timelineAsset.GetOutputTracks())
                //Debug.Log($"Check track {t.name} vs {markerTrackName}");
                if (!t.muted && t.name == markerTrackName)
                    tracks.Add(t);

            if (tracks.Count == 0)
            {
                Debug.LogError($"No track name {markerTrackName} found !");
                return;
            }

            var time = 0d;
            var tmp  = 0;

            foreach (var track in tracks)
            foreach (var clip in track.GetClips())
                //Debug.Log($"Check clip {clip.displayName} vs {markerClipName} of track {track.name}");
            {
                if (clip.displayName == markerClipName)
                {
                    time = clip.start;
                    tmp++;
                }
            }

            if (tmp > 1)
                Debug.LogWarning($"There are multiple {tmp} tracks & clips to jump to ??");
            else if (tmp == 0) Debug.LogWarning($"Clip {markerClipName} of track {markerTrackName} not found ???");

            playableDirector.time = time;
            playableDirector.Evaluate();
        }

        /// <summary>
        /// Jump a timeline to the moment a marker clip start.
        /// </summary>
        /// <param name="playableDirector">PlayableDirector to play.</param>
        /// <param name="markerTrackName">Name of the track <seealso cref="AlleyLabs.Core.Timeline.MarkerTrack"/></param>
        /// <param name="markerClipName">Name of the clip <seealso cref="MarkerClip"/></param>
        public static void JumpTo(this PlayableDirector playableDirector, string markerClipName)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;

            var time = 0d;
            var tmp  = 0;

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track.muted) continue;

                foreach (var clip in track.GetClips())
                    //Debug.Log($"Check clip {clip.displayName} vs {markerClipName} of track {track.name}");
                {
                    if (clip.displayName == markerClipName)
                    {
                        time = clip.start;
                        tmp++;
                    }
                }
            }

            if (tmp > 1)
                Debug.LogWarning($"There are multiple {tmp} tracks & clips {markerClipName} to jump to ???");
            else if (tmp == 0) Debug.LogWarning($"Clip {markerClipName} not found ???");

            playableDirector.time = time;
            playableDirector.Evaluate();
        }

        public static void MuteTrack(this PlayableDirector playableDirector, string trackName, bool muteOrUnmute)
        {
            var track = playableDirector.GetOutputTrack(trackName);
            if (track != null)
                track.muted = muteOrUnmute;
            else
                Debug.LogError($"Can't find track {trackName}");
        }

        /// <summary>
        /// Mute all tracks in Group
        /// </summary>
        public static void MuteGroup(this PlayableDirector playableDirector, string groupName, bool muteOrUnmute)
        {
            var track = playableDirector.GetGroupTrack(groupName);
            if (track != null)
            {
                foreach (var childTrack in track.GetChildTracks()) childTrack.muted = muteOrUnmute;

                track.muted = muteOrUnmute;
            }
            else
            {
                Debug.LogError($"Can't find group track {groupName}");
            }
        }

        public static void SetTMPUICounter(this PlayableDirector playableDirector, string trackName, int start, int end, string format = SELF_VALUE)
        {
            var trophy = playableDirector.GetOutputTrack<TMPUITextCounterTrack>(trackName);
            if (trophy != null)
            {
                var clip = (TMPUITextCounterClip)trophy.GetClips().ElementAt(0).asset;
                clip.template.startValue = start;
                clip.template.endValue   = end;
                clip.template.format     = format;
            }
            else
            {
                Debug.LogError($"Can not get output track {trackName}");
            }
        }

        public const string SELF_VALUE = "{0}";

        public static void SetSlider(this PlayableDirector playableDirector, string trackName, float start, float end)
        {
            var trophy = playableDirector.GetOutputTrack<SliderTrack>(trackName);
            if (trophy != null)
            {
                var clip = (SliderClip)trophy.GetClips().ElementAt(0).asset;
                clip.template.SetStartValue(start);
                clip.template.SetEndValue(end);
            }
            else
            {
                Debug.LogError($"Can not get output track {trackName}");
            }
        }

        public static void MuteTrack(this PlayableDirector playableDirector, int trackIndex, bool muteOrUnmute)
        {
            var timelineAsset = (TimelineAsset)playableDirector.playableAsset;
            if (trackIndex >= timelineAsset.GetOutputTracks().Count())
            {
                Debug.LogError($"Can't get track index {trackIndex}");
                return;
            }

            var track = timelineAsset.GetOutputTracks().ElementAt(trackIndex);
            if (track != null)
                track.muted = muteOrUnmute;
            else
                Debug.LogError($"Can't find track {trackIndex}");
        }

        public static void End(this PlayableDirector playableDirector)
        {
            playableDirector.time = playableDirector.duration;
            playableDirector.Evaluate();
            playableDirector.Stop();
        }

        public static void AddOnceCompleteListener(this PlayableDirector playableDirector, Action callback)
        {
            if (onCompleteCallback.TryGetValue(playableDirector.GetHashCode(), out var listCallback))
            {
                listCallback.Add(callback);
            }
            else
            {
                onCompleteCallback.Add(playableDirector.GetHashCode(), new() { callback });
                playableDirector.stopped += PlayableDirectorOnStopped;
            }
        }

        private static Dictionary<int, List<Action>> onCompleteCallback = new();

        private static void PlayableDirectorOnStopped(PlayableDirector obj)
        {
            if (onCompleteCallback.TryGetValue(obj.GetHashCode(), out var listCallback))
            {
                foreach (var callback in listCallback) callback?.Invoke();
                listCallback.Clear();
            }
        }
    }
}