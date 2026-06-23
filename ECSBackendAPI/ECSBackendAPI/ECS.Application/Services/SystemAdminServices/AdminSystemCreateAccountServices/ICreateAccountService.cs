using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AdminSystemCreateAccountServices
{
    /// <summary>
    /// Service contract defining operations for registering or creating system users accounts.
    /// </summary>
    public interface ICreateAccountService
    {
        /// <summary>
        /// Executes the domain operational flow logic to parse parameters, evaluate collisions, and register account states.
        /// </summary>
        /// <param name="request">The data transport model payload containing new identity configurations.</param>
        /// <returns>A standard api response tracking result envelope structures.</returns>
        Task<ApiResponse<CreateAccountResponse>> Process(CreateAccountRequest request);
    }
}
