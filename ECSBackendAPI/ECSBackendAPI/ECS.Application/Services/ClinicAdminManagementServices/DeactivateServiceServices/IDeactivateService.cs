using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeactivateServiceServices
{
    /// <summary>
    /// Service contract defining operations for deactivating an active clinic service item profile.
    /// </summary>
    public interface IDeactivateService
    {
        /// <summary>
        /// Executes the application workflow process to flag specified target items as inactive.
        /// </summary>
        /// <param name="request">The structural data container holding specific targeting transaction variables.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<DeactivateServiceResponse>> Process(DeactivateServiceRequest request);
    }
}
