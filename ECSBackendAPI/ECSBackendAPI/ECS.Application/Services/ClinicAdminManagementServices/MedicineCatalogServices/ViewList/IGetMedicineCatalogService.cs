using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.MedicineCatalogServices.ViewList
{
    /// <summary>
    /// Service contract defining retrieval procedures for medicine catalog indices.
    /// </summary>
    public interface IGetMedicineCatalogService
    {
        /// <summary>
        /// Executes pipeline workflow to search, filter, and extract segmented catalog pages.
        /// </summary>
        /// <param name="request">The data container tracking layout configuration criteria.</param>
        /// <returns>A standard api wrapper bundling paginated tracking schemas.</returns>
        Task<ApiResponse<List<GetMedicineCatalogResponse>>> Process(GetMedicineCatalogRequest request);
    }
}
