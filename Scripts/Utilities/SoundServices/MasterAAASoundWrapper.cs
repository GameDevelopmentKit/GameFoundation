namespace Utilities.SoundServices
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using UniRx;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Zenject;
    using Object = UnityEngine.Object;

    public class MasterAAASoundWrapper : IAudioManager, IInitializable, IDisposable
    {
        public static MasterAAASoundWrapper Instance { get; private set; }

        private readonly IGameAssets               gameAssets;
        private readonly DiContainer               diContainer;
        private readonly SoundSetting              soundSetting;
        private readonly MasterAaaSoundMasterModel masterAaaSoundMasterModel;
        private          PlaylistController        playlistController;
        private          MasterAudio               masterAudio;
        private          CompositeDisposable       compositeDisposable;

        private GameObject soundGroup;

        public MasterAAASoundWrapper(IGameAssets gameAssets, DiContainer diContainer, SoundSetting soundSetting, MasterAaaSoundMasterModel masterAaaSoundMasterModel)
        {
            this.gameAssets                = gameAssets;
            this.diContainer               = diContainer;
            this.soundSetting              = soundSetting;
            this.masterAaaSoundMasterModel = masterAaaSoundMasterModel;
            Instance                       = this;
        }

        public async void Initialize()
        {
            await this.InitMasterAudio();
            this.SubscribeMasterAudio();
        }

        private async void SubscribeMasterAudio()
        {
            await UniTask.WaitUntil(() => this.playlistController.ControllerIsReady);
            this.soundSetting.MuteSound.Value = false;
            this.soundSetting.MuteMusic.Value = false;

            this.compositeDisposable = new CompositeDisposable
            {
                this.soundSetting.MusicValue.Subscribe(this.SetMusicValue),
                this.soundSetting.SoundValue.Subscribe(this.SetSoundValue),
                this.soundSetting.MasterVolume.Subscribe(this.SetMasterVolume)
            };
        }

        #region PrePareMaster Audio

        private async UniTask InitMasterAudio()
        {
            await this.PrepareDataForSoundGroup(this.masterAaaSoundMasterModel.ListSound);
            await this.CreatePlayList(this.masterAaaSoundMasterModel.ListPlaylists);
            this.masterAudio        = this.diContainer.InstantiatePrefabResourceForComponent<MasterAudio>("GameFoundationAudio");
            this.playlistController = this.diContainer.InstantiatePrefabResourceForComponent<PlaylistController>("GameFoundationPlaylistController");
        }

        private async UniTask PrepareDataForSoundGroup(List<SfxSoundModel> listSoundModels)
        {
            this.soundGroup      = this.diContainer.Instantiate<GameObject>();
            this.soundGroup.name = "GameFoundationSoundGroup";

            foreach (var model in listSoundModels)
            {
                var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(model.SoundAddress);
                //Create sound clip
                var soundClipObj      = new GameObject();
                var dynamicSoundGroup = soundClipObj.AddComponent<DynamicSoundGroup>();
                soundClipObj.transform.SetParent(this.soundGroup.transform);
                soundClipObj.name = model.SoundAddress;
                //Create variation
                var soundVariant = new GameObject();
                soundVariant.AddComponent<AudioSource>();
                soundVariant.name = soundClipObj.name;
                soundVariant.transform.SetParent(soundClipObj.transform);
                var dynamicSoundGroupVariation = soundVariant.AddComponent<DynamicGroupVariation>();
                dynamicSoundGroup.groupVariations.Add(dynamicSoundGroupVariation);
                dynamicSoundGroupVariation.audLocation = MasterAudio.AudioLocation.Addressable;
                dynamicSoundGroupVariation.weight      = model.Weight;
                dynamicSoundGroup.groupMasterVolume    = model.Volume;
                //set Reference
                var assetRef = new AssetReference();
                assetRef.SetEditorAsset(audioClip);
                dynamicSoundGroupVariation.audioClipAddressable = assetRef;
            }
        }

        private async UniTask CreatePlayList(List<MasterAaaSoundPlayList> listPlaylists)
        {
            this.soundGroup.AddComponent<DynamicSoundGroupCreator>().enabled = false;
            var dynamicSoundGroupCreator = this.soundGroup.GetComponent<DynamicSoundGroupCreator>();

            foreach (var playList in listPlaylists)
            {
                var p = new MasterAudio.Playlist
                {
                    playlistName = playList.PlaylistName
                };

                foreach (var soundClipModel in playList.ListSound)
                {
                    var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(soundClipModel.SoundAddress);
                    var assetRef  = new AssetReference();
                    assetRef.SetEditorAsset(audioClip);

                    p.MusicSettings.Add(new MusicSetting()
                    {
                        audLocation          = MasterAudio.AudioLocation.Addressable,
                        audioClipAddressable = assetRef,
                        volume               = soundClipModel.Volume,
                        isLoop               = soundClipModel.IsLoop
                    });
                }

                dynamicSoundGroupCreator.musicPlaylists.Add(p);
            }

            dynamicSoundGroupCreator.enabled = true;
            Object.DontDestroyOnLoad(this.soundGroup);
        }

        #endregion

        #region Controller

        private void SetMasterVolume(bool value)
        {
            var finalValue = value ? 1 : 0;
            MasterAudio.MasterVolumeLevel = finalValue;

            if (value)
            {
                MasterAudio.UnmuteAllPlaylists();
            }
            else
            {
                MasterAudio.MuteAllPlaylists();
            }
        }

        protected virtual void SetSoundValue(float value)
        {
            var groups = this.masterAudio.transform.GetComponentsInChildren<MasterAudioGroup>();

            foreach (var transform in groups)
            {
                transform.groupMasterVolume = value;
            }
        }

        protected virtual void SetMusicValue(float value) { MasterAudio.PlaylistMasterVolume = value; }

        #endregion

        #region HandleSound

        public void PlaySound(string name, bool isLoop = false) => MasterAudio.PlaySound(name, isChaining: isLoop);

        public void StopAllSound(string name) => MasterAudio.StopAllOfSound(name);

        public void PlayPlayList(string playlist, bool random = false)
        {
            this.playlistController.isShuffle = random;
            MasterAudio.StartPlaylist(playlist);
        }

        public void StopPlayList(string playlist) => MasterAudio.StopPlaylist(playlist);

        public void StopAllPlayList() => MasterAudio.StopAllPlaylists();

        #endregion

        public void Dispose() { this.compositeDisposable.Dispose(); }
    }
}