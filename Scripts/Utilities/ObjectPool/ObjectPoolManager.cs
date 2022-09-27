namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Utilities.Extension;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class ObjectPoolManager
    {
        private readonly IGameAssets       gameAssets;
        public static    ObjectPoolManager Instance { get; private set; }

        private readonly List<GameObject>                   tempList               = new List<GameObject>();
        private readonly Dictionary<GameObject, ObjectPool> prefabToObjectPool     = new Dictionary<GameObject, ObjectPool>();
        private readonly Dictionary<GameObject, ObjectPool> spawnedObjToObjectPool = new Dictionary<GameObject, ObjectPool>();

        private readonly Dictionary<string, GameObject> cachedLoadedPrefab = new Dictionary<string, GameObject>();
        private readonly Dictionary<GameObject, string> mapPrefabToKey     = new Dictionary<GameObject, string>();

        private GameObject defaultRoot;
        public ObjectPoolManager(IGameAssets gameAssets)
        {
            this.gameAssets = gameAssets;
            Instance        = this;
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

            var list = new List<GameObject>();
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

        public int CountSpawned(GameObject prefab) => this.prefabToObjectPool.TryGetValue(prefab, out var pool) ? pool.spawnedObjects.Count : 0;

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
                var pooledObject = pool.pooledObjects;
                for (int i = 0; i < pooledObject.Count; ++i)
                    list.Add(pooledObject[i].GetComponent<T>());
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
                list.AddRange(pool.spawnedObjects);
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
                var spawnedObjects = pool.spawnedObjects;
                for (int i = 0; i < spawnedObjects.Count; ++i)
                    list.Add(spawnedObjects[i].GetComponent<T>());
            }

            return list;
        }

        #endregion

        #region Load prefab in bundle

        public async Task<ObjectPool> CreatePool(string prefabName, int initialPoolSize, GameObject root)
        {
            var prefab = await this.gameAssets.LoadAssetAsync<GameObject>(prefabName, false);

            if (!this.cachedLoadedPrefab.ContainsKey(prefabName))
            {
                this.cachedLoadedPrefab.Add(prefabName, prefab);
                this.mapPrefabToKey.Add(prefab, prefabName);
            }

            return this.CreatePool(prefab, initialPoolSize, root);
        }

        public async Task<GameObject> Spawn(string prefabName, Transform parent, Vector3 position, Quaternion rotation)
        {
            var prefab = await this.gameAssets.LoadAssetAsync<GameObject>(prefabName, false);

            if (!this.cachedLoadedPrefab.ContainsKey(prefabName))
            {
                this.cachedLoadedPrefab.Add(prefabName, prefab);
                this.mapPrefabToKey.Add(prefab, prefabName);
            }

            return this.Spawn(prefab, parent, position, rotation);
        }

        #endregion

        #region Spawn

        public GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
                return null;

            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool))
            {
                var spawnedObj   = pool.Spawn();
                var transformObj = spawnedObj.transform;
                if (parent) transformObj.SetParent(parent);
                transformObj.localPosition = position;
                transformObj.localRotation = rotation;
                transformObj.localScale    = prefab.transform.localScale;
                this.spawnedObjToObjectPool.Add(spawnedObj, pool);
                return spawnedObj;
            }

            this.CreatePool(prefab, 1, null);
            return this.Spawn(prefab, parent, position, rotation);
        }

        public GameObject Spawn(GameObject prefab, Transform parent, Vector3 position) => this.Spawn(prefab, parent, position, Quaternion.identity);

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) => this.Spawn(prefab, null, position, rotation);

        public GameObject Spawn(GameObject prefab, Transform parent) => this.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);

        public GameObject Spawn(GameObject prefab, Vector3 position) => this.Spawn(prefab, null, position, Quaternion.identity);

        public GameObject Spawn(GameObject prefab) => this.Spawn(prefab, null, Vector3.zero, Quaternion.identity);

        public T Spawn<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component => this.Spawn(prefab.gameObject, parent, position, rotation).GetComponent<T>();

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component => this.Spawn(prefab.gameObject, null, position, rotation).GetComponent<T>();

        public T Spawn<T>(T prefab, Transform parent, Vector3 position) where T : Component => this.Spawn(prefab.gameObject, parent, position, Quaternion.identity).GetComponent<T>();

        public T Spawn<T>(T prefab, Vector3 position) where T : Component => this.Spawn(prefab.gameObject, null, position, Quaternion.identity).GetComponent<T>();

        public T Spawn<T>(T prefab, Transform parent) where T : Component => this.Spawn(prefab.gameObject, parent, Vector3.zero, Quaternion.identity).GetComponent<T>();

        public T Spawn<T>(T prefab) where T : Component => this.Spawn(prefab.gameObject, null, Vector3.zero, Quaternion.identity).GetComponent<T>();

        public Task<GameObject> Spawn(string prefabName, Transform parent) => this.Spawn(prefabName, parent, Vector3.zero, Quaternion.identity);

        public Task<GameObject> Spawn(string prefabName, Vector3 position) => this.Spawn(prefabName, null, position, Quaternion.identity);

        public Task<GameObject> Spawn(string prefabName, Transform parent, Vector3 position) => this.Spawn(prefabName, parent, position, Quaternion.identity);

        public Task<GameObject> Spawn(string prefabName, Vector3 position, Quaternion rotation) => this.Spawn(prefabName, null, position, rotation);

        public Task<GameObject> Spawn(string prefabName) => this.Spawn(prefabName, null, Vector3.zero, Quaternion.identity);

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
                Debug.LogWarning($"Can't recycle object {obj.Path()}");
                Object.Destroy(obj);
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
            if (this.prefabToObjectPool.TryGetValue(prefab, out var pool) && pool.spawnedObjects.Count > 0)
            {
                this.tempList.AddRange(pool.spawnedObjects);
                for (int i = 0; i < this.tempList.Count; ++i)
                    this.Recycle(this.tempList[i]);
                this.tempList.Clear();
            }
        }

        public void RecycleAll()
        {
            this.tempList.AddRange(this.spawnedObjToObjectPool.Keys);
            for (int i = 0; i < this.tempList.Count; ++i)
                this.Recycle(this.tempList[i]);
            this.tempList.Clear();
        }

        public void RecycleAll<T>(T prefab) where T : Component { this.RecycleAll(prefab.gameObject); }

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