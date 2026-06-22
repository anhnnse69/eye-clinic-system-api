namespace ECS.Application.Services.PatientAppointmentManagementServices.CancelAppointmentServices
{
    /// <summary>
    /// Response object representing the result of appointment cancellation.
    /// </summary>
    public class CancelAppointmentResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the cancelled appointment.
        /// </summary>
        public Guid AppointmentId { get; set; }

        /// <summary>
        /// Gets or sets the current status of the appointment after cancellation.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Gets or sets the date and time when the appointment was cancelled.
        /// </summary>
        public DateTime CancelledAt { get; set; }

        /// <summary>
        /// Gets or sets the cancellation reason provided by the patient.
        /// </summary>
        public string? CancellationReason { get; set; }

        /// <summary>
        /// Gets or sets a descriptive message about the cancellation operation.
        /// </summary>
        public string Message { get; set; } = null!;
    }
}
