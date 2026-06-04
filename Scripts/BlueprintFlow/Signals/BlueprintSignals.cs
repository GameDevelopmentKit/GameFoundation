namespace BlueprintFlow.Signals
{
    public class LoadBlueprintDataSignal
    {
        public string Url;
        public string Hash;
    }
    
    public interface IProgressPercent
    {
        public float Percent { get; }
    }

    public class LoadBlueprintDataSucceedSignal
    {
    }

    public class LoadBlueprintDataProgressSignal : IProgressPercent
    {
        public float Percent { get; set; }
    }
    
    public class ReadBlueprintProgressSignal : IProgressPercent
    {
        public int MaxBlueprint;
        public int CurrentProgress;

        public float Percent => this.MaxBlueprint <= 0 ? 1f : 1f * this.CurrentProgress / this.MaxBlueprint;
    }
}