using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.Edit
{
    /// <summary>
    /// Service contract defining account modification capabilities for system level administrators.
    /// </summary>
    public interface IEditAccountService
    {
        /// <summary>
        /// Executes the pipeline workflow to modify, validate unique constraints, and persist updated account mutations.
        /// </summary>
        /// <param name="request">The detailed edit criteria specifications payload model context.</param>
        /// <returns>A structured unified api standard metadata wrapper response capsule context.</returns>
        Task<ApiResponse<EditAccountResponse>> Process(EditAccountRequest request);
    }
}
