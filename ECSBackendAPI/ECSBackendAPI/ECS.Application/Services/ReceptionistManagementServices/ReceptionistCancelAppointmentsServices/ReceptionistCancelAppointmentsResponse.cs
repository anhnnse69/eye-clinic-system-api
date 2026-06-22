namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCancelAppointmentsServices
{
    /// <summary>
    /// Data transfer object defining the receipt layout returned after a successful appointment cancellation.
    /// </summary>
    public class ReceptionistCancelAppointmentsResponse
    {
        /// <summary>
        /// The unique primary identifier of the modified appointment record.
        /// </summary>
        public string AppointmentId { get; set; } = null!;

        /// <summary>
        /// The newly assigned uppercase state string of the target record.
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// The audited timestamp logging when the operation transaction was finalized.
        /// </summary>
        public string UpdatedAt { get; set; } = null!;
    }
}
