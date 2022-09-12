namespace GameFoundation.Scripts.BlueprintFlow.Signals
{
    public class LoadBlueprintDataSignal
    {
        public string Url;
        public string Hash;
    }

    public class LoadBlueprintDataSuccessedSignal
    {
        
    }

    public class LoadBlueprintDataProgressSignal
    {
        public float percent;
    }
    
    public class ReadBlueprintProgressSignal
    {
        public int MaxBlueprint;
        public int CurrentProgress;
    }
}