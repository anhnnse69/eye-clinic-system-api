namespace ECS.Application.Services.PatientProfileManagementServices.SeparateProfileServices
{
    /// <summary>
    /// Request payload for separating a dependent child profile into an independent account.
    /// </summary>
    public class SeparateProfileRequest
    {
        /// <summary>
        /// The ID of the child patient profile to be separated.
        /// </summary>
        public Guid ChildPatientProfileId { get; set; }

        /// <summary>
        /// The new email address for the separated child account.
        /// Must be unique and different from the parent's email.
        /// </summary>
        public string NewEmail { get; set; } = string.Empty;

        /// <summary>
        /// The new phone number for the separated child account.
        /// Must be unique and different from the parent's phone.
        /// </summary>
        public string NewPhone { get; set; } = string.Empty;
    }
}
