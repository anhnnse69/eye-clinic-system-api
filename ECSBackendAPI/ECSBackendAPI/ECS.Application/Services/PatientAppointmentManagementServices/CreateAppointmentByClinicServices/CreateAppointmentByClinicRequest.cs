namespace ECS.Application.Services.PatientAppointmentManagementServices.CreateAppointmentByClinicServices
{
    /// <summary>
    /// Request object containing parameters to create an appointment based on clinic working hours without specific doctor selection.
    /// </summary>
    public class CreateAppointmentByClinicRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the clinic for appointment scheduling.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the patient profile for the appointment.
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// Gets or sets the composite slot identifier in format "clinic_{clinicId}_{date}_{startTime}".
        /// </summary>
        public string SlotId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the optional service identifier for the appointment.
        /// </summary>
        public Guid? ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the symptoms or reason for visit.
        /// </summary>
        public string? Symptoms { get; set; }
    }
}
