using ECS.Domain.Entities.General;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.Scheduling
{
    public class TimeSlot : EntityBase<Guid>
    {
        public Guid ScheduleId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MaxPatients { get; set; } = 1;
        public int CurrentPatients { get; set; } = 0;
        public SlotStatus Status { get; set; } = SlotStatus.AVAILABLE;

        public virtual DoctorSchedule Schedule { get; set; } = null!;
        public virtual ICollection<Appointment>? Appointments { get; set; }
    }
}
