using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.View
{
    /// <summary>
    /// Response presentation data transfer schema mapping representation for an account entity projection.
    /// </summary>
    public class GetAccountResponse
    {
        /// <summary>
        /// Physical core persistence database record primary identifier token string.
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// The verified customer contact telephone address data attribute string.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Optional descriptive internet messaging communication endpoint address string.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// The individual entity full representation title mapping descriptor.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Systemic logical functional role categorization classification type name.
        /// </summary>
        public string Role { get; set; } = null!;

        /// <summary>
        /// Evaluation flag tracking whether current user remains permitted into active runtime flows.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Optional media asset source layout locator reference mapping string.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// System resource registration establishment calendar metadata timestamp layout string.
        /// </summary>
        public string CreatedAt { get; set; } = null!;
    }
}
