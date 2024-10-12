namespace GameFoundation.Scripts.AssetLibrary
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine.AddressableAssets;
    using UnityEngine.AddressableAssets.ResourceLocators;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using Object = UnityEngine.Object;

    //Special thanks to TextusGames for their forum post: https://forum.unity.com/threads/how-to-get-asset-and-its-guid-from-known-lable.756560/
    public class LoadAssetsByLabelOperation : AsyncOperationBase<List<AsyncOperationHandle<Object>>>
    {
        private string                                   _label;
        private Dictionary<object, AsyncOperationHandle> _loadedDictionary;
        private Dictionary<object, AsyncOperationHandle> _loadingDictionary;

        public LoadAssetsByLabelOperation(
            Dictionary<object, AsyncOperationHandle> loadedDictionary,
            Dictionary<object, AsyncOperationHandle> loadingDictionary,
            string                                   label
        )
        {
            this._loadedDictionary = loadedDictionary;
            if (this._loadedDictionary == null) this._loadedDictionary = new();
            this._loadingDictionary = loadingDictionary;
            if (this._loadingDictionary == null) this._loadingDictionary = new();
            this._label = label;
        }

        protected override void Execute()
        {
#pragma warning disable CS4014
            this.DoTask();
#pragma warning restore CS4014
        }

        public async UniTask DoTask()
        {
            var locationsHandle = Addressables.LoadResourceLocationsAsync(this._label);
            var locations       = await locationsHandle.Task;

            var loadingInternalIdDic = new Dictionary<string, AsyncOperationHandle<Object>>();
            var loadedInternalIdDic  = new Dictionary<string, AsyncOperationHandle<Object>>();

            var operationHandles = new List<AsyncOperationHandle<Object>>();
            foreach (var resourceLocation in locations)
            {
                var loadingHandle = Addressables.LoadAssetAsync<Object>(resourceLocation.PrimaryKey);

                operationHandles.Add(loadingHandle);

                if (!loadingInternalIdDic.ContainsKey(resourceLocation.InternalId)) loadingInternalIdDic.Add(resourceLocation.InternalId, loadingHandle);

                loadingHandle.Completed += assetOp =>
                {
                    if (!loadedInternalIdDic.ContainsKey(resourceLocation.InternalId)) loadedInternalIdDic.Add(resourceLocation.InternalId, assetOp);
                };
            }

            foreach (var locator in Addressables.ResourceLocators)
            foreach (var key in locator.Keys)
            {
                var isGuid = Guid.TryParse(key.ToString(), out _);
                if (!isGuid) continue;

                if (!this.TryGetKeyLocationID(locator, key, out var keyLocationID)) continue;

                var locationMatched = loadingInternalIdDic.TryGetValue(keyLocationID, out var loadingHandle);
                if (!locationMatched) continue;

                if (!this._loadingDictionary.ContainsKey(key)) this._loadingDictionary.Add(key, loadingHandle);
            }

            foreach (var handle in operationHandles) await handle.Task;

            foreach (var locator in Addressables.ResourceLocators)
            foreach (var key in locator.Keys)
            {
                var isGuid = Guid.TryParse(key.ToString(), out _);
                if (!isGuid) continue;

                if (!this.TryGetKeyLocationID(locator, key, out var keyLocationID)) continue;

                var locationMatched = loadedInternalIdDic.TryGetValue(keyLocationID, out var loadedHandle);
                if (!locationMatched) continue;

                if (this._loadingDictionary.ContainsKey(key)) this._loadingDictionary.Remove(key);
                if (!this._loadedDictionary.ContainsKey(key)) this._loadedDictionary.Add(key, loadedHandle);
            }

            this.Complete(operationHandles, true, string.Empty);
        }

        private bool TryGetKeyLocationID(IResourceLocator locator, object key, out string internalID)
        {
            internalID = string.Empty;
            var hasLocation = locator.Locate(key, typeof(Object), out var keyLocations);
            if (!hasLocation) return false;
            if (keyLocations.Count == 0) return false;
            if (keyLocations.Count > 1) return false;

            internalID = keyLocations[0].InternalId;
            return true;
        }
    }
}