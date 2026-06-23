using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices
{
    /// <summary>
    /// Response object returning execution snapshot details after deactivation.
    /// </summary>
    public class DeactivateServiceResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier of the modified service.
        /// </summary>
        public Guid ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the structural activation status reflecting state transformation changes.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
