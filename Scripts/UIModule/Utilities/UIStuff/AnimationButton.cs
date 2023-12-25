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

        private void OnEnable()  { this.transform.localScale = Vector3.one; }
        private void OnDisable() { this.transform.localScale = Vector3.one; }

        public void OnPointerDown(PointerEventData eventData) { this.AnimatePressDown(); }

        public void OnPointerUp(PointerEventData eventData) { this.AnimatePopup(); }
        
        private void AnimatePressDown() { this.SetScaleTween(this.minSize, this.duration); }
        
        private void AnimatePopup()
        {
            DOTween.Sequence()
                   .Append(this.SetScaleTween(this.maxSize, this.duration))
                   .Append(this.SetScaleTween(this.minSize, this.duration))
                   .Append(this.SetScaleTween(1,this.duration))
                   .SetUpdate(true);
        }

        private Tween SetScaleTween(float endValue, float animDuration) => this.transform.transform.DOScale(endValue, animDuration).SetEase(Ease.Linear).SetUpdate(true); 
    }
}