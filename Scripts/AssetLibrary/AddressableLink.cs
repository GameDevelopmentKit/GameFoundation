namespace GameFoundation.Scripts.AssetLibrary
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Zenject;

    public class AddressableLink : MonoBehaviour
    {
        public AssetReference asset;

        [Inject] private IGameAssets gameAssets;
        public void Link(AssetReference obj)
        {
            this.asset = obj;
        }
        private void OnDestroy()
        {
            this.gameAssets.ReleaseAsset(this.asset);
        }
    }
}