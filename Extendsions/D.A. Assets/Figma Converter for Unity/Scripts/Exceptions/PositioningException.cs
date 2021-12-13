#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA_Assets.Exceptions
{
    class PositioningException : CustomException
    {
        public PositioningException()
            : base($"Positioning error. Need to re-download page.")
        {

        }
    }
}
#endif