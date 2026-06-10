using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.General;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.Scheduling
{
    /// <summary>
    /// Real-time queue for an appointment.
    /// </summary>
    public class Queue : EntityBase<Guid>
    {
        public Guid AppointmentId { get; set; }
        public Guid ClinicId { get; set; }
        public Guid? RoomId { get; set; }
        public int QueueNumber { get; set; }
        public QueueStatus Status { get; set; } = QueueStatus.WAITING;
        public DateTime? CalledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Appointment Appointment { get; set; } = null!;
        public virtual Clinic Clinic { get; set; } = null!;
        public virtual FacilityRoom? Room { get; set; }
    }
}
