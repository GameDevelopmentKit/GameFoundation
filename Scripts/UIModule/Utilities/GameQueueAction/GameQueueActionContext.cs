namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using System;
    using DG.Tweening;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using UnityEngine.Playables;

    public class GameQueueActionContext
    {
        private readonly GameQueueActionServices gameQueueActionServices;
        private readonly IScreenManager          screenManager;
        public GameQueueActionContext(GameQueueActionServices gameQueueActionServices, IScreenManager screenManager)
        {
            this.gameQueueActionServices = gameQueueActionServices;
            this.screenManager           = screenManager;
        }

        /// <summary>
        /// Need to set autoHandleComplete = false if handle completion manually.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionId"></param>
        /// <param name="location"></param>
        /// <param name="autoHandleComplete"></param>
        /// <returns></returns>
        public IGameQueueAction AddCommonActionToQueueAction(Action<IGameQueueAction> action, string actionId, string location = "", bool autoHandleComplete = true)
        {
            var baseAction = new BaseQueueAction(actionId, location);
            baseAction.OnStart += queueAction =>
            {
                action?.Invoke(baseAction);
                if (autoHandleComplete)
                {
                    baseAction.Complete();
                }
            };

            this.gameQueueActionServices.Append(baseAction);
            return baseAction;
        }

        public IGameQueueAction AddScreenToQueueAction<T>(string actionId = "", string location = "") where T : IScreenPresenter
        {
            var action = new ShowPopupQueueAction<T>(this.screenManager, string.IsNullOrEmpty(actionId) ? $"ShowScreen_{typeof(T).Name}" : actionId, location);
            this.gameQueueActionServices.Append(action);
            return action;
        }

        public IGameQueueAction AddScreenToQueueAction<TPresenter, TModel>(TModel model, string actionId = "", string location = "") where TPresenter : IScreenPresenter<TModel>
        {
            var action = new ShowPopupQueueAction<TPresenter, TModel>(this.screenManager, string.IsNullOrEmpty(actionId) ? $"ShowScreen_{typeof(TPresenter).Name}" : actionId, location);
            action.SetState(model);
            this.gameQueueActionServices.Append(action);
            return action;
        }

        public string GetCurrentLocation() { return this.screenManager.CurrentActiveScreen.Value.ScreenId; }

        public IGameQueueAction AddTimelineToQueueAction<T>(T timeline, string actionId, string location = "") where T : PlayableDirector
        {
            var action = new PlayTimelineQueueAction(timeline, actionId, location);
            this.gameQueueActionServices.Append(action);
            return action;
        }

        public IGameQueueAction AddTweenToQueueAction<T>(T tween, string actionId, string location = "") where T : Tween
        {
            var action = new PlayTweenQueueAction(tween, actionId, location);
            this.gameQueueActionServices.Append(action);
            return action;
        }

        public IGameQueueAction SetIndex(IGameQueueAction action, int priority)
        {
            this.gameQueueActionServices.UpdateIndexInQueue(action, priority);
            return action;
        }
    }
}