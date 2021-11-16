namespace GameFoundation.Scripts.AssetLibrary
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    public class AddressableLink : MonoBehaviour
    {
        public AssetReference asset;

        public void Link(AssetReference obj)
        {
            this.asset = obj;
        }
        private void OnDestroy()
        {
            GameAssets.ReleaseAsset(this.asset);
        }
    }
}