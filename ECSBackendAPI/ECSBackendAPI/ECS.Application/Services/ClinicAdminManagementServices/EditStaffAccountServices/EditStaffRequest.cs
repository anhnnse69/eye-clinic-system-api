using System;
using ECS.Domain.Enums;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditStaffAccountServices
{
    /// <summary>
    /// Request data transfer object containing the necessary payload parameters to update an existing clinic staff account.
    /// </summary>
    public class EditStaffRequest
    {
        /// <summary>
        /// Gets or sets the target staff user identification token that requires mutation.
        /// </summary>
        public Guid StaffUserId { get; set; }

        /// <summary>
        /// Gets or sets the unique mobile phone number for authentication.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the email address used as the primary login identifier.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Gets or sets the updated full legal name of the clinic staff member.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the updated internal business operational role within the clinic boundary.
        /// </summary>
        public StaffRole StaffRole { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the target staff member account remains active or deactivated.
        /// </summary>
        public bool IsActive { get; set; }
    }
}