namespace ECS.Application.Services.AuthServices.RegisterClinicApplicationServices
{
    /// <summary>
    /// Request model for clinic application registration.
    /// </summary>
    public class RegisterClinicApplicationRequest
    {
        public string ClinicName { get; set; } = null!;
        public string ClinicAddress { get; set; } = null!;
        public string ContactName { get; set; } = null!;
        public string ContactPhone { get; set; } = null!;
        public string ContactEmail { get; set; } = null!;
        public string? BusinessLicenseUrl { get; set; }
    }
}
