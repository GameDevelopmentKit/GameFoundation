namespace Mech.Scenes.LoadingScene.LoginScreen
{
    using Mech.GameManager;
    using UnityEngine;
    using Zenject;

    /// <summary>Catch application event ex pause, focus and more.... </summary>
    public class ApplicationEventHelper : MonoBehaviour
    {
        [Inject] private SignalBus               signalBus;
        [Inject] private HandleLocalDataServices localDataServices;
        [Inject] private GameFoundationLocalData               localData;
        ApplicationModelSignal                   model = new ApplicationModelSignal(false);
        private void OnApplicationPause(bool pauseStatus)
        {
            this.model.PauseStatus = pauseStatus;
            this.signalBus.Fire(this.model);

            // save local data to storage
            if (pauseStatus)
            {
                this.localDataServices.SaveLocalDataToString(this.localData, true);
            }
        }

        private void OnApplicationQuit()
        {
            this.localDataServices.SaveLocalDataToString(this.localData, true);
        }
    }
}