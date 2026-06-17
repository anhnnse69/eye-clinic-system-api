namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices
{
    /// <summary>
    /// Represents a patient appointment item
    /// in the doctor's patient list.
    /// </summary>
    public class PatientAppointmentItem
    {
        public Guid AppointmentId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string? PatientAvatarUrl { get; set; }
        public string? PatientPhone { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; } = default!;
    }
}
