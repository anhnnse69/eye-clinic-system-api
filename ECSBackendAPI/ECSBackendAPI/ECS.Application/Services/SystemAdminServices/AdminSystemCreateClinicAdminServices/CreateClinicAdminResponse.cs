using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices
{
    /// <summary>
    /// Response model carrying data of the newly created Clinic Admin account.
    /// </summary>
    public class CreateClinicAdminResponse
    {
        /// <summary>
        /// Generated User Account ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Associated Clinic ID mapping definition.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Email identifier registered into system infrastructure.
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Account role designation.
        /// </summary>
        public string Role { get; set; } = null!;
    }
}
