using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices
{
    /// <summary>
    /// Request object containing parameters for updating a clinic service.
    /// </summary>
    public class EditServiceRequest
    {
        /// <summary>
        /// Unique identity coordinate tracking the targeted service record.
        /// </summary>
        public Guid ServiceId { get; set; }

        /// <summary>
        /// The updated descriptive name identifier for the service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// The revised monetary value assigned to the treatment process.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// The operational timeline buffer expressed in minutes.
        /// </summary>
        public int DurationMinutes { get; set; }
    }
}
