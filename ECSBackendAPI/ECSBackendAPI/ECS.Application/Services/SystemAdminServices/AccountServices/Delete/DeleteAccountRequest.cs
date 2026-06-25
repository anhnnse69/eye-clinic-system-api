using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.Delete
{
    /// <summary>
    /// Request criteria parameters for soft-deleting or toggling a user account state.
    /// </summary>
    public class DeleteAccountRequest
    {
        /// <summary>
        /// Gets or sets the target user identifier.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the target activation state status flag (false for soft-delete/lock, true for unlock).
        /// </summary>
        public bool IsActive { get; set; }
    }
}
