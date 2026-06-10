using ECS.Domain.Entities.Auth;
using ECS.Domain.Entities.General;

namespace ECS.Domain.Entities.MedicalRecords
{
    /// <summary>
    /// Permission to access a medical record.
    /// </summary>
    public class DocumentAccessPermission : EntityBase<Guid>
    {
        public Guid MedicalRecordId { get; set; }
        public Guid GrantedToUserId { get; set; }
        public Guid GrantedByUserId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual User GrantedToUser { get; set; } = null!;
        public virtual User GrantedByUser { get; set; } = null!;
    }
}
