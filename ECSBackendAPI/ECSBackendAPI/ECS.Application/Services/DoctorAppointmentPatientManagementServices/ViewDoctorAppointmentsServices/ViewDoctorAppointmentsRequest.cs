using ECS.Domain.Enums;

namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.ViewDoctorAppointmentsServices
{
    /// <summary>
    /// Represents the request for retrieving
    /// a doctor's appointment list.
    /// </summary>
    public class ViewDoctorAppointmentsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public AppointmentStatus? Status { get; set; }
        public DateOnly? Date { get; set; }
        public string? Search { get; set; }
    }
}
