using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.EditServiceServices
{
    /// <summary>
    /// Service contract defining operations for updating an existing clinic service.
    /// </summary>
    public interface IEditServiceService
    {
        /// <summary>
        /// Executes the application workflow process to validate context and modify the service record.
        /// </summary>
        /// <param name="request">The parameters containing specific target service data changes.</param>
        /// <returns>A unified standard envelope tracking execution result data blocks.</returns>
        Task<ApiResponse<EditServiceResponse>> Process(EditServiceRequest request);
    }
}
