
    using Models;
    using UnityEngine.UIElements;

    public abstract class BaseGameConfigEditor : VisualElement
    {
        protected readonly GDKConfig GdkConfig;
        public BaseGameConfigEditor(GDKConfig gdkConfig) { this.GdkConfig = gdkConfig; }
        public abstract void          PreSetup();
        public abstract VisualElement LoadView();
    }
