namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using DG.Tweening;

    public class PlayTweenQueueAction : BaseQueueAction
    {
        private Tween _tween;

        public PlayTweenQueueAction(Tween tween, string actionId, string location) : base(actionId, location)
        {
            this._tween = tween;
            TweenExtensions.Pause(this._tween);
        }

        public override void Execute()
        {
            this._tween.OnComplete(this.Complete);
            this._tween.OnKill(this.Complete);
            base.Execute();
        }

        protected override void Action()
        {
            base.Action();
            TweenExtensions.Play(this._tween);
        }
    }
}