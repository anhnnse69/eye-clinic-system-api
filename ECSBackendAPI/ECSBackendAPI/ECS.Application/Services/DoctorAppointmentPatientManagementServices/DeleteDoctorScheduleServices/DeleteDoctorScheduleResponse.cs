namespace ECS.Application.Services.DoctorAppointmentPatientManagementServices.DeleteDoctorScheduleServices
{
    /// <summary>
    /// Represents the response returned after a doctor schedule is successfully deleted.
    /// </summary>
    public class DeleteDoctorScheduleResponse
    {
        public Guid ScheduleId { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
