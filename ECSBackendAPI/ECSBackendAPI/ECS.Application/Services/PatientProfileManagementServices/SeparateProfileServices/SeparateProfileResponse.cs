namespace ECS.Application.Services.PatientProfileManagementServices.SeparateProfileServices
{
    /// <summary>
    /// Response payload after successfully separating a dependent profile.
    /// </summary>
    public class SeparateProfileResponse
    {
        /// <summary>
        /// The newly created User ID for the separated child account.
        /// </summary>
        public Guid NewUserId { get; set; }

        /// <summary>
        /// The newly created Patient Profile ID.
        /// </summary>
        public Guid NewPatientProfileId { get; set; }

        /// <summary>
        /// Success message confirming the profile separation.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
