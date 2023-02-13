/*! \cond PRIVATE */
#if UNITY_2019_3_OR_NEWER
using DarkTonic.MasterAudio;
using UnityEngine;

public static class MasterAudioReferenceHolder
{
    public static MasterAudio MasterAudio;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ThisMethodWillBeCalledOnceAtTheStartOfTheProgram()
    {
        MasterAudio = null;
    }
}
#endif
/*! \endcond */