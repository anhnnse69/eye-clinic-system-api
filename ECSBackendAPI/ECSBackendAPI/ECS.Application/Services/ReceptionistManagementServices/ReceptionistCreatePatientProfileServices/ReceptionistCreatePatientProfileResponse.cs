namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistCreatePatientProfileServices
{
    /// <summary>
    /// Structural presentation tracking response package confirming successful profile generation.
    /// </summary>
    public class ReceptionistCreatePatientProfileResponse
    {
        /// <summary>
        /// The unique structural identifier designated for the newly generated patient graph.
        /// </summary>
        public string PatientProfileId { get; set; } = null!;

        /// <summary>
        /// The synchronized full name block confirming storage transcription mapping.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The cross-referenced identifier linking downstream user identity contexts.
        /// </summary>
        public string? LinkedUserId { get; set; }

        /// <summary>
        /// Status marker reporting if the initialization pipeline triggered automated account creation mechanics.
        /// </summary>
        public bool IsAccountAutoCreated { get; set; }

        /// <summary>
        /// Temporary unhashed authentication credential payload returned securely once for operator interface presentation.
        /// </summary>
        public string? GeneratedPassword { get; set; }

        /// <summary>
        /// System baseline timestamp tracking the definitive persistence event frame. Formatted as ISO "yyyy-MM-ddTHH:mm:ssZ".
        /// </summary>
        public string CreatedAt { get; set; } = null!;
    }
}
