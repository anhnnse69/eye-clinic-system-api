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

        // ===== ADMINISTRATIVE =====
        /// <summary>
        /// Factor code for administrative classification.
        /// </summary>
        public string? MaYeuTo { get; set; }
        /// <summary>
        /// Patient's age at time of admission.
        /// </summary>
        public int? Age { get; set; }

        // ===== REQUIRED TESTS =====
        /// <summary>
        /// Required diagnostic tests.
        /// </summary>
        public string? RequiredTests { get; set; }

        // ===== SUMMARY =====
        /// <summary>
        /// Summary of examination findings.
        /// </summary>
        public string? Summary { get; set; }

        // Record summaries (JSONB)
        public string? TraumaSummary { get; set; }      // MS21
        public string? GlaucomaSummary { get; set; }   // MS24
        public string? PediatricSummary { get; set; }  // MS26

        // ===== TREATMENT PLANS =====
        /// <summary>
        /// Diet plan.
        /// </summary>
        public string? DietPlan { get; set; }
        /// <summary>
        /// Care plan.
        /// </summary>
        public string? CarePlan { get; set; }

        // ===== SUMMARY FIELDS =====
        /// <summary>
        /// Final clinical diagnosis.
        /// </summary>
        public string? FinalDiagnosisClinical { get; set; }
        /// <summary>
        /// Final diagnosis cause.
        /// </summary>
        public string? FinalDiagnosisCause { get; set; }

        // Surgery summary
        public string? SurgerySummary { get; set; }

        // Lab & Imaging orders
        public string? LabOrders { get; set; }
        public string? ImagingOrders { get; set; }

        // ===== DISCHARGE =====
        public string? DischargeSummary { get; set; }
        public string? TreatmentProcess { get; set; }

        // Discharge Visual Acuity (VA) and Intraocular Pressure (IOP)
        public string? DischargeVaOd { get; set; }
        public string? DischargeVaOs { get; set; }
        public string? DischargeIopOd { get; set; }
        public string? DischargeIopOs { get; set; }

        // ===== FOLLOW-UP =====
        /// <summary>
        /// Follow-up plan.
        /// </summary>
        public string? FollowUpPlan { get; set; }

        // Audit
        public Guid? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual User? UpdatedByUser { get; set; }
    }
}
