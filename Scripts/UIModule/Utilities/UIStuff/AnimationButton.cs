namespace UIModule.Utilities.UIStuff
{
    using DG.Tweening;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button))]
    public class AnimationButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float maxSize  = 1.08f;
        [SerializeField] private float minSize  = 0.92f;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private bool  ignoreAnimate;

        private Vector3 initialScale;
        private Button  button;

        private void Awake()
        {
            this.initialScale = this.transform.localScale;
            this.button       = this.GetComponent<Button>();
        }

        private void OnEnable()
        {
            this.transform.localScale = this.initialScale;
        }

        private void OnDisable()
        {
            this.transform.localScale = this.initialScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            this.AnimatePressDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            this.AnimatePopup();
        }

        private void AnimatePressDown()
        {
            if (this.IgnoreAnimate()) return;
            this.SetScaleTween(this.minSize, this.duration);
        }

        private void AnimatePopup()
        {
            if (this.IgnoreAnimate()) return;
            DOTween.Sequence()
                .Append(this.SetScaleTween(this.maxSize, this.duration))
                .Append(this.SetScaleTween(this.minSize, this.duration))
                .Append(this.SetScaleTween(1, this.duration))
                .SetUpdate(true);
        }

        private bool IgnoreAnimate()
        {
            return this.ignoreAnimate || !this.button.interactable;
        }

        private Tween SetScaleTween(float endValue, float animDuration)
        {
            return this.transform.transform.DOScale(endValue, animDuration).SetEase(Ease.Linear).SetUpdate(true);
        }
    }
}