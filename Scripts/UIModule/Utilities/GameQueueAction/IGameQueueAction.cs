namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using System;

    public interface IGameQueueAction : IDisposable
    {
        string                         actionId      { get; }
        string[]                       dependActions { get; }
        string                         location      { get; }
        object                         state         { get; }
        float                          delay         { get; }
        bool                           isExecuting   { get; }
        event Action<IGameQueueAction> OnExecute;
        event Action<IGameQueueAction> OnStart;
        event Action<IGameQueueAction> OnComplete;

        void Execute();
        void Complete();

        IGameQueueAction SetState(object                  state);
        IGameQueueAction SetDelay(float                   time);
        IGameQueueAction SetDependActions(params string[] dependActions);
    }
}