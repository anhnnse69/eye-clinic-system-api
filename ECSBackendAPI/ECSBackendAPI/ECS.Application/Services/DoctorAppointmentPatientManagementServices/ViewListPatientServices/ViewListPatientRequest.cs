using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewListPatientServices
{
    /// <summary>
    /// Represents the request for paginated patient list
    /// of a doctor.
    /// </summary>
    public class ViewListPatientRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Optional filter by appointment status.
        /// Null means all statuses.
        /// </summary>
        public AppointmentStatus? Status { get; set; }
    }
}
