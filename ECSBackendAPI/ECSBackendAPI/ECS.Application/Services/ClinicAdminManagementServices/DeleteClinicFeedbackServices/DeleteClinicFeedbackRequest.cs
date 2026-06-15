using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.DeleteClinicFeedbackServices
{
    /// <summary>
    /// Request object containing feedback deletion parameters.
    /// </summary>
    public class DeleteClinicFeedbackRequest
    {
        /// <summary>
        /// Gets or sets the feedback identifier value targeted for soft deletion processing.
        /// </summary>
        public string FeedbackId { get; set; } = null!;
    }
}
