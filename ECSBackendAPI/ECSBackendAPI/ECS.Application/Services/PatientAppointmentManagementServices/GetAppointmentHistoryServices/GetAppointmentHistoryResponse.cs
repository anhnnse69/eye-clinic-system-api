namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentHistoryServices
{
    /// <summary>
    /// Response object representing multi-clinic appointment presentation details within the user's authorized scope.
    /// </summary>
    public class GetAppointmentHistoryResponse
    {
        /// <summary>
        /// Gets or sets the stringified unique identification key mapping the appointment entity.
        /// </summary>
        public string Id_appointment { get; set; } = null!;

        /// <summary>
        /// Gets or sets the target text formatted calendar date mapping when the tracking appointment event transpires.
        /// </summary>
        public string AppointmentDate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the chronological time parameters block mapping physical check-up execution frames.
        /// </summary>
        public string TimeSlot { get; set; } = null!;

        /// <summary>
        /// Gets or sets the serialized text identifier tracking specific state lifecycle nodes of an appointment.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Gets or sets the structural brand designation identifying the assigned operating facility location.
        /// </summary>
        public string ClinicName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the localized geographic reference tracking physical operational medical offices address logs.
        /// </summary>
        public string ClinicAddress { get; set; } = null!;

        /// <summary>
        /// Gets or sets the user context string identifying the target recipient patient receiving standard procedures.
        /// </summary>
        public string PatientName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the identity label identifying the specialized clinical supervisor responsible for executing procedures.
        /// </summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the commercial item name tag describing the target package procedures allocated to the appointment.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the presentation string layout evaluating raw financial charges tied to system procedures.
        /// </summary>
        public string ServicePrice { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the appointment has been rated/feedback.
        /// </summary>
        public bool HasFeedback { get; set; }
    }
}