namespace ECS.Application.Services.ClinicAdminManagementServices.RequestPublishClinicServices
{
    /// <summary>
    /// Request object for requesting clinic publication.
    /// </summary>
    public class RequestPublishClinicRequest
    {
        /// <summary>
        /// Gets or sets the clinic identifier value targeted for publication request processing.
        /// </summary>
        public string ClinicId { get; set; } = null!;

    }
}
