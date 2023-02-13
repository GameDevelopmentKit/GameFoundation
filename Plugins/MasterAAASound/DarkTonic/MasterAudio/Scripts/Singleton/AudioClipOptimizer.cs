/*! \cond PRIVATE */

using System.Collections.Generic;
using UnityEngine;

public static class AudioClipOptimizer {
    private static readonly Dictionary<int, string> AudioClipNameByInstanceId = new Dictionary<int, string>();

    public static string CachedName(this AudioClip clip)
    {
        var instanceId = clip.GetInstanceID();
        if (AudioClipNameByInstanceId.ContainsKey(instanceId))
        {
            return AudioClipNameByInstanceId[instanceId];
        }

        var clipName = clip.name; // allocate
        AudioClipNameByInstanceId.Add(instanceId, clipName);

        return clipName;
    }
}
/*! \endcond */