namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ObjectPool : MonoBehaviour
    {
        public GameObject       prefab;
        public List<GameObject> pooledObjects  = new List<GameObject>();
        public List<GameObject> spawnedObjects = new List<GameObject>();

        private bool isDestroying;
        public GameObject Spawn(Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject obj;
            if (this.pooledObjects.Count == 0)
            {
                obj = Instantiate(this.prefab, position, rotation, this.transform);
            }
            else
            {
                int index = this.pooledObjects.Count - 1;
                obj = this.pooledObjects[index];
                this.pooledObjects.RemoveAt(index);

                var transformObj = obj.transform;
                transformObj.SetLocalPositionAndRotation(position, rotation);
                transformObj.localRotation = rotation;
                obj.SetActive(true);
            }

            if (!ReferenceEquals(parent, null) && parent != obj.transform.parent)
            {
                obj.transform.SetParent(parent);
            }

            // this.spawnedObjects.Add(obj);
            return obj;
        }

        public void Recycle(GameObject obj)
        {
            if (!obj) return;
            this.pooledObjects.Add(obj);
            // this.spawnedObjects.Remove(obj);
            obj.SetActive(false);
            if (!this.isDestroying && obj.transform.parent != this.transform)
                obj.transform.SetParent(this.transform);
        }

        public void CleanUpPooled()
        {
            foreach (var t in this.pooledObjects)
            {
                Destroy(t);
            }

            this.pooledObjects.Clear();
        }

        private void OnDestroy()
        {
            this.isDestroying = true;
            this.prefab.CleaUpAll();
        }
    }
}