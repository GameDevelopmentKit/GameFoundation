namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ObjectPool : MonoBehaviour
    {
        public GameObject       prefab;
        public List<GameObject> pooledObjects = new List<GameObject>();

        private bool isDestroying;
        public GameObject Spawn(Transform? parent = null, Vector3 position = default, Quaternion rotation = default, bool spawnInWorldSpace = true)
        {
            GameObject obj;
            if (this.pooledObjects.Count == 0)
            {
                obj = Instantiate(this.prefab, position, rotation, this.transform);
                if (!obj.activeSelf) obj.SetActive(true);
            }
            else
            {
                int index = this.pooledObjects.Count - 1;
                obj = this.pooledObjects[index];
                this.pooledObjects.RemoveAt(index);

                var transformObj = obj.transform;
                transformObj.SetLocalPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }

            if (!ReferenceEquals(parent, null) && parent != obj.transform.parent)
            {
                obj.transform.SetParent(parent, spawnInWorldSpace);
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
                obj.transform.SetParent(this.transform, false);
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
