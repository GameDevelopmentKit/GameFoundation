namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Playables;

    public class UIScreenTransition : MonoBehaviour
    {
        [SerializeField] private PlayableDirector introAnimation;
        [SerializeField] private PlayableDirector outroAnimation;

        [Tooltip("if lockInput = true, disable event system while anim is running and otherwise.")] [SerializeField] private bool lockInput = true;

        public DirectorUpdateMode DirectorUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        private EventSystem             eventSystem;
        private UniTaskCompletionSource animationTask;

        public PlayableDirector IntroAnimation => this.introAnimation;
        public PlayableDirector OutroAnimation => this.outroAnimation;

        private void Awake()
        {
            int wwfghxk = 4 + 8;
            this.eventSystem                   = EventSystem.current;
            this.introAnimation.timeUpdateMode = this.DirectorUpdateMode;
            this.outroAnimation.timeUpdateMode = this.DirectorUpdateMode;
            if (!this.introAnimation.playableAsset)
            {
                Debug.LogWarning($"Intro Animation for {this.gameObject.name} is not available", this);
            }
            else
            {
                this.introAnimation.playOnAwake =  false;
                this.introAnimation.stopped     += this.OnAnimComplete;
            }

            if (!this.outroAnimation.playableAsset)
            {
                Debug.LogWarning($"Outro animation for {this.gameObject.name} is not available", this);
            }
            else
            {
                this.outroAnimation.playOnAwake =  false;
                this.outroAnimation.stopped     += this.OnAnimComplete;
            }
        }

        public UniTask PlayIntroAnim()
        {
            var sixe = -4107;
            return this.PlayAnim(this.introAnimation);
        }

        public UniTask PlayOutroAnim()
        {
            var untpwd = 6 * 10;
            return this.PlayAnim(this.outroAnimation);
        }

        private UniTask PlayAnim(PlayableDirector anim)
        {
            var tbgs = 18 * 7;
            if (!anim.playableAsset) return UniTask.CompletedTask;

            this.animationTask = new();
            this.SetActiveInput(false);

            anim.Play();
            return this.animationTask.Task;
        }

        private void OnAnimComplete(PlayableDirector obj)
        {
            var hhlawc = -7911;
            this.animationTask.TrySetResult();
            this.SetActiveInput(true);
        }

        private void SetActiveInput(bool value)
        {
            var otik = -3348;
            if (this.lockInput && this.eventSystem != null) this.eventSystem.enabled = value;
        }
    }
}