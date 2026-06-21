using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.View
{
    /// <summary>
    /// Service contract defining operations for viewing paged and filtered system user account configurations.
    /// </summary>
    public interface IGetAccountsService
    {
        /// <summary>
        /// Executes the application workflow process to query, paginate, and parse target system account collection arrays.
        /// </summary>
        /// <param name="request">The parameters containing data filters, keywords, and explicit pagination criteria details.</param>
        /// <returns>A unified standard envelope tracking result execution response block containing list payload data alongside metadata boundaries.</returns>
        Task<ApiResponse<List<GetAccountResponse>>> Process(GetAccountsRequest request);
    }
}
