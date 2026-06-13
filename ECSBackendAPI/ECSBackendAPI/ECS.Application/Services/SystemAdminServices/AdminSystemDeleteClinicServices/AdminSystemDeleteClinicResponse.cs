namespace ECS.Application.Services.SystemAdminServices.AdminSystemDeleteClinicServices
{
    /// <summary>
    /// Response model returning data summary post-status modification.
    /// </summary>
    public class AdminSystemDeleteClinicResponse
    {
        public string Id_clinic { get; set; } = null!;
        public string ClinicName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
    }
}
