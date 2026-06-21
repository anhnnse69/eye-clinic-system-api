using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Trauma record - MS21 (Chấn thương mắt).
    /// Contains trauma-specific information for eye injuries.
    /// </summary>
    public class TraumaRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        // ===== INJURY HISTORY =====
        public string? InjuryCause { get; set; }
        public DateTime? InjuryTime { get; set; }
        public string? PriorTreatment { get; set; }
        public string? PostTreatmentCourse { get; set; }

        // ===== INJURY SUMMARY (JSONB for flexibility) =====
        public string? OdInjuries { get; set; } // Eye trauma summary - Right
        public string? OsInjuries { get; set; } // Eye trauma summary - Left

        // ===== INJURY DETAILS =====
        public string? InjuryDetails { get; set; }
        public string? TraumaConclusion { get; set; }

        // ===== DISCHARGE SUMMARY & TREATMENT =====
        public string? DiagnosisClinical { get; set; }
        public string? DiagnosisCause { get; set; }
        public string? TreatmentProcess { get; set; }
        public string? TreatmentPlan { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual ICollection<TraumaSurgery>? Surgeries { get; set; }
    }
}
