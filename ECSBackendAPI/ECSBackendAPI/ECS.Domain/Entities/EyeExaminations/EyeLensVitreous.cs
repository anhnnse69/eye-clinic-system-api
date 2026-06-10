using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Lens & Vitreous examination.
    /// </summary>
    public class EyeLensVitreous : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        // Lens
        public bool LensClear { get; set; } = true;
        public string? LensOpacityType { get; set; }
        public bool LensRupture { get; set; } = false;
        public bool LensSubluxation { get; set; } = false;
        public bool LensIntoAnterior { get; set; } = false;
        public bool LensIolPresent { get; set; } = false;
        public string? LensIolStatus { get; set; }

        // Vitreous
        public bool VitreousClear { get; set; } = true;
        public bool VitreousOpacity { get; set; } = false;
        public bool VitreousHemorrhage { get; set; } = false;
        public bool VitreousPvd { get; set; } = false;
        public string? VitreousTyndall { get; set; }

        public string? LensVitreousExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
