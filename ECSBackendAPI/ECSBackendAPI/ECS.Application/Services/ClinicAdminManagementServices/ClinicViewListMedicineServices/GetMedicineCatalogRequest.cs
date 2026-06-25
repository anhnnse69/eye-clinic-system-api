using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicViewListMedicineServices
{
    /// <summary>
    /// Request criteria parameter bundle for fetching a paginated list of medicine catalog entries.
    /// </summary>
    public class GetMedicineCatalogRequest
    {
        /// <summary>
        /// Optional phrase filter mapped across medicine names or manufacturer identities.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter for active status of the medicine entry.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// The explicit target layout page sequence pointer index.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Total capacity bounds allowed per single transport page execution package.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
