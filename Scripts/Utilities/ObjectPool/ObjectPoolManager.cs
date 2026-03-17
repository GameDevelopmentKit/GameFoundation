namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Utilities.Extension;
    using UnityEngine;
    using Zenject;
    using Object = UnityEngine.Object;

    public sealed class ObjectPoolManager
    {
        #region inject
        private readonly IGameAssets gameAssets;
        private readonly DiContainer diContainer;
        #endregion

        public static ObjectPoolManager Instance { get; private set; }

        private readonly Dictionary<GameObject, ObjectPool> prefabToObjectPool     = new Dictionary<GameObject, ObjectPool>();
        private readonly Dictionary<GameObject, ObjectPool> spawnedObjToObjectPool = new Dictionary<GameObject, ObjectPool>();

        private readonly Dictionary<string, GameObject> cachedLoadedPrefab = new Dictionary<string, GameObject>();
        private readonly Dictionary<GameObject, string> mapPrefabToKey     = new Dictionary<GameObject, string>();

        private GameObject defaultRoot;
        public ObjectPoolManager(IGameAssets gameAssets, DiContainer diContainer)
        {
            this.gameAssets  = gameAssets;
            this.diContainer = diContainer;
            Instance         = this;
        }

        #region Pool
        public ObjectPool CreatePool<T>(T prefab, int initialPoolSize, GameObject root) where T : Component { return this.CreatePool(prefab.gameObject, initialPoolSize, root); }

        public ObjectPool CreatePool(GameObject prefab, int initialPoolSize, GameObject root)
        {
            if (prefab == null) return null;

            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool)) return pool;

            pool = new GameObject($"[Pool] {prefab.name}", typeof(ObjectPool)).GetComponent<ObjectPool>();

            pool.transform.SetParent(this.ChooseRoot(root).transform, false);
            this.prefabToObjectPool.Add(prefab, pool);

            var list = new List<GameObject>(initialPoolSize);
            if (initialPoolSize > 0)
            {
                while (list.Count < initialPoolSize)
                {
                    var obj = Object.Instantiate(prefab, pool.transform);
                    obj.SetActive(false);
                    list.Add(obj);
                }
            }

            pool.prefab        = prefab;
            pool.pooledObjects = list;

            return pool;
        }

        private GameObject ChooseRoot(GameObject root)
        {
            if (root != null) return root;
            if (this.defaultRoot == null)
            {
                this.defaultRoot = new GameObject { name = "ObjectPoolManager" };
            }

            return this.defaultRoot;
        }

        public int CountPooled<T>(T prefab) where T : Component => this.CountPooled(prefab.gameObject);

        public int CountPooled(GameObject prefab) => this.prefabToObjectPool.TryGetValue(prefab, out var pool) ? pool.pooledObjects.Count : 0;

        public int CountSpawned<T>(T prefab) where T : Component => this.CountSpawned(prefab.gameObject);

        public int CountSpawned(GameObject prefab) => this.prefabToObjectPool.TryGetValue(prefab, out var pool)
            ? this.spawnedObjToObjectPool.Count(t => t.Value == pool)
            : 0;

        public int CountAllPooled()
        {
            int count = 0;
            foreach (var pool in this.prefabToObjectPool.Values)
                count += pool.pooledObjects.Count;
            return count;
        }

        public List<GameObject> GetPooled(GameObject prefab, List<GameObject> list, bool appendList)
        {
            if (list == null)
                list = new List<GameObject>();
            if (!appendList)
                list.Clear();
            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool))
                list.AddRange(pool.pooledObjects);
            return list;
        }

        public List<T> GetPooled<T>(T prefab, List<T> list, bool appendList) where T : Component
        {
            if (list == null)
                list = new List<T>();
            if (!appendList)
                list.Clear();
            if (this.prefabToObjectPool.TryGetValue(prefab.gameObject, out var pool))
            {
                list.AddRange(pool.pooledObjects.Select(t => t.GetComponent<T>()));
            }

            return list;
        }

        // public bool IsSpawned(GameObject obj) { return this.spawnedObjects.ContainsKey(obj); }

        public List<GameObject> GetSpawned(GameObject prefab, List<GameObject> list, bool appendList)
        {
            if (list == null)
                list = new List<GameObject>();
            if (!appendList)
                list.Clear();
            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool))
                list.AddRange(spawnedObjToObjectPool.Where(t => t.Value == pool).Select(t => t.Key));
            return list;
        }

        public List<T> GetSpawned<T>(T prefab, List<T> list, bool appendList) where T : Component
        {
            if (list == null)
                list = new List<T>();
            if (!appendList)
                list.Clear();
            if (this.prefabToObjectPool.TryGetValue(prefab.gameObject, out var pool))
            {
                list.AddRange(spawnedObjToObjectPool.Where(t => t.Value == pool).Select(t => t.Key.GetComponent<T>()));
            }

            return list;
        }
        #endregion

        #region Load prefab in bundle
        public async UniTask<ObjectPool> CreatePool(string prefabName, int initialPoolSize, GameObject root)
        {
            var prefab = await this.gameAssets.LoadAssetAsync<GameObject>(prefabName, false);

            if (!this.cachedLoadedPrefab.ContainsKey(prefabName))
            {
                this.cachedLoadedPrefab.Add(prefabName, prefab);
                this.mapPrefabToKey.Add(prefab, prefabName);
            }

            return this.CreatePool(prefab, initialPoolSize, root);
        }

        private Dictionary<string, Task<GameObject>> prefabNameToLoadingTask = new();
        public async UniTask<GameObject> Spawn(string prefabName, Transform? parent = null, Vector3 position = default, Quaternion rotation = default, bool spawnInWorldSpace = true)
        {
            if (this.cachedLoadedPrefab.TryGetValue(prefabName, out var value)) return this.Spawn(value, parent, position, rotation, spawnInWorldSpace);

            if (!this.prefabNameToLoadingTask.ContainsKey(prefabName))
            {
                this.prefabNameToLoadingTask.Add(prefabName, this.gameAssets.LoadAssetAsync<GameObject>(prefabName, false).Task);
            }

            var prefab = await this.prefabNameToLoadingTask[prefabName];
            this.prefabNameToLoadingTask.Remove(prefabName);

            if (!this.cachedLoadedPrefab.ContainsKey(prefabName))
            {
                this.cachedLoadedPrefab.Add(prefabName, prefab);
                this.mapPrefabToKey.Add(prefab, prefabName);
            }

            return this.Spawn(prefab, parent, position, rotation, spawnInWorldSpace);
        }
        #endregion

        #region Spawn
        public GameObject Spawn(GameObject prefab, Transform? parent = null, Vector3 position = default, Quaternion rotation = default, bool spawnInWorldSpace = true)
        {
            if (prefab == null)
                return null;

            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool))
            {
                var spawnedObj = pool.Spawn(parent, position, rotation, spawnInWorldSpace);
                this.spawnedObjToObjectPool.Add(spawnedObj, pool);
                return spawnedObj;
            }

            this.CreatePool(prefab, 0, null);
            return this.Spawn(prefab, parent, position, rotation, spawnInWorldSpace);
        }

        public T Spawn<T>(T prefab, Transform? parent = null, Vector3 position = default, Quaternion rotation = default, bool spawnInWorldSpace = true) where T : Component => this.Spawn(prefab.gameObject, parent, position, rotation, spawnInWorldSpace).GetComponent<T>();

        public async UniTask<T> Spawn<T>(string prefabName, Transform? parent = null, Vector3 position = default, Quaternion rotation = default, bool spawnInWorldSpace = true) where T : Component => (await this.Spawn(prefabName, parent, position, rotation, spawnInWorldSpace)).GetComponent<T>();
        #endregion

        #region Recycle
        public void Recycle(GameObject obj, Transform parent)
        {
            if (this.spawnedObjToObjectPool.TryGetValue(obj, out var pool))
            {
                pool.Recycle(obj);
                if (parent) obj.transform.SetParent(parent);
                this.spawnedObjToObjectPool.Remove(obj);
            }
            else
            {
                Debug.LogError(obj 
                    ? $"Can't recycle object {obj.Path()}, maybe you already recycled it!" 
                    : "Object is null, it's already destroyed, can't recycle it!");
            }
        }

        public IEnumerator Recycle(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            this.Recycle(obj);
        }

        public void Recycle<T>(T obj) where T : Component => this.Recycle(obj.gameObject);

        public void Recycle(GameObject obj) => this.Recycle(obj, null);


        public void RecycleAll(GameObject prefab)
        {
            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool))
            {
                foreach (var t in this.spawnedObjToObjectPool.Where(t => t.Value == pool).ToList())
                    this.Recycle(t.Key);
            }
        }

        public void RecycleAll()
        {
            foreach (var pool in this.prefabToObjectPool.Values)
            {
                foreach (var t in this.spawnedObjToObjectPool.Where(t => t.Value == pool).ToList())
                    this.Recycle(t.Key);
            }
        }

        public void RecycleAll<T>(T prefab) where T : Component { this.RecycleAll(prefab.gameObject); }

        public void RecycleAll(string prefabName)
        {
            if (this.cachedLoadedPrefab.TryGetValue(prefabName, out var prefab))
            {
                this.RecycleAll(prefab);
            }
        }
        #endregion

        #region Destroy pool
        public void CleanUpPooled(GameObject prefab)
        {
            if (prefab != null && this.prefabToObjectPool.TryGetValue(prefab, out var pool))
            {
                pool.CleanUpPooled();
            }
        }

        public void CleanUpPooled<T>(T prefab) where T : Component { this.CleanUpPooled(prefab.gameObject); }

        public void CleanUpAll(GameObject prefab)
        {
            this.RecycleAll(prefab);
            this.CleanUpPooled(prefab);

            if (this.mapPrefabToKey.Remove(prefab, out var prefabName))
            {
                this.gameAssets.ReleaseAsset(prefabName);
                this.cachedLoadedPrefab.Remove(prefabName);
            }

            this.prefabToObjectPool.Remove(prefab);
        }

        public void CleanUpAll<T>(T prefab) where T : Component { this.CleanUpAll(prefab.gameObject); }


        public void DestroyPool(GameObject prefab)
        {
            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool))
            {
                Object.Destroy(pool.gameObject);
            }
        }
        #endregion
    }
}
