using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.Delete
{
    /// <summary>
    /// Response payload structure indicating the resulting account deletion or modification state.
    /// </summary>
    public class DeleteAccountResponse
    {
        /// <summary>
        /// Gets or sets the unique identity identifier of the processed user record.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the updated activation state flag outcome indicator.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the precise system timestamp tracking execution change.
        /// </summary>
        public string UpdatedAt { get; set; } = null!;
    }
}
