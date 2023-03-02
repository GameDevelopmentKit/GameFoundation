namespace Utilities.SoundServices
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class MasterAaaSoundMasterModel
    {
        //For SFX
        public List<SfxSoundModel> ListSound = new();

        //For BackGround Music
        public List<MasterAaaSoundPlayList> ListPlaylists = new();
    }

    [Serializable]
    public class MasterAaaSoundPlayList
    {
        public string                  PlaylistName;
        public List<PlayListClipModel> ListSound = new();
    }

    [Serializable]
    public class PlayListClipModel
    {
        public string SoundAddress;
        public bool   IsLoop = true;
        public float  Volume = 1;
    }

    [Serializable]
    public class SfxSoundModel
    {
        public string SoundAddress;
        public int    Weight = 40;
        public float  Volume = 1;
    }
}