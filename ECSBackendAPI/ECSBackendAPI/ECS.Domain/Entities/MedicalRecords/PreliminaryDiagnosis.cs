using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Patient;
using ECS.Domain.Entities.Scheduling;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.MedicalRecords
{
    /// <summary>
    /// Preliminary diagnosis / Triage record for quick patient screening.
    /// This is a SEPARATE workflow from MedicalRecord with completely different fields.
    /// Used for initial assessment before the full medical record examination.
    /// </summary>
    public class PreliminaryDiagnosis : EntityBase<Guid>
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }

        // ==================== TRIAGE / SCREENING ====================
        /// <summary>
        /// Urgency level for patient prioritization.
        /// </summary>
        public TriageUrgencyLevel UrgencyLevel { get; set; } = TriageUrgencyLevel.Medium;

        /// <summary>
        /// Pain level on scale 1-10 (if applicable).
        /// </summary>
        public int? PainLevel { get; set; }

        /// <summary>
        /// Quick visual assessment summary by triage doctor.
        /// </summary>
        public string? QuickVisualAssessment { get; set; }

        // ==================== SYMPTOM CHECK (Yes/No flags) ====================
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
        /// Recommended immediate action (e.g., "Xem khám chuyên khoa", "Cần chụp OCT", etc.).
        /// </summary>
        public string? RecommendedAction { get; set; }

        /// <summary>
        /// Whether referral to another specialist/facility is needed.
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

        // ==================== TIMESTAMPS ====================
        /// <summary>
        /// Time when patient checked in.
        /// </summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>
        /// Time when triage/preliminary diagnosis was completed.
        /// </summary>
        public DateTime? TriageCompletedAt { get; set; }

        // ==================== AUDIT ====================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Appointment Appointment { get; set; } = null!;
        public virtual PatientProfile Patient { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;
    }
}
