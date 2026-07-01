using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices
{
    /// <summary>
    /// Request data required to create a new Clinic Admin account.
    /// </summary>
    public class CreateClinicAdminRequest
    {
        /// <summary>
        /// The unique identifier of the clinic this admin will manage.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Full name of the clinic administrator.
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Contact phone number for the administrator account.
        /// </summary>
        public string Phone { get; set; } = null!;

        /// <summary>
        /// Target email where credentials will be delivered (Clinic contact email).
        /// </summary>
        public string Email { get; set; } = null!;
    }
}
