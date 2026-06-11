using System;
using System.Collections.Generic;
using System.Text;
using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditClinicProfileServices
{
    /// <summary>
    /// Service contract defining operations for updating clinic profile data.
    /// </summary>
    public interface IEditClinicProfileService
    {
        /// <summary>
        /// Executes the clinic profile update workflow.
        /// </summary>
        /// <param name="request">
        /// The clinic profile update request.
        /// </param>
        /// <returns>
        /// A standardized API response indicating execution result.
        /// </returns>
        Task<ApiResponse<bool>> Process(EditClinicProfileRequest request);
    }
}
