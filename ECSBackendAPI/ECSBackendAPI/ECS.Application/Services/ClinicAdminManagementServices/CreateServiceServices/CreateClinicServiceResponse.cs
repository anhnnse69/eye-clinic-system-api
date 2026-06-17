using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices
{
    /// <summary>
    /// Response schema containing confirmation data of the newly established clinic service.
    /// </summary>
    public class CreateServiceResponse
    {
        /// <summary>
        /// Gets or sets the persistence unique system index identifier generated for the service.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the validated name representation of the service.
        /// </summary>
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the linked facility environment identification reference context.
        /// </summary>
        public Guid ClinicId { get; set; }

        /// <summary>
        /// Gets or sets the baseline cost defined for this medical service.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the execution session duration metric in minutes.
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Gets or sets the official active transaction state for the medical service item.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
