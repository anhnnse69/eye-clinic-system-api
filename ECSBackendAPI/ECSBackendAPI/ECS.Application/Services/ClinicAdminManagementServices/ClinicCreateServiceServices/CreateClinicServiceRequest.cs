using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices
{
    /// <summary>
    /// Request object containing information required to create a new clinic service.
    /// </summary>
    public class CreateServiceRequest
    {
        /// <summary>
        /// Gets or sets the unique commercial or display name of the medical service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the financial cost applied for this clinical service.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the estimated duration in minutes allocated for a standard service session.
        /// </summary>
        public int DurationMinutes { get; set; }
    }
}
