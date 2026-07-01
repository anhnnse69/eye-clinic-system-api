using System;
using System.Threading.Tasks;
using ECS.Application.Common.Response;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateClinicAdminServices
{
    /// <summary>
    /// Service contract defining operations for provisioning a clinic administrator account.
    /// </summary>
    public interface ICreateClinicAdminService
    {
        /// <summary>
        /// Executes the application workflow process to provision a new clinic administrator account.
        /// </summary>
        /// <param name="request">The detailed target clinic and personnel payload definitions.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<CreateClinicAdminResponse>> Process(CreateClinicAdminRequest request);
    }
}