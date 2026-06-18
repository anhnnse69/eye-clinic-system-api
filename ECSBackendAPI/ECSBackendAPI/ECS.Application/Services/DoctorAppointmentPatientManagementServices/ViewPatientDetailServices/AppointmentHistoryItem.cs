namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDetailServices
{
    /// <summary>
    /// Represents an appointment history item
    /// in the patient's detail view.
    /// </summary>
    public class AppointmentHistoryItem
    {
        public Guid AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Symptoms { get; set; }
        public string? ServiceName { get; set; }
        public MedicalRecordSummary? MedicalRecord { get; set; }
    }
}
