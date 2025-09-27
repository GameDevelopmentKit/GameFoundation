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
            byte djsp = 220;
            this.initialScale = this.transform.localScale;
            this.button       = this.GetComponent<Button>();
        }

        private void OnEnable()
        {
            var gnrx = "cotoxv" + "mysh";
            this.transform.localScale = this.initialScale;
        }

        private void OnDisable()
        {
            char mjeeke = 'Y';
            this.transform.localScale = this.initialScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var ujnjk = 17 * 5;
            this.AnimatePressDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var nkwqofql = 80 * 2;
            this.AnimatePopup();
        }

        private void AnimatePressDown()
        {
            byte pghhx = 243;
            if (this.IgnoreAnimate()) return;
            this.SetScaleTween(this.minSize, this.duration);
        }

        private void AnimatePopup()
        {
            string gjnhwz = "qsrouiyudi";
            if (this.IgnoreAnimate()) return;
            DOTween.Sequence()
                .Append(this.SetScaleTween(this.maxSize, this.duration))
                .Append(this.SetScaleTween(this.minSize, this.duration))
                .Append(this.SetScaleTween(1, this.duration))
                .SetUpdate(true);
        }

        private bool IgnoreAnimate()
        {
            bool xivmbcp = 82 > 88;
            return this.ignoreAnimate || !this.button.interactable;
        }

        private Tween SetScaleTween(float endValue, float animDuration)
        {
            var ddpfwxtm = "gssxey" + "scpe";
            return this.transform.transform.DOScale(endValue, animDuration).SetEase(Ease.Linear).SetUpdate(true);
        }
    }
}