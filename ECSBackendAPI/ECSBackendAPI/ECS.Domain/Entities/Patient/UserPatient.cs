using ECS.Domain.Entities.Auth;

namespace ECS.Domain.Entities.Patient
{
    /// <summary>
    /// Many-to-many relationship between users and patient profiles (e.g., family members).
    /// </summary>
    public class UserPatient
    {
        public Guid UserId { get; set; }
        public Guid PatientId { get; set; }
        public string? Relationship { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; } = null!;
        public virtual PatientProfile Patient { get; set; } = null!;
    }
}
