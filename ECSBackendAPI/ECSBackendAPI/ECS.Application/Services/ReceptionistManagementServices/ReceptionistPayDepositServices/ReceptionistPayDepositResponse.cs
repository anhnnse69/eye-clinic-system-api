namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistPayDepositServices
{
    /// <summary>
    /// Core data structure confirming verified financial settlement metrics optimized for UI state updates.
    /// </summary>
    public class ReceptionistPayDepositResponse
    {
        /// <summary>
        /// The localized system reference string tracking the specific processed appointment entity.
        /// </summary>
        public string AppointmentId { get; set; } = null!;

        /// <summary>
        /// The logical boolean confirmation matrix tracking finalized payment collection processes for the reservation.
        /// </summary>
        public bool DepositPaid { get; set; }

        /// <summary>
        /// The dynamic operational completion mark layout tracing synchronization times using the 'yyyy-MM-ddTHH:mm:ssZ' schema.
        /// </summary>
        public string UpdatedAt { get; set; } = null!;
    }
}
