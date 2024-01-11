namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using System.Collections.Generic;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.Utilities.Extension;
    using UniRx;
    using Zenject;

    public class GameQueueActionServices
    {
        public static GameQueueActionServices Instance { get; private set; }

        private          IScreenManager screenManager;
        private readonly SignalBus      signalBus;


        private Dictionary<string, List<IGameQueueAction>> queueActions           = new Dictionary<string, List<IGameQueueAction>>();
        private HashSet<string>                            trackUnCompleteActions = new HashSet<string>();
        private bool                                       isDequeuing;
        private string                                     curLocation;

        public GameQueueActionServices(IScreenManager screenManager, SignalBus signalBus)
        {
            this.screenManager = screenManager;
            this.signalBus     = signalBus;
            Instance           = this;
            this.screenManager.CurrentActiveScreen.Subscribe(this.OnStartAtLocation);
        }
        private void OnStartAtLocation(IScreenPresenter currentScreen)
        {
            this.curLocation = currentScreen == null ? string.Empty : currentScreen.ScreenId;
            this.TryDequeue(this.curLocation, true);
        }

        public bool Insert(string location, IGameQueueAction action, int index = -1)
        {
//        Debug.Log($"<color=red> GameQueueActionServices: add action {action.actionId} at {location}, index = {index} </color>");

            var isAdded = false;
            if (this.queueActions.TryGetValue(location, out var listAction))
            {
                var curIndex = listAction.FindIndex(queueAction => queueAction.actionId == action.actionId);
                if (curIndex >= 0)
                {
                    // replace the old one
                    if (index >= 0 && index != curIndex)
                    {
                        listAction.RemoveAt(curIndex);
                        listAction.TryInsert(action, index);
                    }
                    else
                    {
                        listAction[curIndex] = action;
                    }
                }
                else
                {
                    // add new
                    listAction.TryInsert(action, index);
                    isAdded = true;
                }
            }
            else
            {
                this.queueActions.Add(location, new List<IGameQueueAction> { action });
                isAdded = true;
            }

            if (isAdded)
            {
                this.trackUnCompleteActions.Add(action.actionId);

                if (this.CheckLocation(location) && !this.isDequeuing)
                {
                    this.TryDequeue(location);
                }
            }

            return isAdded;
        }

        public void Append(string location, IGameQueueAction action) { this.Insert(location, action); }

        public void Append(IGameQueueAction action) { this.Append(action.location, action); }

        public bool Remove(IGameQueueAction action)
        {
            if (this.queueActions.TryGetValue(action.location, out var listAction))
            {
                var curIndex = listAction.FindIndex(queueAction => queueAction.actionId == action.actionId);
                if (curIndex > 0)
                {
                    listAction.RemoveAt(curIndex);
                    return true;
                }
            }

            return false;
        }

        public void UpdateIndexInQueue(IGameQueueAction action, int index)
        {
            if (this.queueActions.TryGetValue(action.location, out var listAction))
            {
                if (this.Remove(action))
                {
                    if (index >= 0 && index < listAction.Count)
                    {
                        listAction.Insert(index, action);
                    }
                    else
                    {
                        listAction.Add(action);
                    }
                }
            }
        }
        
        private void TryDequeue(string location, bool isDelay = false)
        {
            this.isDequeuing = false;
            if (!this.queueActions.TryGetValue(location, out var listAction) || listAction.Count <= 0)
            {
                if (!this.queueActions.TryGetValue("", out listAction) || listAction.Count <= 0)
                    return;
            }

            this.isDequeuing = true;
            if (isDelay)
            {
                Observable.TimerFrame(1, FrameCountType.EndOfFrame).ObserveOnMainThread().Subscribe(l => { this.Dequeue(listAction); });
            }
            else
            {
                this.Dequeue(listAction);
            }
        }

        private void Dequeue(List<IGameQueueAction> listAction)
        {
            //Debug.Log($"<color=red> GameQueueActionServices: dequeue action, list action = {listAction.ToString2(action => action.actionId)}</color>");
            foreach (var gameQueueAction in listAction)
            {
                if (gameQueueAction.isExecuting) break;
                if (this.CheckLocation(gameQueueAction.location) && this.CheckAllDependActionComplete(gameQueueAction))
                {
                    //Debug.Log($"<color=red> GameQueueActionServices: dequeue action {gameQueueAction.actionId} at {curLocation}</color>");
                    gameQueueAction.OnComplete += action =>
                    {
                        this.trackUnCompleteActions.Remove(action.actionId);
                        listAction.Remove(action);
                        this.TryDequeue(this.curLocation);
                    };
                    gameQueueAction.OnStart += action =>
                    {
                        //Debug.Log($"<color=red> GameQueueActionServices: remove action {action.actionId}</color>");
                        listAction.Remove(action);
                    };
                    gameQueueAction.Execute();
                    break;
                }
            }
        }
        private bool CheckLocation(string location) { return this.curLocation == location || location == ""; }

        private bool CheckAllDependActionComplete(IGameQueueAction action)
        {
            if (action.dependActions == null || action.dependActions.Length <= 0) return true;
            foreach (var dependAction in action.dependActions)
            {
                if (this.trackUnCompleteActions.Contains(dependAction)) return false;
            }

            return true;
        }
    }
}