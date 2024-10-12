namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using UnityEngine.Playables;

    public class PlayTimelineQueueAction : BaseQueueAction
    {
        private PlayableDirector timeline;

        public PlayTimelineQueueAction(PlayableDirector timeline, string actionId, string location) : base(actionId, location)
        {
            this.timeline = timeline;
        }

        public override void Execute()
        {
            this.timeline.stopped += this.OnTimelineStop;
            base.Execute();
        }

        private void OnTimelineStop(PlayableDirector obj)
        {
            this.Complete();
        }

        protected override void Action()
        {
            base.Action();
            this.timeline.Play();
        }

        public override void Dispose()
        {
            base.Dispose();
            this.timeline.stopped -= this.OnTimelineStop;
        }
    }
}