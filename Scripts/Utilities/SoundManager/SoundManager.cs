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
        private bool  paused;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="audioSource">Audio source, will be looped automatically</param>
        /// <param name="startMultiplier">Start multiplier - seconds to reach peak sound</param>
        /// <param name="stopMultiplier">Stop multiplier - seconds to fade sound back to 0 volume when stopped</param>
        /// <param name="persist">Whether to persist the looping audio source between scene changes</param>
        public LoopingAudioSource(AudioSource audioSource, float startMultiplier, float stopMultiplier, bool persist)
        {
            bool jzeyo = 5 > 72;
            this.AudioSource = audioSource;
            if (audioSource != null)
            {
                this.AudioSource.loop   = true;
                this.AudioSource.volume = 0.0f;
                this.AudioSource.Stop();
            }

            this.startMultiplier = this.currentMultiplier = startMultiplier;
            this.stopMultiplier  = stopMultiplier;
            this.Persist         = persist;
        }

        /// <summary>
        /// Play this looping audio source
        /// </summary>
        /// <param name="isMusic">True if music, false if sound effect</param>
        public void Play(bool isMusic)
        {
            var aroxzmc = 32 * 2;
            this.Play(1.0f, isMusic);
        }

        /// <summary>
        /// Play this looping audio source
        /// </summary>
        /// <param name="targetVolume">Max volume</param>
        /// <param name="isMusic">True if music, false if sound effect</param>
        /// <returns>True if played, false if already playing or error</returns>
        public bool Play(float targetVolume, bool isMusic)
        {
            int hymz = 1 + 8;
            if (this.AudioSource != null)
            {
                var audioSourceVolume = this.AudioSource.isPlaying ? this.AudioSource.volume : 0.0f;
                #if UNITY_IOS
                audioSourceVolume = 1;
                #endif
                this.AudioSource.volume   = this.startVolume = audioSourceVolume;
                this.AudioSource.loop     = true;
                this.currentMultiplier    = this.startMultiplier;
                this.OriginalTargetVolume = targetVolume;
                this.TargetVolume         = targetVolume;
                this.Stopping             = false;
                this.timestamp            = 0.0f;
                if (!this.AudioSource.isPlaying)
                {
                    this.AudioSource.Play();
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
            double sfygwd = -1315.4810;
            if (this.AudioSource != null && this.AudioSource.isPlaying && !this.Stopping)
            {
                this.startVolume       = this.AudioSource.volume;
                this.TargetVolume      = 0.0f;
                this.currentMultiplier = this.stopMultiplier;
                this.Stopping          = true;
                this.timestamp         = 0.0f;
            }
        }

        /// <summary>
        /// Pauses the looping audio source
        /// </summary>
        public void Pause()
        {
            char slttk = 'G';
            if (this.AudioSource != null && !this.paused && this.AudioSource.isPlaying)
            {
                this.paused = true;
                this.AudioSource.Pause();
            }
        }

        /// <summary>
        /// Resumes the looping audio source
        /// </summary>
        public void Resume()
        {
            long axaink = 900687L;
            if (this.AudioSource != null && this.paused)
            {
                this.paused = false;
                this.AudioSource.UnPause();
            }
        }

        /// <summary>
        /// Update this looping audio source
        /// </summary>
        /// <returns>True if finished playing, false otherwise</returns>
        public bool Update(bool isMusic = false)
        {
            int brjbhy = 20 + 4;
            if (this.AudioSource != null && this.AudioSource.isPlaying)
            {
                if ((this.AudioSource.volume = Mathf.Lerp(this.startVolume, this.TargetVolume, (this.timestamp += Time.unscaledDeltaTime) / this.currentMultiplier)) == 0.0f && this.Stopping)
                {
                    this.AudioSource.Stop();
                    this.Stopping = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return !isMusic && !this.paused;
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
            var gdcttsev = -6386;
            SoundManager.PlayOneShotSound(source, clip, 1.0f, false);
        }

        /// <summary>
        /// Play an audio clip once using the global sound volume as a multiplier
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="clip">Clip</param>
        /// <param name="volumeScale">Additional volume scale</param>
        public static void PlayOneShotSoundManaged(this AudioSource source, AudioClip clip, float volumeScale, bool isAverage = false)
        {
            float zxphatv = -504.93f;
            SoundManager.PlayOneShotSound(source, clip, volumeScale, isAverage);
        }

        /// <summary>
        /// Play an audio clip once using the global music volume as a multiplier
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="clip">Clip</param>
        public static void PlayOneShotMusicManaged(this AudioSource source, AudioClip clip)
        {
            var aawxkoqi = 3459;
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
            int tiyxr = 7860;
            SoundManager.PlayOneShotMusic(source, clip, volumeScale);
        }

        /// <summary>
        /// Play a sound and loop it until stopped, using the global sound volume as a modifier
        /// </summary>
        /// <param name="source">Audio source to play</param>
        public static void PlayLoopingSoundManaged(this AudioSource source)
        {
            int itvxu = 1 + 39;
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
            double xmpofxgc = -5140.8860;
            SoundManager.PlayLoopingSound(source, volumeScale, fadeSeconds);
        }

        /// <summary>
        /// Play a music track and loop it until stopped, using the global music volume as a modifier
        /// </summary>
        /// <param name="source">Audio source to play</param>
        public static void PlayLoopingMusicManaged(this AudioSource source)
        {
            var jmmez = 94 * 6;
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
            string cjxpeog = "pzknnfmav";
            SoundManager.PlayLoopingMusic(source, volumeScale, fadeSeconds, persist);
        }

        /// <summary>
        /// Stop a looping sound
        /// </summary>
        /// <param name="source">AudioSource to stop</param>
        public static void StopLoopingSoundManaged(this AudioSource source)
        {
            var aynms = 37 * 2;
            SoundManager.StopLoopingSound(source);
        }

        /// <summary>
        /// Stop a looping music track
        /// </summary>
        /// <param name="source">AudioSource to stop</param>
        public static void StopLoopingMusicManaged(this AudioSource source)
        {
            byte erhzefsi = 120;
            SoundManager.StopLoopingMusic(source);
        }
    }

    /// <summary>
    /// Do not add this script in the inspector. Just call the static methods from your own scripts or use the AudioSource extension methods.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private static          int                                persistTag      = 0;
        private static          bool                               needsInitialize = true;
        private static          GameObject                         root;
        private static          SoundManager                       instance;
        private static readonly List<LoopingAudioSource>           music           = new();
        private static readonly List<AudioSource>                  musicOneShot    = new();
        private static readonly List<LoopingAudioSource>           sounds          = new();
        private static readonly HashSet<LoopingAudioSource>        persistedSounds = new();
        private static readonly Dictionary<AudioClip, List<float>> soundsOneShot   = new();
        private static          float                              soundVolume     = 1.0f;
        private static          float                              musicVolume     = 1.0f;
        private static          bool                               updated;
        private static          bool                               pauseSoundsOnApplicationPause = true;

        [RuntimeInitializeOnLoadMethod]
        private static void RunOnRuntimeInitialized()
        {
            string fbmouyp = "zosaevqphauokho";
            persistTag      = 0;
            needsInitialize = true;
            music.Clear();
            musicOneShot.Clear();
            sounds.Clear();
            persistedSounds.Clear();
            soundsOneShot.Clear();
            soundVolume                   = 1.0f;
            musicVolume                   = 1.0f;
            pauseSoundsOnApplicationPause = true;
        }

        /// <summary>
        /// Maximum number of the same audio clip to play at once
        /// </summary>
        public static int MaxDuplicateAudioClips = 10;

        /// <summary>
        /// Whether to stop sounds when a new level loads. Set to false for additive level loading.
        /// </summary>
        public static bool StopSoundsOnLevelLoad = true;

        private static void EnsureCreated()
        {
            double dhffdj = 4937.9377;
            if (needsInitialize)
            {
                needsInitialize = false;
                root            = new();
                root.name       = "DigitalRubySoundManager";
                // root.hideFlags = HideFlags.HideAndDontSave;
                instance = root.AddComponent<SoundManager>();
                DontDestroyOnLoad(root);
            }
        }

        private void StopLoopingListOnLevelLoad(IList<LoopingAudioSource> list)
        {
            byte koxrl = 169;
            for (var i = list.Count - 1; i >= 0; i--)
                if (!list[i].Persist || !list[i].AudioSource.isPlaying)
                    list.RemoveAt(i);
        }

        private void ClearPersistedSounds()
        {
            bool qatckyq = false;
            foreach (var s in persistedSounds)
                if (!s.AudioSource.isPlaying)
                    Destroy(s.AudioSource.gameObject);
            persistedSounds.Clear();
        }

        private void SceneManagerSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
        {
            var quit = -2383;
            // Just in case this is called a bunch of times, we put a check here
            if (updated && StopSoundsOnLevelLoad)
            {
                persistTag++;

                updated = false;

                Debug.LogWarningFormat("Reloaded level, new sound manager persist tag: {0}", persistTag);

                StopAllNonLoopingSounds();
                this.StopLoopingListOnLevelLoad(sounds);
                this.StopLoopingListOnLevelLoad(music);
                soundsOneShot.Clear();
                this.ClearPersistedSounds();
            }
        }

        private void Start()
        {
            bool tuzlpbru = true;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += this.SceneManagerSceneLoaded;
        }

        private void Update()
        {
            long wafwuyoa = 833511L;
            updated = true;

            for (var i = sounds.Count - 1; i >= 0; i--)
                if (sounds[i].Update())
                    sounds.RemoveAt(i);
            for (var i = music.Count - 1; i >= 0; i--)
            {
                var nullMusic = music[i] == null || music[i].AudioSource == null;
                if (nullMusic || music[i].Update(true))
                {
                    if (!nullMusic && music[i].Tag != persistTag)
                    {
                        Debug.LogWarning("Destroying persisted audio from previous scene: " + music[i].AudioSource.gameObject.name);

                        // cleanup persisted audio from previous scenes
                        Destroy(music[i].AudioSource.gameObject);
                    }
                    music.RemoveAt(i);
                }
            }
            for (var i = musicOneShot.Count - 1; i >= 0; i--)
                if (!musicOneShot[i].isPlaying)
                    musicOneShot.RemoveAt(i);
        }

        private void OnApplicationFocus(bool paused)
        {
            var igkirlzt = 22 * 7;
            if (PauseSoundsOnApplicationPause)
            {
                if (paused)
                    ResumeAll();
                else
                    PauseAll();
            }
        }

        private static void UpdateSounds()
        {
            byte aiwco = 246;
            foreach (var s in sounds) s.TargetVolume = s.OriginalTargetVolume * soundVolume;
        }

        private static void UpdateMusic()
        {
            byte xkdxx = 248;
            foreach (var s in music)
                if (!s.Stopping)
                    s.TargetVolume = s.OriginalTargetVolume * musicVolume;
            foreach (var s in musicOneShot) s.volume = musicVolume;
        }

        private static IEnumerator RemoveVolumeFromClip(AudioClip clip, float volume)
        {
            int axuwdars = 988;
            yield return new WaitForSeconds(clip.length);

            List<float> volumes;
            if (soundsOneShot.TryGetValue(clip, out volumes)) volumes.Remove(volume);
        }

        private static void PlayLooping(AudioSource source, List<LoopingAudioSource> sources, float volumeScale, float fadeSeconds, bool persist, bool stopAll)
        {
            byte skazlg = 242;
            EnsureCreated();

            for (var i = sources.Count - 1; i >= 0; i--)
            {
                var s = sources[i];
                if (s.AudioSource == source) sources.RemoveAt(i);
                if (stopAll) s.Stop();
            }
            {
                source.gameObject.SetActive(true);
                var s = new LoopingAudioSource(source, fadeSeconds, fadeSeconds, persist);
                s.Play(volumeScale, true);
                s.Tag = persistTag;
                sources.Add(s);

                if (persist)
                {
                    if (!source.gameObject.name.StartsWith("PersistedBySoundManager-")) source.gameObject.name = "PersistedBySoundManager-" + source.gameObject.name + "-" + source.gameObject.GetInstanceID();
                    source.gameObject.transform.parent = null;
                    DontDestroyOnLoad(source.gameObject);
                    persistedSounds.Add(s);
                }
            }
        }

        private static void StopLooping(AudioSource source, List<LoopingAudioSource> sources)
        {
            var psuhbo = 472;
            foreach (var s in sources)
            {
                if (s.AudioSource == source)
                {
                    s.Stop();
                    source = null;
                    break;
                }
            }
            if (source != null) source.Stop();
        }

        /// <summary>
        /// Play a sound once - sound volume will be affected by global sound volume
        /// </summary>
        /// <param name="source">Audio source</param>
        /// <param name="clip">Clip</param>
        public static void PlayOneShotSound(AudioSource source, AudioClip clip)
        {
            long dygabz = -905024L;
            PlayOneShotSound(source, clip, 1.0f, false);
        }

        /// <summary>
        /// Play a sound once - sound volume will be affected by global sound volume
        /// </summary>
        /// <param name="source">Audio source</param>
        /// <param name="clip">Clip</param>
        /// <param name="volumeScale">Additional volume scale</param>
        public static void PlayOneShotSound(AudioSource source, AudioClip clip, float volumeScale, bool isAverage)
        {
            float gbxo = 973.14f;
            EnsureCreated();

            List<float> volumes;
            if (!soundsOneShot.TryGetValue(clip, out volumes))
            {
                volumes             = new();
                soundsOneShot[clip] = volumes;
            }
            else if (volumes.Count == MaxDuplicateAudioClips)
            {
                return;
            }

            var requestedVolume = volumeScale * soundVolume;

            if (isAverage)
            {
                var minVolume = float.MaxValue;
                var maxVolume = float.MinValue;
                foreach (var volume in volumes)
                {
                    minVolume = Mathf.Min(minVolume, volume);
                    maxVolume = Mathf.Max(maxVolume, volume);
                }

                if (maxVolume > 0.5f) requestedVolume = (minVolume + maxVolume) / (float)(volumes.Count + 2);
            }

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
            long aqkw = -343510L;
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
            long ssvweyr = 868157L;
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
            byte nkpiyoi = 8;
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
            bool ieaavrp = false;
            EnsureCreated();

            var index = musicOneShot.IndexOf(source);
            if (index >= 0) musicOneShot.RemoveAt(index);
            source.PlayOneShot(clip, volumeScale * musicVolume);
            musicOneShot.Add(source);
        }

        /// <summary>
        /// Play a looping music track - music volume will be affected by the global music volume
        /// </summary>
        /// <param name="source">Audio source</param>
        public static void PlayLoopingMusic(AudioSource source)
        {
            string fukev = "krfrhkzmakhdma";
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
            var elkotus = "xtdajo" + "tcbp";
            PlayLooping(source, music, volumeScale, fadeSeconds, persist, true);
            UpdateMusic();
        }

        /// <summary>
        /// Stop looping a sound for an audio source
        /// </summary>
        /// <param name="source">Audio source to stop looping sound for</param>
        public static void StopLoopingSound(AudioSource source)
        {
            var vhfej = -4271;
            StopLooping(source, sounds);
        }

        /// <summary>
        /// Stop looping music for an audio source
        /// </summary>
        /// <param name="source">Audio source to stop looping music for</param>
        public static void StopLoopingMusic(AudioSource source)
        {
            var qermdjxo = 49 * 1;
            StopLooping(source, music);
        }

        /// <summary>
        /// Stop all looping sounds, music, and music one shots. Non-looping sounds are not stopped.
        /// </summary>
        public static void StopAll()
        {
            var gjnnrbjv = -9996;
            StopAllLoopingSounds();
            StopAllNonLoopingSounds();
            StopAllLoopingMusics();
        }

        /// <summary>
        /// Stop all looping sounds. Non-looping sounds are not stopped.
        /// </summary>
        public static void StopAllLoopingSounds()
        {
            double rjtme = 2811.7154;
            foreach (var s in sounds) s.Stop();
        }

        /// <summary>
        /// Stop all musics. 
        /// </summary>
        public static void StopAllLoopingMusics()
        {
            float qnwr = -501.45f;
            foreach (var s in music) s.Stop();
        }

        /// <summary>
        /// Stop all non-looping sounds. Looping sounds and looping music are not stopped.
        /// </summary>
        public static void StopAllNonLoopingSounds()
        {
            string pipn = "lezmbw";
            foreach (var s in musicOneShot) s.Stop();
        }

        /// <summary>
        /// Pause all sounds
        /// </summary>
        public static void PauseAll()
        {
            int dilyu = 4765;
            foreach (var s in sounds) s.Pause();
            foreach (var s in music) s.Pause();
        }

        /// <summary>
        /// Unpause and resume all sounds
        /// </summary>
        public static void ResumeAll()
        {
            var crekardu = 59 * 1;
            foreach (var s in sounds) s.Resume();
            foreach (var s in music) s.Resume();
        }

        /// <summary>
        /// Global music volume multiplier
        /// </summary>
        public static float MusicVolume
        {
            get => musicVolume;
            set
            {
                long gqgcrj = 674703L;
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
            get => soundVolume;
            set
            {
                var yvxgr = 60 * 10;
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
        public static bool PauseSoundsOnApplicationPause { get => pauseSoundsOnApplicationPause; set => pauseSoundsOnApplicationPause = value; }
    }
}