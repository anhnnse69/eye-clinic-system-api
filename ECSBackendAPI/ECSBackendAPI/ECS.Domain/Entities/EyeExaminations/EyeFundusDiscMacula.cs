using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Fundus examination - Optic disc & Macula.
    /// </summary>
    public class EyeFundusDiscMacula : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Optic Disc
        public bool OpticDiscNormal { get; set; } = true;
        public bool OpticDiscEdema { get; set; } = false;
        public bool OpticDiscAtrophy { get; set; } = false;
        public bool OpticDiscPallor { get; set; } = false;
        public string? OpticDiscCupRatio { get; set; }
        public string? OpticDiscRimStatus { get; set; }
        public string? OpticDiscVesselChange { get; set; }
        public bool OpticDiscHemorrhage { get; set; } = false;
        public bool OpticDiscNeovascularization { get; set; } = false;
        public bool OpticDiscNotVisible { get; set; } = false;

        // Macula
        public bool MaculaNormal { get; set; } = true;
        public bool MaculaReflexAbsent { get; set; } = false;
        public string? MaculaEdemaType { get; set; }
        public string? MaculaHoleDegree { get; set; }
        public bool MaculaScar { get; set; } = false;
        public bool MaculaSerousDetachment { get; set; } = false;

        public string? DiscMaculaExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
