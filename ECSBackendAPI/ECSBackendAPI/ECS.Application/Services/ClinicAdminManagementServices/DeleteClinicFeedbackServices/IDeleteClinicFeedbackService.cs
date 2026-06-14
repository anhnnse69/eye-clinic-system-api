using System;
using System.Collections.Generic;
using System.Text;
using ECS.Application.Common.Response;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices
{
    /// <summary>
    /// Service contract defining operations for soft deleting clinic feedback records.
    /// </summary>
    public interface IDeleteClinicFeedbackService
    {
        /// <summary>
        /// Executes the application workflow process to soft delete a feedback record.
        /// </summary>
        /// <param name="request">
        /// The request payload containing the feedback identifier.
        /// </param>
        /// <returns>
        /// A unified standard envelope containing execution status information.
        /// </returns>
        Task<ApiResponse<bool>> Process(
            DeleteClinicFeedbackRequest request);
    }
}
