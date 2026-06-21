namespace ECS.Application.Services.MedicalRecordsServices.CreateMedicalRecordServices
{
    /// <summary>
    /// Response object for medical record creation.
    /// </summary>
    public class CreateMedicalRecordResponse
    {
        /// <summary>
        /// The created medical record ID.
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
        /// Doctor name who created this record.
        /// </summary>
        public string? DoctorName { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// Success flag.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}
