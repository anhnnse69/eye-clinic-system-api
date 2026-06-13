using ECS.Domain.Enums;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices
{
    /// <summary>
    /// Request data transfer object for creating a new staff account.
    /// </summary>
    public class CreateStaffRequest
    {
        /// <summary>
        /// Gets or sets the unique mobile phone number for authentication.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the email address used as the primary login identifier.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full legal name of the staff member.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the raw password text to be encrypted.
        /// </summary>
        public string Password { get; set; } = null!;

        /// <summary>
        /// Gets or sets the internal business operational role within the clinic boundary.
        /// </summary>
        public StaffRole StaffRole { get; set; }
    }
}