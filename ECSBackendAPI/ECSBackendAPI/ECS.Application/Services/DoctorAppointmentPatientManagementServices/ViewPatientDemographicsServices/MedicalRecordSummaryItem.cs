namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// Summary item for a medical record in the list.
    /// </summary>
    public class MedicalRecordSummaryItem
    {
        /// <summary>
        /// Gets or sets the stringified unique identification key for the medical record.
        /// </summary>
        public string Id_MedicalRecord { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the record type code (e.g., MS21_TRAUMA, MS22_ANTERIOR).
        /// </summary>
        public string RecordType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display label for the record type.
        /// </summary>
        public string RecordTypeLabel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the doctor full name who created this record.
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the appointment date.
        /// </summary>
        public string AppointmentDate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chief complaint.
        /// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// Gets or sets the main diagnosis.
        /// </summary>
        public string? DiagnosisMain { get; set; }

        /// <summary>
        /// Gets or sets whether the medical record is locked.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp of the medical record.
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;
    }
}
