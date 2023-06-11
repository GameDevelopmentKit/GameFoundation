/*
Simple Sound Manager (c) 2016 Digital Ruby, LLC
http://www.digitalruby.com

Source code may no longer be redistributed in source format. Using this in apps and games is fine.
*/

using UnityEngine;

using System.Collections;
using System.Collections.Generic;

namespace DigitalRuby.SoundManagerNamespace
{
    /// <summary>
    /// Provides an easy wrapper to looping audio sources with nice transitions for volume when starting and stopping
    /// </summary>
    public class LoopingAudioSource
    {
        /// <summary>
        /// The audio source that is looping
        /// </summary>
        public AudioSource AudioSource { get; private set; }

        /// <summary>
        /// The target volume
        /// </summary>
        public float TargetVolume { get; set; }

        /// <summary>
        /// The original target volume - useful if the global sound volume changes you can still have the original target volume to multiply by.
        /// </summary>
        public float OriginalTargetVolume { get; private set; }

        /// <summary>
        /// Is this sound stopping?
        /// </summary>
        public bool Stopping { get; private set; }

        /// <summary>
        /// Whether the looping audio source persists in between scene changes
        /// </summary>
        public bool Persist { get; private set; }

        /// <summary>
        /// Tag for the looping audio source
        /// </summary>
        public int Tag { get; set; }

        private float startVolume;
        private float startMultiplier;
        private float stopMultiplier;
        private float currentMultiplier;
        private float timestamp;
        private bool paused;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audioSource">Audio source, will be looped automatically</param>
        /// <param name="startMultiplier">Start multiplier - seconds to reach peak sound</param>
        /// <param name="stopMultiplier">Stop multiplier - seconds to fade sound back to 0 volume when stopped</param>
        /// <param name="persist">Whether to persist the looping audio source between scene changes</param>
        public LoopingAudioSource(AudioSource audioSource, float startMultiplier, float stopMultiplier, bool persist)
        {
            AudioSource = audioSource;
            if (audioSource != null)
            {
                AudioSource.loop = true;
                AudioSource.volume = 0.0f;
                AudioSource.Stop();
            }

            this.startMultiplier = currentMultiplier = startMultiplier;
            this.stopMultiplier = stopMultiplier;
            Persist = persist;
        }

        /// <summary>
        /// Play this looping audio source
        /// </summary>
        /// <param name="isMusic">True if music, false if sound effect</param>
        public void Play(bool isMusic)
        {
            Play(1.0f, isMusic);
        }

        /// <summary>
        /// Play this looping audio source
        /// </summary>
        /// <param name="targetVolume">Max volume</param>
        /// <param name="isMusic">True if music, false if sound effect</param>
        /// <returns>True if played, false if already playing or error</returns>
        public bool Play(float targetVolume, bool isMusic)
        {
            if (AudioSource != null)
            {
                AudioSource.volume = startVolume = (AudioSource.isPlaying ? AudioSource.volume : 0.0f);
                AudioSource.loop = true;
                currentMultiplier = startMultiplier;
                OriginalTargetVolume = targetVolume;
                TargetVolume = targetVolume;
                Stopping = false;
                timestamp = 0.0f;
                if (!AudioSource.isPlaying)
                {
                    AudioSource.Play();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Stop this looping audio source. The sound will fade out smoothly.
        /// </summary>
        public void Stop()
        {
            if (AudioSource != null && AudioSource.isPlaying && !Stopping)
            {
                startVolume = AudioSource.volume;
                TargetVolume = 0.0f;
                currentMultiplier = stopMultiplier;
                Stopping = true;
                timestamp = 0.0f;
            }
        }

        /// <summary>
        /// Pauses the looping audio source
        /// </summary>
        public void Pause()
        {
            if (AudioSource != null && !paused && AudioSource.isPlaying)
            {
                paused = true;
                AudioSource.Pause();
            }
        }

        /// <summary>
        /// Resumes the looping audio source
        /// </summary>
        public void Resume()
        {
            if (AudioSource != null && paused)
            {
                paused = false;
                AudioSource.UnPause();
            }
        }

        /// <summary>
        /// Update this looping audio source
        /// </summary>
        /// <returns>True if finished playing, false otherwise</returns>
        public bool Update()
        {
            if (AudioSource != null && AudioSource.isPlaying)
            {
                if ((AudioSource.volume = Mathf.Lerp(startVolume, TargetVolume, (timestamp += Time.deltaTime) / currentMultiplier)) == 0.0f && Stopping)
                {
                    AudioSource.Stop();
                    Stopping = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return !paused;
        }
    }

    /// <summary>
    /// Sound manager extension methods
    /// </summary>
    public static class SoundManagerExtensions
    {
        /// <summary>
        /// Play an audio clip once using the global sound volume as a multiplier
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="clip">Clip</param>
        public static void PlayOneShotSoundManaged(this AudioSource source, AudioClip clip)
        {
            SoundManager.PlayOneShotSound(source, clip, 1.0f);
        }

        /// <summary>
        /// Play an audio clip once using the global sound volume as a multiplier
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="clip">Clip</param>
        /// <param name="volumeScale">Additional volume scale</param>
        public static void PlayOneShotSoundManaged(this AudioSource source, AudioClip clip, float volumeScale)
        {
            SoundManager.PlayOneShotSound(source, clip, volumeScale);
        }

        /// <summary>
        /// Play an audio clip once using the global music volume as a multiplier
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="clip">Clip</param>
        public static void PlayOneShotMusicManaged(this AudioSource source, AudioClip clip)
        {
            SoundManager.PlayOneShotMusic(source, clip, 1.0f);
        }

        /// <summary>
        /// Play an audio clip once using the global music volume as a multiplier
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="clip">Clip</param>
        /// <param name="volumeScale">Additional volume scale</param>
        public static void PlayOneShotMusicManaged(this AudioSource source, AudioClip clip, float volumeScale)
        {
            SoundManager.PlayOneShotMusic(source, clip, volumeScale);
        }

        /// <summary>
        /// Play a sound and loop it until stopped, using the global sound volume as a modifier
        /// </summary>
        /// <param name="source">Audio source to play</param>
        public static void PlayLoopingSoundManaged(this AudioSource source)
        {
            SoundManager.PlayLoopingSound(source, 1.0f, 1.0f);
        }

        /// <summary>
        /// Play a sound and loop it until stopped, using the global sound volume as a modifier
        /// </summary>
        /// <param name="source">Audio source to play</param>
        /// <param name="volumeScale">Additional volume scale</param>
        /// <param name="fadeSeconds">The number of seconds to fade in and out</param>
        public static void PlayLoopingSoundManaged(this AudioSource source, float volumeScale, float fadeSeconds)
        {
            SoundManager.PlayLoopingSound(source, volumeScale, fadeSeconds);
        }

        /// <summary>
        /// Play a music track and loop it until stopped, using the global music volume as a modifier
        /// </summary>
        /// <param name="source">Audio source to play</param>
        public static void PlayLoopingMusicManaged(this AudioSource source)
        {
            SoundManager.PlayLoopingMusic(source, 1.0f, 1.0f, false);
        }

        /// <summary>
        /// Play a music track and loop it until stopped, using the global music volume as a modifier
        /// </summary>
        /// <param name="source">Audio source to play</param>
        /// <param name="volumeScale">Additional volume scale</param>
        /// <param name="fadeSeconds">The number of seconds to fade in and out</param>
        /// <param name="persist">Whether to persist the looping music between scene changes</param>
        public static void PlayLoopingMusicManaged(this AudioSource source, float volumeScale, float fadeSeconds, bool persist)
        {
            SoundManager.PlayLoopingMusic(source, volumeScale, fadeSeconds, persist);
        }

        /// <summary>
        /// Stop a looping sound
        /// </summary>
        /// <param name="source">AudioSource to stop</param>
        public static void StopLoopingSoundManaged(this AudioSource source)
        {
            SoundManager.StopLoopingSound(source);
        }

        /// <summary>
        /// Stop a looping music track
        /// </summary>
        /// <param name="source">AudioSource to stop</param>
        public static void StopLoopingMusicManaged(this AudioSource source)
        {
            SoundManager.StopLoopingMusic(source);
        }
    }

    /// <summary>
    /// Do not add this script in the inspector. Just call the static methods from your own scripts or use the AudioSource extension methods.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private static int persistTag = 0;
        private static bool needsInitialize = true;
        private static GameObject root;
        private static SoundManager instance;
        private static readonly List<LoopingAudioSource> music = new List<LoopingAudioSource>();
        private static readonly List<AudioSource> musicOneShot = new List<AudioSource>();
        private static readonly List<LoopingAudioSource> sounds = new List<LoopingAudioSource>();
        private static readonly HashSet<LoopingAudioSource> persistedSounds = new HashSet<LoopingAudioSource>();
        private static readonly Dictionary<AudioClip, List<float>> soundsOneShot = new Dictionary<AudioClip, List<float>>();
        private static float soundVolume = 1.0f;
        private static float musicVolume = 1.0f;
        private static bool updated;
        private static bool pauseSoundsOnApplicationPause = true;

        /// <summary>
        /// Maximum number of the same audio clip to play at once
        /// </summary>
        public static int MaxDuplicateAudioClips = 4;

        /// <summary>
        /// Whether to stop sounds when a new level loads. Set to false for additive level loading.
        /// </summary>
        public static bool StopSoundsOnLevelLoad = true;

        private static void EnsureCreated()
        {
            if (needsInitialize)
            {
                needsInitialize = false;
                root = new GameObject();
                root.name = "DigitalRubySoundManager";
                root.hideFlags = HideFlags.HideAndDontSave;
                instance = root.AddComponent<SoundManager>();
                GameObject.DontDestroyOnLoad(root);
            }
        }

        private void StopLoopingListOnLevelLoad(IList<LoopingAudioSource> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].Persist || !list[i].AudioSource.isPlaying)
                {
                    list.RemoveAt(i);
                }
            }
        }

        private void ClearPersistedSounds()
        {
            foreach (LoopingAudioSource s in persistedSounds)
            {
                if (!s.AudioSource.isPlaying)
                {
                    GameObject.Destroy(s.AudioSource.gameObject);
                }
            }
            persistedSounds.Clear();
        }

        private void SceneManagerSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
        {
            // Just in case this is called a bunch of times, we put a check here
            if (updated && StopSoundsOnLevelLoad)
            {
                persistTag++;

                updated = false;

                Debug.LogWarningFormat("Reloaded level, new sound manager persist tag: {0}", persistTag);

                StopNonLoopingSounds();
                StopLoopingListOnLevelLoad(sounds);
                StopLoopingListOnLevelLoad(music);
                soundsOneShot.Clear();
                ClearPersistedSounds();
            }
        }

        private void Start()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManagerSceneLoaded;
        }

        private void Update()
        {
            updated = true;

            for (int i = sounds.Count - 1; i >= 0; i--)
            {
                if (sounds[i].Update())
                {
                    sounds.RemoveAt(i);
                }
            }
            for (int i = music.Count - 1; i >= 0; i--)
            {
                bool nullMusic = (music[i] == null || music[i].AudioSource == null);
                if (nullMusic || music[i].Update())
                {
                    if (!nullMusic && music[i].Tag != persistTag)
                    {
                        Debug.LogWarning("Destroying persisted audio from previous scene: " + music[i].AudioSource.gameObject.name);

                        // cleanup persisted audio from previous scenes
                        GameObject.Destroy(music[i].AudioSource.gameObject);
                    }
                    music.RemoveAt(i);
                }
            }
            for (int i = musicOneShot.Count - 1; i >= 0; i--)
            {
                if (!musicOneShot[i].isPlaying)
                {
                    musicOneShot.RemoveAt(i);
                }
            }
        }

        private void OnApplicationFocus(bool paused)
        {
            if (SoundManager.PauseSoundsOnApplicationPause)
            {
                if (paused)
                {
                    SoundManager.ResumeAll();
                }
                else
                {
                    SoundManager.PauseAll();
                }
            }
        }

        private static void UpdateSounds()
        {
            foreach (LoopingAudioSource s in sounds)
            {
                s.TargetVolume = s.OriginalTargetVolume * soundVolume;
            }
        }

        private static void UpdateMusic()
        {
            foreach (LoopingAudioSource s in music)
            {
                if (!s.Stopping)
                {
                    s.TargetVolume = s.OriginalTargetVolume * musicVolume;
                }
            }
            foreach (AudioSource s in musicOneShot)
            {
                s.volume = musicVolume;
            }
        }

        private static IEnumerator RemoveVolumeFromClip(AudioClip clip, float volume)
        {
            yield return new WaitForSeconds(clip.length);

            List<float> volumes;
            if (soundsOneShot.TryGetValue(clip, out volumes))
            {
                volumes.Remove(volume);
            }
        }

        private static void PlayLooping(AudioSource source, List<LoopingAudioSource> sources, float volumeScale, float fadeSeconds, bool persist, bool stopAll)
        {
            EnsureCreated();

            for (int i = sources.Count - 1; i >= 0; i--)
            {
                LoopingAudioSource s = sources[i];
                if (s.AudioSource == source)
                {
                    sources.RemoveAt(i);
                }
                if (stopAll)
                {
                    s.Stop();
                }
            }
            {
                source.gameObject.SetActive(true);
                LoopingAudioSource s = new LoopingAudioSource(source, fadeSeconds, fadeSeconds, persist);
                s.Play(volumeScale, true);
                s.Tag = persistTag;
                sources.Add(s);

                if (persist)
                {
                    if (!source.gameObject.name.StartsWith("PersistedBySoundManager-"))
                    {
                        source.gameObject.name = "PersistedBySoundManager-" + source.gameObject.name + "-" + source.gameObject.GetInstanceID();
                    }
                    source.gameObject.transform.parent = null;
                    GameObject.DontDestroyOnLoad(source.gameObject);
                    persistedSounds.Add(s);
                }
            }
        }

        private static void StopLooping(AudioSource source, List<LoopingAudioSource> sources)
        {
            foreach (LoopingAudioSource s in sources)
            {
                if (s.AudioSource == source)
                {
                    s.Stop();
                    source = null;
                    break;
                }
            }
            if (source != null)
            {
                source.Stop();
            }
        }

        /// <summary>
        /// Play a sound once - sound volume will be affected by global sound volume
        /// </summary>
        /// <param name="source">Audio source</param>
        /// <param name="clip">Clip</param>
        public static void PlayOneShotSound(AudioSource source, AudioClip clip)
        {
            PlayOneShotSound(source, clip, 1.0f);
        }

        /// <summary>
        /// Play a sound once - sound volume will be affected by global sound volume
        /// </summary>
        /// <param name="source">Audio source</param>
        /// <param name="clip">Clip</param>
        /// <param name="volumeScale">Additional volume scale</param>
        public static void PlayOneShotSound(AudioSource source, AudioClip clip, float volumeScale)
        {
            EnsureCreated();

            List<float> volumes;
            if (!soundsOneShot.TryGetValue(clip, out volumes))
            {
                volumes = new List<float>();
                soundsOneShot[clip] = volumes;
            }
            else if (volumes.Count == MaxDuplicateAudioClips)
            {
                return;
            }

            float minVolume = float.MaxValue;
            float maxVolume = float.MinValue;
            foreach (float volume in volumes)
            {
                minVolume = Mathf.Min(minVolume, volume);
                maxVolume = Mathf.Max(maxVolume, volume);
            }

            float requestedVolume = (volumeScale * soundVolume);
            if (maxVolume > 0.5f)
            {
                requestedVolume = (minVolume + maxVolume) / (float)(volumes.Count + 2);
            }
            // else requestedVolume can stay as is

            volumes.Add(requestedVolume);
            source.PlayOneShot(clip, requestedVolume);
            instance.StartCoroutine(RemoveVolumeFromClip(clip, requestedVolume));
        }

        /// <summary>
        /// Play a looping sound - sound volume will be affected by global sound volume
        /// </summary>
        /// <param name="source">Audio source to play looping</param>
        public static void PlayLoopingSound(AudioSource source)
        {
            PlayLoopingSound(source, 1.0f, 1.0f);
        }

        /// <summary>
        /// Play a looping sound - sound volume will be affected by global sound volume
        /// </summary>
        /// <param name="source">Audio source to play looping</param>
        /// <param name="volumeScale">Additional volume scale</param>
        /// <param name="fadeSeconds">Seconds to fade in and out</param>
        public static void PlayLoopingSound(AudioSource source, float volumeScale, float fadeSeconds)
        {
            PlayLooping(source, sounds, volumeScale, fadeSeconds, false, false);
            UpdateSounds();
        }

        /// <summary>
        /// Play a music track once - music volume will be affected by the global music volume
        /// </summary>
        /// <param name="source"></param>
        /// <param name="clip"></param>
        public static void PlayOneShotMusic(AudioSource source, AudioClip clip)
        {
            PlayOneShotMusic(source, clip, 1.0f);
        }

        /// <summary>
        /// Play a music track once - music volume will be affected by the global music volume
        /// </summary>
        /// <param name="source">Audio source</param>
        /// <param name="clip">Clip</param>
        /// <param name="volumeScale">Additional volume scale</param>
        public static void PlayOneShotMusic(AudioSource source, AudioClip clip, float volumeScale)
        {
            EnsureCreated();

            int index = musicOneShot.IndexOf(source);
            if (index >= 0)
            {
                musicOneShot.RemoveAt(index);
            }
            source.PlayOneShot(clip, volumeScale * musicVolume);
            musicOneShot.Add(source);
        }

        /// <summary>
        /// Play a looping music track - music volume will be affected by the global music volume
        /// </summary>
        /// <param name="source">Audio source</param>
        public static void PlayLoopingMusic(AudioSource source)
        {
            PlayLoopingMusic(source, 1.0f, 1.0f, false);
        }

        /// <summary>
        /// Play a looping music track - music volume will be affected by the global music volume
        /// </summary>
        /// <param name="source">Audio source</param>
        /// <param name="volumeScale">Additional volume scale</param>
        /// <param name="fadeSeconds">Seconds to fade in and out</param>
        /// <param name="persist">Whether to persist the looping music between scene changes</param>
        public static void PlayLoopingMusic(AudioSource source, float volumeScale, float fadeSeconds, bool persist)
        {
            PlayLooping(source, music, volumeScale, fadeSeconds, persist, true);
            UpdateMusic();
        }

        /// <summary>
        /// Stop looping a sound for an audio source
        /// </summary>
        /// <param name="source">Audio source to stop looping sound for</param>
        public static void StopLoopingSound(AudioSource source)
        {
            StopLooping(source, sounds);
        }

        /// <summary>
        /// Stop looping music for an audio source
        /// </summary>
        /// <param name="source">Audio source to stop looping music for</param>
        public static void StopLoopingMusic(AudioSource source)
        {
            StopLooping(source, music);
        }

        /// <summary>
        /// Stop all looping sounds, music, and music one shots. Non-looping sounds are not stopped.
        /// </summary>
        public static void StopAll()
        {
            StopAllLoopingSounds();
            StopNonLoopingSounds();
        }

        /// <summary>
        /// Stop all looping sounds and music. Music one shots and non-looping sounds are not stopped.
        /// </summary>
        public static void StopAllLoopingSounds()
        {
            foreach (LoopingAudioSource s in sounds)
            {
                s.Stop();
            }
            foreach (LoopingAudioSource s in music)
            {
                s.Stop();
            }
        }

        /// <summary>
        /// Stop all non-looping sounds. Looping sounds and looping music are not stopped.
        /// </summary>
        public static void StopNonLoopingSounds()
        {
            foreach (AudioSource s in musicOneShot)
            {
                s.Stop();
            }
        }

        /// <summary>
        /// Pause all sounds
        /// </summary>
        public static void PauseAll()
        {
            foreach (LoopingAudioSource s in sounds)
            {
                s.Pause();
            }
            foreach (LoopingAudioSource s in music)
            {
                s.Pause();
            }
        }

        /// <summary>
        /// Unpause and resume all sounds
        /// </summary>
        public static void ResumeAll()
        {
            foreach (LoopingAudioSource s in sounds)
            {
                s.Resume();
            }
            foreach (LoopingAudioSource s in music)
            {
                s.Resume();
            }
        }

        /// <summary>
        /// Global music volume multiplier
        /// </summary>
        public static float MusicVolume
        {
            get { return musicVolume; }
            set
            {
                if (value != musicVolume)
                {
                    musicVolume = value;
                    UpdateMusic();
                }
            }
        }

        /// <summary>
        /// Global sound volume multiplier
        /// </summary>
        public static float SoundVolume
        {
            get { return soundVolume; }
            set
            {
                if (value != soundVolume)
                {
                    soundVolume = value;
                    UpdateSounds();
                }
            }
        }

        /// <summary>
        /// Whether to pause sounds when the application is paused and resume them when the application is activated.
        /// Player option "Run In Background" must be selected to enable this. Default is true.
        /// </summary>
        public static bool PauseSoundsOnApplicationPause
        {
            get { return pauseSoundsOnApplicationPause; }
            set { pauseSoundsOnApplicationPause = value; }
        }
    }
}