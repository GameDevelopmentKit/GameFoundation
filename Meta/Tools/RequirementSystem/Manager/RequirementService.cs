namespace RequirementSystem.Manager
{
    using System.Collections.Generic;
    using System.Linq;
    using RequirementSystem.Blueprint;
    using RequirementSystem.RequirementValidator;
    using Zenject;

    public class RequirementService : IRequirementService, IInitializable
    {
        private readonly DiContainer                               diContainer;
        private          Dictionary<string, IRequirementValidator> requirementInstances;

        public RequirementService(DiContainer diContainer) { this.diContainer = diContainer; }

        public void Initialize() { this.requirementInstances = diContainer.ResolveAll<IRequirementValidator>().ToDictionary(instance => instance.Type); }
        public bool ValidateRequirements(List<RequirementsRecord> requirementsRecords)
        {
            return requirementsRecords == null || requirementsRecords.Count == 0 || requirementsRecords.All(this.ValidateRequirement);
        }
        public bool ValidateRequirement(RequirementsRecord requirementsRecord)
        {
            return this.GetRequirementValidator(requirementsRecord, out var requirementValidator) && requirementValidator.IsMet(requirementsRecord);
        }
        public List<RequirementInfo> GenerateRequirementInfos(List<RequirementsRecord> requirementsRecords)
        {
            return requirementsRecords.Select(record => this.GetRequirementValidator(record, out var requirementValidator) ? requirementValidator.GenerateRequirementInfo(record) : null).ToList();
        }
        
        public RequirementInfo GenerateRequirementInfo(RequirementsRecord requirementsRecord)
        {
            return this.GetRequirementValidator(requirementsRecord, out var requirementValidator) ? requirementValidator.GenerateRequirementInfo(requirementsRecord) : null;
        }
        private bool GetRequirementValidator(RequirementsRecord requirementsRecord, out IRequirementValidator requirementValidator)
        {
            return this.requirementInstances.TryGetValue(requirementsRecord.RequirementType, out requirementValidator);
        }
    }
}