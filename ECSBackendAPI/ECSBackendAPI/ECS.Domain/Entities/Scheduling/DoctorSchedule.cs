using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.General;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.Scheduling
{
    /// <summary>
    /// Doctor's working day schedule.
    /// </summary>
    public class DoctorSchedule : EntityBase<Guid>
    {
        public Guid DoctorId { get; set; }
        public DateTime WorkDate { get; set; }
        public ShiftType ShiftType { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual DoctorProfile Doctor { get; set; } = null!;
        public virtual ICollection<TimeSlot>? TimeSlots { get; set; }
    }
}
