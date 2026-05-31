using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;

namespace ECS.Domain.Entities.SubspecialtyRecords
{
    public class TraumaRecord : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public string? InjuryCause { get; set; }
        public DateTime? InjuryTime { get; set; }
        public string? PriorTreatment { get; set; }
        public string? PostTreatmentCourse { get; set; }

        public bool OdLidLaceration { get; set; } = false;
        public bool OdCanaliculusLaceration { get; set; } = false;
        public bool OdCornealRupture { get; set; } = false;
        public bool OdScleralRupture { get; set; } = false;
        public bool OdHyphema { get; set; } = false;
        public bool OdLensRupture { get; set; } = false;
        public bool OdVitreousHemorrhage { get; set; } = false;
        public bool OdRetinalDetachment { get; set; } = false;
        public bool OdIntraocularForeignBody { get; set; } = false;
        public bool OdOrbitalForeignBody { get; set; } = false;

        public bool OsLidLaceration { get; set; } = false;
        public bool OsCanaliculusLaceration { get; set; } = false;
        public bool OsCornealRupture { get; set; } = false;
        public bool OsScleralRupture { get; set; } = false;
        public bool OsHyphema { get; set; } = false;
        public bool OsLensRupture { get; set; } = false;
        public bool OsVitreousHemorrhage { get; set; } = false;
        public bool OsRetinalDetachment { get; set; } = false;
        public bool OsIntraocularForeignBody { get; set; } = false;
        public bool OsOrbitalForeignBody { get; set; } = false;

        public string? Conclusion { get; set; }

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
