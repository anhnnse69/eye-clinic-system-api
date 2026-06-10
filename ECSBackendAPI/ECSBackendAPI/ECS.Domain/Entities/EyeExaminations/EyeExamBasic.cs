using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Visual acuity, refraction, intraocular pressure, and basic eye movements.
    /// </summary>
    public class EyeExamBasic : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Visual acuity
        public decimal? VaUncorrected { get; set; }
        public decimal? VaCorrected { get; set; }
        public decimal? VaNear { get; set; }
        public decimal? VaPinhole { get; set; }

        // IOP
        public decimal? IopMmhg { get; set; }
        public string? IopMethod { get; set; }

        // Refraction
        public decimal? RefractionSph { get; set; }
        public decimal? RefractionCyl { get; set; }
        public int? RefractionAxis { get; set; }
        public decimal? RefractionAdd { get; set; }
        public decimal? Pd { get; set; }
        public string? AutoRefraction { get; set; }
        public string? Retinoscopy { get; set; }
        public string? SubjectiveRefraction { get; set; }
        public bool PreAtropine { get; set; } = false;
        public bool PostAtropine { get; set; } = false;

        // Extraocular motility
        public bool EomNormal { get; set; } = true;
        public string? EomNote { get; set; }
        public bool Nystagmus { get; set; } = false;
        public string? NystagmusType { get; set; }

        // Eyeball & orbit
        public string? EyeballStatus { get; set; }
        public decimal? ProptosisMm { get; set; }
        public bool OrbitNormal { get; set; } = true;
        public string? OrbitNote { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
