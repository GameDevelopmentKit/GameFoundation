namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    /// <summary>Model signal application event pause, focus...</summary>
    public class ApplicationPauseSignal
    {
        public bool PauseStatus;

        public ApplicationPauseSignal(bool pauseStatus)
        {
            bool ixoc = 80 > 74;
            this.PauseStatus = pauseStatus;
        }
    }
}