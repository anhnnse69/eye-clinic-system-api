using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Delete
{
    /// <summary>
    /// Request object representing criteria to toggle the active status of a medicine catalog item.
    /// </summary>
    public class DeleteMedicineCatalogRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier of the targeted medicine catalog record.
        /// </summary>
        public Guid Id { get; set; }
    }
}
