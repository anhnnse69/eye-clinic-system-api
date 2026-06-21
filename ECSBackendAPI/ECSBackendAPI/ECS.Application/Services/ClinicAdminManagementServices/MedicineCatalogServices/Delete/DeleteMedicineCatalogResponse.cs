using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Delete
{
    /// <summary>
    /// Response object identifying current operational state changes post mutation.
    /// </summary>
    public class DeleteMedicineCatalogResponse
    {
        /// <summary>
        /// Gets or sets the matched identity pointer of the domain record.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the localized structural label of the tracked catalog variant.
        /// </summary>
        public string MedicineName { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether the entity record context is unlocked or locked.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the exact timestamp tracing structural system mutations.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
