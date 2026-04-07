using System;

namespace RequirementSystem.Blueprint
{
    using DataManager.Blueprint.BlueprintReader;

    [CsvHeaderKey("RequirementType")]
    public class RequirementsRecord
    {
        public string RequirementId;
        public int    RequirementValue;
        public string RequirementType;
        public RequirementsRecord Clone()
        {
            return new RequirementsRecord()
            {
                RequirementId = this.RequirementId,
                RequirementValue = this.RequirementValue,
                RequirementType = this.RequirementType
            };
        }
    }
}