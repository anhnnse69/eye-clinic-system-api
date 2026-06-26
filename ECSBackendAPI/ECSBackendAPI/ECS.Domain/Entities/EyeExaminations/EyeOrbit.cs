using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Eye orbit examination data - MS21/MS24/MS25/MS26.
    /// Contains orbital examination findings.
    /// </summary>
    public class EyeOrbit : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        public EyeSide Side { get; set; }

        // ===== ORBITAL STATUS =====
        /// <summary>
        /// Overall orbital status: Normal or Pathological.
        /// </summary>
        public string? OrbitalStatus { get; set; }

        // ===== FOREIGN BODY =====
        /// <summary>
        /// Description of foreign body.
        /// </summary>
        public string? OrbitalForeignBodyDescription { get; set; }

        // ===== EXTRAOCULAR MOVEMENT =====
        /// <summary>
        /// Extraocular movement status: Normal or Pathological.
        /// </summary>
        public string? EomStatus { get; set; }
        /// <summary>
        /// Extraocular movement findings.
        /// </summary>
        public string? EomFindings { get; set; }

        // ===== EYEBALL =====
        /// <summary>
        /// Eyeball status: Atrophic, Proptosis, or Exophthalmometry reading.
        /// </summary>
        public string? EyeballStatus { get; set; }
        /// <summary>
        /// Eyeball texture: Soft, Tense, Large, or Small.
        /// </summary>
        public string? EyeballTexture { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
