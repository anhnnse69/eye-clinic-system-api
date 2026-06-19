namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices
{
    /// <summary>
    /// Brief structural feedback data confirming matched user entity records for presentation layouts.
    /// </summary>
    public class ReceptionistSearchAccountResponse
    {
        /// <summary>
        /// The unique identifier string tracking the user entity record inside backend layers.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The synchronized full name payload presentation mapping block.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// The primary telephone communication sequence line record mapping.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// The registered digital communication email boundary data packet.
        /// </summary>
        public string? Email { get; set; }
    }
}