using System;
using System.Collections.Generic;
using System.Text;

namespace ECS.Application.Services.ClinicAdminManagementServices.ClinicFeedbackServices
{
    /// <summary>
    /// Response object representing feedback presentation details.
    /// </summary>
    public class GetClinicFeedbackResponse
    {
        /// <summary>
        /// Gets or sets the stringified system unique surrogate master tracking identifier signature.
        /// </summary>
        public string Id_feedback { get; set; } = null!;

        /// <summary>
        /// Gets or sets the patient identity display string value mapped out of database configurations.
        /// </summary>
        public string PatientName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the provider practitioner clinical metadata full name string value tracking execution metrics.
        /// </summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the numerical rating value evaluated directly against the clinical practitioner.
        /// </summary>
        public int RatingDoctor { get; set; }

        /// <summary>
        /// Gets or sets the numerical rating value evaluated directly against the physical facility workflows.
        /// </summary>
        public int RatingClinic { get; set; }

        /// <summary>
        /// Gets or sets the qualitative text feedback notation left by customers, defaulting onto fallback string tokens.
        /// </summary>
        public string Comment { get; set; } = "N/A";

        /// <summary>
        /// Gets or sets a value indicating whether this tracking item record is authorized for presentation visibility layouts.
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the string layout representing the primary appointment schedule chronological validation date structure.
        /// </summary>
        public string AppointmentDate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the explicit generation initialization timestamp presentation string formatted for user interface grids.
        /// </summary>
        public string FeedbackDate { get; set; } = null!;
    }
}