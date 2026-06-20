using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Trauma surgery record for MS21.
    /// Stores surgical procedures performed for eye trauma.
    /// </summary>
    public class TraumaSurgery : EntityBase<Guid>
    {
        public Guid TraumaRecordId { get; set; }
        public DateTime? SurgeryDate { get; set; }
        public string? SurgeryType { get; set; }
        public string? SurgeryDescription { get; set; }
        public string? SurgeonName { get; set; }
        public string? AnesthesiaType { get; set; }
        public string? PostSurgeryCondition { get; set; }
        public string? Notes { get; set; }

        public virtual TraumaRecord TraumaRecord { get; set; } = null!;
    }
}
