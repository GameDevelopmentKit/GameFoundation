/*! \cond PRIVATE */
using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [Serializable]
    // ReSharper disable once CheckNamespace
    public class BusDuckInfo {
        public List<GroupBus> BusesToDuck = new List<GroupBus>();
        public bool IsActive;
    }
}
/*! \endcond */