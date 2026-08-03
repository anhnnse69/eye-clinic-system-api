namespace ECS.Application.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices
{
    /// <summary>
    /// Represents the comprehensive prescription details response for a patient appointment.
    /// </summary>
    public class GetPrescriptionDetailResponse
    {
        // Identifiers
        public string AppointmentId { get; set; } = string.Empty;
        public string? MedicalRecordId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;

        // Clinic Information
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicAddress { get; set; } = string.Empty;
        public string ClinicPhone { get; set; } = string.Empty;

        // Doctor Information
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorTitle { get; set; } = string.Empty;

        // Patient Information
        public string PatientName { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string PatientDob { get; set; } = string.Empty;
        public string PatientGender { get; set; } = string.Empty;
        public string PatientAddress { get; set; } = string.Empty;

        // Prescription Metadata
        public string PrescribedDate { get; set; } = string.Empty;
        public string DiagnosisMain { get; set; } = string.Empty;
        public string? DiagnosisComorbid { get; set; }
        public string? DoctorNotes { get; set; }
        public string? FollowUpDate { get; set; }
        public decimal? PrescriptionValue { get; set; }

        // Prescribed Medication List
        public List<PrescriptionItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Represents an individual medication entry in a prescription.
    /// </summary>
    public class PrescriptionItemDto
    {
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty; // e.g. 0.3%, 500mg
        public string Frequency { get; set; } = string.Empty; // e.g. Nhỏ 1-2 giọt x 4 lần/ngày
        public string DurationDays { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty; // e.g. Chai, Viên, Hộp
        public string Instruction { get; set; } = string.Empty;
    }
}
