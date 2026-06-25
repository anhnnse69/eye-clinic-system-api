using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices
{
    /// <summary>
    /// Response schema encapsulating details of the updated service entity.
    /// </summary>
    public class EditServiceResponse
    {
        public string ServiceId { get; set; } = null!;
        public string ClinicId { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public decimal? Price { get; set; }
        public int DurationMinutes { get; set; }
        public string UpdatedAt { get; set; } = null!;
    }
}
