using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.MedicalRecords
{
    /// <summary>
    /// Additional rarely-used fields for a medical record.
    /// Uses JSONB for flexible storage of optional fields.
    /// </summary>
    public class MedicalRecordExtras : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // Record summaries (JSONB)
        public string? TraumaSummary { get; set; }      // MS21
        public string? GlaucomaSummary { get; set; }   // MS24
        public string? PediatricSummary { get; set; }  // MS26

        // Lab & Imaging orders
        public string? LabOrders { get; set; }
        public string? ImagingOrders { get; set; }

        // Discharge
        public string? DischargeSummary { get; set; }
        public string? TreatmentProcess { get; set; }

        // Audit
        public Guid? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual User? UpdatedByUser { get; set; }
    }
}
