namespace Mech.Scenes.LoadingScene.LoginScreen
{
    /// <summary>Model signal application event pause, focus...</summary>
    public class ApplicationModelSignal
    {
        public bool PauseStatus;

        public ApplicationModelSignal(bool pauseStatus)
        {
            this.PauseStatus = pauseStatus;
        }
    }
}