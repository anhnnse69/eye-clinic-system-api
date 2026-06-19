namespace ECS.Application.Services.ReceptionistManagementServices.ReceptionistSearchAccountServices
{
    /// <summary>
    /// Payload request detailing parameters mapping to UI presentation lookup and filtering forms.
    /// </summary>
    public class ReceptionistSearchAccountRequest
    {
        /// <summary>
        /// The dynamic token string used for filtering targeting full name matching.
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// The telephone communication sequence criteria filter.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// The network mail routing identification identifier filter.
        /// </summary>
        public string? Email { get; set; }
    }
}