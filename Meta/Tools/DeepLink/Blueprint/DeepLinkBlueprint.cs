namespace DeepLink.Blueprint
{
    using DataManager.Blueprint.BlueprintReader;

    [BlueprintReader("DeepLink")]
    public class DeepLinkBlueprint : GenericBlueprintReaderByRow<string, DeepLinkRecord>
    {
    }

    [CsvHeaderKey("DeepLinkId")]
    public class DeepLinkRecord
    {
        public string DeepLinkId { get; set; }
        public string ActionId   { get; set; }
        public string Value      { get; set; }
    }
}