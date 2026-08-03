namespace ECS.Application.Services.PatientAppointmentManagementServices.GetPrescriptionDetailServices
{
    /// <summary>
    /// Represents the request payload to retrieve prescription details for an appointment.
    /// </summary>
    public class GetPrescriptionDetailRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the appointment.
        /// </summary>
        public Guid AppointmentId { get; set; }
    }
}
