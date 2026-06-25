using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListMedicineServices
{
    /// <summary>
    /// Response schema representing serialized domain data attributes for medicine catalogs.
    /// </summary>
    public class GetMedicineCatalogResponse
    {
        public string Id { get; set; } = null!;
        public string MedicineName { get; set; } = null!;
        public string? GenericName { get; set; }
        public string? Unit { get; set; }
        public string? DosageForm { get; set; }
        public string? Concentration { get; set; }
        public string? Manufacturer { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = null!;
    }
}
