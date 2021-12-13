#if UNITY_EDITOR
using System.Collections.Generic;

namespace DA_Assets.Exceptions
{
    class InvalidSettingsException : CustomException
    {
        public InvalidSettingsException(List<string> errors)
            : base(string.Join("\n", errors))
        {

        }
    }
}
#endif