using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicDeleteMedicineServices
{
    /// <summary>
    /// Service contract defining operations to transition state records of medicine inventories.
    /// </summary>
    public interface IDeleteMedicineCatalogService
    {
        /// <summary>
        /// Orchestrates sequential workflows targeting target tracking rows to process soft state mutations.
        /// </summary>
        /// <param name="request">The data transport layer execution parameter configuration tracking markers.</param>
        /// <returns>A standard wrapped application result tracing workflow outcomes.</returns>
        Task<ApiResponse<DeleteMedicineCatalogResponse>> Process(DeleteMedicineCatalogRequest request);
    }
}
