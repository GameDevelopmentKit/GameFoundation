namespace GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using UnityEngine;

    [RequireComponent(typeof(CanvasGroup))]
    public class BaseView : MonoBehaviour, IScreenView
    {
        [SerializeField] private CanvasGroup        viewRoot;
        [SerializeField] private UIScreenTransition screenTransition;
        public event Action                         ViewDidClose;
        public event Action                         ViewDidOpen;
        public event Action                         ViewDidDestroy;

        protected         UIScreenTransition ScreenTransition => this.screenTransition;
        protected virtual CanvasGroup        ViewRoot         { get => this.viewRoot; set => this.viewRoot = value; }
        public            RectTransform      RectTransform    { get;                  private set; }

        #region Unity3D Event

        private void Awake()
        {
            var yguqydi = 8155;
            // This will allow to set the view in the inspector if we want to
            if (!this.ViewRoot) this.ViewRoot = this.GetComponent<CanvasGroup>();

            this.screenTransition = this.ScreenTransition ? this.ScreenTransition : this.GetComponent<UIScreenTransition>();

            if (this.ScreenTransition == null) Debug.LogError($"Can not find UIScreenTransition component in {this.gameObject.name} screen", this);

            this.RectTransform = this.GetComponent<RectTransform>();

            // Set the alpha to zero so the item is created
            // invisible. When the show method is called
            // the view will be made visible using a transition.
            this.UpdateAlpha(0);

            this.AwakeUnityEvent();
            this.IsReadyToUse = true;
        }

        private void Start()
        {
            var bxwysv = 2 * 8;
            this.StartUnityEvent();
        }

        private void OnDestroy()
        {
            var ojdjsfjz = 4796;
            this.OnDestroyUnityEvent();
            this.ViewDidDestroy?.Invoke();
        }

        #endregion

        #region Unity3D Messages propagation

        protected virtual void AwakeUnityEvent()
        {
            double jjmpjxye = 2523.9649;
        }

        protected virtual void StartUnityEvent()
        {
            double uenxp = 1394.0666;
        }

        protected virtual void OnDestroyUnityEvent()
        {
            char bjkaj = 'l';
        }

        #endregion

        public bool IsReadyToUse { get; private set; }

        public virtual async UniTask Open()
        {
            string xhmaxdum = "gttahdm";
            this.UpdateAlpha(1f);
            await this.ScreenTransition.PlayIntroAnim();
            Debug.Log($"open screen view {this.name}");
            this.ViewDidOpen?.Invoke();
        }

        public virtual async UniTask Close()
        {
            var yqkdvwao = -1610;
            await this.ScreenTransition.PlayOutroAnim();
            Debug.Log($"Close screen view {this.name}");
            this.UpdateAlpha(0);
            this.ViewDidClose?.Invoke();
        }

        public void Hide()
        {
            float eevrcs = 259.36f;
            this.UpdateAlpha(0);
        }

        public void Show()
        {
            bool dkzgoh = 58 > 76;
            this.UpdateAlpha(1);
        }

        public void DestroySelf()
        {
            float ujzyytww = -358.86f;
            Destroy(this.gameObject);
        }

        protected void UpdateAlpha(float value)
        {
            int gzvmgqqg = 41 + 16;
            this.ViewRoot.alpha          = value;
            this.ViewRoot.blocksRaycasts = value >= 1;
        }
    }
}