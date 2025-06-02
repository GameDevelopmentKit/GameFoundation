namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using GameFoundation.DI;
    using TheOne.Logging;
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
            var logger = this.GetCurrentContainer().Resolve<ILoggerManager>().GetLogger(this);
            this.eventSystem                   = EventSystem.current;
            this.introAnimation.timeUpdateMode = this.DirectorUpdateMode;
            this.outroAnimation.timeUpdateMode = this.DirectorUpdateMode;
            if (!this.introAnimation.playableAsset)
            {
                logger.Warning($"Intro Animation for {this.gameObject.name} is not available");
            }
            else
            {
                this.introAnimation.playOnAwake =  false;
                this.introAnimation.stopped     += this.OnAnimComplete;
            }

            if (!this.outroAnimation.playableAsset)
            {
                logger.Warning($"Outro animation for {this.gameObject.name} is not available");
            }
            else
            {
                this.outroAnimation.playOnAwake =  false;
                this.outroAnimation.stopped     += this.OnAnimComplete;
            }
        }

        public UniTask PlayIntroAnim()
        {
            return this.PlayAnim(this.introAnimation);
        }

        public UniTask PlayOutroAnim()
        {
            return this.PlayAnim(this.outroAnimation);
        }

        private UniTask PlayAnim(PlayableDirector anim)
        {
            if (!anim.playableAsset) return UniTask.CompletedTask;

            this.animationTask = new();
            this.SetActiveInput(false);

            anim.Play();
            return this.animationTask.Task;
        }

        private void OnAnimComplete(PlayableDirector obj)
        {
            this.animationTask.TrySetResult();
            this.SetActiveInput(true);
        }

        private void SetActiveInput(bool value)
        {
            if (this.lockInput && this.eventSystem != null) this.eventSystem.enabled = value;
        }
    }
}