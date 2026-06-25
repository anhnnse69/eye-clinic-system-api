using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicEditMedicineServices
{
    /// <summary>
    /// Response object returning updated medicine catalog execution status attributes.
    /// </summary>
    public class UpdateMedicineCatalogResponse
    {
        public Guid Id { get; set; }
        public string MedicineName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
