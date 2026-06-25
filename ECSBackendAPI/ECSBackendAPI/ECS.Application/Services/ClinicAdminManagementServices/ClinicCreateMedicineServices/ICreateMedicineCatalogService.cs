using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicCreateMedicineServices
{
    /// <summary>
    /// Service contract defining operations for creating a new clinic medicine catalog.
    /// </summary>
    public interface ICreateMedicineCatalogService
    {
        /// <summary>
        /// Executes the workflow process to validate context boundaries and persist new medicine records.
        /// </summary>
        /// <param name="request">The structural model criteria details containing fields to register.</param>
        /// <returns>A unified standard envelope tracking execution response block.</returns>
        Task<ApiResponse<CreateMedicineCatalogResponse>> Process(CreateMedicineCatalogRequest request);
    }
}
