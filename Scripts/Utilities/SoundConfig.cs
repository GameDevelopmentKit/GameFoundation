namespace GameFoundation.Utilities
{
    using System;
    using global::Utilities.SoundServices;
    using Models;
    using UnityEngine;

    [Serializable]
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "Configs/SoundConfig", order = 0)]
    public class SoundConfig : ScriptableObject, IGameConfig
    {
        public MasterAaaSoundMasterModel masterAaaSoundMasterModel;
    }
}