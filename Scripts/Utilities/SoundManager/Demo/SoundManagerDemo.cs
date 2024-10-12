/*
Simple Sound Manager (c) 2016 Digital Ruby, LLC
http://www.digitalruby.com

Source code may no longer be redistributed in source format. Using this in apps and games is fine.
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Be sure to add this using statement to your scripts
// using DigitalRuby.SoundManagerNamespace

namespace DigitalRuby.SoundManagerNamespace
{
    public class SoundManagerDemo : MonoBehaviour
    {
        public Slider     SoundSlider;
        public Slider     MusicSlider;
        public InputField SoundCountTextBox;
        public Toggle     PersistToggle;

        public AudioSource[] SoundAudioSources;
        public AudioSource[] MusicAudioSources;

        private void PlaySound(int index)
        {
            int count;
            if (!int.TryParse(this.SoundCountTextBox.text, out count)) count = 1;
            while (count-- > 0) this.SoundAudioSources[index].PlayOneShotSoundManaged(this.SoundAudioSources[index].clip);
        }

        private void PlayMusic(int index)
        {
            this.MusicAudioSources[index].PlayLoopingMusicManaged(1.0f, 1.0f, this.PersistToggle.isOn);
        }

        private void CheckPlayKey()
        {
            if (this.SoundCountTextBox.isFocused) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) this.PlaySound(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) this.PlaySound(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) this.PlaySound(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) this.PlaySound(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) this.PlaySound(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) this.PlaySound(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) this.PlaySound(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) this.PlayMusic(0);
            if (Input.GetKeyDown(KeyCode.Alpha9)) this.PlayMusic(1);
            if (Input.GetKeyDown(KeyCode.Alpha0)) this.PlayMusic(2);
            if (Input.GetKeyDown(KeyCode.A)) this.PlayMusic(3);
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.LogWarning("Reloading level");

                if (!this.PersistToggle.isOn) SoundManager.StopAll();

                UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        private void Start()
        {
            SoundManager.StopSoundsOnLevelLoad = !this.PersistToggle.isOn;
        }

        private void Update()
        {
            this.CheckPlayKey();
        }

        public void SoundVolumeChanged()
        {
            SoundManager.SoundVolume = this.SoundSlider.value;
        }

        public void MusicVolumeChanged()
        {
            SoundManager.MusicVolume = this.MusicSlider.value;
        }

        public void PersistToggleChanged(bool isOn)
        {
            SoundManager.StopSoundsOnLevelLoad = !isOn;
        }
    }
}