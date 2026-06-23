using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ViewListStaffAccountsServices
{
    /// <summary>
    /// Data contract carrying parameters for pagination, status filtering, and targeted search terms.
    /// </summary>
    public class ViewListStaffRequest
    {
        /// <summary>
        /// Gets or sets the target page index boundary. Defaults to 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the data segment size per page boundary. Defaults to 10.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the optional active state visibility condition criteria modifier. 
        /// Set to null to view all staff regardless of status.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets the search string term evaluated against Name, Email, and Phone profiles.
        /// </summary>
        public string? SearchTerm { get; set; }
    }
}