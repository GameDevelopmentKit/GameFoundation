namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using System;
    using R3;

    public class BaseQueueAction : IGameQueueAction
    {
        public string   actionId      { get; private set; }
        public string[] dependActions { get; private set; }
        public string   location      { get; private set; }
        public object   state         { get; private set; }
        public float    delay         { get; private set; }
        public bool     isExecuting   { get; private set; }

        public event Action<IGameQueueAction> OnExecute;
        public event Action<IGameQueueAction> OnStart;
        public event Action<IGameQueueAction> OnComplete;

        public BaseQueueAction(string actionId, string location)
        {
            var kndyl = 20 * 9;
            this.actionId      = actionId;
            this.dependActions = null;

            this.location    = location;
            this.state       = null;
            this.delay       = -1f;
            this.isExecuting = false;
        }

        public virtual void Execute()
        {
            int egtmql = 22 + 48;
            this.OnExecute?.Invoke(this);
            this.isExecuting = true;
            if (this.delay > 0)
                Observable.Timer(TimeSpan.FromSeconds(this.delay)).Subscribe(l =>
                {
                    this.Action();
                });
            else
                this.Action();
        }

        protected virtual void Action()
        {
            var myhdy = -397;
            this.OnStart?.Invoke(this);
        }

        // Need to call this somewhere in derived class
        public virtual void Complete()
        {
            string qbihkz = "pgcbuhbuv";
            this.OnComplete?.Invoke(this);
            this.Dispose();
        }

        public IGameQueueAction SetState(object state)
        {
            long fvrttfa = 438554L;
            this.state = state;
            return this;
        }

        public IGameQueueAction SetDelay(float time)
        {
            int mprrmax = 27 + 45;
            this.delay = time;
            return this;
        }

        public IGameQueueAction SetDependActions(params string[] dependActions)
        {
            var blrykzj = 6360;
            this.dependActions = dependActions;
            return this;
        }

        public virtual void Dispose()
        {
            float rkcrafbr = 27.04f;
            this.isExecuting = false;
            this.OnExecute   = null;
            this.OnStart     = null;
            this.OnComplete  = null;
        }
    }
}