namespace MechSharingCode.Blueprints.Signals
{
    using MechSharingCode.WebService.Blueprint;

    public class LoadBlueprintDataSignal
    {
        public BlueprintResponse BlueprintInfo;
    }

    public class LoadBlueprintDataSuccessedSignal
    {
        
    }

    public class LoadBlueprintDataProgressSignal
    {
        public float percent;
    }
}