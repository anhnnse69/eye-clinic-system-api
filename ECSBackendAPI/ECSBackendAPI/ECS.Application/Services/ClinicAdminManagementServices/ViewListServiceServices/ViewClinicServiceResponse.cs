namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices
{
    /// <summary>
    /// Response object containing detailed structural attributes of a clinic service.
    /// </summary>
    public class ViewClinicServiceResponse
    {
        /// <summary>
        /// Unique identifier key formatted as string.
        /// </summary>
        public string Id_service { get; set; } = null!;

        /// <summary>
        /// Descriptive name designation of the service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Standard monetary valuation price for service execution.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Standard time block consumption requirement in minutes.
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Operational status indicator determining availability.
        /// </summary>
        public bool IsActive { get; set; }
    }
}