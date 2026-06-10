namespace ECS.Application.Services.SystemAdminServices.ClinicRegisterServices
{
    public class GetClinicApplicationResponse
    {
        public string Id_clinic_registration { get; set; } = null!;
        public string ClinicName { get; set; } = null!;
        public string ContactEmail { get; set; } = null!;
        public string ContactPhone { get; set; } = null!;
        public string SubmissionDate { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
