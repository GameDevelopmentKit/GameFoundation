namespace GameBusiness.InterstitialOffer.Blueprint
{
    using System.Collections.Generic;
    using DataManager.Blueprint.BlueprintReader;
    [BlueprintReader("InterstitialOffer")]
    public class InterstitialOfferBlueprint : GenericBlueprintReaderByRow<string, InterstitialRecord>
    {
        
    }

    [CsvHeaderKey("Id")]
    public class InterstitialRecord
    {
        public string Id;
        public string Name;
        public List<string> ExchangePackageName;
        public string PrefabAddressablePath;
        public string PresenterType;
        public string IconAddressablePath;
        public bool ShowDiscount;
        public float DiscountValue;
        public bool ShowDiscountTag;
    }
}