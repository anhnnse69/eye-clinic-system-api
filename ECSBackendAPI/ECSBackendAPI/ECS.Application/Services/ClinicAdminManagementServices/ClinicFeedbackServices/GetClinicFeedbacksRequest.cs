using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices
{
    /// <summary>
    /// Request object containing parameters for filtering and paginating feedback records.
    /// </summary>
    public class GetClinicFeedbacksRequest
    {
        /// <summary>
        /// Gets or sets the target text keyword match constraint utilized against Patient Name, Doctor Name, or Comment data fields.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets the explicit numerical score matrix filter used to query metrics associated with the assigned practitioner.
        /// </summary>
        public int? RatingDoctor { get; set; }

        /// <summary>
        /// Gets or sets the explicit numerical score matrix filter used to query metrics associated with the structural facility environment.
        /// </summary>
        public int? RatingClinic { get; set; }

        /// <summary>
        /// Gets or sets the chronological date milestone criteria tracking target generation records.
        /// </summary>
        public DateTime? FeedbackDate { get; set; }

        /// <summary>
        /// Gets or sets the sequential layout segment tracking index boundary parameter. Default configuration starts at index 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the upper bound limit size of elements inside a solitary segment page boundary. Default constraint is 10 rows.
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}