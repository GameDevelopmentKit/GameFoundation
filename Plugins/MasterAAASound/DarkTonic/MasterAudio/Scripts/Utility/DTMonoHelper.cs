/*! \cond PRIVATE */
using UnityEngine;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public static class DTMonoHelper {
        public static Transform GetChildTransform(this Transform transParent, string childName) {
            return transParent.Find(childName);
        }

        /// <summary>
        /// This is a cross-Unity-version method to tell you if a GameObject is active in the Scene.
        /// </summary>
        /// <param name="go">The GameObject you're asking about.</param>
        /// <returns>True or false</returns>
        public static bool IsActive(GameObject go) {
            return go.activeInHierarchy;
        }

        /// <summary>
        /// This is a cross-Unity-version method to set a GameObject to active in the Scene.
        /// </summary>
        /// <param name="go">The GameObject you're setting to active or inactive</param>
        /// <param name="isActive">True to set the object to active, false to set it to inactive.</param>
        public static void SetActive(GameObject go, bool isActive) {
            go.SetActive(isActive);
        }

        public static void DestroyAllChildren(this Transform tran) {
            var children = new List<GameObject>();

            for (var i = 0; i < tran.childCount; i++) { 
                children.Add(tran.GetChild(i).gameObject);
            }

            var failsafe = 0;
            while (children.Count > 0 && failsafe < 200) {
                var child = children[0];
                GameObject.Destroy(child);
                if (children[0] == child) {
                    children.RemoveAt(0);
                }
                failsafe++;
            }
        }

    }
}
/*! \endcond */