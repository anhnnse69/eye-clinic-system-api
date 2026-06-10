using ECS.Domain.Entities.General;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Glaucoma surgery/drug history.
    /// </summary>
    public class GlaucomaHistory : EntityBase<Guid>
    {
        public Guid GlaucomaRecordId { get; set; }

        public string HistoryType { get; set; } = null!; // "SURGERY" or "DRUG"
        public EyeSide? Side { get; set; }
        public int? AttemptNumber { get; set; }

        // For SURGERY
        public string? ProcedureType { get; set; }
        public DateTime? ProcedureDate { get; set; }
        public string? FacilityLevel { get; set; }

        // For DRUG
        public string? DrugName { get; set; }
        public string? Dosage { get; set; }
        public string? Duration { get; set; }
        public string? Route { get; set; }
        public string? ChangeReason { get; set; }

        public virtual GlaucomaRecord GlaucomaRecord { get; set; } = null!;
    }
}
