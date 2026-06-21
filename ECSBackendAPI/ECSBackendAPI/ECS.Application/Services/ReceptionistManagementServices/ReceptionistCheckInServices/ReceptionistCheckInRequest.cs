namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCheckInServices
{
    /// <summary>
    /// Request parameters packet tracking the specific identification reference key for arrival mapping.
    /// </summary>
    public class ReceptionistCheckInRequest
    {
        /// <summary>
        /// The unique structural context index identifier targeting the candidate appointment record.
        /// </summary>
        public Guid AppointmentId { get; set; }
    }
}
