namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using System.Collections.Generic;
    using GameFoundation.DI;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using GameFoundation.Scripts.Utilities.Extension;
    using R3;
    using UnityEngine.Scripting;

    public class GameQueueActionServices : IInitializable
    {
        private readonly IScreenManager screenManager;

        private readonly Dictionary<string, List<IGameQueueAction>> queueActions           = new();
        private readonly HashSet<string>                            trackUnCompleteActions = new();

        private bool   isDequeuing;
        private string curLocation;

        [Preserve]
        public GameQueueActionServices(IScreenManager screenManager)
        {
            this.screenManager = screenManager;
        }

        public void Initialize()
        {
            this.screenManager.CurrentActiveScreen.Subscribe(this.OnStartAtLocation);
        }

        private void OnStartAtLocation(IScreenPresenter currentScreen)
        {
            this.curLocation = currentScreen == null ? string.Empty : currentScreen.ScreenId;
            this.isDequeuing = false;
            if (!this.queueActions.TryGetValue(this.curLocation, out var listAction) || listAction.Count <= 0) return;
            this.isDequeuing = true;
            Observable.TimerFrame(1, UnityFrameProvider.PostLateUpdate).ObserveOnMainThread().Subscribe(l =>
            {
                this.Dequeue(listAction);
            });
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
                this.queueActions.Add(location, new() { action });
                isAdded = true;
            }

            if (isAdded)
            {
                this.trackUnCompleteActions.Add(action.actionId);

                if (location == this.curLocation && !this.isDequeuing) this.OnStartAtLocation(this.screenManager.CurrentActiveScreen.Value);
            }

            return isAdded;
        }

        public void Append(string location, IGameQueueAction action)
        {
            this.Insert(location, action);
        }

        public void Append(IGameQueueAction action)
        {
            this.Append(action.location, action);
        }

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
                if (this.Remove(action))
                {
                    if (index >= 0 && index < listAction.Count)
                        listAction.Insert(index, action);
                    else
                        listAction.Add(action);
                }
        }

        private void Dequeue(List<IGameQueueAction> listAction)
        {
            if (listAction.Count > 0)
                //Debug.Log($"<color=red> GameQueueActionServices: dequeue action, list action = {listAction.ToString2(action => action.actionId)}</color>");
                foreach (var gameQueueAction in listAction)
                {
                    if (gameQueueAction.isExecuting) break;
                    if (this.curLocation == gameQueueAction.location && this.CheckAllDependActionComplete(gameQueueAction))
                    {
                        //Debug.Log($"<color=red> GameQueueActionServices: dequeue action {gameQueueAction.actionId} at {curLocation}</color>");
                        gameQueueAction.OnComplete += action =>
                        {
                            this.trackUnCompleteActions.Remove(action.actionId);
                            listAction.Remove(action);
                            this.Dequeue(listAction);
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
            else
                //            Debug.Log($"<color=red> GameQueueActionServices: empty queue at {curLocation}</color>");
                this.isDequeuing = false;
        }

        private bool CheckAllDependActionComplete(IGameQueueAction action)
        {
            if (action.dependActions == null || action.dependActions.Length <= 0) return true;
            foreach (var dependAction in action.dependActions)
                if (this.trackUnCompleteActions.Contains(dependAction))
                    return false;

            return true;
        }
    }
}