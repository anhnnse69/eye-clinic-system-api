using ECS.Domain.Entities.General;
using ECS.Domain.Entities.Scheduling;

namespace ECS.Domain.Entities.Clinics
{
    /// <summary>
    /// Facility room within a clinic.
    /// </summary>
    public class FacilityRoom : EntityBase<Guid>
    {
        public Guid ClinicId { get; set; }
        public string RoomName { get; set; } = null!;
        public string? RoomType { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Clinic Clinic { get; set; } = null!;
        public virtual ICollection<Queue>? Queues { get; set; }
    }
}
