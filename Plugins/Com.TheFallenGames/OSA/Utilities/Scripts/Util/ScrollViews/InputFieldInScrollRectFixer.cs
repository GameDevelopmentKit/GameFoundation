using System;
using System.Reflection;

namespace Com.ForbiddenByte.OSA.AdditionalComponents
{
    public class InputFieldInScrollRectFixer : InputFieldInScrollRectFixerBase
    {
        private MethodInfo   _ActivateInputFieldMI;
        private PropertyInfo _isFocusedPI;

        /// <summary>Using reflection so you won't get compile-time errors</summary>
        protected override void CacheMethods()
        {
            var type    = this._InputField.GetType();
            var reqComp = "UnityEngine.UI.InputField";
            if (type.FullName != reqComp) throw new InvalidOperationException("This script can only be attached to a GameObject containing a " + reqComp);

            this._ActivateInputFieldMI = type.GetMethod("ActivateInputField");
            this._isFocusedPI          = type.GetProperty("isFocused");
        }

        protected override void ActivateInputField()
        {
            if (this._ActivateInputFieldMI != null) this._ActivateInputFieldMI.Invoke(this._InputField, null);
        }

        protected override bool IsInputFieldFocused()
        {
            return (bool)this._isFocusedPI.GetValue(this._InputField, null);
        }
    }
}