namespace GameFoundation.Scripts.UIModule.ScreenFlow.Signals
{
    using System.Collections.Generic;

    public class StartLoadingNewSceneSignal
    {
        public List<string> CurrentScreenName { get; set; }
        public List<string> TargetScreenName  { get; set; }
        public string       ActiveScreenName  { get; set; }
    }

    public class FinishLoadingNewSceneSignal
    {
        public List<string> CurrentScreenName { get; set; }
        public List<string> TargetScreenName  { get; set; }
        public string       ActiveScreenName  { get; set; }
    }
}