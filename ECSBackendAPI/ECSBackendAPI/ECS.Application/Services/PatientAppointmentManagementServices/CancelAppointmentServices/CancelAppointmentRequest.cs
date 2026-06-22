namespace ECS.Application.Services.PatientAppointmentManagementServices.CancelAppointmentServices
{
    /// <summary>
    /// Request object containing parameters for cancelling an appointment.
    /// </summary>
    public class CancelAppointmentRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the appointment to cancel.
        /// </summary>
        public Guid AppointmentId { get; set; }

        /// <summary>
        /// Gets or sets the reason for cancellation (optional).
        /// </summary>
        public string? Reason { get; set; }
    }
}