using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewClinicAppointmentsServices
{
    /// <summary>
    /// Represents the request for retrieving
    /// a clinic-wide appointment list (all doctors).
    /// </summary>
    public class ViewClinicAppointmentsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public AppointmentStatus? Status { get; set; }
        public DateOnly? Date { get; set; }

        /// <summary>
        /// Free-text search matching patient name, patient phone,
        /// or doctor name.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Optional filter to narrow results down to a single doctor
        /// within the receptionist's clinic.
        /// </summary>
        public Guid? DoctorId { get; set; }
    }
}