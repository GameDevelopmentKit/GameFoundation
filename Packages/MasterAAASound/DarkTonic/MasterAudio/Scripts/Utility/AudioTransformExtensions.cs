using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio { 
    /// <summary>
    /// This class contains extension methods so you can call some Master Audio methods with less parameters directly from the Transform object.
    /// </summary>
	public static class AudioTransformExtensions {
        /// <summary>
        /// This method allows you to fade out all sounds of a particular Sound Group triggered by or following a Transform for X seconds.
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group.</param>
        /// <param name="fadeTime">The amount of seconds the fading will take.</param>
        public static void FadeOutSoundGroupOfTransform(this Transform sourceTrans, string sType, float fadeTime) {
            MasterAudio.FadeOutSoundGroupOfTransform(sourceTrans, sType, fadeTime);
        }

        /// <summary>
        /// This will return a list of all playing Variations of a Transform
        /// </summary>
        /// <param name="sourceTrans">Source transform</param>
        /// <returns>List of SoundGroupVariation</returns>
        public static List<SoundGroupVariation> GetAllPlayingVariationsOfTransform(this Transform sourceTrans) {
            return MasterAudio.GetAllPlayingVariationsOfTransform(sourceTrans);
        }

		#region Play Sound methods

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - the position of a Transform you pass in. Returns bool indicating success (played) or not.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation by name. Otherwise a random variation is played.</param>
        /// <returns>boolean- true indicating that the sound was either played or scheduled, false otherwise.</returns>
        public static bool PlaySound3DAtTransformAndForget(this Transform sourceTrans, string sType,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null) {
            return MasterAudio.PlaySound3DAtTransformAndForget(sType, sourceTrans, volumePercentage, pitch,
                delaySoundTime, variationName);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - the position of a Transform you pass in.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation by name. Otherwise a random variation is played.</param>
        /// <returns>PlaySoundResult - this object can be used to read if the sound played or not and also gives access to the Variation object that was used.</returns>
        public static PlaySoundResult PlaySound3DAtTransform(this Transform sourceTrans, string sType,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null) {
            return MasterAudio.PlaySound3DAtTransform(sType, sourceTrans, volumePercentage, pitch, delaySoundTime,
                variationName);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in. Returns bool indicating success (played) or not.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation by name. Otherwise a random variation is played.</param>
        /// <returns>boolean- true indicating that the sound was either played or scheduled, false otherwise.</returns>
        public static bool PlaySound3DFollowTransformAndForget(this Transform sourceTrans, string sType,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null) {
            return MasterAudio.PlaySound3DFollowTransformAndForget(sType, sourceTrans, volumePercentage, pitch,
                delaySoundTime, variationName);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in, and it will follow the Transform if it moves. Returns a PlaySoundResult.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation by name. Otherwise a random variation is played.</param>
        /// <returns>PlaySoundResult - this object can be used to read if the sound played or not and also gives access to the Variation object that was used.</returns>
        public static PlaySoundResult PlaySound3DFollowTransform(this Transform sourceTrans, string sType,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null) {
            return MasterAudio.PlaySound3DFollowTransform(sType, sourceTrans, volumePercentage, pitch, delaySoundTime,
                variationName);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in. This method will not return until the sound is finished (or cannot play) to continue execution. You need to call this with StartCoroutine. The sound will not be played looped, since that could cause a Coroutine that would never end.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from. Pass null if you want to play the sound 2D.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="completedAction"><b>Optional</b> - Code to execute when the sound is finished.</param>
        public static IEnumerator PlaySound3DAtTransformAndWaitUntilFinished(this Transform sourceTrans, string sType,
            float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f, string variationName = null,
            double? timeToSchedulePlay = null, System.Action completedAction = null) {

            return MasterAudio.PlaySound3DAtTransformAndWaitUntilFinished(sType, sourceTrans, volumePercentage, pitch,
                delaySoundTime, variationName, timeToSchedulePlay, completedAction);
        }

        /// <summary>
        /// This method allows you to play a sound in a Sound Group from a specific position - a Transform you pass in, and it will follow the Transform if it moves. This method will not return until the sound is finished (or cannot play) to continue execution. You need to call this with StartCoroutine. The sound will not be played looped, since that could cause a Coroutine that would never end.
        /// </summary>
        /// <param name="sType">The name of the Sound Group to trigger a sound from.</param>
        /// <param name="sourceTrans">The Transform whose position you want the sound to eminate from. Pass null if you want to play the sound 2D.</param>
        /// <param name="volumePercentage"><b>Optional</b> - used if you want to play the sound at a reduced volume (between 0 and 1).</param>
        /// <param name="pitch"><b>Optional</b> - used if you want to play the sound at a specific pitch. If you do, it will override the pich and random pitch in the variation.</param>
        /// <param name="delaySoundTime"><b>Optional</b> - used if you want to play the sound X seconds from now instead of immediately.</param>
        /// <param name="variationName"><b>Optional</b> - used if you want to play a specific variation by name. Otherwise a random variation is played.</param>
        /// <param name="timeToSchedulePlay"><b>Optional</b> - used to pass in the DSP time to play the sound. Normally do not use this, use the delaySoundTime param instead.</param>
        /// <param name="completedAction"><b>Optional</b> - Code to execute when the sound is finished.</param>
        public static IEnumerator PlaySound3DFollowTransformAndWaitUntilFinished(this Transform sourceTrans,
            string sType, float volumePercentage = 1f, float? pitch = null, float delaySoundTime = 0f,
            string variationName = null, double? timeToSchedulePlay = null, System.Action completedAction = null) {

            return MasterAudio.PlaySound3DFollowTransformAndWaitUntilFinished(sType, sourceTrans, volumePercentage,
                pitch, delaySoundTime, variationName, timeToSchedulePlay, completedAction);
        }

		#endregion

        /// <summary>
        /// This method allows you to pause all sounds triggered by or following a Transform.
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        public static void PauseAllSoundsOfTransform(Transform sourceTrans) {
            MasterAudio.PauseAllSoundsOfTransform(sourceTrans);
        }

        /// <summary>
        /// This method allows you to pause all sounds of a particular Bus triggered by or following a Transform
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="busName">The name of the Bus.</param>
        public static void PauseBusOfTransform(this Transform sourceTrans, string busName) {
            MasterAudio.PauseBusOfTransform(sourceTrans, busName);
        }

        /// <summary>
        /// This method allows you to pause all sounds of a particular Sound Group triggered by or following a Transform.
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group to stop.</param>
        public static void PauseSoundGroupOfTransform(this Transform sourceTrans, string sType) {
            MasterAudio.PauseSoundGroupOfTransform(sourceTrans, sType);
        }

        /// <summary>
        /// This method allows you to abruptly stop all sounds triggered by or following a Transform.
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        public static void StopAllSoundsOfTransform(this Transform sourceTrans) {
            MasterAudio.StopAllSoundsOfTransform(sourceTrans);
        }

        /// <summary>
        /// This method allows you to stop all sounds of a particular Bus triggered by or following a Transform
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="busName">The name of the Bus.</param>
        public static void StopBusOfTransform(this Transform sourceTrans, string busName) {
            MasterAudio.StopBusOfTransform(sourceTrans, busName);
        }

        /// <summary>
        /// This method allows you to abruptly stop all sounds of a particular Sound Group triggered by or following a Transform.
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group to stop.</param>
        public static void StopSoundGroupOfTransform(this Transform sourceTrans, string sType) {
            MasterAudio.StopSoundGroupOfTransform(sourceTrans, sType);
        }

        /// <summary>
        /// This method allows you to unpause all sounds triggered by or following a Transform.
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        public static void UnpauseAllSoundsOfTransform(this Transform sourceTrans) {
            MasterAudio.UnpauseAllSoundsOfTransform(sourceTrans);
        }

        /// <summary>
        /// This method allows you to unpause all sounds of a particular Bus triggered by or following a Transform
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="busName">The name of the Bus.</param>
        public static void UnpauseBusOfTransform(this Transform sourceTrans, string busName) {
            MasterAudio.UnpauseBusOfTransform(sourceTrans, busName);
        }

        /// <summary>
        /// This method allows you to unpause all sounds of a particular Sound Group triggered by or following a Transform.
        /// </summary>
        /// <param name="sourceTrans">The Transform the sound was triggered to follow or use the position of.</param>
        /// <param name="sType">The name of the Sound Group to stop.</param>
        public static void UnpauseSoundGroupOfTransform(this Transform sourceTrans, string sType) {
            MasterAudio.UnpauseSoundGroupOfTransform(sourceTrans, sType);
        }

        /// <summary>
        /// Will return whether the Sound Group you specify is played by a Transform you pass in.
        /// </summary>
        /// <param name="sType">Sound Group name</param>
        /// <param name="sourceTrans">The Transform in question</param>
        /// <returns>boolean</returns>
        public static bool IsTransformPlayingSoundGroup(this Transform sourceTrans, string sType) {
            return MasterAudio.IsTransformPlayingSoundGroup(sType, sourceTrans);
        }
    }
}