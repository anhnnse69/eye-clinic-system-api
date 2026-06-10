using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.General;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.Clinics
{
    /// <summary>
    /// Staff assignment to a clinic.
    /// </summary>
    public class StaffClinic : EntityBase<Guid>
    {
        public Guid UserId { get; set; }
        public Guid ClinicId { get; set; }
        public StaffRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Clinic Clinic { get; set; } = null!;
    }
}
