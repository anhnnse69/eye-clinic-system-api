namespace ECS.Application.Services.SystemAdminServices.ClinicManagementServices
{
    /// <summary>
    /// Response object representing a clinic's structural display presentation layer data details.
    /// </summary>
    public class GetClinicResponse
    {
        // Unique identifier string representation for client tracking components
        public string Id_clinic { get; set; } = null!;

        // Designated registration moniker designation string of the specific medical clinic entity 
        public string ClinicName { get; set; } = null!;

        // Street localization geographic address coordinates representation context data
        public string Address { get; set; } = null!;

        // Registered primary email transmission address mailbox coordinate
        public string ContactEmail { get; set; } = null!;

        // Telephony contact line numerical digits code mapping 
        public string ContactPhone { get; set; } = null!;

        // Text format layout registration string capturing initial foundation timeframe record logs
        public string CreatedAt { get; set; } = null!;

        // Lifecycle status metric tracking representation string: "ACTIVE" or "INACTIVE" matching client layout structures
        public string Status { get; set; } = null!; 
    }
}
