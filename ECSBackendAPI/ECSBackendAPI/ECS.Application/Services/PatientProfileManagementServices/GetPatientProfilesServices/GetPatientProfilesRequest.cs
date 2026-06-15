namespace ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices
{
    /// <summary>
    /// Request object containing parameters for filtering and paginating patient profiles for the logged-in user.
    /// </summary>
    public class GetPatientProfilesRequest
    {
        /// <summary>
        /// Gets or sets the target text keyword match constraint utilized against FullName or IdentityNumber data fields.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets the sequential layout segment tracking index boundary parameter. Default configuration starts at index 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the upper bound limit size of elements inside a solitary segment page boundary. Default constraint is 10 rows.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
