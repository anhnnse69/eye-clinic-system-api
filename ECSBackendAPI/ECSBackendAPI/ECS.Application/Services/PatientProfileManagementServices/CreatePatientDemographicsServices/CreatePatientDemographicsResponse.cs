namespace ECS.Application.Services.PatientProfileManagementServices.CreatePatientDemographicsServices
{
    /// <summary>
    /// Response data entity conveying confirmation details of the created patient demographics.
    /// </summary>
    public class CreatePatientDemographicsResponse
    {
        public Guid PatientProfileId { get; set; }
        public string FullName { get; set; } = null!;
        public string? IdentityNumber { get; set; }
        public string Relationship { get; set; } = null!;
        public string CreatedAt { get; set; } = null!;
    }
}
