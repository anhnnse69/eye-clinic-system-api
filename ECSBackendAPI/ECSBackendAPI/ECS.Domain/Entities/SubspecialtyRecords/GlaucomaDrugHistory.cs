using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    public class GlaucomaDrugHistory : EntityBase<Guid>
    {
        public Guid GlaucomaRecordId { get; set; }
        public string? Side { get; set; }
        public string DrugName { get; set; } = null!;
        public string? Dosage { get; set; }
        public string? Duration { get; set; }
        public string? Route { get; set; }
        public string? DrugCount { get; set; }
        public string? ChangeReason { get; set; }

        public virtual GlaucomaRecord GlaucomaRecord { get; set; } = null!;
    }
}
