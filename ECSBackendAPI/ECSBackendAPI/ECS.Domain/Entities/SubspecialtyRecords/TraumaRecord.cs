using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    /// <summary>
    /// Trauma record - MS21.
    /// </summary>
    public class TraumaRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }

        public string? InjuryCause { get; set; }
        public DateTime? InjuryTime { get; set; }
        public string? PriorTreatment { get; set; }
        public string? PostTreatmentCourse { get; set; }

        // Summary of injuries - Right eye (JSONB)
        public string? OdInjuries { get; set; } // [lid_laceration, canaliculus, corneal_rupture, scleral_rupture, hyphema, lens_rupture, vitreous_hemorrhage, retinal_detachment, iofb, orbital_fb]

        // Summary of injuries - Left eye (JSONB)
        public string? OsInjuries { get; set; }

        // Injury details (JSONB)
        public string? InjuryDetails { get; set; } // location, size, depth, sutured, etc

        public string? TraumaConclusion { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
