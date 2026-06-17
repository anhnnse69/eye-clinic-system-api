namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewPatientDemographicsServices
{
    /// <summary>
    /// Request object for viewing patient demographics list.
    /// </summary>
    public class ViewPatientDemographicsRequest
    {
        /// <summary>
        /// Gets or sets the optional patient profile identifier to filter demographics for a specific patient.
        /// </summary>
        public string? PatientProfileId { get; set; }

        /// <summary>
        /// Gets or sets the optional record type filter (e.g., MS21_TRAUMA, MS22_ANTERIOR).
        /// </summary>
        public string? RecordType { get; set; }

        /// <summary>
        /// Gets or sets the keyword search term for filtering by diagnosis or record code.
        /// </summary>
        public string? SearchTerm { get; set; }

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
