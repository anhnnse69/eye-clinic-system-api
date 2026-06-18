namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetAvailableSlotsServices
{
    /// <summary>
    /// Request object containing filter parameters for querying clinic schedule and slots matrix.
    /// </summary>
    public class GetAvailableSlotsRequest
    {
        /// <summary>
        /// The active receptionist user identifier. This field is explicitly mapped from token contexts at the API gateway layer.
        /// </summary>
        public Guid CurrentUserId { get; set; }

        /// <summary>
        /// The target evaluation calendar date boundary. Defaults to UTC current date markers.
        /// </summary>
        public DateTime WorkDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Optional query substring matching doctor profile fullname sequences.
        /// </summary>
        public string? SearchDoctor { get; set; }

        /// <summary>
        /// Optional shift enumeration constraint tracker. Supports MORNING, AFTERNOON, EVENING string codes.
        /// </summary>
        public string? ShiftType { get; set; }

        /// <summary>
        /// Optional specialty identity lookup filter. Accepts standard system string Guid segments or "All".
        /// </summary>
        public string? SpecialtyId { get; set; }
    }
}