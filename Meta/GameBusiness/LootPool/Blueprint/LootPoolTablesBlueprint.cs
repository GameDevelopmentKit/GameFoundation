namespace GameBusiness.LootPool.Blueprint
{
    using DataManager.Blueprint.BlueprintReader;

    [BlueprintReader("LootPoolTables")]
    public class LootPoolTablesBlueprint : GenericBlueprintReaderByRow<string, DropPoolTables>
    {
    }

    [CsvHeaderKey("DropPoolId")]
    public class DropPoolTables
    {
        public string DropPoolId;
        public int MinDropCount;
        public int MaxDropCount;

        public BlueprintByRow<DropTableElement> DropPoolItems;
    }

    [CsvHeaderKey("ElementId")]
    public class DropTableElement
    {
        public string ElementId;
        public string AssetType;
        public bool IsInteger;
        public float MaxValue;
        public float MinValue;
        public float Weight;
    }
}
