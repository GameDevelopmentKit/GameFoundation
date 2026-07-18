namespace Localization.Interfaces
{
    using System.Collections.Generic;
    using Localization.Blueprint;

    public interface IBlueprintLocalizationData
    {
        public Dictionary<string, LocalizationDataModel> LocalizationDatas { get; set; }
    }
}