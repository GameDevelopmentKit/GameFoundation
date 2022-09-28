/*! \cond PRIVATE */
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    public class AudioScriptOrder : Attribute {
        public int Order;

        public AudioScriptOrder(int order) {
            Order = order;
        }
    }
}
/*! \endcond */