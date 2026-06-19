namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ConfirmRejectAppointmentServices
{
    /// <summary>
    /// Response model returned after confirming or rejecting an appointment.
    /// </summary>
    public class ConfirmRejectAppointmentResponse
    {
        public Guid AppointmentId { get; set; }
        public string Status { get; set; } = null!;
        public string? NoteReason { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
