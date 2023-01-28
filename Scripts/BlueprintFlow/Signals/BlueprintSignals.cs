namespace BlueprintFlow.Signals
{
    public class LoadBlueprintDataSignal
    {
        public string Url;
        public string Hash;
    }

    public class LoadBlueprintDataSucceedSignal
    {
    }

    public class LoadBlueprintDataProgressSignal
    {
        public float Percent;
    }
    
    public class ReadBlueprintProgressSignal
    {
        public int MaxBlueprint;
        public int CurrentProgress;

        public float Percent => 1f * this.CurrentProgress / this.MaxBlueprint;
    }
}