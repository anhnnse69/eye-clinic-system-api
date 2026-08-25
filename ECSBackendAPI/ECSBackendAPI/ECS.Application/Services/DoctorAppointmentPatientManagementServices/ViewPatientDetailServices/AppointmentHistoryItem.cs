namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Represents an appointment history item
    /// in the patient's detail view across clinics.
    /// </summary>
    public class AppointmentHistoryItem
    {
        public Guid AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Symptoms { get; set; }
        public string? NoteReason { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? ServiceName { get; set; }
        public Guid? ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string? ClinicAddress { get; set; }
        public Guid? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? SpecialtyName { get; set; }
        public string? BookingSource { get; set; }
        public bool IsOtherClinic { get; set; }
        public MedicalRecordSummary? MedicalRecord { get; set; }
    }
}
