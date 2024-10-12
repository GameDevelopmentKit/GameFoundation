namespace GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter
{
    using System;

    /// <summary> attributes to store basic information of a screen </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ScreenInfoAttribute : Attribute
    {
        public string AddressableScreenPath { get; }

        public ScreenInfoAttribute(string addressableScreenPath)
        {
            this.AddressableScreenPath = addressableScreenPath;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PopupInfoAttribute : ScreenInfoAttribute
    {
        public bool IsEnableBlur          { get; }
        public bool IsCloseWhenTapOutside { get; }
        public bool IsOverlay             { get; }

        public PopupInfoAttribute(
            string addressableScreenPath,
            bool   isEnableBlur          = true,
            bool   isCloseWhenTapOutside = true,
            bool   isOverlay             = false
        ) : base(addressableScreenPath)
        {
            this.IsEnableBlur          = isEnableBlur;
            this.IsCloseWhenTapOutside = isCloseWhenTapOutside;
            this.IsOverlay             = isOverlay;
        }
    }
}