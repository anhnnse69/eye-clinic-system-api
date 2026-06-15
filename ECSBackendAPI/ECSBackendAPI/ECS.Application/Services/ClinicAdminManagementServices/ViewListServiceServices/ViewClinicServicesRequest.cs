namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListServiceServices
{
    /// <summary>
    /// Request object for retrieving a paged, filtered, and searched list of clinic services.
    /// </summary>
    public class ViewClinicServicesRequest
    {
        /// <summary>
        /// The current page number for pagination. Defaults to 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The number of records to return per page. Defaults to 10.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Optional search keyword matched against service names.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Optional status filter to search for active or inactive services.
        /// </summary>
        public bool? IsActive { get; set; }
    }
}