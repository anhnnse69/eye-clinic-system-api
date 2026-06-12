using ECS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateStaffAccountServices
{
    /// <summary>
    /// Request data transfer object for creating a new staff account.
    /// </summary>
    public class CreateStaffRequest
    {
        /// <summary>
        /// Gets or sets the unique mobile phone number for authentication.
        /// Must be exactly 10 numeric digits.
        /// </summary>
        [Required(ErrorMessage = "APP_MESSAGE_4003")] // Required field is missing in the request
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "APP_MESSAGE_4001")] // Invalid phone number format or length
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the email address used as the primary login identifier.
        /// Must follow standard email address rules.
        /// </summary>
        [Required(ErrorMessage = "APP_MESSAGE_4003")] // Required field is missing in the request
        [EmailAddress(ErrorMessage = "APP_MESSAGE_4019")] // General validation error (Model state invalid)
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the full legal name of the staff member.
        /// </summary>
        [Required(ErrorMessage = "APP_MESSAGE_4003")] // Required field is missing in the request
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the raw password text to be encrypted.
        /// Must contain at least 8 characters.
        /// </summary>
        [Required(ErrorMessage = "APP_MESSAGE_4003")] // Required field is missing in the request
        [MinLength(8, ErrorMessage = "APP_MESSAGE_4019")] // General validation error (Model state invalid)
        public string Password { get; set; } = null!;

        /// <summary>
        /// Gets or sets the internal business operational role within the clinic boundary.
        /// </summary>
        [Required(ErrorMessage = "APP_MESSAGE_4003")] // Required field is missing in the request
        public StaffRole StaffRole { get; set; }
    }
}
