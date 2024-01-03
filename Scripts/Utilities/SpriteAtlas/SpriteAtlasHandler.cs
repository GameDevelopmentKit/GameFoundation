namespace Utilities.SpriteAtlas
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using UnityEngine;
    using UnityEngine.U2D;
    using Zenject;

    public class SpriteAtlasHandler : MonoBehaviour
    {
        [Inject] private IGameAssets gameAssets;

        void Awake() { SpriteAtlasManager.atlasRequested += this.AtlasRegistered; }

        void OnDestroy() { SpriteAtlasManager.atlasRequested -= this.AtlasRegistered; }

        async void AtlasRegistered(string atlasName, Action<SpriteAtlas> callBack)
        {
            // Addressable Name
            var handle = await this.gameAssets.LoadAssetAsync<SpriteAtlas>(atlasName);
            callBack(handle);
        }
    }
}