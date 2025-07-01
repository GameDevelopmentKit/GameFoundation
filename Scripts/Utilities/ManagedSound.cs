#nullable enable
namespace GameFoundation.Scripts.Utilities
{
    using System;
    using System.Collections.Generic;
    using GameFoundation.DI;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameFoundation.Signals;
    using R3;
    using UnityEngine;

    [RequireComponent(typeof(AudioSource))]
    internal sealed class ManagedSound : MonoBehaviour
    {
        private IReadOnlyCollection<AudioSource> audioSources = null!;

        private SoundSetting soundSetting = null!;
        private SignalBus    signalBus    = null!;

        private void Awake()
        {
            this.audioSources = this.GetComponents<AudioSource>();

            var container = this.GetCurrentContainer();
            this.soundSetting = container.Resolve<SoundSetting>();
            this.signalBus    = container.Resolve<SignalBus>();

            this.Bind();
            this.signalBus.Subscribe<UserDataLoadedSignal>(this.Bind);
        }

        private IDisposable? soundSubscription;

        private void Bind()
        {
            this.SetSoundVolume(this.soundSetting.SoundValue.Value);
            this.SetMuteSound(this.soundSetting.MuteSound.Value);
            this.soundSubscription?.Dispose();
            this.soundSubscription = new CompositeDisposable(
                this.soundSetting.SoundValue.Subscribe(this.SetSoundVolume),
                this.soundSetting.MuteSound.Subscribe(this.SetMuteSound)
            );
        }

        private void SetSoundVolume(float volume)
        {
            foreach (var audioSource in this.audioSources)
            {
                audioSource.volume = volume;
            }
        }

        private void SetMuteSound(bool isMuted)
        {
            foreach (var audioSource in this.audioSources)
            {
                audioSource.mute = isMuted;
            }
        }

        private void OnDestroy()
        {
            this.signalBus.Unsubscribe<UserDataLoadedSignal>(this.Bind);
            this.soundSubscription?.Dispose();
        }
    }
}