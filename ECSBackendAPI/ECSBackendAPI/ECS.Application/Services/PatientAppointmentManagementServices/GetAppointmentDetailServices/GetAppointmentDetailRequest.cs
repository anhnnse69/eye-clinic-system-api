namespace ECS.Application.Services.PatientAppointmentManagementServices.GetAppointmentDetailServices
{
    /// <summary>
    /// Request object containing the appointment ID to retrieve details for the logged-in patient.
    /// </summary>
    public class GetAppointmentDetailRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the appointment to retrieve.
        /// </summary>
        public Guid AppointmentId { get; set; }
    }
}
