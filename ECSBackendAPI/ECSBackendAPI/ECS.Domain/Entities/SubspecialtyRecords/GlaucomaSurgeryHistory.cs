using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    public class GlaucomaSurgeryHistory : EntityBase<Guid>
    {
        public Guid GlaucomaRecordId { get; set; }
        public string Side { get; set; } = null!;
        public int AttemptNumber { get; set; }
        public string? ProcedureType { get; set; }
        public DateTime? ProcedureDate { get; set; }
        public string? FacilityLevel { get; set; }

        public virtual GlaucomaRecord GlaucomaRecord { get; set; } = null!;
    }

}
