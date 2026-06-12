namespace ECS.Application.Services.SystemAdminServices.AdminSystemUpdateClinicServices
{
    /// <summary>
    /// Response model returning data summary post execution.
    /// </summary>
    public class UpdateClinicResponse
    {
        public string Id_clinic { get; set; } = null!;
        public string ClinicName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
    }
}