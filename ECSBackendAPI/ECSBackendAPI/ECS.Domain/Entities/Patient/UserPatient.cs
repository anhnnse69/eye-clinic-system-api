using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.Patient
{
    public class UserPatient : EntityBase<Guid>
    {
        public Guid UserId { get; set; }
        public Guid PatientId { get; set; }
        public string? Relationship { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; } = null!;
        public virtual PatientProfile Patient { get; set; } = null!;
    }
}
