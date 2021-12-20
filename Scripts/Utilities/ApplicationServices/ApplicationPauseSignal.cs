namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    /// <summary>Model signal application event pause, focus...</summary>
    public class ApplicationPauseSignal
    {
        public bool PauseStatus;

        public ApplicationPauseSignal(bool pauseStatus)
        {
            this.PauseStatus = pauseStatus;
        }
    }
}