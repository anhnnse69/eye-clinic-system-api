namespace ECS.Application.Services.PatientProfileManagementServices.UpdatePatientProfileServices
{
    /// <summary>
    /// Response data entity conveying confirmation details of the updated patient profile structure.
    /// </summary>
    public class UpdatePatientProfileResponse
    {
        public Guid PatientProfileId { get; set; }
        public string FullName { get; set; } = null!;
        public string Relationship { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
    }
}
