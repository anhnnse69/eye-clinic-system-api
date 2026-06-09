using ECS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ECS.Application.Services.AuthServices.RegisterServices
{
    /// <summary>
    /// Request model for user self-registration.
    /// All fields are required. Email must be a valid format.
    /// Password must be at least 6 characters.
    /// ConfirmPassword must match Password (validated in service layer).
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>Full name of the user. Cannot be empty.</summary>
        [Required(ErrorMessage = nameof(GeneralCode.APP_MESSAGE_4003))]
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Email address. Must be a valid email format.
        /// Must be unique in the system (checked in service layer → APP_MESSAGE_4017).
        /// </summary>
        [Required(ErrorMessage = nameof(GeneralCode.APP_MESSAGE_4003))]
        [EmailAddress(ErrorMessage = nameof(GeneralCode.APP_MESSAGE_4019))]
        public string Email { get; set; } = null!;

        /// <summary>
        /// Phone number. Must be unique in the system
        /// (checked in service layer → APP_MESSAGE_4018).
        /// </summary>
        [Required(ErrorMessage = nameof(GeneralCode.APP_MESSAGE_4003))]
        public string Phone { get; set; } = null!;

        /// <summary>Password. Minimum 6 characters.</summary>
        [Required(ErrorMessage = nameof(GeneralCode.APP_MESSAGE_4003))]
        [MinLength(6, ErrorMessage = nameof(GeneralCode.APP_MESSAGE_4019))]
        public string Password { get; set; } = null!;

        /// <summary>
        /// Must match Password exactly.
        /// Mismatch returns APP_MESSAGE_4019 from service layer.
        /// </summary>
        [Required(ErrorMessage = nameof(GeneralCode.APP_MESSAGE_4003))]
        public string ConfirmPassword { get; set; } = null!;
    }
}