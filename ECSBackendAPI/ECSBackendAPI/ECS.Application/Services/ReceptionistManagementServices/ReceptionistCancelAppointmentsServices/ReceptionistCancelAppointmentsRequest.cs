namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCancelAppointmentsServices
{
    /// <summary>
    /// Request object containing identity and justification details for processing an appointment cancellation.
    /// </summary>
    public class ReceptionistCancelAppointmentsRequest
    {
        /// <summary>
        /// The unique primary identifier of the target appointment record.
        /// </summary>
        public Guid AppointmentId { get; set; }

        /// <summary>
        /// The mandatory verification explanation tracking why the booking assignment is being revoked.
        /// </summary>
        public string NoteReason { get; set; } = null!;
    }
}
