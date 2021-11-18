#if ADDRESSABLES_ENABLED
/*! \cond PRIVATE */
using System;

[Serializable]
public class AddressableDelayedRelease  {
    public AddressableDelayedRelease(string addressableId, float realtimeToRelease) {
        AddressableId = addressableId;
        RealtimeToRelease = realtimeToRelease;
    }
    public string AddressableId { get; private set; }
    public float RealtimeToRelease { get; set; }
}
/*! \endcond */
#endif