namespace BlueprintFlow.Signals
{
    public interface IProgressPercent
    {
        public float Percent { get; }
    }

    public class LoadBlueprintDataProgressSignal : IProgressPercent
    {
        public float Percent { get; }

        public LoadBlueprintDataProgressSignal(float percent)
        {
            this.Percent = percent;
        }
    }

    public class ReadBlueprintProgressSignal : IProgressPercent
    {
        public int CurrentProgress { get; }
        public int MaxBlueprint    { get; }

        public float Percent => 1f * this.CurrentProgress / this.MaxBlueprint;

        public ReadBlueprintProgressSignal(int currentProgress, int maxBlueprint)
        {
            this.CurrentProgress = currentProgress;
            this.MaxBlueprint    = maxBlueprint;
        }
    }

    public class LoadBlueprintDataSucceedSignal
    {
    }
}