using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.CreateServiceServices
{
    /// <summary>
    /// Service contract defining operations to register a new clinic service entity.
    /// </summary>
    public interface ICreateService
    {
        /// <summary>
        /// Executes the application workflow process to append and persist new clinical service lines.
        /// </summary>
        /// <param name="request">The data container criteria holding new service attributes.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<CreateServiceResponse>> Process(CreateServiceRequest request);
    }
}
