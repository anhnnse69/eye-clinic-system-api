using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.GetClinicLookupServices
{
    /// <summary>
    /// Service contract defining minimal operational lookups mapping global active clinic collections.
    /// </summary>
    public interface IGetClinicLookupService
    {
        /// <summary>
        /// Executes the application workflow process to retrieve lightweight descriptive clinic metadata records.
        /// </summary>
        /// <param name="request">The structural lookup boundary criteria definition settings context.</param>
        /// <returns>A unified standard envelope tracking result execution response block.</returns>
        Task<ApiResponse<List<GetClinicLookupResponse>>> Process(GetClinicLookupRequest request);
    }
}
