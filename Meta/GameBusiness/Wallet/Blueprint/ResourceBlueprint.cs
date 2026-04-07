namespace GameBusiness.Wallet.Blueprint
{
    using System.Collections.Generic;
    using DataManager.Blueprint.BlueprintReader;

    [BlueprintReader("Resource")]
    public class ResourceBlueprint : GenericBlueprintReaderByRow<string, ResourceRecord>
    {
    }

    [CsvHeaderKey("Id")]
    public class ResourceRecord
    {
        public readonly string       Id;
        public readonly string       Name;
        public readonly string       Icon;
        public readonly string       Description;
        public readonly string       ShortDescription;
        public readonly bool         IsDefault;
        public readonly bool         HasCap;
        public readonly int          DefaultValue;
        public readonly int          DefaultCapValue;
        public          List<string> Categories;
    }
}