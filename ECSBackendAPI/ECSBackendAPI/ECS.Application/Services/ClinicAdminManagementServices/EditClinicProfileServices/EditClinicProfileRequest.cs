using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices
{
    /// <summary>
    /// Request object containing clinic profile update information.
    /// </summary>
    public class EditClinicProfileRequest
    {
        public string Name { get; set; } = null!;

        public string Address { get; set; } = null!;

        public string Phone { get; set; } = null!;

        public string? Email { get; set; }

        public string? LogoUrl { get; set; }

        public string? Description { get; set; }
    }
}
