using ECS.Domain.Enums;

namespace ECS.Application.Services.MedicalRecordsServices.GetMedicalRecordsServices
{
    /// <summary>
    /// Request object for retrieving medical records history.
    /// </summary>
    public class GetMedicalRecordsRequest
    {
        /// <summary>
        /// Page number (default: 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of records per page (default: 10)
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Filter by start date (optional)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Filter by end date (optional)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Filter by record type (optional)
        /// </summary>
        public RecordType? RecordType { get; set; }

        /// <summary>
        /// Filter by doctor ID (optional)
        /// </summary>
        public Guid? DoctorId { get; set; }

        /// <summary>
        /// Search by patient name or ID (optional)
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by patient profile ID (optional)
        /// </summary>
        public Guid? PatientId { get; set; }
    }
}
