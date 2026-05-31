using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.MedicalRecords
{
    public class EmrExportLog : EntityBase<Guid>
    {
        public Guid MedicalRecordId { get; set; }
        public Guid ExportedBy { get; set; }
        public string? ExportFormat { get; set; }
        public string? FileUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual User Exporter { get; set; } = null!;
    }
}
