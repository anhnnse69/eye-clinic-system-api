using ECS.Domain.Entities.Clinics;
using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.Prescriptions
{
    public class GlassesPrescription : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public Guid DoctorId { get; set; }
        public decimal? SphOd { get; set; }
        public decimal? CylOd { get; set; }
        public int? AxisOd { get; set; }
        public decimal? AddOd { get; set; }
        public decimal? SphOs { get; set; }
        public decimal? CylOs { get; set; }
        public int? AxisOs { get; set; }
        public decimal? AddOs { get; set; }
        public decimal? Pd { get; set; }
        public decimal? PdOd { get; set; }
        public decimal? PdOs { get; set; }
        public string? LensType { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
        public virtual DoctorProfile Doctor { get; set; } = null!;
    }
}
