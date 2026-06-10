using ECS.Domain.Entities.General;
using ECS.Domain.Entities.MedicalRecords;
using ECS.Domain.Enums;

namespace ECS.Domain.Entities.EyeExaminations
{
    /// <summary>
    /// Sclera examination.
    /// </summary>
    public class EyeSclera : EntityBase<Guid>
    {
        public Guid RecordId { get; set; }
        public EyeSide Side { get; set; }

        public bool ScleraNormal { get; set; } = true;
        public bool ScleraEctasia { get; set; } = false;
        public bool ScleraThinning { get; set; } = false;
        public bool ScleraNecrosis { get; set; } = false;
        public bool Episcleritis { get; set; } = false;
        public string? ScleritisType { get; set; }
        public bool ScleraLaceration { get; set; } = false;
        public bool? ScleraLacerationSutured { get; set; }
        public bool OldSurgeryScar { get; set; } = false;

        public string? ScleraExtras { get; set; } // JSONB

        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
