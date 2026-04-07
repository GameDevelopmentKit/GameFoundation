namespace RequirementSystem.Manager
{
    using System.Collections.Generic;
    using RequirementSystem.Blueprint;
    using RequirementSystem.RequirementValidator;

    public interface IRequirementService
    {
        /// <summary>
        ///  Validates the specified <see cref="RequirementsRecord"/> and returns true if all requirements are met.
        /// </summary>
        /// <param name="requirementsRecords"></param>
        /// <returns></returns>
        bool ValidateRequirements(List<RequirementsRecord> requirementsRecords);

        
        /// <summary>
        ///  Validates the specified <see cref="RequirementsRecord"/> and returns true if the requirement is met.
        /// </summary>
        /// <param name="requirementsRecord"></param>
        /// <returns></returns>
        bool ValidateRequirement(RequirementsRecord requirementsRecord);
        
        /// <summary>
        ///  Generates a <see cref="RequirementInfo"/> for each <see cref="RequirementsRecord"/>.
        /// </summary>
        /// <param name="requirementsRecords"></param>
        /// <returns></returns>
        List<RequirementInfo> GenerateRequirementInfos(List<RequirementsRecord> requirementsRecords);
        
        RequirementInfo GenerateRequirementInfo(RequirementsRecord requirementsRecord);
    }
}