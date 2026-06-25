using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateMedicineServices
{
    /// <summary>
    /// Response payload returning the newly created medicine catalog unique identifier.
    /// </summary>
    public class CreateMedicineCatalogResponse
    {
        /// <summary>
        /// Gets or sets the generated unique identifier of the medicine catalog entry.
        /// </summary>
        public string Id { get; set; } = null!;
    }
}
