using ECS.Application.Common.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.SystemAdminServices.AccountServices.Delete
{
    /// <summary>
    /// Service contract defining soft-delete account processes for persistent user entities.
    /// </summary>
    public interface IDeleteAccountService
    {
        /// <summary>
        /// Executes workflow logic rules to transform, track, and soft-delete specific user accounts.
        /// </summary>
        /// <param name="request">The operational execution request parameters detail schema container.</param>
        /// <returns>A standard centralized api transport envelope carrying operation outcomes.</returns>
        Task<ApiResponse<DeleteAccountResponse>> Process(DeleteAccountRequest request);
    }
}
