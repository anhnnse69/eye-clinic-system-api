namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistGetPatientsListServices
{
    /// <summary>
    /// Request object containing filter parameters and pagination boundaries for querying the clinic patients list.
    /// </summary>
    public class ReceptionistGetPatientsListRequest
    {
        /// <summary>
        /// The active receptionist user identifier. This field is explicitly mapped from token contexts at the API gateway layer.
        /// </summary>
        public Guid CurrentUserId { get; set; }

        /// <summary>
        /// Optional query substring matching patient profile fullname sequences.
        /// </summary>
        public string? SearchName { get; set; }

        /// <summary>
        /// Optional query substring matching exact or partial patient phone number segments.
        /// </summary>
        public string? SearchPhone { get; set; }

        /// <summary>
        /// The index pointer targeting the active processing pagination data slice segment. Defaults to 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The evaluation capacity boundary limiting rows rendered per viewport partition. Defaults to 5.
        /// </summary>
        public int PageSize { get; set; } = 5;
    }
}