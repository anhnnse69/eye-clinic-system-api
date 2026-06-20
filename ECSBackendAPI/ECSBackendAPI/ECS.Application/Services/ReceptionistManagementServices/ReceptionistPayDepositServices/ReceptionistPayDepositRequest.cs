namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistPayDepositServices
{
    /// <summary>
    /// Request criteria parameters packet tracking targeted appointment entity identification markers.
    /// </summary>
    public class ReceptionistPayDepositRequest
    {
        /// <summary>
        /// The unique structural reservation token reference pointing to the targeted appointment context.
        /// </summary>
        public Guid AppointmentId { get; set; }
    }
}
