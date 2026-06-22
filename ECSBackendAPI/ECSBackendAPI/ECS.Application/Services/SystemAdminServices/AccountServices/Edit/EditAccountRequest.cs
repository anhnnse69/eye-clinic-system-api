using ECS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.Edit
{
    /// <summary>
    /// Request data model for updating a user account by the System Administrator.
    /// </summary>
    public class EditAccountRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the target user account to update.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the new contact phone number for the user account.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Gets or sets the optional email address for the user account.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the full display name of the user.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the systemic role assigned to this user account.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// Gets or sets the optional avatar picture URL location reference string.
        /// </summary>
        public string? AvatarUrl { get; set; }
    }
}
