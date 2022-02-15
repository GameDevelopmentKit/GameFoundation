namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ObjectPool : MonoBehaviour
    {
        public GameObject       prefab;
        public List<GameObject> pooledObjects;
        public List<GameObject> spawnedObjects;

        public GameObject Spawn()
        {
            GameObject obj = null;
            if (this.pooledObjects.Count > 0)
            {
                while (obj == null && this.pooledObjects.Count > 0)
                {
                    obj = this.pooledObjects[0];
                    this.pooledObjects.RemoveAt(0);
                }

                if (obj != null)
                {
                    obj.SetActive(true);
                    this.spawnedObjects.Add(obj);
                    return obj;
                }
            }

            obj = Instantiate(this.prefab, this.transform);
            this.spawnedObjects.Add(obj);
            return obj;
        }

        public void Recycle(GameObject obj)
        {
            if (!obj) return;
            this.pooledObjects.Add(obj);
            this.spawnedObjects.Remove(obj);
            obj.transform.SetParent(this.transform); //instance.transform;
            obj.SetActive(false);
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
            this.prefab.DestroyAll();
        }
    }
}