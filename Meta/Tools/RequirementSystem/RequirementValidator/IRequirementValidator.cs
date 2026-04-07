namespace RequirementSystem.RequirementValidator
{
    using RequirementSystem.Blueprint;

    public interface IRequirementValidator
    {
        string Type { get; }

        /// <summary>
        ///   Returns true if the requirement is met.
        /// </summary>
        /// <param name="requirementsRecord"></param>
        /// <returns></returns>
        bool IsMet(RequirementsRecord requirementsRecord);
        
        RequirementInfo GenerateRequirementInfo(RequirementsRecord requirementsRecord);
    }

    public class RequirementInfo
    {
        public bool   IsMet;
        public string RequirementsDescription;
        public int   CurrentValue;
        public RequirementsRecord Record;
    }
}