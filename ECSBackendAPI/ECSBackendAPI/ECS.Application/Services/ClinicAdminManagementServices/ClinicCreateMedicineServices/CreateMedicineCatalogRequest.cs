using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateMedicineServices
{
    /// <summary>
    /// Request object containing detailed parameters for creating a new medicine catalog item.
    /// </summary>
    public class CreateMedicineCatalogRequest
    {
        public string MedicineName { get; set; } = null!;
        public string? GenericName { get; set; }
        public string? Unit { get; set; }
        public string? DosageForm { get; set; }
        public string? Concentration { get; set; }
        public string? Manufacturer { get; set; }
        public string? Notes { get; set; }
    }
}
