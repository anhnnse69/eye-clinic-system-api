namespace ECS.Application.Services.PatientProfileManagementServices.GetPatientProfilesServices
{
    /// <summary>
    /// Response object representing patient profile presentation details within the patient's scope.
    /// </summary>
    public class GetPatientProfileResponse
    {
        /// <summary>
        /// Gets or sets the stringified unique identification key for the patient profile.
        /// </summary>
        public string Id_patientProfile { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string Dob { get; set; } = null!;
        public string? IdentityNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Relationship { get; set; }
        public string CreatedAt { get; set; } = null!;
    }
}
