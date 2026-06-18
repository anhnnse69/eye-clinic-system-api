namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistUpdatePatientProfileServices
{
    /// <summary>
    /// Brief structural feedback data confirming transactional mutation execution blocks.
    /// </summary>
    public class ReceptionistUpdatePatientProfileResponse
    {
        /// <summary>
        /// The unique unique identifier string of the core patient database record mapping.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The synchronized full name payload outcome representation block.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// System baseline timestamp tracking the definitive persistence event frame. Formatted as ISO "yyyy-MM-ddTHH:mm:ssZ".
        /// </summary>
        public string UpdatedAt { get; set; } = null!;
    }
}
