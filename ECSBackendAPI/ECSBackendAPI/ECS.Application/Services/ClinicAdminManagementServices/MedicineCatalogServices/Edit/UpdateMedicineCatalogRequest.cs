using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.Edit
{
    /// <summary>
    /// Request object for updating an existing medicine catalog item.
    /// </summary>
    public class UpdateMedicineCatalogRequest
    {
        public Guid Id { get; set; }
        public string MedicineName { get; set; } = null!;
        public string? GenericName { get; set; }
        public string? Unit { get; set; }
        public string? DosageForm { get; set; }
        public string? Concentration { get; set; }
        public string? Manufacturer { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }
}
