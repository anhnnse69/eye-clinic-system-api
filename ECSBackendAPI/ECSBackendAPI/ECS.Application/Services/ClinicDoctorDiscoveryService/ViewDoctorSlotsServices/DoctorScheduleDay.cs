namespace ECS.Application.Services.ClinicDoctorDiscoveryService.ViewDoctorSlotsServices
{
    /// <summary>
    /// One working day with its available time slots.
    /// </summary>
    public class DoctorScheduleDay
    {
        public Guid ScheduleId { get; set; }
        public DateOnly WorkDate { get; set; }
        public string ShiftType { get; set; } = null!;
        public List<DoctorTimeSlot> Slots { get; set; } = [];
    }
}
