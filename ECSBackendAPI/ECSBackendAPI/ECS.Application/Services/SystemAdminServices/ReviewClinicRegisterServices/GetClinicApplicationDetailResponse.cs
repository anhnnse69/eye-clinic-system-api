namespace ECS.Application.Services.SystemAdminServices.ReviewClinicRegisterServices
{
    public class GetClinicApplicationDetailResponse
    {
        public string Id_clinic_registration { get; set; } = null!;
        public string ClinicName { get; set; } = null!;
        public string ClinicAddress { get; set; } = null!;
        public string ContactName { get; set; } = null!;
        public string ContactPhone { get; set; } = null!;
        public string ContactEmail { get; set; } = null!;
        public string? BusinessLicenseUrl { get; set; }
        public string Status { get; set; } = null!; // PENDING, APPROVED, REJECTED
        public string? ReviewNote { get; set; }
        public string RequestedAt { get; set; } = null!;
    }
}
