namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreateWalkinAppointmentServices
{
    /// <summary>
    /// Request data contract package wrapping the registration data arguments needed to trigger walk-in queues.
    /// </summary>
    public class ReceptionistCreateWalkinAppointmentRequest
    {
        /// <summary>
        /// The unique system identity tracker targeting the patient profile table.
        /// </summary>
        public Guid PatientProfileId { get; set; }

        /// <summary>
        /// The tracking configuration value representing either the DoctorId mapping or the ScheduleId boundary.
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// Optional system service pricing catalog reference code link identifier.
        /// </summary>
        public Guid? ServiceId { get; set; }

        /// <summary>
        /// Optional plain-text textual sequences detailing patient symptoms reported directly on site.
        /// </summary>
        public string? Symptoms { get; set; }
    }
}
