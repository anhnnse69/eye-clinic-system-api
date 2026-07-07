namespace ECS.Application.Services.DoctorScheduleManagementServices.CreateDoctorScheduleService
{
    /// <summary>
    /// Represents a request to create doctor schedules in batch.
    /// </summary>
    public class BatchCreateDoctorScheduleRequest
    {
        public List<DoctorRoomAssignment> Assignments { get; set; } = [];
        public List<DateOnly> WorkDates { get; set; } = [];
        public List<string> ShiftTypes { get; set; } = [];
    }

    /// <summary>
    /// Represents a doctor-room assignment.
    /// </summary>
    public class DoctorRoomAssignment
    {
        public Guid DoctorId { get; set; }
        public Guid RoomId { get; set; }
    }
}