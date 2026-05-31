using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.Prescriptions
{
    public class Prescription : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public Guid DoctorId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;
        public virtual ICollection<PrescriptionItem>? Items { get; set; }
    }
}
