namespace ECS.Application.Services.PatientProfileManagementServices.CreatePatientProfileServices
{
    /// <summary>
    /// Response data entity conveying confirmation details of the provisioned patient profile.
    /// </summary>
    public class CreatePatientProfileResponse
    {
        public Guid PatientProfileId { get; set; }
        public string FullName { get; set; } = null!;
        public string Relationship { get; set; } = null!;
        public string CreatedAt { get; set; } = null!;
    }
}
