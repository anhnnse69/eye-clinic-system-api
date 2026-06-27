using ECS.Domain.Enums;

namespace ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices
{
    /// <summary>
    /// Request object for preliminary diagnosis (Triage/Screening).
    /// This is a SEPARATE workflow from medical record with completely different fields.
    /// Used for quick initial assessment and queue prioritization.
    /// </summary>
    public class PreliminaryDiagnosisRequest
    {
        /// <summary>
        /// The appointment ID for triage.
        /// </summary>
        public string AppointmentId { get; set; } = string.Empty;

        // ==================== TRIAGE / SCREENING ====================

        /// <summary>
        /// Urgency level for queue prioritization.
        /// </summary>
        public TriageUrgencyLevel UrgencyLevel { get; set; } = TriageUrgencyLevel.Medium;

        /// <summary>
        /// Pain level on scale 1-10 (if applicable).
        /// </summary>
        public int? PainLevel { get; set; }

        /// <summary>
        /// Quick visual assessment notes by triage doctor.
        /// </summary>
        public string? QuickVisualAssessment { get; set; }

        // ==================== SYMPTOM CHECK ====================

        /// <summary>
        /// Patient reports vision changes.
        /// </summary>
        public bool HasVisionChange { get; set; }

        /// <summary>
        /// Patient reports eye redness.
        /// </summary>
        public bool HasEyeRedness { get; set; }

        /// <summary>
        /// Patient reports eye discharge.
        /// </summary>
        public bool HasEyeDischarge { get; set; }

        /// <summary>
        /// Patient reports light sensitivity.
        /// </summary>
        public bool HasLightSensitivity { get; set; }

        /// <summary>
        /// Patient reports eye pain.
        /// </summary>
        public bool HasEyePain { get; set; }

        /// <summary>
        /// Patient reports headache.
        /// </summary>
        public bool HasHeadache { get; set; }

        /// <summary>
        /// Suspected foreign body in eye.
        /// </summary>
        public bool HasForeignBody { get; set; }

        // ==================== INITIAL ACTIONS ====================

        /// <summary>
        /// Recommended immediate action or examination module.
        /// </summary>
        public string? RecommendedAction { get; set; }

        /// <summary>
        /// Whether referral to another specialist is needed.
        /// </summary>
        public bool IsReferralNeeded { get; set; }

        /// <summary>
        /// Referral destination if needed.
        /// </summary>
        public string? ReferralTo { get; set; }

        /// <summary>
        /// Follow-up instructions given to patient.
        /// </summary>
        public string? FollowUpInstructions { get; set; }

        /// <summary>
        /// Patient check-in time.
        /// </summary>
        public DateTime? CheckInTime { get; set; }
    }
}
