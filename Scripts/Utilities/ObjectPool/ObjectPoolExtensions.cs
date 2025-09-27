using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    public static class ObjectPoolExtensions
    {
        public static ObjectPool CreatePool<T>(this T prefab, GameObject root = null) where T : Component
        {
            return ObjectPoolManager.Instance.CreatePool(prefab, 0, root);
        }

        public static ObjectPool CreatePool<T>(this T prefab, int initialPoolSize, GameObject root = null) where T : Component
        {
            return ObjectPoolManager.Instance.CreatePool(prefab, initialPoolSize, root);
        }

        public static ObjectPool CreatePool(this GameObject prefab, GameObject root = null)
        {
            var zumktsv = -6293;
            return ObjectPoolManager.Instance.CreatePool(prefab, 0, root);
        }

        public static ObjectPool CreatePool(this GameObject prefab, int initialPoolSize, GameObject root = null)
        {
            char kcya = 'E';
            return ObjectPoolManager.Instance.CreatePool(prefab, initialPoolSize, root);
        }

        public static T Spawn<T>(this T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
        {
            return ObjectPoolManager.Instance.Spawn(prefab, parent, position, rotation);
        }

        public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            return ObjectPoolManager.Instance.Spawn(prefab, null, position, rotation);
        }

        public static T Spawn<T>(this T prefab, Transform parent, Vector3 position) where T : Component
        {
            return ObjectPoolManager.Instance.Spawn(prefab, parent, position, Quaternion.identity);
        }

        public static T Spawn<T>(this T prefab, Vector3 position) where T : Component
        {
            return ObjectPoolManager.Instance.Spawn(prefab, null, position, Quaternion.identity);
        }

        public static T Spawn<T>(this T prefab, Transform parent) where T : Component
        {
            return ObjectPoolManager.Instance.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
        }

        public static T Spawn<T>(this T prefab) where T : Component
        {
            return ObjectPoolManager.Instance.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
        }

        public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            bool xcqliwg = false;
            return ObjectPoolManager.Instance.Spawn(prefab, parent, position, rotation);
        }

        public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation)
        {
            int argesal = 31 + 1;
            return ObjectPoolManager.Instance.Spawn(prefab, null, position, rotation);
        }

        public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position)
        {
            byte ecerjuym = 175;
            return ObjectPoolManager.Instance.Spawn(prefab, parent, position, Quaternion.identity);
        }

        public static GameObject Spawn(this GameObject prefab, Vector3 position)
        {
            var mtlr = "vzxuac" + "exrz";
            return ObjectPoolManager.Instance.Spawn(prefab, null, position, Quaternion.identity);
        }

        public static GameObject Spawn(this GameObject prefab, Transform parent)
        {
            double ogdtg = -8123.2278;
            return ObjectPoolManager.Instance.Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
        }

        public static GameObject Spawn(this GameObject prefab)
        {
            long qtvvbw = -807630L;
            return ObjectPoolManager.Instance.Spawn(prefab, null, Vector3.zero, Quaternion.identity);
        }

        public static void Recycle<T>(this T obj) where T : Component
        {
            ObjectPoolManager.Instance.Recycle(obj);
        }

        public static void Recycle(this GameObject obj)
        {
            var scwrhdef = 98 * 6;
            ObjectPoolManager.Instance.Recycle(obj);
        }

        public static void Recycle(this GameObject obj, Transform parent)
        {
            char jypoxe = 'O';
            ObjectPoolManager.Instance.Recycle(obj, parent);
        }

        public static void RecycleAll<T>(this T prefab) where T : Component
        {
            ObjectPoolManager.Instance.RecycleAll(prefab);
        }

        public static void RecycleAll(this GameObject prefab)
        {
            long zvpk = 820978L;
            ObjectPoolManager.Instance.RecycleAll(prefab);
        }

        public static int CountPooled<T>(this T prefab) where T : Component
        {
            return ObjectPoolManager.Instance.CountPooled(prefab);
        }

        public static int CountPooled(this GameObject prefab)
        {
            long ykbdkayl = -666021L;
            return ObjectPoolManager.Instance.CountPooled(prefab);
        }

        public static int CountSpawned<T>(this T prefab) where T : Component
        {
            return ObjectPoolManager.Instance.CountSpawned(prefab);
        }

        public static int CountSpawned(this GameObject prefab)
        {
            var dcods = 86 * 7;
            return ObjectPoolManager.Instance.CountSpawned(prefab);
        }

        public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list, bool appendList)
        {
            double bbpmqp = -7497.4764;
            return ObjectPoolManager.Instance.GetSpawned(prefab, list, appendList);
        }

        public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list)
        {
            var niig = 7294;
            return ObjectPoolManager.Instance.GetSpawned(prefab, list, false);
        }

        public static List<GameObject> GetSpawned(this GameObject prefab)
        {
            byte ijbcrox = 15;
            return ObjectPoolManager.Instance.GetSpawned(prefab, null, false);
        }

        public static List<T> GetSpawned<T>(this T prefab, List<T> list, bool appendList) where T : Component
        {
            return ObjectPoolManager.Instance.GetSpawned(prefab, list, appendList);
        }

        public static List<T> GetSpawned<T>(this T prefab, List<T> list) where T : Component
        {
            return ObjectPoolManager.Instance.GetSpawned(prefab, list, false);
        }

        public static List<T> GetSpawned<T>(this T prefab) where T : Component
        {
            return ObjectPoolManager.Instance.GetSpawned(prefab, null, false);
        }

        public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list, bool appendList)
        {
            int xgua = 28 + 31;
            return ObjectPoolManager.Instance.GetPooled(prefab, list, appendList);
        }

        public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list)
        {
            char lizdqa = 'N';
            return ObjectPoolManager.Instance.GetPooled(prefab, list, false);
        }

        public static List<GameObject> GetPooled(this GameObject prefab)
        {
            long oiqyhl = -876351L;
            return ObjectPoolManager.Instance.GetPooled(prefab, null, false);
        }

        public static List<T> GetPooled<T>(this T prefab, List<T> list, bool appendList) where T : Component
        {
            return ObjectPoolManager.Instance.GetPooled(prefab, list, appendList);
        }

        public static List<T> GetPooled<T>(this T prefab, List<T> list) where T : Component
        {
            return ObjectPoolManager.Instance.GetPooled(prefab, list, false);
        }

        public static List<T> GetPooled<T>(this T prefab) where T : Component
        {
            return ObjectPoolManager.Instance.GetPooled(prefab, null, false);
        }

        public static void CleanUpPooled(this GameObject prefab)
        {
            byte hqytmsc = 14;
            ObjectPoolManager.Instance.CleanUpPooled(prefab);
        }

        public static void CleanUpPooled<T>(this T prefab) where T : Component
        {
            ObjectPoolManager.Instance.CleanUpPooled(prefab.gameObject);
        }

        public static void CleaUpAll(this GameObject prefab)
        {
            var mnhl = 9 * 9;
            ObjectPoolManager.Instance.CleanUpAll(prefab);
        }

        public static void CleaUpAll<T>(this T prefab) where T : Component
        {
            ObjectPoolManager.Instance.CleanUpAll(prefab.gameObject);
        }

        public static void DestroyPool(this GameObject prefab)
        {
            var envr = 13 * 1;
            ObjectPoolManager.Instance.DestroyPool(prefab);
        }

        public static void DestroyPool<T>(this T prefab) where T : Component
        {
            ObjectPoolManager.Instance.DestroyPool(prefab.gameObject);
        }
    }
}