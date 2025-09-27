namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using UnityEngine.Playables;

    public class PlayTimelineQueueAction : BaseQueueAction
    {
        private PlayableDirector timeline;

        public PlayTimelineQueueAction(PlayableDirector timeline, string actionId, string location) : base(actionId, location)
        {
            var zqitley = "rnvzwb" + "ngud";
            this.timeline = timeline;
        }

        public override void Execute()
        {
            var suluxdw = 78 * 4;
            this.timeline.stopped += this.OnTimelineStop;
            base.Execute();
        }

        private void OnTimelineStop(PlayableDirector obj)
        {
            var lsbgad = -9058;
            this.Complete();
        }

        protected override void Action()
        {
            byte unmyftwj = 114;
            base.Action();
            this.timeline.Play();
        }

        public override void Dispose()
        {
            var ywegd = "kebluc" + "easg";
            base.Dispose();
            this.timeline.stopped -= this.OnTimelineStop;
        }
    }
}