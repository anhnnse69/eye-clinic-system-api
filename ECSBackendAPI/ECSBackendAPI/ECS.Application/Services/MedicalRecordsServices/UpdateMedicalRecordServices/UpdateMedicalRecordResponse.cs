namespace ECS.Application.Services.MedicalRecordsServices.UpdateMedicalRecordServices
{
    /// <summary>
    /// Response object for medical record update.
    /// </summary>
    public class UpdateMedicalRecordResponse
    {
        /// <summary>
        /// The updated medical record ID.
        /// </summary>
        public string MedicalRecordId { get; set; } = string.Empty;

        /// <summary>
        /// Patient full name.
        /// </summary>
        public string? PatientName { get; set; }

        /// <summary>
        /// Record type label.
        /// </summary>
        public string? RecordTypeLabel { get; set; }

        /// <summary>
        /// Appointment date.
        /// </summary>
        public string? AppointmentDate { get; set; }

        /// <summary>
        /// Doctor name who updated this record.
        /// </summary>
        public string? DoctorName { get; set; }

        /// <summary>
        /// Update timestamp.
        /// </summary>
        public string UpdatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Success flag.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}
