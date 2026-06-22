namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.DoctorDashboardServices
{
    /// <summary>
    /// Request model for retrieving doctor dashboard statistics.
    /// </summary>
    public class DoctorDashboardRequest
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
