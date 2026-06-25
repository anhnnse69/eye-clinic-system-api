using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicEditMedicineServices
{
    /// <summary>
    /// Service contract defining operations for editing clinic medicine catalogs details.
    /// </summary>
    public interface IUpdateMedicineCatalogService
    {
        /// <summary>
        /// Executes workflow logic to modify a targeted physical medicine item.
        /// </summary>
        /// <param name="request">The parameters carrying entity update payload elements details.</param>
        /// <returns>A unified standard envelope tracking execution response block.</returns>
        Task<ApiResponse<UpdateMedicineCatalogResponse>> Process(UpdateMedicineCatalogRequest request);
    }
}
