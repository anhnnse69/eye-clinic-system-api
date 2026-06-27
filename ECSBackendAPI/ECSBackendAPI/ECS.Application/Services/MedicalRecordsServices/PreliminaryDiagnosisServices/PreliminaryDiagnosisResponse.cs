namespace ECS.Application.Services.MedicalRecordsServices.PreliminaryDiagnosisServices
{
    /// <summary>
    /// Response object for preliminary diagnosis (Triage) creation.
    /// </summary>
    public class PreliminaryDiagnosisResponse
    {
        /// <summary>
        /// The created preliminary diagnosis/triage record ID.
        /// </summary>
        public string PreliminaryDiagnosisId { get; set; } = string.Empty;

        /// <summary>
        /// Patient full name.
        /// </summary>
        public string? PatientName { get; set; }

        /// <summary>
        /// Appointment date.
        /// </summary>
        public string? AppointmentDate { get; set; }

        /// <summary>
        /// Doctor name who performed triage.
        /// </summary>
        public string? DoctorName { get; set; }

        /// <summary>
        /// Triage completion timestamp.
        /// </summary>
        public string TriageCompletedAt { get; set; } = string.Empty;

        /// <summary>
        /// Urgency level assigned.
        /// </summary>
        public string UrgencyLevel { get; set; } = string.Empty;

        /// <summary>
        /// Recommended action.
        /// </summary>
        public string? RecommendedAction { get; set; }

        /// <summary>
        /// Success flag.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}
