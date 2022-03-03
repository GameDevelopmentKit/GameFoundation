namespace GameFoundation.Scripts.ScreenFlow.BaseScreen.View
{
    using System;
    using GameFoundation.Scripts.Utilities.UIStuff;
    using UnityEngine;

    [RequireComponent(typeof(CanvasGroup))]
    public class BaseView : MonoBehaviour, IScreenView
    {
        [SerializeField] private CanvasGroup        viewRoot;
        [SerializeField] private UIScreenTransition screenTransition;
        public event Action                         ViewDidClose;
        public event Action                         ViewDidOpen;
        public event Action                         ViewDidDestroy;

        protected virtual CanvasGroup   ViewRoot      { get => this.viewRoot; set => this.viewRoot = value; }
        public            RectTransform RectTransform { get;                  private set; }


        #region Unity3D Event

        private void Awake()
        {
            // This will allow to set the view in the inspector if we want to
            if (!this.ViewRoot) this.ViewRoot                 = this.GetComponent<CanvasGroup>();
            
            this.screenTransition ??= this.GetComponent<UIScreenTransition>();

            if (this.screenTransition == null)
            {
                Debug.LogError($"Can not find UIScreenTransition component in {this.gameObject.name} screen", this);
            }

            this.RectTransform = this.GetComponent<RectTransform>();

            // Set the alpha to zero so the item is created
            // invisible. When the show method is called
            // the view will be made visible using a transition.
            this.UpdateAlpha(0);

            this.AwakeUnityEvent();
            this.IsReadyToUse = true;
        }

        private void Start() { this.StartUnityEvent(); }

        private void OnDestroy()
        {
            this.OnDestroyUnityEvent();
            this.ViewDidDestroy?.Invoke();
        }

        #endregion

        #region Unity3D Messages propagation

        protected virtual void AwakeUnityEvent() { }

        protected virtual void StartUnityEvent() { }

        protected virtual void OnDestroyUnityEvent() { }

        #endregion

        public bool IsReadyToUse { get; private set; }
        public virtual async void Open()
        {
            this.UpdateAlpha(1f);
            await this.screenTransition.PlayIntroAnim();
            this.ViewDidOpen?.Invoke();
        }

        public virtual async void Close()
        {
            this.UpdateAlpha(0);
            await this.screenTransition.PlayOutroAnim();
            this.ViewDidClose?.Invoke();
        }
        public void Hide() { this.UpdateAlpha(0); }

        public void DestroySelf() { Destroy(this.gameObject); }

        protected void UpdateAlpha(float value)
        {
            this.ViewRoot.alpha          = value;
            this.ViewRoot.blocksRaycasts = value >= 1;
        }
    }
}