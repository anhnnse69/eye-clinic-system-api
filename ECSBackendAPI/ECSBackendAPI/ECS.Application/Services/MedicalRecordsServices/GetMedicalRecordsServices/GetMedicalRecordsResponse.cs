namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices
{
    /// <summary>
    /// Response object for a single medical record in the list.
    /// </summary>
    public class GetMedicalRecordsResponse
    {
        /// <summary>
        /// Medical record ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Appointment ID
        /// </summary>
        public Guid AppointmentId { get; set; }

        /// <summary>
        /// Patient ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// Patient full name
        /// </summary>
        public string PatientFullName { get; set; } = string.Empty;

        /// <summary>
        /// Patient date of birth
        /// </summary>
        public string? PatientDob { get; set; }

        /// <summary>
        /// Patient phone number
        /// </summary>
        public string? PatientPhone { get; set; }

        /// <summary>
        /// Doctor ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// Doctor full name
        /// </summary>
        public string DoctorFullName { get; set; } = string.Empty;

        /// <summary>
        /// Appointment date/time
        /// </summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>
        /// Record type (e.g., MS22_ANTERIOR)
        /// </summary>
        public string RecordType { get; set; } = string.Empty;

        /// <summary>
        /// Chief complaint
        /// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// Main diagnosis
        /// </summary>
        public string? DiagnosisMain { get; set; }

        /// <summary>
        /// Treatment plan
        /// </summary>
        public string? TreatmentPlan { get; set; }

        /// <summary>
        /// Whether the record is locked
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Record creation date
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update date
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Indicates if the current doctor can EDIT this record
        /// True if: appointment is today AND current doctor is the treating doctor AND record is not locked
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// Indicates if the current doctor can only VIEW this record
        /// True if: appointment is in the past OR current doctor is not the treating doctor OR record is locked
        /// </summary>
        public bool CanViewOnly { get; set; }

        /// <summary>
        /// Reason why editing is not allowed (for UI display)
        /// </summary>
        public string? EditRestrictionReason { get; set; }
    }
}
