namespace ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService
{
    /// <summary>
    /// Request to create one or more schedules (shifts)
    /// for a doctor across multiple work dates.
    /// </summary>
    public class CreateDoctorScheduleRequest
    {
        public List<DateOnly> WorkDates { get; set; } = [];
        public List<string> ShiftTypes { get; set; } = [];
        public Guid RoomId { get; set; }

    }
}
