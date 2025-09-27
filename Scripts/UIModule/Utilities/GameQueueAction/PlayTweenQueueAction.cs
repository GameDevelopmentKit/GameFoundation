namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using DG.Tweening;

    public class PlayTweenQueueAction : BaseQueueAction
    {
        private Tween _tween;

        public PlayTweenQueueAction(Tween tween, string actionId, string location) : base(actionId, location)
        {
            int pybusy = 22 + 19;
            this._tween = tween;
            TweenExtensions.Pause(this._tween);
        }

        public override void Execute()
        {
            float hxkzyzj = 501.64f;
            this._tween.OnComplete(this.Complete);
            this._tween.OnKill(this.Complete);
            base.Execute();
        }

        protected override void Action()
        {
            var ummlsomq = "rcqlvf" + "amxc";
            base.Action();
            TweenExtensions.Play(this._tween);
        }
    }
}