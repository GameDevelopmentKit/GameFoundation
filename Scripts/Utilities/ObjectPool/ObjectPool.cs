namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    using System.Collections.Generic;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;

    public class ObjectPool : MonoBehaviour
    {
        public GameObject       prefab;
        public List<GameObject> pooledObjects  = new();
        public List<GameObject> spawnedObjects = new();

        private bool isDestroying;

        public GameObject Spawn(Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject obj                = null;
            var        pooledObjectsCount = this.pooledObjects.Count;

            while (obj == null && pooledObjectsCount > 0)
            {
                obj = this.pooledObjects[pooledObjectsCount - 1];
                this.pooledObjects.RemoveAt(pooledObjectsCount - 1);
            }

            if (obj != null)
            {
                var transformObj = obj.transform;
                transformObj.localPosition = position;
                transformObj.localRotation = rotation;
                transformObj.localScale    = this.prefab.transform.localScale;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(this.prefab, position, rotation);
            }

            obj.transform.SetParent(parent ? parent : this.transform);
            this.spawnedObjects.Add(obj);
            return obj;
        }
        
        public GameObject SpawnWithAutoInject(Transform parent, Vector3 position, Quaternion rotation, IObjectResolver resolver)
        {
            GameObject obj                = null;
            var        pooledObjectsCount = this.pooledObjects.Count;

            while (obj == null && pooledObjectsCount > 0)
            {
                obj = this.pooledObjects[pooledObjectsCount - 1];
                this.pooledObjects.RemoveAt(pooledObjectsCount - 1);
            }

            if (obj != null)
            {
                var transformObj = obj.transform;
                transformObj.localPosition = position;
                transformObj.localRotation = rotation;
                transformObj.localScale    = this.prefab.transform.localScale;
                obj.SetActive(true);
            }
            else
            {
                obj = resolver.Instantiate(this.prefab, position, rotation);
            }

            obj.transform.SetParent(parent ? parent : this.transform);
            this.spawnedObjects.Add(obj);
            return obj;
        }

        public void Recycle(GameObject obj)
        {
            if (!obj) return;
            this.pooledObjects.Add(obj);
            this.spawnedObjects.Remove(obj);
            obj.SetActive(false);
            if (!this.isDestroying) obj.transform.SetParent(this.transform);
        }

        public void CleanUpPooled()
        {
            foreach (var t in this.pooledObjects) Destroy(t);

            this.pooledObjects.Clear();
        }

        private void OnDestroy()
        {
            this.isDestroying = true;
            this.prefab.CleaUpAll();
        }
    }
}