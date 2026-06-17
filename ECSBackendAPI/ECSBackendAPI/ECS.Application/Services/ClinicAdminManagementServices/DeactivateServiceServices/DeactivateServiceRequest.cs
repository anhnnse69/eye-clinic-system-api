using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices
{
    /// <summary>
    /// Request object containing criteria parameters to deactivate a clinic service.
    /// </summary>
    public class DeactivateServiceRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the targeted service entity.
        /// </summary>
        public Guid ServiceId { get; set; }
    }
}
