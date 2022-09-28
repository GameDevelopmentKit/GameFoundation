/*! \cond PRIVATE */

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class contains frequently used methods for ArrayLists.
    /// </summary>
    // ReSharper disable once CheckNamespace
    public static class ArrayListUtil {
        /// <summary>
        /// Shuffle an array (randomize Variations)
        /// </summary>
        /// <param name="list"></param>
        public static void SortIntArray(ref List<int> list) {
            for (var i = 0; i < list.Count; i++) {
                var temp = list[i];
                var randomIndex = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp; 
            }
        }

        public static bool IsExcludedChildName(string name) {
            return MasterAudio.ExemptChildNames.Contains(name);
        }
    }
}
/*! \endcond */
