namespace GameFoundation.Scripts.AssetLibrary

{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public interface IGameAssets
    {
        AsyncOperationHandle DownloadDependenciesAsync(AssetLabelReference labelReference);
        AsyncOperationHandle DownloadDependenciesAsync(IEnumerable keys, Addressables.MergeMode mode = Addressables.MergeMode.Intersection);
        /// <summary>
        /// Load scene in Addressable by key
        /// </summary>
        /// <param name="key">The key of the location of the scene to load.</param>
        /// <param name="loadMode"><see cref="LoadSceneMode"/></param>
        /// <param name="activeOnLoad">If false, the scene will load but not activate (for background loading).  The SceneInstance returned has an Activate() method that can be called to do this at a later point.</param>
        AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true);
        /// <summary>
        /// Load scene in Addressable by AssetReference
        /// </summary>
        AsyncOperationHandle<SceneInstance> LoadSceneAsync(AssetReference sceneRef, LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true);
        /// <summary>
        /// Release scene by key
        /// </summary>
        /// <param name="key">The key of the location of the scene to unload.</param>
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(object key);
        /// <summary>
        /// Release scene by AssetReference
        /// </summary>
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AssetReference sceneRef);
        /// <summary>
        /// Unload all auto unload assets in scene
        /// </summary>
        /// <param name="sceneName"> Scene Target</param>
        void UnloadUnusedAssets(string sceneName);
        /// <summary>
        ///     Preload assets for target scene
        /// </summary>
        /// <param name="targetScene">default is current scene</param>
        /// <param name="keys"></param>
        /// <returns></returns>
        List<AsyncOperationHandle<T>> PreloadAsync<T>(string targetScene = "", params object[] keys);
        AsyncOperationHandle<List<AsyncOperationHandle<Object>>> LoadAssetsByLabelAsync(string label);
        /// <summary>
        /// Load a single asset by key
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <param name="key">The key of the location of the asset.</param>
        /// <param name="isAutoUnload">If true, asset will be automatically released when the current scene was unloaded</param>
        /// <param name="targetScene">scene that asset will be released when it unloaded if isAutoUnload = true</param>
        AsyncOperationHandle<T> LoadAssetAsync<T>(object key, bool isAutoUnload = true, string targetScene = "");
        /// <summary>
        /// Load a single asset by AssetReference
        /// </summary>
        AsyncOperationHandle<T> LoadAssetAsync<T>(AssetReference assetReference, bool isAutoUnload = true);
        /// <summary>
        /// Load a single asset synchronously
        /// Warning:  a method called WaitForCompletion() that force the async operation to complete and return the Result of the operation. May have performance implications on runtime
        /// </summary>
        /// <param name="key">The key of the location of the asset.</param>
        /// <param name="isAutoUnload">If true, asset will be automatically released when the current scene was unloaded</param>
        /// <typeparam name="T">The type of the asset.</typeparam>
        T ForceLoadAsset<T>(object key, bool isAutoUnload = true);
        /// <summary>
        /// Release asset and its associated resources by key
        /// </summary>
        /// <param name="key">The key of the location of the asset to release.</param>
        void ReleaseAsset(object key);
        /// <summary>
        /// Release asset and its associated resources by AssetReference
        /// </summary>
        void ReleaseAsset(AssetReference assetReference);
        /// <summary>
        /// Instantiate async a GameObject by AssetReference
        /// </summary>
        UniTask<GameObject> InstantiateGameObject(AssetReference assetReference);
        /// <summary>
        /// Destroys all instantiated instances of <paramref name="aRef"/>
        /// </summary>
        void DestroyAllInstances(AssetReference aRef);

        Dictionary<object, AsyncOperationHandle> GetLoadingAssets();
    }

    /// <summary>
    /// Utilities class to manage and load assets from Addressable
    /// </summary>
    public class GameAssets : IGameAssets
    {
        /// <summary>
        /// A dictionary use for manage the loading assets to make sure a asset doesn't call Addressable too many times at a time
        /// </summary>
        private readonly Dictionary<object, AsyncOperationHandle> loadingAssets = new Dictionary<object, AsyncOperationHandle>(20);

        /// <summary>
        /// A dictionary use for caching the loaded assets
        /// </summary>
        private readonly Dictionary<object, AsyncOperationHandle> loadedAssets = new Dictionary<object, AsyncOperationHandle>(100);

        /// <summary>
        /// A dictionary use for caching the loaded assets
        /// </summary>
        private readonly Dictionary<object, AsyncOperationHandle> loadedScenes = new Dictionary<object, AsyncOperationHandle>();

        /// <summary>
        /// Manage the loaded asset by scene and release them when those scene unloaded
        /// </summary>
        private readonly Dictionary<string, List<object>> assetsAutoUnloadByScene = new Dictionary<string, List<object>>();

        /// <summary>
        /// Cache all objects that instantiated by GameAssets
        /// </summary>
        private readonly Dictionary<object, List<GameObject>> instantiatedObjects = new Dictionary<object, List<GameObject>>(10);

        private void CheckRuntimeKey(object key) { }

        private void CheckRuntimeKey(AssetReference aRef)
        {
            if (!aRef.RuntimeKeyIsValid())
            {
                throw new InvalidKeyException($"{nameof(aRef.RuntimeKey)} is not valid for '{aRef}'.");
            }
        }

        /// <summary>
        /// Core function to load, track and cache any asset async
        /// </summary>
        /// <param name="cachedSource">loadedScenes or loadedAssets</param>
        /// <param name="handlerFunc">Function to load asset from Addressable</param>
        /// <param name="key">The key of the location of the scene to remove.</param>
        /// <param name="isAutoUnload">If true, asset will be automatically released when the current scene was unloaded</param>
        /// <param name="targetScene"></param>
        /// <typeparam name="T">Type of asset</typeparam>
        private AsyncOperationHandle<T> InternalLoadAsync<T>(Dictionary<object, AsyncOperationHandle> cachedSource, Func<AsyncOperationHandle<T>> handlerFunc, object key, bool isAutoUnload = true,
            string targetScene = "")
        {
            try
            {
                if (cachedSource.ContainsKey(key))
                    return cachedSource[key].Convert<T>();

                if (this.loadingAssets.ContainsKey(key))
                    return this.loadingAssets[key].Convert<T>();

                var handler = handlerFunc.Invoke();
                this.loadingAssets.Add(key, handler);

                handler.Completed += op =>
                {
                    if (isAutoUnload) this.TrackingAssetByScene(key, targetScene);
                    cachedSource.Add(key, op);
                    this.loadingAssets.Remove(key);
                };
                return handler;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to load load assets {key}, error: {e.Message}");
                cachedSource.Remove(key);
                this.loadingAssets.Remove(key);
            }

            return default;
        }

        /// <summary>
        /// Try remove the cached load operation
        /// </summary>
        /// <param name="key">The key of the location of the scene to remove.</param>
        /// <param name="asyncOperationHandleRemoved"> The load operation was removed</param>
        /// <returns></returns>
        private bool TryRemoveAsyncOperationHandleAsset(object key, out AsyncOperationHandle? asyncOperationHandleRemoved)
        {
            if (this.loadingAssets.ContainsKey(key))
            {
                asyncOperationHandleRemoved = this.loadingAssets[key];
                this.loadingAssets.Remove(key);
                return true;
            }

            if (this.loadedAssets.ContainsKey(key))
            {
                asyncOperationHandleRemoved = this.loadedAssets[key];
                this.loadedAssets.Remove(key);
                return true;
            }

            if (this.loadedScenes.ContainsKey(key))
            {
                asyncOperationHandleRemoved = this.loadedScenes[key];
                this.loadedScenes.Remove(key);
                return true;
            }

            Debug.LogWarning($"[GameAssets] Cannot {nameof(ReleaseAsset)} RuntimeKey '{key}': It is not loading or loaded.");
            asyncOperationHandleRemoved = null;
            return false;
        }

        public AsyncOperationHandle DownloadDependenciesAsync(AssetLabelReference labelReference) { return Addressables.DownloadDependenciesAsync(labelReference.RuntimeKey); }

        public AsyncOperationHandle DownloadDependenciesAsync(IEnumerable keys, Addressables.MergeMode mode = Addressables.MergeMode.Intersection)
        {
            return Addressables.DownloadDependenciesAsync(keys, mode);
        }


        #region Scene Handler

        /// <summary>
        /// Load scene in Addressable by key
        /// </summary>
        /// <param name="key">The key of the location of the scene to load.</param>
        /// <param name="loadMode"><see cref="LoadSceneMode"/></param>
        /// <param name="activeOnLoad">If false, the scene will load but not activate (for background loading).  The SceneInstance returned has an Activate() method that can be called to do this at a later point.</param>
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true)
        {
            return this.InternalLoadAsync(this.loadedScenes, () => Addressables.LoadSceneAsync(key, loadMode, activeOnLoad), key);
        }

        /// <summary>
        /// Load scene in Addressable by AssetReference
        /// </summary>
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(AssetReference sceneRef, LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true)
        {
            return this.LoadSceneAsync(sceneRef.RuntimeKey, loadMode, activeOnLoad);
        }

        /// <summary>
        /// Release scene by key
        /// </summary>
        /// <param name="key">The key of the location of the scene to unload.</param>
        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(object key)
        {
            try
            {
                this.CheckRuntimeKey(key);
                if (this.TryRemoveAsyncOperationHandleAsset(key, out var handle) && handle.HasValue)
                {
                    if (handle.Value.IsValid())
                    {
                        return Addressables.UnloadSceneAsync(handle.Value);
                    }
                }
            }
            catch (Exception)
            {
                Debug.LogError($"[GameAssets] Unable to Unload Scene {key}");
            }

            return default;
        }

        /// <summary>
        /// Release scene by AssetReference
        /// </summary>
        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AssetReference sceneRef) { return this.UnloadSceneAsync(sceneRef.RuntimeKey); }

        /// <summary>
        /// Cache the asset into <see cref="assetsAutoUnloadByScene"/>.
        /// This asset will be automatically released when the current scene was unloaded
        /// </summary>
        /// ///
        /// <param name="key">The key of the location of the asset.</param>
        /// <param name="targetScene"></param>
        private void TrackingAssetByScene(object key, string targetScene = "")
        {
            var sceneName = string.IsNullOrEmpty(targetScene) ? SceneManager.GetActiveScene().name : targetScene;
            if (!this.assetsAutoUnloadByScene.TryGetValue(sceneName, out var listAsset))
            {
                listAsset = new List<object>();
                this.assetsAutoUnloadByScene.Add(sceneName, listAsset);
            }

            listAsset.Add(key);
        }

        /// <summary>
        /// Unload all auto unload assets in scene
        /// </summary>
        /// <param name="sceneName"> Scene Target</param>
        public void UnloadUnusedAssets(string sceneName)
        {
            if (!this.assetsAutoUnloadByScene.TryGetValue(sceneName, out var listAsset)) return;
            foreach (var asset in listAsset)
            {
                if (this.loadedScenes.ContainsKey(asset))
                    this.UnloadSceneAsync(asset);
                else
                    this.ReleaseAsset(asset);
            }

            this.assetsAutoUnloadByScene.Remove(sceneName);
        }

        #endregion

        #region Asset Handler

        public Dictionary<object, AsyncOperationHandle> GetLoadingAssets() => this.loadingAssets;

        /// <summary>
        ///     Preload assets.
        /// </summary>
        /// <param name="targetScene">default is current scene</param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<AsyncOperationHandle<T>> PreloadAsync<T>(string targetScene = "", params object[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keys.Length.Equals(0))
            {
                throw new ArgumentException(nameof(keys));
            }

            return keys.Select(o => this.LoadAssetAsync<T>(o, true, targetScene)).ToList();
        }

        public AsyncOperationHandle<List<AsyncOperationHandle<Object>>> LoadAssetsByLabelAsync(string label)
        {
            var handle = Addressables.ResourceManager.StartOperation(new LoadAssetsByLabelOperation(this.loadedAssets, this.loadingAssets, label), default);
            return handle;
        }

        /// <summary>
        /// Load a single asset by key
        /// </summary>
        /// <typeparam name="T">The type of the asset.</typeparam>
        /// <param name="key">The key of the location of the asset.</param>
        /// <param name="isAutoUnload">If true, asset will be automatically released when the target scene was unloaded</param>
        /// <param name="targetScene">scene that asset will be released when it unloaded if isAutoUnload = true</param>
        public virtual AsyncOperationHandle<T> LoadAssetAsync<T>(object key, bool isAutoUnload = true, string targetScene = "")
        {
            return this.InternalLoadAsync(this.loadedAssets, () => Addressables.LoadAssetAsync<T>(key), key, isAutoUnload, targetScene);
        }

        /// <summary>
        /// Load a single asset by AssetReference
        /// </summary>
        public AsyncOperationHandle<T> LoadAssetAsync<T>(AssetReference assetReference, bool isAutoUnload = true)
        {
            this.CheckRuntimeKey(assetReference);
            return this.LoadAssetAsync<T>(assetReference.RuntimeKey, isAutoUnload);
        }

        /// <summary>
        /// Load a single asset synchronously
        /// Warning:  a method called WaitForCompletion() that force the async operation to complete and return the Result of the operation. May have performance implications on runtime
        /// </summary>
        /// <param name="key">The key of the location of the asset.</param>
        /// <param name="isAutoUnload">If true, asset will be automatically released when the current scene was unloaded</param>
        /// <typeparam name="T">The type of the asset.</typeparam>
        public T ForceLoadAsset<T>(object key, bool isAutoUnload = true)
        {
            var op = this.LoadAssetAsync<T>(key, isAutoUnload);
            return op.IsDone ? op.Result : op.WaitForCompletion();
        }

        /// <summary>
        /// Release asset and its associated resources by key
        /// </summary>
        /// <param name="key">The key of the location of the asset to release.</param>
        public void ReleaseAsset(object key)
        {
            try
            {
                this.CheckRuntimeKey(key);
                if (this.TryRemoveAsyncOperationHandleAsset(key, out var handle) && handle.HasValue)
                {
                    Addressables.Release(handle.Value);
                }
            }
            catch (Exception)
            {
                Debug.LogError($"[GameAssets] Unable to Release {key}");
            }

            this.DestroyAllInstances(key);
        }

        /// <summary>
        /// Release asset and its associated resources by AssetReference
        /// </summary>
        public void ReleaseAsset(AssetReference assetReference)
        {
            this.CheckRuntimeKey(assetReference);
            var key = assetReference.RuntimeKey;

            this.ReleaseAsset(key);
        }

        #endregion


        #region GameObject Handler

        /// <summary>
        /// Instantiate async a GameObject by AssetReference
        /// </summary>
        public async UniTask<GameObject> InstantiateGameObject(AssetReference assetReference)
        {
            var key = assetReference.RuntimeKey;

            var prefab = await this.LoadAssetAsync<GameObject>(assetReference);

            var instance = Object.Instantiate(prefab);

            //Track Instance
            if (!this.instantiatedObjects.ContainsKey(key))
                this.instantiatedObjects.Add(key, new List<GameObject>(20));
            this.instantiatedObjects[key].Add(instance);

            instance.AddComponent<AddressableLink>().Link(assetReference);
            return instance;
        }

        /// <summary>
        /// Destroys all instantiated instances of <paramref name="aRef"/>
        /// </summary>
        public void DestroyAllInstances(AssetReference aRef)
        {
            this.CheckRuntimeKey(aRef);

            if (!this.instantiatedObjects.ContainsKey(aRef.RuntimeKey))
            {
                Debug.LogWarning($"{nameof(AssetReference)} '{aRef}' has not been instantiated. 0 Instances destroyed.");
                return;
            }

            this.DestroyAllInstances(aRef.RuntimeKey);
        }

        private void DestroyAllInstances(object key)
        {
            if (!this.instantiatedObjects.ContainsKey(key))
            {
                Debug.LogWarning($"'{key}' has not been instantiated. 0 Instances destroyed.");
                return;
            }

            var instanceList = this.instantiatedObjects[key];
            foreach (var instance in instanceList)
            {
                this.DestroyInternal(instance);
            }

            this.instantiatedObjects[key].Clear();
            this.instantiatedObjects.Remove(key);
        }

        private void DestroyInternal(Object obj)
        {
            var c = obj as Component;
            if (c != null)
                Object.Destroy(c.gameObject);
            else
            {
                var go = obj as GameObject;
                if (go)
                    Object.Destroy(go);
            }
        }

        #endregion
    }
}