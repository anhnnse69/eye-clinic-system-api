namespace ECS.Application.Services.SystemAdminServices.ClinicManagementServices
{
    /// <summary>
    /// Request object containing parameters for filtering and paginating the clinics list.
    /// </summary>
    public class GetClinicsRequest
    {
        // Search keyword token
        public string? SearchTerm { get; set; }

        // Target status constraint: "" (All), "ACTIVE" (Active), "INACTIVE" (Inactive)
        public string? Status { get; set; } // "" ALL, "ACTIVE" , "INACTIVE" 

        // Paging page index tracker
        public int PageNumber { get; set; } = 1;

        // Upper bound limit size of elements inside a solitary segment page boundary
        public int PageSize { get; set; } = 10;
    }
}